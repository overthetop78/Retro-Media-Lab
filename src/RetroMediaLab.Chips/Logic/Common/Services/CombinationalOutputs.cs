using System.Numerics;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.Common.Interfaces;

namespace RetroMediaLab.Chips.Logic.Common.Services;

/// <summary>Local propagation storage, owned by one package. No global clock or shared state.</summary>
internal sealed class CombinationalOutputs(int count)
{
    private readonly long[] _due = new long[count];
    private readonly ushort _outputMask = (ushort)((1u << (count * 2)) - 1);
    private ushort _outputs, _targets;
    private uint _pending;
    // The pending mask distinguishes absence from a real long.MaxValue date.
    private long _next = long.MaxValue;
    private long? _announced;
    private bool _publishing, _executingRange;
    private TimedLogicOutput[]? _rangeOutput;
    private long _time;
    public long Time => _time;
    internal ushort Targets => _targets;
    public long? Next => _pending == 0 ? null : _next;
    public Action<int, LogicPinState>? Changed { get; set; }
    public Action<long?>? DeadlineChanged { get; set; }

    public LogicPinState Get(int pin)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)pin, (uint)count);
        return (LogicPinState)((_outputs >> (pin * 2)) & 3);
    }

    /// <summary>Preflight every affected date before the package commits inputs.</summary>
    public void Validate(ushort targets, ReadOnlySpan<long> delays)
    {
        if (delays.Length != count) throw new ArgumentException(nameof(delays));
        uint changed = (uint)((targets ^ _targets) & _outputMask);
        while (changed != 0)
        {
            int pin = BitOperations.TrailingZeroCount(changed) >> 1;
            uint mask = 3u << (pin * 2);
            changed &= ~mask;
            if (((targets ^ _outputs) & mask) == 0) continue;
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delays[pin]);
            _ = checked(_time + delays[pin]);
        }
    }

    public void SetTargets(ushort targets, ReadOnlySpan<long> delays)
    {
        uint changed = (uint)(targets ^ _targets);
        if (changed == 0) return;
        _targets = targets;
        changed &= _outputMask;
        long earliest = _next;
        bool displacedMinimum = false;
        while (changed != 0)
        {
            int pin = BitOperations.TrailingZeroCount(changed) >> 1;
            uint mask = 3u << (pin * 2), bit = 1u << pin;
            changed &= ~mask;
            if ((_pending & bit) != 0 && _due[pin] == _next) displacedMinimum = true;
            if (((_targets ^ _outputs) & mask) == 0)
            {
                _pending &= ~bit;
                _due[pin] = 0;
            }
            else
            {
                long due = _time + delays[pin];
                _due[pin] = due;
                _pending |= bit;
                if (due < earliest) earliest = due;
            }
        }
        if (_pending == 0) _next = long.MaxValue;
        else if (displacedMinimum) FindNext();
        else _next = earliest;
        PublishDeadline();
    }

    public void AdvanceTo(long time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, _time);
        if (_publishing)
        {
            if (time != _time) throw new InvalidOperationException("Cannot advance logic from a pin callback.");
            return;
        }
        while (_pending != 0 && _next <= time)
        {
            uint changed = ResolveOutputsAt(_next);
            _publishing = true;
            try
            {
                // Resolve all simultaneous pins first, then preserve their
                // ascending publication order and dynamic observer changes.
                while (changed != 0)
                {
                    int pin = BitOperations.TrailingZeroCount(changed);
                    changed &= changed - 1;
                    Changed?.Invoke(pin, (LogicPinState)((_outputs >> (pin * 2)) & 3));
                }
            }
            finally { _publishing = false; PublishDeadline(); }
        }
        _time = time;
    }

    private byte ResolveOutputsAt(long next)
    {
        _time = next;
        byte changed = 0;
        long following = long.MaxValue;
        uint pending = _pending;
        while (pending != 0)
        {
            int pin = BitOperations.TrailingZeroCount(pending);
            uint bit = 1u << pin;
            pending &= pending - 1;
            long due = _due[pin];
            if (due != next)
            {
                if (due < following) following = due;
                continue;
            }
            _due[pin] = 0; _pending &= ~bit;
            int mask = 3 << (pin * 2);
            if (((_outputs ^ _targets) & mask) == 0) continue;
            _outputs = (ushort)((_outputs & ~mask) | (_targets & mask));
            changed |= (byte)bit;
        }
        _next = following;
        return changed;
    }

    internal LogicOutputRange ExecuteInputRange(in LogicInputRange range, ulong inputMask,
        ICombinationalRangeModel model)
    {
        range.Validate(_time, inputMask);
        var changes = range.Changes;
        long end = range.KnownUntilNanoseconds;
        if (_publishing || _executingRange)
            throw new InvalidOperationException("Cannot recursively execute a logic input range.");
        if (Changed is not null || DeadlineChanged is not null)
            throw new InvalidOperationException("Scalar observers require explicit input delivery, rather than a guaranteed range.");
        _rangeOutput ??= new TimedLogicOutput[(LogicInputRange.MaximumChanges + 1) * count];
        long start = _time;
        ushort initial = _outputs;
        int inputIndex = 0, outputCount = 0;
        Span<long> delays = stackalloc long[count];
        _executingRange = true;
        try
        {
            while (true)
            {
                bool hasInput = inputIndex < changes.Length;
                long inputTime = hasInput ? changes[inputIndex].TimeNanoseconds : end;
                if (_pending != 0 && _next <= inputTime)
                {
                    long due = _next;
                    byte changed = ResolveOutputsAt(due);
                    if (changed != 0) _rangeOutput[outputCount++] = new(due, _outputs, changed);
                    continue;
                }
                if (!hasInput) break;
                ref readonly var input = ref changes[inputIndex++];
                _time = input.TimeNanoseconds;
                if (!model.PrepareRangeInput(in input, delays, out ushort targets)) continue;
                Validate(targets, delays);
                model.CommitRangeInput(in input);
                SetTargets(targets, delays);
            }
            _time = end;
            return new(start, _time, initial, _rangeOutput.AsSpan(0, outputCount), Next);
        }
        finally { _announced = Next; _executingRange = false; }
    }

    private void FindNext()
    {
        long next = long.MaxValue;
        uint pending = _pending;
        while (pending != 0)
        {
            int pin = BitOperations.TrailingZeroCount(pending);
            pending &= pending - 1;
            if (_due[pin] < next) next = _due[pin];
        }
        _next = next;
    }

    private void PublishDeadline()
    {
        if (_publishing || _executingRange) return;
        long? next = Next;
        while (_announced != next)
        {
            _announced = next;
            _publishing = true;
            try { DeadlineChanged?.Invoke(next); }
            finally { _publishing = false; }
            next = Next;
        }
    }

    public CombinationalState Capture(string part, ulong levels, ulong known, long[] timing)
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot a partial pin publication.");
        var due = new long?[count];
        uint pending = _pending;
        while (pending != 0)
        {
            int pin = BitOperations.TrailingZeroCount(pending);
            pending &= pending - 1;
            due[pin] = _due[pin];
        }
        return new(1, part, _time, levels, known, _outputs, _targets, due, (long[])timing.Clone());
    }

    public void ValidateRestore(CombinationalState state, string part, ulong inputMask, ushort targets, long[] timing)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore during pin publication.");
        if (state.Version != 1 || state.Part != part || state.TimeNanoseconds < 0
            || state.DueNanoseconds?.Length != count || state.TimingNanoseconds is null
            || !state.TimingNanoseconds.SequenceEqual(timing) || (state.InputLevels & ~state.InputKnown) != 0
            || ((state.InputLevels | state.InputKnown) & ~inputMask) != 0 || state.Targets != targets
            || ((state.Outputs | state.Targets) >> (count * 2)) != 0)
            throw new ArgumentException(nameof(state));
        for (int pin = 0; pin < count; pin++)
        {
            bool differs = ((state.Outputs ^ state.Targets) & (3 << (pin * 2))) != 0;
            if (differs != state.DueNanoseconds[pin].HasValue || state.DueNanoseconds[pin] <= state.TimeNanoseconds)
                throw new ArgumentException(nameof(state));
        }
    }

    public void Restore(CombinationalState state)
    {
        _time = state.TimeNanoseconds;
        _outputs = state.Outputs; _targets = state.Targets; _pending = 0;
        for (int pin = 0; pin < count; pin++)
        {
            _due[pin] = state.DueNanoseconds[pin].GetValueOrDefault();
            if (state.DueNanoseconds[pin].HasValue) _pending |= 1u << pin;
        }
        FindNext();
        _announced = Next;
    }
}
