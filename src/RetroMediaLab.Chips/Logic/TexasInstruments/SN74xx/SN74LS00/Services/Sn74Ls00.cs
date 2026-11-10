using System.Numerics;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Models;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Constants.Sn74Ls00Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Services;

/// <summary>Four combinatorial NAND gates with local output propagation, no oscillator.</summary>
/// <remarks>Each pin has one canonical two-bit code: unknown 00, low 10, high 11.
/// Packed words are a software representation of independent pins, not a new physical bus.</remarks>
public sealed class Sn74Ls00 : ISn74Ls00
{
    private readonly long[] _due = new long[C.Gates];
    private readonly long _rise, _fall, _unknown;
    private byte _a, _b, _outputs, _targets;
    private long _time, _next, _publishedNext;
    private byte _pending;
    private byte _observed = C.Mask;
    private bool _resolving;
    private Action<long?>? _nextChanged;
    private Action<int, bool?>? _outputChanged;

    public Sn74Ls00(Sn74Ls00Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        Timing = timing;
        _rise = timing.RiseNanoseconds;
        _fall = timing.FallNanoseconds;
        _unknown = Math.Max(_rise, _fall);
    }

    public Sn74Ls00Timing Timing { get; }
    public long TimeNanoseconds => _time;
    public long? NextEventNanoseconds => _next == 0 ? null : _next;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextChanged; set => _nextChanged = value; }
    public Action<int, bool?>? OutputChanged { get => _outputChanged; set => _outputChanged = value; }
    public bool? GetOutput(int gate) => Decode((byte)((_outputs >> Shift(gate)) & 3));

    public void SetObservedOutputs(byte mask)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mask, C.Mask);
        _observed = mask;
    }

    public void SetInputs(int gate, bool? a, bool? b) => SetOne(Shift(gate), Encode(a), Encode(b));
    public void SetInputs(int gate, bool a, bool b) =>
        SetOne(Shift(gate), a ? C.High : C.Low, b ? C.High : C.Low);
    public void SetInputA(int gate, bool? high)
    {
        var shift = Shift(gate);
        SetOne(shift, Encode(high), (byte)((_b >> shift) & 3));
    }
    public void SetInputA(int gate, bool high)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)gate, (uint)C.Gates);
        var shift = gate << 1;
        SetOne(shift, high ? C.High : C.Low, (byte)((_b >> shift) & 3));
    }
    public void SetInputB(int gate, bool? high)
    {
        var shift = Shift(gate);
        SetOne(shift, (byte)((_a >> shift) & 3), Encode(high));
    }
    public void SetInputB(int gate, bool high)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)gate, (uint)C.Gates);
        var shift = gate << 1;
        SetOne(shift, (byte)((_a >> shift) & 3), high ? C.High : C.Low);
    }
    public void SetInputLevels(byte aLevels, byte aKnown, byte bLevels, byte bKnown)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(aLevels, C.Mask);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(aKnown, C.Mask);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bLevels, C.Mask);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bKnown, C.Mask);
        var a = (byte)(Spread((byte)(aLevels & aKnown)) | (Spread(aKnown) << 1));
        var b = (byte)(Spread((byte)(bLevels & bKnown)) | (Spread(bKnown) << 1));
        SetPacked(a, b);
    }

    private void SetOne(int shift, byte a, byte b)
    {
        var mask = 3 << shift;
        var nextA = (byte)((_a & ~mask) | (a << shift));
        var nextB = (byte)((_b & ~mask) | (b << shift));
        if (nextA == _a && nextB == _b) return;
        var target = a == C.Low || b == C.Low ? C.High
            : a == C.High && b == C.High ? C.Low : C.Unknown;
        if (((_targets >> shift) & 3) == target)
        {
            _a = nextA; _b = nextB;
            return;
        }
        var due = target == ((_outputs >> shift) & 3) ? 0
            : checked(_time + (target == C.High ? _rise : target == C.Low ? _fall : _unknown));
        _a = nextA; _b = nextB;
        _targets = (byte)((_targets & ~mask) | (target << shift));
        if (ArmOutput(shift >> 1, due, _next)) _next = FindDeadline();
        PublishDeadline();
    }
    private void SetPacked(byte a, byte b)
    {
        if (_a == a && _b == b) return;
        // A known-low input establishes Y=high even if the other input is unknown.
        var lows = (((a & C.PackedKnown) >> 1) & ~a) | (((b & C.PackedKnown) >> 1) & ~b);
        var highs = a & b & C.PackedValues;
        var targets = (byte)(lows | ((lows | highs) << 1));
        ApplyInputs(a, b, targets);
    }

    private void ApplyInputs(byte a, byte b, byte targets)
    {
        var changed = _targets ^ targets;
        var changedValues = (byte)((changed | (changed >> 1)) & C.PackedValues);
        // Normal dates need no speculative per-pin validation. Near the representation
        // limit, validate the entire shared input transaction before any mutation.
        if (changedValues != 0 && _time > long.MaxValue - _unknown)
        {
            var check = changedValues;
            while (check != 0)
            {
                var shift = BitOperations.TrailingZeroCount((uint)check);
                check &= (byte)(check - 1);
                var target = (targets >> shift) & 3;
                if (target != ((_outputs >> shift) & 3))
                    _ = checked(_time + (target == C.High ? _rise : target == C.Low ? _fall : _unknown));
            }
        }
        _a = a; _b = b;
        if (changedValues == 0) return;
        _targets = targets;
        var oldNext = _next;
        var recompute = false;
        while (changedValues != 0)
        {
            var shift = BitOperations.TrailingZeroCount((uint)changedValues);
            changedValues &= (byte)(changedValues - 1);
            var gate = shift >> 1;
            var target = (targets >> shift) & 3;
            var due = target == ((_outputs >> shift) & 3) ? 0
                : _time + (target == C.High ? _rise : target == C.Low ? _fall : _unknown);
            recompute |= ArmOutput(gate, due, oldNext);
        }
        if (recompute) _next = FindDeadline();
        PublishDeadline();
    }

    private bool ArmOutput(int gate, long due, long oldNext)
    {
        var previous = _due[gate];
        _due[gate] = due;
        var mask = (byte)(1 << gate);
        if (due == 0) _pending &= (byte)~mask;
        else _pending |= mask;
        if (due != 0 && (_next == 0 || due < _next)) _next = due;
        return previous != 0 && previous == oldNext;
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
                byte changed = 0;
                var pending = _pending;
                while (pending != 0)
                {
                    var gate = BitOperations.TrailingZeroCount((uint)pending);
                    pending &= (byte)(pending - 1);
                    if (_due[gate] != _time) continue;
                    _due[gate] = 0;
                    _pending &= (byte)~(1 << gate);
                    var shift = gate << 1;
                    var mask = 3 << shift;
                    if ((_outputs & mask) == (_targets & mask)) continue;
                    _outputs = (byte)((_outputs & ~mask) | (_targets & mask));
                    changed |= (byte)(1 << gate);
                }
                _next = FindDeadline();
                while (changed != 0)
                {
                    var gate = BitOperations.TrailingZeroCount((uint)changed);
                    changed &= (byte)(changed - 1);
                    if ((_observed & (1 << gate)) == 0) continue;
                    var level = (_outputs >> (gate << 1)) & 3;
                    bool? output = level == C.Unknown ? null : level == C.High;
                    _outputChanged?.Invoke(gate, output);
                }
            }
            _time = nanoseconds;
        }
        finally { _resolving = false; PublishDeadline(); }
    }

    private long FindDeadline()
    {
        var pending = _pending;
        if (pending == 0) return 0;
        if ((pending & (pending - 1)) == 0) return _due[BitOperations.TrailingZeroCount((uint)pending)];
        long next = 0;
        while (pending != 0)
        {
            var gate = BitOperations.TrailingZeroCount((uint)pending);
            pending &= (byte)(pending - 1);
            var due = _due[gate];
            if (next == 0 || due < next) next = due;
        }
        return next;
    }
    private void PublishDeadline()
    {
        if (_resolving || _publishedNext == _next) return;
        _publishedNext = _next;
        _nextChanged?.Invoke(_next == 0 ? null : _next);
    }
    private static byte Spread(byte bits)
    {
        var value = (bits | (bits << 2)) & 0x33;
        return (byte)((value | (value << 1)) & C.PackedValues);
    }
    private static int Shift(int gate)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)gate, (uint)C.Gates);
        return gate << 1;
    }
    private static byte Encode(bool? level) => level.HasValue ? level.Value ? C.High : C.Low : C.Unknown;
    private static bool? Decode(byte level) => level == C.Unknown ? null : level == C.High;
}
