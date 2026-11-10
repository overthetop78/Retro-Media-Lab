using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
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
