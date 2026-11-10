using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;
using Via = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    // Retain the ordinary exact path for comparison and for unqualified wiring.
    internal bool GroupPrivateClockPeriods { get; set; } = true;
    internal long RepresentedPrivateClockPeriods { get; private set; }
    internal bool GroupViaReadPeriods { get; set; } = true;
    internal long RepresentedViaReadPeriods { get; private set; }

    internal bool IsIndependentViaReadAddress(ushort address)
        => _viaReadCanRunIndependently && ViaAddress(address) && (address & C.ViaRegisterMask) == Via.PortA;

    internal bool IsPrivateCpuMemoryAddress(ushort address)
        => !_video!.Handles(address) && !ViaAddress(address) && !RamIoAddress(address)
            && (_floppyBoard is null || SystemFloppyBoard.AddressInhibitsDecoder(address));

    private bool TryRunPrivateClockPrefix()
    {
        if (!GroupPrivateClockPeriods || _fastClock is null || !_fastClock.CanRepresentReferencePeriods
            || !_fastCpuClock!.CanRepresentClockPeriods || _video?.IsProgrammed != true
            || _tapeClockRequired) return false;
        var privateCycle = _fastCpuBus!.CanRepresentPreparedCycle;
        var viaRead = !privateCycle && GroupViaReadPeriods && _fastCpuBus.CanRepresentPreparedViaRead;
        if (!privateCycle && !viaRead) return false;
        if (_floppyBoard is not null && (!_floppyBoard.CpuClockCanRunIndependently || !_floppyBoard.PrivateBusPinsSettled)) return false;
        // The VIA transaction itself still executes at its real CPU phase.
        // Limit this case to one cycle so no intermediate bus address is lost.
        var maximum = viaRead ? 1 : Math.Min(C.FastPrivateClockBatch, _video.ClocksToLineBoundary);
        if (_floppyBoard is not null && _floppyBoard.NextDriveInteractionNanoseconds != long.MaxValue)
        {
            var now = (_fastClock.InputClockCount - 1) * 125 / 3;
            var until = _floppyBoard.NextDriveInteractionNanoseconds - now;
            if (until <= C.FastReferencePeriodNanoseconds) return false;
            maximum = (int)Math.Min(maximum, (until - 1) / C.FastReferencePeriodNanoseconds);
        }
        if (maximum < 2 && !viaRead) return false;
        var consumed = 0;
        _fastCpuBus.BeginRepresentedPrivateCycles();
        try
        {
            while (consumed < maximum && (_fastCpuBus.CanRepresentPreparedCycle
                || (viaRead && consumed == 0 && _fastCpuBus.CanRepresentPreparedViaRead)))
            {
                _cpu6502!.SetPhaseZeroInput(true);
                TickReferenceClock(advanceVia: false, advanceVideo: false);
                _cpu6502.SetPhaseZeroInput(false);
                consumed++;
            }
        }
        finally
        {
            try
            {
                if (consumed != 0)
                {
                    _video.AdvanceIndependentCharacterPeriods(consumed, _fastClock.Clock12MHz);
                    _fastCpuClock.AdvanceRepresentedClockPeriods(consumed);
                    _fastClock.AdvanceRepresentedNetworkClocks(consumed * (long)C.FastSourceClocksPerReferencePeriod);
                    AdvanceFloppyToSource();
                    RepresentedPrivateClockPeriods += consumed;
                    if (viaRead) RepresentedViaReadPeriods += consumed;
                }
            }
            finally { _fastCpuBus.EndRepresentedPrivateCycles(); }
        }
        return consumed != 0;
    }
}
