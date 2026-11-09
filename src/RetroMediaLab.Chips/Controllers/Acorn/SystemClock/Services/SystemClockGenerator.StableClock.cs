using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
    private Action<bool>? _clock12Observer;
    private IStableClockPeriodReceiver? _clock12PeriodReceiver;
    private bool _clock12CanGroup;
    private bool _stableClock12Eligible;
    private bool _stableDomainsAligned;
    private bool _ic4ClearHigh = true;
    private byte _ic2Pins; // Received pins, not a second counter state.
    private long _nextClock2Edge;

    public IStableClockPeriodReceiver? Clock12MHzReceiver
    {
        get => _clock12PeriodReceiver;
        set { _clock12PeriodReceiver = value; UpdateClock12Route(); }
    }

    public bool Clock12MHzCanGroup
    {
        get => _clock12CanGroup;
        set { _clock12CanGroup = value; UpdateStableClockEligibility(); }
    }

    private void UpdateClock12Route()
    {
        Action<bool>? receiver = _clock12PeriodReceiver is null ? _clock12Observer
            : _clock12Observer is null ? _clock12PeriodReceiver.SetClock : PublishClock12ToBoth;
        SetReceiver(receiver, C.Clock12);
    }

    private void PublishClock12ToBoth(bool high)
    {
        _clock12PeriodReceiver?.SetClock(high);
        _clock12Observer?.Invoke(high);
    }

    private void UpdateStableClockEligibility()
    {
        _stableClock12Eligible = _clock12CanGroup && _clock12PeriodReceiver is not null
            && _clock12Observer is null && (_observedClocks & ~C.StableReceiverMask) == 0;
        if (_stableClock12Eligible && _stableDomainsAligned) UpdateNextClock2Edge();
    }

    private void UpdateNextClock2Edge()
    {
        // After physical IC4 resynchronization, IC2 follows its /3 sequence.
        // IC3 increments when the preceding IC2/QA is high. QB changes after
        // one or two such increments, according to the retained IC3/QA input.
        var firstIncrement = _ic2Pins == 0 ? 2 : _ic2Pins == 1 ? 1 : 3;
        var clocks = firstIncrement + ((_levels & C.Clock4) == 0 ? 3 : 0);
        _nextClock2Edge = _inputClockCount > long.MaxValue - clocks
            ? long.MaxValue : _inputClockCount + clocks;
    }

    private void AdvanceWithStableReceiver(long clocks)
    {
        while (clocks > 0)
        {
            if (!_stableClock12Eligible)
            {
                for (long clock = 0; clock < clocks; clock++) ClockPeriod();
                return;
            }
            var consumed = 0;
            if (_stableDomainsAligned && (_levels & C.Clock12) == 0)
            {
                var span = Math.Min(clocks, _nextClock2Edge - _inputClockCount);
                var offered = (int)Math.Min(span / 2, int.MaxValue);
                if (offered > 0)
                {
                    _ = checked(_inputClockCount + offered * 2L);
                    var periods = _clock12PeriodReceiver!.AdvanceStablePeriods(offered);
                    if (periods < 0 || periods > offered) throw new InvalidOperationException(nameof(periods));
                    consumed = periods * 2;
                }
            }
            if (consumed != 0)
            {
                AdvanceStableClockNetwork(consumed);
                clocks -= consumed;
            }
            else
            {
                ClockPeriod();
                clocks--;
            }
        }
    }
}
