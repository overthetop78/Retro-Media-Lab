using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Models;
using System.Numerics;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Constants.Sn74Ls393Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;

/// <summary>SN74LS393, including ordered ripple transitions and asynchronous clear.</summary>
/// <remarks>Initial words are explicit reproducible phase seeds, not power-on guarantees.
/// Timing parameters are a digital propagation profile within the published limits;
/// analogue behaviour outside the manufacturer's input constraints is not invented.</remarks>
public sealed partial class Sn74Ls393 : ISn74Ls393
{
    private sealed class Channel(long frequency, byte initial)
    {
        internal readonly long Frequency = frequency;
        internal long Clocks;
        internal long QaRises, RequestedQaClock;
        internal long Period, HighDuration, NextFall, LastFall;
        internal bool ClockHigh;
        internal bool ClearHigh;
        internal byte Outputs = initial;
        internal byte Observed = C.OutputMask;
        internal byte EffectiveObserved;
        // One pending transition per physical stage, not a software input FIFO.
        internal readonly long[] Due = new long[C.Stages];
        internal byte Clearing;
        internal byte Pending;
    }

    private Action<byte, byte>? _firstOutputsChanged;
    private Action<byte, byte>? _secondOutputsChanged;
    private Action<long?>? _nextEventChanged;
    private Action<long?>? _nextOutputEventChanged;
    private Action<long>? _firstQaClockReached, _secondQaClockReached;
    private long _publishedOutputDeadline;
    private readonly Channel[] _channels;
    private readonly long[] _toggleDelays;
    private readonly long _maximumRippleNanoseconds;
    private readonly long _clearNanoseconds;
    private readonly long _upperRippleNanoseconds;
    private bool _resolving;
    // Derived minimum updated by its causes, not rediscovered on every pin access.
    // Zero cannot be a propagation date because every configured delay is positive.
    private long _nextEvent;
    private long _publishedDeadline;
    public Sn74Ls393Timing Timing { get; }
    private long _timeNanoseconds;
    public long TimeNanoseconds => _timeNanoseconds;
    public long? NextEventNanoseconds => (_periodicChannels == 0 ? _nextEvent : InputDeadline()) is var next && next != 0 ? next : null;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextEventChanged; set => _nextEventChanged = value; }
    /// <summary>Next connected pin or requested QA rising-clock response.
    /// Unobserved internal stages are still resolved before every input.</summary>
    public long? NextOutputEventNanoseconds => (_periodicChannels == 0 ? OutputDeadline() : ConnectedDeadline()) is var next && next != 0 ? next : null;
    public Action<long?>? NextOutputEventNanosecondsChanged { get => _nextOutputEventChanged; set => _nextOutputEventChanged = value; }
    public Action<long>? FirstQaClockReached { get => _firstQaClockReached; set { _firstQaClockReached = value; PublishDeadline(); } }
    public Action<long>? SecondQaClockReached { get => _secondQaClockReached; set { _secondQaClockReached = value; PublishDeadline(); } }
    public long GetQaRisingClockCount(int channel) => Get(channel).QaRises;
    /// <summary>Qualify a listener by an absolute output-clock count, without
    /// altering any counter, pin, clear or physical propagation.</summary>
    public void RequestQaClock(int channel, long? risingClockCount)
    {
        var state = Get(channel);
        if (risingClockCount.HasValue) ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(risingClockCount.Value, state.QaRises);
        state.RequestedQaClock = risingClockCount ?? 0;
        PublishDeadline();
    }
    public Action<byte, byte>? FirstOutputsChanged
    {
        get => _firstOutputsChanged;
        set { _firstOutputsChanged = value; _channels[0].EffectiveObserved = value is null ? (byte)0 : _channels[0].Observed; PublishDeadline(); }
    }
    public Action<byte, byte>? SecondOutputsChanged
    {
        get => _secondOutputsChanged;
        set { _secondOutputsChanged = value; _channels[1].EffectiveObserved = value is null ? (byte)0 : _channels[1].Observed; PublishDeadline(); }
    }

    public Sn74Ls393(long firstClockFrequency, long secondClockFrequency,
        byte firstInitial, byte secondInitial, Sn74Ls393Timing timing)
    {
        ValidateFrequency(firstClockFrequency);
        ValidateFrequency(secondClockFrequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstInitial, C.OutputMask);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(secondInitial, C.OutputMask);
        ArgumentNullException.ThrowIfNull(timing);
        Timing = timing;
        _toggleDelays = [timing.QaRiseNanoseconds, timing.QbStageNanoseconds, timing.QcStageNanoseconds, timing.QdStageNanoseconds,
            timing.QaFallNanoseconds, 0, timing.QbStageNanoseconds,
            timing.QbStageNanoseconds + timing.QcStageNanoseconds,
            timing.QbStageNanoseconds + timing.QcStageNanoseconds + timing.QdStageNanoseconds];
        _maximumRippleNanoseconds = timing.MaximumRippleNanoseconds;
        _clearNanoseconds = timing.ClearNanoseconds;
        _upperRippleNanoseconds = timing.QcStageNanoseconds + timing.QdStageNanoseconds;
        _channels = [new(firstClockFrequency, firstInitial), new(secondClockFrequency, secondInitial)];
    }

    public long GetClockFrequency(int channel) => Get(channel).Frequency;
    public long GetInputClockCount(int channel) => Get(channel).Clocks;
    public bool GetClock(int channel)
    {
        var state = Get(channel);
        return state.Period == 0 ? state.ClockHigh : ClockLevel(state);
    }
    public bool GetClear(int channel) => Get(channel).ClearHigh;
    public byte GetOutputs(int channel) => Get(channel).Outputs;

    public void SetObservedOutputs(int channel, byte mask)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mask, C.OutputMask);
        var state = Get(channel);
        state.Observed = mask;
        var receiver = channel == 0 ? _firstOutputsChanged : _secondOutputsChanged;
        state.EffectiveObserved = receiver is null ? (byte)0 : mask;
        PublishDeadline();
    }

    public void SetClock(int channel, bool high)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)channel, (uint)C.Channels);
        var state = _channels[channel];
        if (state.Period != 0) DisconnectPeriodicClock(channel);
        if (state.ClockHigh == high) return;
        if (high)
        {
            state.ClockHigh = true;
            return;
        }
        ApplyFallingClock(state);
    }

    private void ApplyFallingClock(Channel state)
    {
        var count = checked(state.Clocks + 1);
        if (!state.ClearHigh)
        {
            if (state.Due[0] != 0) throw new InvalidOperationException("Input edge overlaps an unresolved stage transition.");
            var due = checked(_timeNanoseconds + _toggleDelays[(state.Outputs & 1) * C.Stages]);
            state.Due[0] = due;
            state.Pending |= 1;
            if (_nextEvent == 0 || due < _nextEvent) _nextEvent = due;
        }
        state.Clocks = count;
        state.ClockHigh = false;
        PublishDeadline();
    }

    public void SetClear(int channel, bool high)
    {
        var state = Get(channel);
        if (state.ClearHigh == high) return;
        // Check the duration before mutating the input or cancelling pending stages.
        var due = high ? checked(_timeNanoseconds + _clearNanoseconds) : 0;
        state.ClearHigh = high;
        if (high)
        {
            for (var stage = 0; stage < C.Stages; stage++)
            {
                var mask = (byte)(1 << stage);
                state.Due[stage] = (state.Outputs & mask) != 0 ? due : 0;
            }
            state.Clearing = state.Outputs;
            state.Pending = state.Outputs;
            _nextEvent = FindDeadline();
        }
        // Deassertion does not clock the counter or cancel the preceding clear pulse.
        PublishDeadline();
    }

    /// <summary>Resolve deadlines at their exact configured dates, before reaching the target.</summary>
    public void AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _timeNanoseconds);
        if (_resolving || _advancingPeriodic)
        {
            if (nanoseconds != _timeNanoseconds) throw new InvalidOperationException("Cannot advance time from a signal callback.");
            return;
        }
        if (_periodicChannels != 0) { AdvancePeriodicTime(nanoseconds); return; }
        if (_nextEvent == 0 || _nextEvent > nanoseconds)
        {
            _timeNanoseconds = nanoseconds;
            return;
        }
        ResolvePending(nanoseconds);
    }

    private void ResolvePending(long nanoseconds)
    {
        _resolving = true;
        try
        {
            var first = _channels[0];
            var second = _channels[1];
            while (_nextEvent != 0 && _nextEvent <= nanoseconds)
            {
                var next = _nextEvent;
                _timeNanoseconds = next;
                var firstPending = first.Pending;
                var secondPending = second.Pending;
                var sole = firstPending | secondPending;
                if ((firstPending == 0 || secondPending == 0) && (sole & (sole - 1)) == 0)
                {
                    // One canonical slot identifies both the responding channel
                    // and stage. No other channel has a response to publish.
                    var single = firstPending != 0 ? first : second;
                    var stage = BitOperations.TrailingZeroCount((uint)sole);
                    var fallingCount = (single.Outputs & ~single.Clearing & sole) != 0;
                    single.Due[stage] = 0;
                    single.Pending = 0;
                    single.Clearing &= (byte)~sole;
                    single.Outputs ^= (byte)sole;
                    _nextEvent = 0;
                    var singleChanged = sole;
                    if (fallingCount && !single.ClearHigh && stage + 1 < C.Stages)
                    {
                        if (stage == 1 && (single.Outputs & C.QcMask) != 0
                            && (single.EffectiveObserved & C.QbQcMask) == 0
                            && _upperRippleNanoseconds <= nanoseconds - _timeNanoseconds)
                        {
                            // Same proven unobserved upper ripple as the general
                            // path, bounded by the caller and with no other slot.
                            single.Outputs ^= C.QcQdMask;
                            _timeNanoseconds += _upperRippleNanoseconds;
                            singleChanged |= C.QcQdMask;
                        }
                        else
                        {
                            var carryStage = stage + 1;
                            var due = checked(_timeNanoseconds + _toggleDelays[carryStage]);
                            single.Due[carryStage] = _nextEvent = due;
                            single.Pending = (byte)(sole << 1);
                        }
                    }
                    // Canonical carry state is ready before external feedback.
                    var qaRise = sole == 1 && (single.Outputs & 1) != 0;
                    if (qaRise) single.QaRises = checked(single.QaRises + 1);
                    var observed = (byte)(singleChanged & single.Observed);
                    if (observed != 0)
                    {
                        if (single == first) _firstOutputsChanged?.Invoke(single.Outputs, observed);
                        else _secondOutputsChanged?.Invoke(single.Outputs, observed);
                    }
                    if (qaRise) PublishRequestedQaClock(single, single == first ? 0 : 1);
                    continue;
                }
                // Independent events at the same date resolve before either notification.
                var firstBefore = first.Outputs;
                var secondBefore = second.Outputs;
                long remaining = 0;
                var state = firstPending != 0 ? first : second;
                while (true)
                {
                    var pending = state.Pending;
                    while (pending != 0)
                    {
                        var stage = BitOperations.TrailingZeroCount((uint)pending);
                        pending &= (byte)(pending - 1);
                        var due = state.Due[stage];
                        if (due != next)
                        {
                            if (remaining == 0 || due < remaining) remaining = due;
                            continue;
                        }
                        state.Due[stage] = 0;
                        var mask = (byte)(1 << stage);
                        state.Pending &= (byte)~mask;
                        var fallingCount = (state.Outputs & ~state.Clearing & mask) != 0;
                        state.Clearing &= (byte)~mask;
                        // A count toggles its stage; clear only arms a high stage.
                        state.Outputs ^= mask;
                        if (fallingCount && !state.ClearHigh && stage + 1 < C.Stages)
                        {
                            var carryStage = stage + 1;
                            if (state.Due[carryStage] != 0) throw new InvalidOperationException("Input edge overlaps an unresolved stage transition.");
                            // The inter-stage delay does not depend on direction.
                            var carryDue = checked(_timeNanoseconds + _toggleDelays[carryStage]);
                            state.Due[carryStage] = carryDue;
                            state.Pending |= (byte)(1 << carryStage);
                            if (remaining == 0 || carryDue < remaining) remaining = carryDue;
                        }
                    }
                    if (state == second || second.Pending == 0) break;
                    state = second;
                }
                _nextEvent = remaining;
                var firstQaRise = (firstBefore & 1) == 0 && (first.Outputs & 1) != 0;
                var secondQaRise = (secondBefore & 1) == 0 && (second.Outputs & 1) != 0;
                if (firstQaRise) first.QaRises = checked(first.QaRises + 1);
                if (secondQaRise) second.QaRises = checked(second.QaRises + 1);
                var changed = (byte)((firstBefore ^ first.Outputs) & first.Observed);
                if (changed != 0) _firstOutputsChanged?.Invoke(first.Outputs, changed);
                if (firstQaRise) PublishRequestedQaClock(first, 0);
                // A first-channel callback can change the second recipient or its mask.
                changed = (byte)((secondBefore ^ second.Outputs) & second.Observed);
                if (changed != 0) _secondOutputsChanged?.Invoke(second.Outputs, changed);
                if (secondQaRise) PublishRequestedQaClock(second, 1);
            }
            _timeNanoseconds = nanoseconds;
        }
        finally
        {
            _resolving = false;
            PublishDeadline();
        }
    }

    public void AdvanceClocks(int channel, long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        var state = Get(channel);
        if (state.Period != 0) throw new InvalidOperationException("Disconnect the periodic input before supplying explicit periods.");
        if (_resolving || state.ClockHigh) throw new InvalidOperationException("Periods require CLK low outside a notification.");
        if (C.NanosecondsPerSecond % state.Frequency != 0)
            throw new InvalidOperationException("Use timed SetClock inputs for fractional-nanosecond periods.");
        var period = C.NanosecondsPerSecond / state.Frequency;
        var half = period / 2;
        _ = checked(_timeNanoseconds + checked(period * clocks));
        _ = checked(state.Clocks + clocks);
        while (clocks > 0)
        {
            if ((state.ClearHigh || (state.EffectiveObserved & 1) == 0)
                && _nextEventChanged is null && _nextOutputEventChanged is null
                && _firstQaClockReached is null && _secondQaClockReached is null && _nextEvent == 0
                && half >= _maximumRippleNanoseconds)
            {
                var observed = state.EffectiveObserved;
                var stable = clocks;
                if (!state.ClearHigh && observed != 0)
                {
                    // The lowest observed bit gives the first possible output transition.
                    var step = observed & -observed;
                    stable = Math.Min(stable, step - (state.Outputs & (step - 1)) - 1);
                }
                if (stable > 0)
                {
                    // All unobserved ripples settle before the period boundary. The other
                    // channel has no pending edge and receives no invented input clocks.
                    state.Clocks += stable;
                    state.ClockHigh = false;
                    if (!state.ClearHigh)
                    {
                        state.QaRises = checked(state.QaRises + (stable + ((state.Outputs & 1) == 0 ? 1 : 0)) / 2);
                        state.Outputs = (byte)((state.Outputs + (stable & C.OutputMask)) & C.OutputMask);
                    }
                    _timeNanoseconds = checked(_timeNanoseconds + stable * period);
                    clocks -= stable;
                    continue;
                }
            }
            if (_nextEvent == 0 && !state.ClearHigh && half >= _maximumRippleNanoseconds
                && _nextOutputEventChanged is null && _firstQaClockReached is null && _secondQaClockReached is null
                && TryAdvanceStablePeriod(state, channel, period))
            {
                clocks--;
                continue;
            }
            var start = _timeNanoseconds;
            state.ClockHigh = true;
            var fallingAt = checked(start + half);
            if (_nextEvent == 0 || _nextEvent > fallingAt) _timeNanoseconds = fallingAt;
            else AdvanceTo(fallingAt);
            if (state.ClockHigh) ApplyFallingClock(state);
            AdvanceTo(checked(start + period));
            clocks--;
        }
    }

    private bool TryAdvanceStablePeriod(Channel state, int channel, long period)
    {
        if (period > long.MaxValue - _timeNanoseconds) return false;
        var end = _timeNanoseconds + period;
        var fallingAt = _timeNanoseconds + period / 2;
        var before = state.Outputs;
        var qaAt = fallingAt + _toggleDelays[(before & 1) * C.Stages];
        var other = _channels[1 - channel];
        var hasDeadlineReceiver = _nextEventChanged is not null;
        state.ClockHigh = true;
        _timeNanoseconds = fallingAt;
        ApplyFallingClock(state);
        // A deadline callback runs outside resolution and may move time, clear
        // the counter, or supply another input. Keep the ordinary path then.
        if (hasDeadlineReceiver && (_timeNanoseconds != fallingAt || state.Outputs != before
            || state.Pending != 1 || state.Clearing != 0 || other.Pending != 0 || _nextEvent != qaAt))
        {
            AdvanceTo(end);
            PublishDeadline();
            return true;
        }
        var final = (byte)((before + 1) & C.OutputMask);
        var changes = (byte)(before ^ final);
        var pendingStage = 0;
        var fallback = false;
        _resolving = true;
        try
        {
            if ((before & 1) == 0) state.QaRises = checked(state.QaRises + 1);
            while (true)
            {
                var observed = changes & state.EffectiveObserved & ~((1 << pendingStage) - 1);
                state.Due[pendingStage] = 0;
                if (observed == 0)
                {
                    // No recipient can observe the remaining internal ripple.
                    state.Outputs = final;
                    state.Pending = 0;
                    _nextEvent = 0;
                    break;
                }
                var stage = BitOperations.TrailingZeroCount((uint)observed);
                _timeNanoseconds = qaAt + _toggleDelays[C.RippleOffsetBase + stage];
                state.Outputs = (byte)(before ^ (changes & ((1 << (stage + 1)) - 1)));
                pendingStage = stage + 1;
                var pendingMask = (byte)(changes & (1 << pendingStage));
                var next = pendingMask == 0 ? 0 : qaAt + _toggleDelays[C.RippleOffsetBase + pendingStage];
                if (pendingMask != 0) state.Due[pendingStage] = next;
                state.Pending = pendingMask;
                _nextEvent = next;
                var changed = (byte)(1 << stage);
                if (channel == 0) _firstOutputsChanged?.Invoke(state.Outputs, changed);
                else _secondOutputsChanged?.Invoke(state.Outputs, changed);
                // These are the canonical remaining responses, not shadow copies
                // of the inputs. A callback that changes them ends prediction.
                if (state.Pending != pendingMask || state.Clearing != 0 || other.Pending != 0 || _nextEvent != next)
                {
                    fallback = true;
                    break;
                }
                if (pendingMask == 0) break;
            }
            if (!fallback) _timeNanoseconds = end;
        }
        finally
        {
            _resolving = false;
            if (!fallback) PublishDeadline();
        }
        if (fallback)
        {
            AdvanceTo(end);
            PublishDeadline();
        }
        return true;
    }

    private long FindDeadline()
    {
        var first = _channels[0];
        var second = _channels[1];
        if (second.Pending == 0)
        {
            var pending = first.Pending;
            if (pending == 0) return 0;
            if ((pending & (pending - 1)) == 0)
                return first.Due[BitOperations.TrailingZeroCount((uint)pending)];
        }
        else if (first.Pending == 0 && (second.Pending & (second.Pending - 1)) == 0)
            return second.Due[BitOperations.TrailingZeroCount((uint)second.Pending)];
        long next = 0;
        foreach (var state in _channels)
        {
            var pending = state.Pending;
            while (pending != 0)
            {
                var stage = BitOperations.TrailingZeroCount((uint)pending);
                pending &= (byte)(pending - 1);
                var due = state.Due[stage];
                if (next == 0 || due < next) next = due;
            }
        }
        return next;
    }

    private void PublishDeadline()
    {
        if (_resolving || _advancingPeriodic) return;
        var next = _periodicChannels == 0 ? _nextEvent : InputDeadline();
        if (_publishedDeadline != next)
        {
            _publishedDeadline = next;
            _nextEventChanged?.Invoke(next == 0 ? null : next);
        }
        if (_nextOutputEventChanged is null) return;
        next = _periodicChannels == 0 ? OutputDeadline() : ConnectedDeadline();
        if (_publishedOutputDeadline == next) return;
        _publishedOutputDeadline = next;
        _nextOutputEventChanged(next == 0 ? null : next);
    }

    private void PublishRequestedQaClock(Channel state, int channel)
    {
        if (state.RequestedQaClock == 0 || state.RequestedQaClock > state.QaRises) return;
        state.RequestedQaClock = 0;
        if (channel == 0) _firstQaClockReached?.Invoke(state.QaRises);
        else _secondQaClockReached?.Invoke(state.QaRises);
    }

    private long OutputDeadline()
    {
        var first = _channels[0]; var second = _channels[1];
        var pending = first.Pending | second.Pending;
        if (pending == 0) return 0;
        if ((first.Pending != 0 && second.Pending != 0) || (pending & (pending - 1)) != 0) return _nextEvent;
        var state = first.Pending != 0 ? first : second;
        var observed = state.EffectiveObserved;
        if (state.RequestedQaClock == state.QaRises + 1 && (state.Outputs & 1) == 0
            && (state == first ? _firstQaClockReached : _secondQaClockReached) is not null) observed |= 1;
        if ((pending & observed) != 0) return _nextEvent;
        if ((pending & state.Clearing) != 0 || state.ClearHigh) return 0;
        var stage = BitOperations.TrailingZeroCount((uint)pending);
        // The ripple toggles exactly the carry prefix of this binary increment.
        // Select its first connected bit once; cumulative stage delays already
        // belong to this package. No per-stage wake or speculative queue.
        var connectedChanges = (state.Outputs ^ (state.Outputs + pending)) & observed;
        if (connectedChanges == 0) return 0;
        var target = BitOperations.TrailingZeroCount((uint)connectedChanges);
        return checked(_nextEvent + (_toggleDelays[C.RippleOffsetBase + target]
            - _toggleDelays[C.RippleOffsetBase + stage]));
    }

    private Channel Get(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)channel, (uint)C.Channels);
        return _channels[channel];
    }

    private static void ValidateFrequency(long frequency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frequency, C.MaximumClockFrequency);
    }
}
