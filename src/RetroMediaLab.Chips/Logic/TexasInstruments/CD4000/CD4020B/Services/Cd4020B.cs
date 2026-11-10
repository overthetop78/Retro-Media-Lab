using System.Numerics;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Models;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Constants.Cd4020BConstants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Services;

/// <summary>CD4020B ripple stages, asynchronous reset and component-owned propagation.</summary>
/// <remarks>The explicit initial word is a reproducible phase, not a power-on guarantee.
/// Analogue/metastable behaviour outside the input timing constraints is not invented.</remarks>
public sealed class Cd4020B : ICd4020B
{
    private readonly long[] _due = new long[C.Stages];
    private readonly long _firstStageDelay;
    private readonly long _stageDelay;
    private readonly long _resetDelay;
    private readonly long _fullRipple;
    private long _time;
    private long _clocks;
    private bool _clockHigh;
    private bool _resetHigh;
    private ushort _levels;
    private ushort _targets;
    private ushort _resetting;
    private ushort _pending;
    private ushort _observed = C.ExposedMask;
    private ushort _effectiveObserved;
    private long _next;
    private long _publishedNext;
    private bool _resolving;
    private Action<ushort, ushort>? _outputsChanged;
    private Action<long?>? _nextChanged;

    public Cd4020B(long clockFrequency, ushort initialStages, Cd4020BTiming timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(clockFrequency, timing.MaximumClockFrequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialStages, C.CounterMask);
        ClockFrequency = clockFrequency;
        Timing = timing;
        _levels = initialStages;
        _firstStageDelay = timing.FirstStageNanoseconds;
        _stageDelay = timing.SubsequentStageNanoseconds;
        _resetDelay = timing.ResetNanoseconds;
        _fullRipple = timing.FullRippleNanoseconds;
    }

    public Cd4020BTiming Timing { get; }
    public long ClockFrequency { get; }
    public long InputClockCount => _clocks;
    public long TimeNanoseconds => _time;
    public bool ClockHigh => _clockHigh;
    public bool ResetHigh => _resetHigh;
    public ushort StageLevels => _levels;
    public ushort OutputLevels => (ushort)(_levels & C.ExposedMask);
    public long? NextEventNanoseconds => _next == 0 ? null : _next;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextChanged; set => _nextChanged = value; }
    public Action<ushort, ushort>? OutputsChanged
    {
        get => _outputsChanged;
        set { _outputsChanged = value; _effectiveObserved = value is null ? (ushort)0 : _observed; }
    }

    public void SetObservedOutputs(ushort mask)
    {
        if ((mask & ~C.ExposedMask) != 0) throw new ArgumentOutOfRangeException(nameof(mask));
        _observed = mask;
        _effectiveObserved = _outputsChanged is null ? (ushort)0 : mask;
    }

    public void SetClock(bool high)
    {
        if (_clockHigh == high) return;
        if (high) { _clockHigh = true; return; }
        FallingEdge();
    }

    private void FallingEdge()
    {
        var count = checked(_clocks + 1);
        if (!_resetHigh) ScheduleToggle(0);
        _clocks = count;
        _clockHigh = false;
        PublishDeadline();
    }

    public void SetReset(bool high)
    {
        if (_resetHigh == high) return;
        var due = high ? checked(_time + _resetDelay) : 0;
        _resetHigh = high;
        if (high)
        {
            // Cancel every counting transition; reset each actual stage independently.
            for (var stage = 0; stage < C.Stages; stage++)
                _due[stage] = (_levels & (1 << stage)) != 0 ? due : 0;
            _pending = _levels;
            _resetting = _levels;
            _targets = 0;
            _next = _pending == 0 ? 0 : due;
        }
        PublishDeadline();
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
                var previous = _levels;
                Resolve(_next);
                _next = FindDeadline();
                var changed = (ushort)((previous ^ _levels) & _effectiveObserved);
                if (changed != 0) _outputsChanged?.Invoke((ushort)(_levels & C.ExposedMask), changed);
            }
            _time = nanoseconds;
        }
        finally { _resolving = false; PublishDeadline(); }
    }

    private void Resolve(long time)
    {
        var pending = _pending;
        while (pending != 0)
        {
            var stage = BitOperations.TrailingZeroCount((uint)pending);
            pending &= (ushort)(pending - 1);
            if (_due[stage] != time) continue;
            var mask = (ushort)(1 << stage);
            _due[stage] = 0;
            _pending &= (ushort)~mask;
            var wasHigh = (_levels & mask) != 0;
            var high = (_targets & mask) != 0;
            var reset = (_resetting & mask) != 0;
            _resetting &= (ushort)~mask;
            _levels = (ushort)((_levels & ~mask) | (high ? mask : 0));
            if (wasHigh && !high && !reset && !_resetHigh && stage + 1 < C.Stages)
                ScheduleToggle(stage + 1);
        }
    }

    private void ScheduleToggle(int stage)
    {
        if (_due[stage] != 0) throw new InvalidOperationException("Input edge overlaps an unresolved stage transition.");
        var mask = (ushort)(1 << stage);
        var high = (_levels & mask) == 0;
        var due = checked(_time + (stage == 0 ? _firstStageDelay : _stageDelay));
        _targets = (ushort)((_targets & ~mask) | (high ? mask : 0));
        _due[stage] = due;
        _pending |= mask;
        if (_next == 0 || due < _next) _next = due;
    }

    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_resolving || _clockHigh) throw new InvalidOperationException("Periods require CLK low outside a callback.");
        if (C.NanosecondsPerSecond % ClockFrequency != 0)
            throw new InvalidOperationException("Use timed clock levels for fractional-nanosecond periods.");
        var period = C.NanosecondsPerSecond / ClockFrequency;
        var half = period / 2;
        _ = checked(_time + checked(period * clocks));
        _ = checked(_clocks + clocks);
        while (clocks > 0)
        {
            if ((_resetHigh || (_effectiveObserved & 1) == 0) && _nextChanged is null
                && _next == 0 && half >= _fullRipple)
            {
                var stable = clocks;
                if (!_resetHigh && _effectiveObserved != 0)
                {
                    var step = _effectiveObserved & -_effectiveObserved;
                    stable = Math.Min(stable, step - (_levels & (step - 1)) - 1);
                }
                if (stable > 0)
                {
                    _clocks += stable;
                    if (!_resetHigh) _levels = (ushort)((_levels + (stable & C.CounterMask)) & C.CounterMask);
                    _time = checked(_time + stable * period);
                    clocks -= stable;
                    continue;
                }
            }
            var start = _time;
            _clockHigh = true;
            var fallingAt = checked(start + half);
            if (_next == 0 || _next > fallingAt) _time = fallingAt;
            else AdvanceTo(fallingAt);
            FallingEdge();
            AdvanceTo(checked(start + period));
            clocks--;
        }
    }

    private long FindDeadline()
    {
        var pending = _pending;
        if (pending == 0) return 0;
        if ((pending & (pending - 1)) == 0) return _due[BitOperations.TrailingZeroCount((uint)pending)];
        long next = 0;
        while (pending != 0)
        {
            var stage = BitOperations.TrailingZeroCount((uint)pending);
            pending &= (ushort)(pending - 1);
            var due = _due[stage];
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
}
