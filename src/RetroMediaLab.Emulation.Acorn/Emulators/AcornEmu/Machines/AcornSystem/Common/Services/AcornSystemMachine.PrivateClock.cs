using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    // Retain the ordinary exact path for comparison and for unqualified wiring.
    internal bool GroupPrivateClockPeriods { get; set; } = true;
    internal long RepresentedPrivateClockPeriods { get; private set; }

    internal bool IsPrivateCpuMemoryAddress(ushort address)
        => !_video!.Handles(address) && !ViaAddress(address) && !RamIoAddress(address)
            && (_floppyBoard is null || SystemFloppyBoard.AddressInhibitsDecoder(address));

    private bool TryRunPrivateClockPrefix()
    {
        if (!GroupPrivateClockPeriods || _fastClock is null || !_fastClock.CanRepresentReferencePeriods
            || !_fastCpuClock!.CanRepresentClockPeriods || _video?.IsProgrammed != true
            || _tapeClockRequired || !_fastCpuBus!.CanRepresentPreparedCycle) return false;
        if (_floppyBoard is not null && (!_floppyBoard.CpuClockCanRunIndependently || !_floppyBoard.PrivateBusPinsSettled)) return false;
        var maximum = Math.Min(C.FastPrivateClockBatch, _video.ClocksToLineBoundary);
        if (_floppyBoard is not null && _floppyBoard.NextDriveInteractionNanoseconds != long.MaxValue)
        {
            var now = (_fastClock.InputClockCount - 1) * 125 / 3;
            var until = _floppyBoard.NextDriveInteractionNanoseconds - now;
            if (until <= C.FastReferencePeriodNanoseconds) return false;
            maximum = (int)Math.Min(maximum, (until - 1) / C.FastReferencePeriodNanoseconds);
        }
        if (maximum < 2) return false;
        var consumed = 0;
        _fastCpuBus.BeginRepresentedPrivateCycles();
        try
        {
            while (consumed < maximum && _fastCpuBus.CanRepresentPreparedCycle)
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
                }
            }
            finally { _fastCpuBus.EndRepresentedPrivateCycles(); }
        }
        return consumed != 0;
    }
}
