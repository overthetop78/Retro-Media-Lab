using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
    private Action<bool>? _clock12Observer;
    private IStableClockPeriodReceiver? _clock12PeriodReceiver;
    private IIndependentClockPeriodReceiver? _clock12IndependentReceiver;
    private bool _clock12CanGroup;
    private bool _stableClock12Eligible;
    private bool _stableDomainsAligned;
    private bool _ic4ClearHigh = true;
    private byte _ic2Pins; // Received pins, not a second counter state.
    private long _nextReceiverEdge;
    private bool _clock4CanChangeClock12Inputs = true;
    private long _receiverPreAdvancedEnd;

    /// <summary>Conservative by default. Set false only when the physical4 MHz
    /// receiver cannot change or inspect the12 MHz receiver before the next2 MHz
    /// edge, and clock bindings remain fixed through the accepted interval.</summary>
    public bool Clock4MHzCanChangeClock12Inputs
    {
        get => _clock4CanChangeClock12Inputs;
        set { RequireReceiverBoundary(); ResolveDeferredClockNetwork(); _clock4CanChangeClock12Inputs = value; UpdateStableClockEligibility(); }
    }

    private void RequireReceiverBoundary()
    {
        if (_inputClockCount < _receiverPreAdvancedEnd)
            throw new InvalidOperationException("Clock bindings cannot change inside an accepted independent receiver interval.");
    }

    public IStableClockPeriodReceiver? Clock12MHzReceiver
    {
        get => _clock12PeriodReceiver;
        set { RequireReceiverBoundary(); ResolveDeferredClockNetwork(); _clock12PeriodReceiver = value; UpdateClock12Route(); }
    }

    public bool Clock12MHzCanGroup
    {
        get => _clock12CanGroup;
        set { ResolveDeferredClockNetwork(); _clock12CanGroup = value; UpdateStableClockEligibility(); }
    }

    private void UpdateClock12Route()
    {
        _clock12IndependentReceiver = _clock12PeriodReceiver as IIndependentClockPeriodReceiver;
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
        _stableClock12Eligible = (_clock12CanGroup || _clock12IndependentReceiver is not null) && _clock12PeriodReceiver is not null
            && _clock12Observer is null && (_observedClocks & ~C.StableReceiverMask) == 0;
        if (_stableClock12Eligible && _stableDomainsAligned) UpdateNextReceiverEdge();
    }

    private void UpdateNextReceiverEdge()
    {
        // After physical IC4 resynchronization, IC2 follows its /3 sequence.
        // IC3 increments when the preceding IC2/QA is high. QB changes after
        // one or two such increments, according to the retained IC3/QA input.
        var firstIncrement = _ic2Pins == 0 ? 2 : _ic2Pins == 1 ? 1 : 3;
        // QA (4 MHz) changes at every IC3 increment. When wired, that closer
        // boundary must be published before another receiver interval begins.
        var clocks = _clock4CanChangeClock12Inputs && (_observedClocks & C.Clock4) != 0
            ? firstIncrement : firstIncrement + ((_levels & C.Clock4) == 0 ? 3 : 0);
        _nextReceiverEdge = _inputClockCount > long.MaxValue - clocks
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
            var represented = 0;
            if (_stableDomainsAligned)
            {
                var span = Math.Min(clocks, _nextReceiverEdge - _inputClockCount);
                var offered = (int)Math.Min(span / 2, int.MaxValue);
                if (offered > 0)
                {
                    _ = checked(_inputClockCount + offered * 2L);
                    var startHigh = (_levels & C.Clock12) != 0;
                    var periods = startHigh
                        ? _clock12PeriodReceiver!.AdvanceStablePeriods(offered, true)
                        : _clock12PeriodReceiver!.AdvanceStablePeriods(offered);
                    if (periods < 0 || periods > offered) throw new InvalidOperationException(nameof(periods));
                    if (periods < offered && _clock12IndependentReceiver is not null)
                    {
                        _receiverPreAdvancedEnd = checked(_inputClockCount + offered * 2L);
                        try { _clock12IndependentReceiver.AdvancePeriods(offered - periods, startHigh); }
                        finally { _receiverPreAdvancedEnd = 0; }
                        periods = offered;
                    }
                    represented = consumed = periods * 2;
                    // A final single source edge need not restart the scalar
                    // network. Capture it with the accepted stable prefix; the
                    // changed CLK12 level is still delivered normally by
                    // PublishOutputs, before the coincident CLK4/CLK2 outputs.
                    if (consumed > 0 && consumed < span) consumed++;
                }
            }
            if (consumed != 0)
            {
                if (!_clock4CanChangeClock12Inputs && (_observedClocks & C.Clock4) != 0)
                    AdvanceWithIndependentFourMhz(consumed, represented);
                else AdvanceStableClockNetwork(consumed);
                clocks -= consumed;
            }
            else
            {
                ClockPeriod();
                clocks--;
            }
        }
    }

    private void AdvanceWithIndependentFourMhz(int clocks, int represented)
    {
        _receiverPreAdvancedEnd = checked(_inputClockCount + represented);
        var extra = clocks > represented;
        try
        {
            while (clocks != 0)
            {
                // Keep every4 MHz boundary and the canonical counter state there.
                var nextFour = _ic2Pins == 0 ? 2 : _ic2Pins == 1 ? 1 : 3;
                var step = Math.Min(clocks, nextFour);
                var finalExtra = extra && step == clocks;
                AdvanceStableClockNetwork(step, C.Clock12, finalExtra ? C.Clock12 : (byte)0);
                clocks -= step;
            }
        }
        finally { _receiverPreAdvancedEnd = 0; }
    }
}
