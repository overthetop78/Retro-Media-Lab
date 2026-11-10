using System.Numerics;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Models;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Constants.Sn74Ls93Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;

/// <summary>Independent divide-by-two and divide-by-eight ripple sections.
/// QA to CKB is an external wire; no internal cascade is invented.</summary>
public sealed partial class Sn74Ls93 : ISn74Ls93
{
    private readonly long[] _frequency, _counts = new long[2], _due = new long[4], _rise, _fall;
    private byte _clocks, _resets, _outputs, _targets, _pending, _clearing;
    private byte _observed = C.OutputMask;
    private long _time, _next, _publishedNext;
    private bool _resolving;
    private Action<byte, byte>? _outputsChanged;
    private Action<long?>? _nextChanged;

    public Sn74Ls93(long clockA, long clockB, byte initial, Sn74Ls93Timing timing)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clockA);
        ArgumentOutOfRangeException.ThrowIfNegative(clockB);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(clockA, C.MaximumClockA);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(clockB, C.MaximumClockB);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initial, C.OutputMask);
        ArgumentNullException.ThrowIfNull(timing);
        Timing = timing;
        _frequency = [clockA, clockB];
        _outputs = _targets = initial;
        _rise = [timing.QaRise, timing.QbRise, timing.QcRise, timing.QdRise];
        _fall = [timing.QaFall, timing.QbFall, timing.QcFall, timing.QdFall];
    }

    public Sn74Ls93Timing Timing { get; }
    public long TimeNanoseconds => _time;
    public long? NextEventNanoseconds => ReceivedDeadline() is var next && next != 0 ? next : null;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextChanged; set => _nextChanged = value; }
    public Action<byte, byte>? OutputsChanged { get => _outputsChanged; set => _outputsChanged = value; }
    public byte Outputs => _outputs;
    public long GetClockFrequency(int clock) { ValidateIndex(clock); return _frequency[clock]; }
    public long GetInputClockCount(int clock) { ValidateIndex(clock); return _counts[clock]; }
    public bool GetClock(int clock) { ValidateIndex(clock); return InputLevel(clock); }
    public bool GetReset(int input) { ValidateIndex(input); return (_resets & (1 << input)) != 0; }
    public void SetObservedOutputs(byte mask)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mask, C.OutputMask);
        _observed = mask;
        PublishDeadline();
    }

    public void SetClock(int clock, bool high)
    {
        ValidateIndex(clock);
        if (_periodicInputs != 0) DisconnectPeriodicClock(clock);
        var mask = (byte)(1 << clock);
        if (((_clocks & mask) != 0) == high) return;
        if (high) { _clocks |= mask; return; }
        ApplyFallingClock(clock, mask);
    }

    private void ApplyFallingClock(int clock, byte mask)
    {
        var count = checked(_counts[clock] + 1);
        if (_resets != 3) ScheduleToggle(clock == 0 ? 0 : 1);
        _counts[clock] = count;
        _clocks &= (byte)~mask;
        PublishDeadline();
    }

    public void SetReset(int input, bool high)
    {
        ValidateIndex(input);
        var mask = 1 << input;
        var levels = (byte)(high ? _resets | mask : _resets & ~mask);
        ApplyResets(levels);
    }
    public void SetResets(bool first, bool second) => ApplyResets((byte)((first ? 1 : 0) | (second ? 2 : 0)));

    private void ApplyResets(byte levels)
    {
        if (_resets == levels) return;
        InvalidateQaPattern();
        if (levels == 3)
        {
            var affected = (byte)(_outputs | _pending);
            var due = affected == 0 ? 0 : checked(_time + Timing.Reset);
            _pending = _clearing = affected;
            _targets = 0;
            _next = due;
            for (var bit = 0; bit < 4; bit++) _due[bit] = (affected & (1 << bit)) != 0 ? due : 0;
        }
        _resets = levels;
        UpdateQaPattern();
        PublishDeadline();
    }

    private void ScheduleToggle(int bit)
    {
        var mask = (byte)(1 << bit);
        var high = (_outputs & mask) == 0;
        var due = checked(_time + (high ? _rise[bit] : _fall[bit]));
        var previous = _due[bit];
        var oldNext = _next;
        if (high) _targets |= mask; else _targets &= (byte)~mask;
        _due[bit] = due;
        _pending |= mask;
        _clearing &= (byte)~mask;
        if (_next == 0 || due < _next) _next = due;
        if (previous != 0 && previous == oldNext) _next = FindDeadline();
    }

    public void AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _time);
        if (_resolving)
        {
            if (nanoseconds != _time) throw new InvalidOperationException("Cannot advance time from an output callback.");
            return;
        }
        if (_periodicInputs != 0) AdvancePeriodicInputs(nanoseconds);
        else AdvanceResponses(nanoseconds);
    }

    private void AdvanceResponses(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _time);
        if (_resolving)
        {
            if (nanoseconds != _time) throw new InvalidOperationException("Cannot advance time from an output callback.");
            return;
        }
        if (_next == 0 || _next > nanoseconds) { _time = nanoseconds; return; }
        _resolving = true;
        try
        {
            while (_next != 0 && _next <= nanoseconds)
            {
                _time = _next;
                // A lone stage already identifies the whole response. Resolve it
                // directly, keeping the general simultaneous-response path below.
                // Prepare its real carry before notifying any external feedback.
                var sole = _pending;
                if ((sole & (sole - 1)) == 0)
                {
                    var bit = BitOperations.TrailingZeroCount((uint)sole);
                    var before = _outputs;
                    _due[bit] = 0;
                    _pending = 0;
                    _next = 0;
                    var clearing = (_clearing & sole) != 0;
                    _clearing &= (byte)~sole;
                    _outputs = (byte)((before & ~sole) | (_targets & sole));
                    if ((before & sole) != 0 && (_outputs & sole) == 0
                        && !clearing && _resets != 3 && bit is 1 or 2)
                    {
                        var carry = bit + 1;
                        var carryMask = (byte)(sole << 1);
                        var high = (_outputs & carryMask) == 0;
                        var due = checked(_time + (high ? _rise[carry] : _fall[carry]));
                        if (high) _targets |= carryMask; else _targets &= (byte)~carryMask;
                        _due[carry] = _next = due;
                        _pending = carryMask;
                        _clearing &= (byte)~carryMask;
                    }
                    var visible = (byte)((before ^ _outputs) & _observed);
                    if (visible != 0) _outputsChanged?.Invoke(_outputs, visible);
                    continue;
                }
                byte changed = 0, carries = 0;
                var pending = _pending;
                while (pending != 0)
                {
                    var bit = BitOperations.TrailingZeroCount((uint)pending);
                    pending &= (byte)(pending - 1);
                    if (_due[bit] != _time) continue;
                    var mask = (byte)(1 << bit);
                    _due[bit] = 0; _pending &= (byte)~mask;
                    var clearing = (_clearing & mask) != 0;
                    _clearing &= (byte)~mask;
                    var high = (_targets & mask) != 0;
                    if (((_outputs & mask) != 0) == high) continue;
                    if (high) _outputs |= mask; else _outputs &= (byte)~mask;
                    changed |= mask;
                    if (!high && !clearing && _resets != 3 && bit is 1 or 2)
                        carries |= (byte)(1 << (bit + 1));
                }
                _next = _pending == 0 ? 0 : FindDeadline();
                while (carries != 0)
                {
                    var bit = BitOperations.TrailingZeroCount((uint)carries);
                    carries &= (byte)(carries - 1);
                    ScheduleToggle(bit);
                }
                var observed = (byte)(changed & _observed);
                if (observed != 0) _outputsChanged?.Invoke(_outputs, observed);
            }
            _time = nanoseconds;
        }
        finally { _resolving = false; UpdateQaPattern(); PublishDeadline(); }
    }

    public void AdvanceClocks(int clock, long periods)
    {
        ValidateIndex(clock);
        ArgumentOutOfRangeException.ThrowIfNegative(periods);
        if (periods == 0) return;
        if (_periodicInputs != 0) throw new InvalidOperationException("Disconnect the external periodic inputs before advancing explicit periods.");
        if (_resolving) throw new InvalidOperationException("Cannot advance clock periods from an output callback.");
        var frequency = _frequency[clock];
        if (frequency == 0 || C.NanosecondsPerSecond % frequency != 0)
            throw new InvalidOperationException("Supply explicit clock levels for an absent or fractional-nanosecond period.");
        var period = C.NanosecondsPerSecond / frequency;
        var half = period / 2;
        _ = checked(_time + checked(periods * period));
        _ = checked(_counts[clock] + periods);
        var clockMask = (byte)(1 << clock);
        var firstOutput = clock == 0 ? 1 : 2;
        while (periods != 0)
        {
            var skip = _resets != 3 && _outputsChanged is not null && (_observed & firstOutput) != 0
                ? 0 : StablePeriods(clock, periods, period - half);
            if (skip != 0)
            {
                _counts[clock] += skip;
                _clocks &= (byte)~(1 << clock);
                if (_resets != 3)
                {
                    if (clock == 0) _outputs ^= (byte)(skip & 1);
                    else
                    {
                        var section = (byte)(((_outputs >> 1) + skip) & 7);
                        _outputs = (byte)((_outputs & 1) | (section << 1));
                    }
                    _targets = _outputs;
                }
                _time += skip * period;
                periods -= skip;
                continue;
            }
            _clocks |= clockMask;
            AdvanceTo(_time + half);
            // A recipient may have supplied an earlier falling level during the
            // first half-period. Do not replay that physical transfer.
            if ((_clocks & clockMask) != 0) ApplyFallingClock(clock, clockMask);
            AdvanceTo(_time + period - half);
            periods--;
        }
    }

    private long StablePeriods(int clock, long requested, long settlingTime)
    {
        if (_pending != 0) return 0;
        if (_resets == 3) return requested;
        if (_nextChanged is not null || Timing.MaximumRipple > settlingTime) return 0;
        var observed = _outputsChanged is null ? 0 : _observed;
        if (clock == 0) return (observed & 1) == 0 ? requested : 0;
        observed >>= 1;
        if (observed == 0) return requested;
        var modulus = 1 << BitOperations.TrailingZeroCount((uint)observed);
        var remaining = modulus - ((_outputs >> 1) & (modulus - 1)) - 1;
        return Math.Min(requested, remaining);
    }

    private long FindDeadline()
    {
        var pending = _pending;
        if (pending == 0) return 0;
        if ((pending & (pending - 1)) == 0) return _due[BitOperations.TrailingZeroCount((uint)pending)];
        long next = 0;
        while (pending != 0)
        {
            var bit = BitOperations.TrailingZeroCount((uint)pending);
            pending &= (byte)(pending - 1);
            var due = _due[bit];
            if (next == 0 || due < next) next = due;
        }
        return next;
    }
    private void PublishDeadline()
    {
        if (_resolving || _advancingPeriodic) return;
        var next = ReceivedDeadline();
        if (_publishedNext != next)
        {
            _publishedNext = next;
            _nextChanged?.Invoke(next == 0 ? null : next);
        }
        var output = ConnectedResponseDeadline();
        if (_publishedOutput == output) return;
        _publishedOutput = output;
        _outputDeadlineChanged?.Invoke(output == 0 ? null : output);
    }
    private static void ValidateIndex(int index) => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, 2u);
}
