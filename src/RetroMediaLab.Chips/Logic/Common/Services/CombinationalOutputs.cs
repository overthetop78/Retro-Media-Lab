using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Logic.Common.Services;

/// <summary>Local propagation storage, owned by one package. No global clock or shared state.</summary>
internal sealed class CombinationalOutputs(int count)
{
    private readonly long?[] _due = new long?[count];
    private ushort _outputs, _targets;
    private long? _next, _announced;
    private bool _publishing;
    public long Time { get; private set; }
    public long? Next => _next;
    public Action<int, LogicPinState>? Changed { get; set; }
    public Action<long?>? DeadlineChanged { get; set; }
    public LogicPinState Get(int pin)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)pin, (uint)count);
        return (LogicPinState)((_outputs >> (pin * 2)) & 3);
    }

    /// <summary>Validate every date before changing any pin; callers commit inputs after this preflight.</summary>
    public void Validate(ushort targets, ReadOnlySpan<long> delays)
    {
        if (delays.Length != count) throw new ArgumentException(nameof(delays));
        for (int pin = 0; pin < count; pin++)
        {
            int shift = pin * 2;
            if (((targets ^ _targets) & (3 << shift)) == 0 || ((targets ^ _outputs) & (3 << shift)) == 0) continue;
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delays[pin]);
            _ = checked(Time + delays[pin]);
        }
    }

    public void SetTargets(ushort targets, ReadOnlySpan<long> delays)
    {
        ushort changed = (ushort)(targets ^ _targets);
        if (changed == 0) return;
        _targets = targets;
        for (int pin = 0; pin < count; pin++)
        {
            int mask = 3 << (pin * 2);
            if ((changed & mask) == 0) continue;
            _due[pin] = ((_targets ^ _outputs) & mask) == 0 ? null : Time + delays[pin];
        }
        FindNext();
        PublishDeadline();
    }

    public void AdvanceTo(long time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, Time);
        if (_publishing)
        {
            if (time != Time) throw new InvalidOperationException("Cannot advance logic from a pin callback.");
            return;
        }
        while (_next is { } next && next <= time)
        {
            Time = next;
            byte changed = 0;
            for (int pin = 0; pin < count; pin++)
            {
                if (_due[pin] != next) continue;
                _due[pin] = null;
                int mask = 3 << (pin * 2);
                if (((_outputs ^ _targets) & mask) == 0) continue;
                _outputs = (ushort)((_outputs & ~mask) | (_targets & mask));
                changed |= (byte)(1 << pin);
            }
            FindNext();
            _publishing = true;
            try
            {
                for (int pin = 0; pin < count; pin++)
                    if ((changed & (1 << pin)) != 0) Changed?.Invoke(pin, Get(pin));
            }
            finally { _publishing = false; PublishDeadline(); }
        }
        Time = time;
    }

    private void FindNext()
    {
        _next = null;
        for (int pin = 0; pin < count; pin++)
            if (_due[pin] is { } due && (_next is null || due < _next)) _next = due;
    }

    private void PublishDeadline()
    {
        if (_publishing) return;
        while (_announced != _next)
        {
            _announced = _next;
            _publishing = true;
            try { DeadlineChanged?.Invoke(_next); }
            finally { _publishing = false; }
        }
    }

    public CombinationalState Capture(string part, ulong levels, ulong known, long[] timing)
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot a partial pin publication.");
        return new(1, part, Time, levels, known, _outputs, _targets, (long?[])_due.Clone(), (long[])timing.Clone());
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
        Time = state.TimeNanoseconds;
        _outputs = state.Outputs;
        _targets = state.Targets;
        state.DueNanoseconds.CopyTo(_due, 0);
        FindNext();
        _announced = _next;
    }
}
