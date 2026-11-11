using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    /// <inheritdoc />
    public long TryAdvanceStableClocks(long clocks) => TryAdvanceStableClocks(clocks, out _);

    /// <inheritdoc />
    public long TryAdvanceStableClocks(long clocks, out bool retryAfterBoundary)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        retryAfterBoundary = true;
        if (clocks == 0) return 0;
        // These sources cannot amortize a qualification every period. The caller
        // keeps the canonical edges until the current stable-input interval ends.
        if ((_shiftActive && _shiftSystemClock)
            || (_timer1Running && _latch1 <= 1 && (_timer1 <= 1 || _timer1Reload))
            || (_timer2Running && _timer2PulseMode && (_registers[C.DirectionB] & C.Pb6) == 0
                && !_pb6Input.HasValue && ReadPortB is not null))
        {
            retryAfterBoundary = false;
            return 0;
        }
        if (!_phi2High || (_ca2Pulse | _cb2Pulse) != 0
            || _externalShiftPending || _internalShiftPending || _shiftLeadingPending
            || _shiftInterruptPending || _shiftFinishOutputPending || _timerInterruptsPending != 0)
            return 0;

        var count = clocks;
        if (_timer1Running)
        {
            if (_timer1LoadDelay != 0 || _timer1Reload) return 0;
            count = Math.Min(count, _timer1);
        }
        if (_timer2Running)
        {
            if (_timer2PulseMode)
            {
                // Never call a legacy provider to predict a future PB6 sample.
                if (_timer2PulseSampled) return 0;
                if (Pb6Level != _lastPb6) return 0;
            }
            else
            {
                if (_timer2LoadDelay != 0 || _shiftUsesTimer && _timer2Reload) return 0;
                count = Math.Min(count, _shiftUsesTimer ? _timer2 & byte.MaxValue : _timer2);
            }
        }
        if (count <= 0) return 0;

        // All effects at the next boundary remain in the canonical edge methods.
        // When a counter runs, its bound above also makes the int conversion safe.
        if (_timer1Running) _timer1 -= (int)count;
        if (_timer2Running && !_timer2PulseMode) _timer2 -= (int)count;
        _timer1JustFired = _timer2JustFired = false;
        _timer2PulseSampled = false;
        return count;
    }
}
