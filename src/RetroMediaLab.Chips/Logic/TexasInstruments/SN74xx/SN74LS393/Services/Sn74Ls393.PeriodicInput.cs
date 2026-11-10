using System.Numerics;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Constants.Sn74Ls393Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;

public sealed partial class Sn74Ls393
{
    private byte _periodicChannels;
    private bool _advancingPeriodic;

    /// <summary>Receive a periodic external CLK waveform, preserving its current
    /// level. This describes the connected source; it creates no oscillator.</summary>
    public void ConnectPeriodicClock(int channel, long nextFallingNanoseconds, long periodNanoseconds, long highNanoseconds)
    {
        var state = Get(channel);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nextFallingNanoseconds, _timeNanoseconds);
        ArgumentOutOfRangeException.ThrowIfLessThan(periodNanoseconds,
            (C.NanosecondsPerSecond + C.MaximumClockFrequency - 1) / C.MaximumClockFrequency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(highNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(highNanoseconds, periodNanoseconds);
        if (nextFallingNanoseconds - _timeNanoseconds > periodNanoseconds)
            throw new ArgumentOutOfRangeException(nameof(nextFallingNanoseconds));
        if (ClockLevel(state) != (nextFallingNanoseconds - _timeNanoseconds <= highNanoseconds))
            throw new ArgumentException("The waveform must preserve the received CLK level.");
        state.Period = periodNanoseconds;
        state.HighDuration = highNanoseconds;
        state.NextFall = nextFallingNanoseconds;
        _periodicChannels |= (byte)(1 << channel);
        PublishDeadline();
    }

    public void DisconnectPeriodicClock(int channel)
    {
        var state = Get(channel);
        if (state.Period == 0) return;
        state.ClockHigh = ClockLevel(state);
        state.Period = state.NextFall = 0;
        _periodicChannels &= (byte)~(1 << channel);
        PublishDeadline();
    }

    private bool ClockLevel(Channel state)
    {
        if (state.Period == 0) return state.ClockHigh;
        if (state.NextFall == 0)
            return _resolving ? _timeNanoseconds - state.LastFall > state.Period - state.HighDuration
                : _timeNanoseconds - state.LastFall >= state.Period - state.HighDuration;
        var until = state.NextFall - _timeNanoseconds;
        // Responses already due at a shared date precede the new input edge,
        // just as AdvanceTo(date), SetClock(level) in the explicit input path.
        return _resolving ? until < state.HighDuration : until <= state.HighDuration;
    }

    private static long Earlier(long first, long second)
        => first == 0 ? second : second == 0 ? first : Math.Min(first, second);

    private long NextInputFall() => Earlier(_channels[0].NextFall, _channels[1].NextFall);
    private long InputDeadline() => Earlier(_nextEvent, NextInputFall());
    private static long Future(long time, long delay) => delay > long.MaxValue - time ? 0 : time + delay;

    private void AdvancePeriodicTime(long target)
    {
        _advancingPeriodic = true;
        try
        {
            while (true)
            {
                if (TrySkipUnobservedInputPeriods(target)) continue;
                var next = NextInputFall();
                if (_nextEvent != 0 && _nextEvent <= target && (next == 0 || _nextEvent <= next))
                {
                    // A response may replace a waveform. Reconsider the input
                    // dates immediately afterwards rather than crossing a new
                    // earlier falling edge with the previous source descriptor.
                    ResolvePending(_nextEvent);
                    continue;
                }
                if (next == 0 || next > target) break;
                _timeNanoseconds = next;
                // Both external falling inputs are presented before any of
                // their delayed outputs can resolve. Binding changes received
                // from an earlier response take effect before this input.
                for (var channel = 0; channel < C.Channels; channel++)
                {
                    var state = _channels[channel];
                    if (state.Period == 0 || state.NextFall != next) continue;
                    state.LastFall = next;
                    state.NextFall = Future(next, state.Period);
                    state.ClockHigh = true;
                    ApplyFallingClock(state);
                }
            }
            _timeNanoseconds = target;
        }
        finally { _advancingPeriodic = false; PublishDeadline(); }
    }

    private bool TrySkipUnobservedInputPeriods(long target)
    {
        if (_periodicChannels is 0 or 3 || _nextEvent != 0 || target < _maximumRippleNanoseconds) return false;
        var channel = _periodicChannels == 1 ? 0 : 1;
        var state = _channels[channel];
        var first = state.NextFall;
        var limit = target - _maximumRippleNanoseconds;
        if (first == 0 || first > limit || state.Period < _maximumRippleNanoseconds) return false;
        var periods = (limit - first) / state.Period + 1;
        if (!state.ClearHigh)
        {
            var observed = state.EffectiveObserved;
            if (observed != 0)
            {
                var step = observed & -observed;
                periods = Math.Min(periods, step - (state.Outputs & (step - 1)) - 1);
            }
            var listener = channel == 0 ? _firstQaClockReached : _secondQaClockReached;
            var needed = state.RequestedQaClock - state.QaRises;
            if (listener is not null && needed > 0 && needed <= periods / 2 + 1)
                periods = Math.Min(periods, needed * 2 - ((state.Outputs & 1) == 0 ? 1 : 0) - 1);
        }
        if (periods <= 0) return false;
        var last = checked(first + (periods - 1) * state.Period);
        state.Clocks = checked(state.Clocks + periods);
        if (!state.ClearHigh)
        {
            state.QaRises = checked(state.QaRises + periods / 2
                + (((periods & 1) != 0 && (state.Outputs & 1) == 0) ? 1 : 0));
            state.Outputs = (byte)((state.Outputs + (periods & C.OutputMask)) & C.OutputMask);
        }
        state.LastFall = last;
        state.NextFall = Future(last, state.Period);
        _timeNanoseconds = checked(last + _maximumRippleNanoseconds);
        return true;
    }

    private long ConnectedDeadline()
    {
        var next = OutputDeadline();
        if (_periodicChannels == 0) return next;
        var first = _channels[0]; var second = _channels[1];
        var pending = first.Pending | second.Pending;
        if ((first.Pending != 0 && second.Pending != 0) || (pending & (pending - 1)) != 0)
            return Earlier(next, _nextEvent);
        for (var channel = 0; channel < C.Channels; channel++)
        {
            var state = _channels[channel];
            if (state.Period == 0 || state.NextFall == 0 || state.ClearHigh) continue;
            var word = state.Outputs;
            var qaRises = state.QaRises;
            if (state.Pending != 0)
            {
                if (state.NextFall - _nextEvent < _maximumRippleNanoseconds)
                    return Earlier(next, _nextEvent);
                if (state.Clearing != 0) word &= (byte)~state.Pending;
                else
                {
                    if (state.Pending == 1 && (word & 1) == 0) qaRises++;
                    word = (byte)((word + state.Pending) & C.OutputMask);
                }
            }
            var observed = state.EffectiveObserved;
            if (observed != 0)
            {
                var bit = BitOperations.TrailingZeroCount((uint)observed);
                var step = 1 << bit;
                var clocks = step - (word & (step - 1));
                var delay = bit == 0 ? _toggleDelays[(word & 1) * C.Stages]
                    : _toggleDelays[C.Stages] + _toggleDelays[C.RippleOffsetBase + bit];
                next = Earlier(next, FutureOutputTime(state, clocks, delay));
            }
            var requested = state.RequestedQaClock - qaRises;
            if (requested <= 0 || requested > long.MaxValue / 2
                || (channel == 0 ? _firstQaClockReached : _secondQaClockReached) is null) continue;
            var qaClocks = requested * 2 - ((word & 1) == 0 ? 1 : 0);
            next = Earlier(next, FutureOutputTime(state, qaClocks, _toggleDelays[0]));
        }
        return next;
    }

    private static long FutureOutputTime(Channel state, long clocks, long delay)
    {
        if (clocks - 1 > (long.MaxValue - state.NextFall) / state.Period) return 0;
        return Future(state.NextFall + (clocks - 1) * state.Period, delay);
    }
}
