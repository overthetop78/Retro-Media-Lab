using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Models;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Constants.Cd4013BConstants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Services;

/// <summary>CD4013B capture, active-high asynchronous controls and delayed output pins.</summary>
/// <remarks>Unknown initial Q is not silently initialized to zero. A simultaneous
/// release of SET=RESET=high does not select an arbitrary analogue resolution.</remarks>
public sealed class Cd4013B : ICd4013B
{
    private sealed class Channel(long frequency, byte initial)
    {
        internal readonly long Frequency = frequency;
        internal long Clocks;
        internal byte Data;
        internal bool ClockHigh;
        internal bool SetHigh;
        internal bool ResetHigh;
        internal byte Logical = initial;
        internal byte Outputs = initial;
        internal long QDue;
        internal long NotQDue;
    }

    private readonly Channel[] _channels;
    private readonly long _clockDelay;
    private readonly long _riseDelay;
    private readonly long _fallDelay;
    private long _time;
    private long _next;
    private long _publishedNext;
    private bool _resolving;
    private Action<long?>? _nextChanged;
    private Action<bool?, bool?>? _firstChanged;
    private Action<bool?, bool?>? _secondChanged;

    public Cd4013B(long firstClockFrequency, long secondClockFrequency, Cd4013BTiming timing,
        bool? firstQ = null, bool? secondQ = null)
    {
        ArgumentNullException.ThrowIfNull(timing);
        ValidateFrequency(firstClockFrequency, timing.MaximumClockFrequency);
        ValidateFrequency(secondClockFrequency, timing.MaximumClockFrequency);
        Timing = timing;
        _clockDelay = timing.ClockNanoseconds;
        _riseDelay = timing.AsynchronousRiseNanoseconds;
        _fallDelay = timing.AsynchronousFallNanoseconds;
        _channels = [new(firstClockFrequency, Encode(firstQ)), new(secondClockFrequency, Encode(secondQ))];
    }

    public Cd4013BTiming Timing { get; }
    public long TimeNanoseconds => _time;
    public long? NextEventNanoseconds => _next == 0 ? null : _next;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextChanged; set => _nextChanged = value; }
    public Action<bool?, bool?>? FirstOutputsChanged { get => _firstChanged; set => _firstChanged = value; }
    public Action<bool?, bool?>? SecondOutputsChanged { get => _secondChanged; set => _secondChanged = value; }
    public long GetClockFrequency(int channel) => Get(channel).Frequency;
    public long GetInputClockCount(int channel) => Get(channel).Clocks;
    public bool GetClock(int channel) => Get(channel).ClockHigh;
    public bool GetSet(int channel) => Get(channel).SetHigh;
    public bool GetReset(int channel) => Get(channel).ResetHigh;
    public bool? GetQ(int channel) => Decode(Get(channel).Outputs, C.Q, C.QKnown);
    public bool? GetNotQ(int channel) => Decode(Get(channel).Outputs, C.NotQ, C.NotQKnown);
    public void SetData(int channel, bool? high) => Get(channel).Data = Encode(high);

    public void SetClock(int channel, bool high)
    {
        Capture(Get(channel), high);
        PublishDeadline();
    }

    /// <summary>Capture both old D inputs before allowing any shared-edge publication.</summary>
    public void SetClocks(bool firstHigh, bool secondHigh)
    {
        var first = _channels[0];
        var second = _channels[1];
        if (firstHigh && !first.ClockHigh) _ = checked(first.Clocks + 1);
        if (secondHigh && !second.ClockHigh) _ = checked(second.Clocks + 1);
        Capture(first, firstHigh);
        Capture(second, secondHigh);
        PublishDeadline();
    }

    private void Capture(Channel state, bool high)
    {
        if (state.ClockHigh == high) return;
        if (!high) { state.ClockHigh = false; return; }
        var clocks = checked(state.Clocks + 1);
        if (!state.SetHigh && !state.ResetHigh) ScheduleOutputs(state, state.Data, false);
        state.Clocks = clocks;
        state.ClockHigh = true;
    }

    public void SetSet(int channel, bool high)
    {
        var state = Get(channel);
        SetControls(channel, state, high, state.ResetHigh);
    }

    public void SetReset(int channel, bool high)
    {
        var state = Get(channel);
        SetControls(channel, state, state.SetHigh, high);
    }

    public void SetAsynchronousInputs(int channel, bool setHigh, bool resetHigh) =>
        SetControls(channel, Get(channel), setHigh, resetHigh);

    private void SetControls(int channel, Channel state, bool setHigh, bool resetHigh)
    {
        if (state.SetHigh == setHigh && state.ResetHigh == resetHigh) return;
        var simultaneousRelease = state.SetHigh && state.ResetHigh && !setHigh && !resetHigh;
        if (setHigh || resetHigh)
            ScheduleOutputs(state, setHigh && resetHigh ? C.BothHigh : setHigh ? C.High : C.Low, true);
        state.SetHigh = setHigh;
        state.ResetHigh = resetHigh;
        if (simultaneousRelease)
        {
            // Knowledge becomes indeterminate; this does not invent a physical voltage edge.
            var previous = state.Outputs;
            state.Logical = C.Unknown;
            state.Outputs = C.Unknown;
            state.QDue = state.NotQDue = 0;
            _next = FindDeadline();
            if (previous != state.Outputs)
            {
                var resolving = _resolving;
                _resolving = true;
                try { Notify(channel, state); }
                finally { _resolving = resolving; }
            }
        }
        PublishDeadline();
    }

    private void ScheduleOutputs(Channel state, byte logical, bool asynchronous)
    {
        if (logical == state.Logical) return;
        if (!asynchronous && state.QDue == 0 && state.NotQDue == 0)
        {
            // Both physical output responses share the clock propagation duration.
            // With no old response to cancel, the existing minimum only receives additions.
            var changed = state.Outputs ^ logical;
            var due = checked(_time + _clockDelay);
            state.Logical = logical;
            state.QDue = (changed & (C.Q | C.QKnown)) != 0 ? due : 0;
            state.NotQDue = (changed & (C.NotQ | C.NotQKnown)) != 0 ? due : 0;
            if ((state.QDue != 0 || state.NotQDue != 0) && (_next == 0 || due < _next)) _next = due;
            return;
        }
        var previous = state.Logical;
        var qDue = SchedulePin(state.Outputs, previous, logical, C.Q, C.QKnown, state.QDue, asynchronous);
        var notQDue = SchedulePin(state.Outputs, previous, logical, C.NotQ, C.NotQKnown, state.NotQDue, asynchronous);
        state.Logical = logical;
        state.QDue = qDue;
        state.NotQDue = notQDue;
        _next = FindDeadline();
    }

    private long SchedulePin(byte outputs, byte previous, byte target, byte pin, byte known,
        long due, bool asynchronous)
    {
        var mask = pin | known;
        if ((outputs & mask) == (target & mask)) return 0;
        if (due != 0 && (previous & mask) == (target & mask)) return due;
        var delay = asynchronous ? (target & pin) != 0 ? _riseDelay : _fallDelay : _clockDelay;
        return checked(_time + delay);
    }

    public void AdvanceTo(long nanoseconds)
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
                var first = _channels[0];
                var second = _channels[1];
                var firstOutputs = first.Outputs;
                var secondOutputs = second.Outputs;
                Resolve(first, _next);
                Resolve(second, _next);
                _next = FindDeadline();
                if (first.Outputs != firstOutputs) Notify(0, first);
                if (second.Outputs != secondOutputs) Notify(1, second);
            }
            _time = nanoseconds;
        }
        finally { _resolving = false; PublishDeadline(); }
    }

    private static void Resolve(Channel state, long time)
    {
        if (state.QDue == time)
        {
            state.QDue = 0;
            state.Outputs = (byte)((state.Outputs & ~(C.Q | C.QKnown)) | (state.Logical & (C.Q | C.QKnown)));
        }
        if (state.NotQDue == time)
        {
            state.NotQDue = 0;
            state.Outputs = (byte)((state.Outputs & ~(C.NotQ | C.NotQKnown)) | (state.Logical & (C.NotQ | C.NotQKnown)));
        }
    }

    public void AdvanceClocks(int channel, long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        var state = Get(channel);
        if (_resolving || state.ClockHigh) throw new InvalidOperationException("Periods require CLK low outside a callback.");
        if (C.NanosecondsPerSecond % state.Frequency != 0)
            throw new InvalidOperationException("Use timed SetClock inputs for fractional-nanosecond periods.");
        var period = C.NanosecondsPerSecond / state.Frequency;
        _ = checked(_time + checked(period * clocks));
        _ = checked(state.Clocks + clocks);
        while (clocks > 0)
        {
            var logical = state.SetHigh || state.ResetHigh
                ? state.SetHigh && state.ResetHigh ? C.BothHigh : state.SetHigh ? C.High : C.Low
                : state.Data;
            if (_next == 0 && state.Logical == logical && state.Outputs == logical)
            {
                // Real captures retain the same state, with no pending output response.
                state.Clocks += clocks;
                _time = checked(_time + clocks * period);
                return;
            }
            var start = _time;
            Capture(state, true);
            PublishDeadline();
            AdvanceTo(checked(start + period / 2));
            state.ClockHigh = false;
            AdvanceTo(checked(start + period));
            clocks--;
        }
    }

    private long FindDeadline()
    {
        var first = _channels[0];
        var second = _channels[1];
        var next = first.QDue;
        var due = first.NotQDue;
        if (due != 0 && (next == 0 || due < next)) next = due;
        due = second.QDue;
        if (due != 0 && (next == 0 || due < next)) next = due;
        due = second.NotQDue;
        if (due != 0 && (next == 0 || due < next)) next = due;
        return next;
    }

    private void PublishDeadline()
    {
        if (_resolving || _publishedNext == _next) return;
        _publishedNext = _next;
        _nextChanged?.Invoke(_next == 0 ? null : _next);
    }

    private void Notify(int channel, Channel state)
    {
        var receiver = channel == 0 ? _firstChanged : _secondChanged;
        if (receiver is null) return;
        var levels = state.Outputs;
        bool? q = (levels & C.QKnown) == 0 ? null : (levels & C.Q) != 0;
        bool? notQ = (levels & C.NotQKnown) == 0 ? null : (levels & C.NotQ) != 0;
        receiver(q, notQ);
    }

    private Channel Get(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)channel, (uint)C.Channels);
        return _channels[channel];
    }

    private static byte Encode(bool? value) => value.HasValue ? value.Value ? C.High : C.Low : C.Unknown;
    private static bool? Decode(byte value, byte pin, byte known) => (value & known) != 0 ? (value & pin) != 0 : null;
    private static void ValidateFrequency(long frequency, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frequency, maximum);
    }
}
