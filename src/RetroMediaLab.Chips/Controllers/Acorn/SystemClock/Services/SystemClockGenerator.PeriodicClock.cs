using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
    /// <summary>Boundary suitable for complete represented2 MHz periods. The
    /// caller still has to qualify all receiver interactions independently.</summary>
    public bool CanRepresentReferencePeriods => !_oscillatorHigh && _stableDomainsAligned
        && _stableClock12Eligible && _clock12IndependentReceiver is not null
        && (_observedClocks & ~(C.Clock12 | C.Clock2)) == 0 && !Clock2MHz
        && _nextReceiverEdge - _inputClockCount == C.Clock2HalfPeriodSourceClocks;

    /// <summary>Resolve a source interval whose12/2 MHz receivers have already
    /// represented every effect and interaction. This does not run receivers or
    /// replay their callbacks. Other observed clocks forbid this operation.</summary>
    public void AdvanceRepresentedNetworkClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (clocks == 0) return;
        RequireReceiverBoundary();
        if (_oscillatorHigh || !_stableDomainsAligned || !_stableClock12Eligible
            || _clock12IndependentReceiver is null || (_observedClocks & ~(C.Clock12 | C.Clock2)) != 0)
            throw new InvalidOperationException("Every connected source output must be represented before advancing this range.");
        var end = checked(_inputClockCount + clocks);
        if (_deferRepresentedResolution && clocks % C.Clock2PeriodSourceClocks == 0 && CanRepresentReferencePeriods)
        {
            _deferredNetworkClocks = checked(_deferredNetworkClocks + clocks);
            _inputClockCount = end;
            // Complete CLK2 periods retain both connected pin levels and the
            // same IC2 phase. The next connected edge stays six source clocks away.
            _nextReceiverEdge = end > long.MaxValue - C.Clock2HalfPeriodSourceClocks
                ? long.MaxValue : end + C.Clock2HalfPeriodSourceClocks;
            return;
        }
        ResolveDeferredClockNetwork();
        ResolveRepresentedClockNetwork(clocks, end);
    }

    private void ResolveRepresentedClockNetwork(long clocks, long end)
    {
        var ic2 = _ic2.ResolveRepresentedClockRange(clocks, Ic2PeriodicInputs, _stableClockOrigin);
        var ic3 = _ic3.ResolveRepresentedClockRange(clocks, Ic3PeriodicInputs, _stableClockOrigin);
        var ic4 = _ic4.ResolveRepresentedClockRange(clocks, Ic4PeriodicInputs, _stableClockOrigin);
        _inputClockCount = end;
        _ic2Pins = (byte)(ic2 & 15); _ic4ClearHigh = (ic3 & 16) == 0;
        _levels = (byte)(((ic2 & 1) << 1) | ((ic3 & 1) << 3) | ((ic3 & 6) << 4)
            | (ic4 & 1) | ((ic4 & 2) << 1) | ((ic4 & 4) << 2));
        _publishedLevels = _levels;
        FallingClock();
        UpdateNextReceiverEdge();
    }

    // Actual post-edge input waveforms from figure 9, anchored at IC4 clear
    // with IC2=2. Immutable wiring data; every counter retains its own time/state.
    private static readonly Sn74S163PeriodicInputs Ic2PeriodicInputs = new(
        [14, 15, 15], C.Clock2HalfPeriodSourceClocks);
    private static readonly Sn74S163PeriodicInputs Ic3PeriodicInputs = new(
        [3, 3, 15], C.Clock2HalfPeriodSourceClocks);
    private static readonly Sn74S163PeriodicInputs Ic4PeriodicInputs = CreateIc4Inputs();
    private long _stableClockOrigin;

    private static Sn74S163PeriodicInputs CreateIc4Inputs()
    {
        byte[] pins = new byte[48]; // /3 followed by the sixteen-count RCO cycle.
        Array.Fill(pins, (byte)15);
        pins[^1] = 14;
        return new Sn74S163PeriodicInputs(pins, C.Clock2HalfPeriodSourceClocks);
    }

    private void AdvanceStableClockNetwork(int periods, byte representedClocks = 0, byte extraClocks = 0)
    {
        // The receiver has accepted the intervening 12 MHz periods without
        // changing output. This interval ends no later than the next 2 MHz edge.
        _oscillatorHigh = true;
        _inputClockCount = checked(_inputClockCount + periods);
        // These internal input waveforms already represent the complete wired
        // network. Resolve each component's canonical state directly; replaying
        // its pin callbacks would repeat that same internal work. External pins
        // are published only once all three states have reached the shared edge.
        var ic2 = _ic2.ResolveRepresentedClockPeriods(periods, Ic2PeriodicInputs, _stableClockOrigin);
        var ic3 = _ic3.ResolveRepresentedClockPeriods(periods, Ic3PeriodicInputs, _stableClockOrigin);
        var ic4 = _ic4.ResolveRepresentedClockPeriods(periods, Ic4PeriodicInputs, _stableClockOrigin);
        _ic2Pins = (byte)(ic2 & 15);
        _ic4ClearHigh = (ic3 & 16) == 0;
        _levels = (byte)(((ic2 & 1) << 1) | ((ic3 & 1) << 3) | ((ic3 & 6) << 4)
            | (ic4 & 1) | ((ic4 & 2) << 1) | ((ic4 & 4) << 2));
        if (representedClocks != 0)
        {
            // These receiver fronts were delivered by its accepted periods.
            // Only publication bookkeeping changes here, never the real pins.
            // For the one additional physical front, retain the receiver's
            // opposite preceding level so ordinary publication delivers it once.
            _publishedLevels = (byte)((_publishedLevels & ~representedClocks)
                | ((_levels ^ extraClocks) & representedClocks));
        }
        PublishOutputs();
        FallingClock();
    }
}
