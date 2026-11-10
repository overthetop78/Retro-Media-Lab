using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;
using Via = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    // Retain the ordinary exact path for comparison and for unqualified wiring.
    internal bool GroupPrivateClockPeriods { get; set; } = true;
    internal long RepresentedPrivateClockPeriods { get; private set; }
    internal bool GroupViaReadPeriods { get; set; } = true;
    internal bool GroupIndependentBusCycles { get; set; } = true;
    internal long RepresentedViaReadPeriods { get; private set; }
    internal bool GroupActiveFloppyPeriods { get; set; } = true;
    internal bool GroupIdleDriveInteractions { get; set; } = true;

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
        var clockFloppyAtCpuEdges = _floppyBoard is not null && !_floppyBoard.CpuClockCanRunIndependently;
        if (_floppyBoard is not null && (!_floppyBoard.PrivateBusPinsSettled
            || (clockFloppyAtCpuEdges && !GroupActiveFloppyPeriods))) return false;
        // Every qualified transaction still executes at its actual CPU phase.
        // In 0E00-0FFF IC4 has A9=A10=1, so its only connected output Y1
        // stays inactive. Private addresses inhibit the decoder entirely. No
        // intermediate address in this prefix can select the disk controller;
        // changed VIA control modes or another address end the prefix below.
        var maximum = viaRead && !GroupIndependentBusCycles ? 1
            : Math.Min(C.FastPrivateClockBatch, _video.ClocksToLineBoundary);
        // With an idle, disconnected controller no drive event can affect this
        // private span. The existing prefix-end advance still consumes every
        // drive/card event at its local date before the next interaction.
        if (!clockFloppyAtCpuEdges && _floppyBoard is not null
            && _floppyBoard.NextDriveInteractionNanoseconds != long.MaxValue
            && (!GroupIdleDriveInteractions || !_floppyBoard.DriveEventsIndependentOfCpu))
        {
            var now = (_fastClock.InputClockCount - 1) * 125 / 3;
            var until = _floppyBoard.NextDriveInteractionNanoseconds - now;
            if (until <= C.FastReferencePeriodNanoseconds) return false;
            maximum = (int)Math.Min(maximum, (until - 1) / C.FastReferencePeriodNanoseconds);
        }
        if (maximum < 2 && !viaRead) return false;
        var source = _fastClock.InputClockCount;
        _ = checked(source + maximum * (long)C.FastSourceClocksPerReferencePeriod);
        var consumed = 0;
        var viaCycles = 0;
        _fastCpuBus.BeginRepresentedPrivateCycles();
        try
        {
            while (consumed < maximum)
            {
                var isViaCycle = false;
                if (!_fastCpuBus.CanRepresentPreparedCycle)
                {
                    if (!GroupViaReadPeriods || !_fastCpuBus.CanRepresentPreparedViaRead
                        || (!GroupIndependentBusCycles && (!viaRead || consumed != 0))) break;
                    isViaCycle = true;
                }
                // The established 4 MHz wire lets the disk card consume its
                // own events independently. Deliver NMI before the same PH0
                // edges as the scalar source path, never at the prefix end.
                if (clockFloppyAtCpuEdges)
                    AdvanceFloppyToSource(source + consumed * (long)C.FastSourceClocksPerReferencePeriod
                        + C.FastSourceClocksPerReferencePeriod / 2);
                _cpu6502!.SetPhaseZeroInput(true);
                if (clockFloppyAtCpuEdges)
                    AdvanceFloppyToSource(source + (consumed + 1L) * C.FastSourceClocksPerReferencePeriod);
                TickReferenceClock(advanceVia: false, advanceVideo: false);
                _cpu6502.SetPhaseZeroInput(false);
                consumed++;
                if (isViaCycle) viaCycles++;
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
                    RepresentedViaReadPeriods += viaCycles;
                }
            }
            finally { _fastCpuBus.EndRepresentedPrivateCycles(); }
        }
        return consumed != 0;
    }
}
