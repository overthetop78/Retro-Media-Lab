using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;
using R = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronVideo
{
    private bool TickAlignedClock(int dot)
    {
        TickCpuClock(dot);
        BeginRamClock(dot);
        _ram.LatchRow();
        RenderPoint(dot);

        if (_cpuClockDirty) TickCpuClock(dot + R.EvenColumnPhase);
        _ram.SelectEvenColumn();
        RenderPoint(dot + R.EvenColumnPhase);

        if (_cpuClockDirty) TickCpuClock(dot + R.EvenIdlePhase);
        _ram.AdvanceIdle();
        RenderPoint(dot + R.EvenIdlePhase);

        if (_cpuClockDirty) TickCpuClock(dot + R.EvenDataPhase);
        _ram.LoadEvenData();
        RenderPoint(dot + R.EvenDataPhase);

        // PHI rises before the second column strobe, even without a RAM request.
        TickCpuClock(dot + R.OddColumnPhase);
        _ram.SelectOddColumn();
        RenderPoint(dot + R.OddColumnPhase);

        if (_cpuClockDirty) TickCpuClock(dot + R.OddIdlePhase);
        _ram.AdvanceIdle();
        RenderPoint(dot + R.OddIdlePhase);

        if (_cpuClockDirty) TickCpuClock(dot + R.OddDataPhase);
        _ram.LoadOddData();
        RenderPoint(dot + R.OddDataPhase);

        if (_cpuClockDirty) TickCpuClock(dot + R.RecoveryPhase);
        if (_ram.CompleteTransfer()) CompleteRamClock();
        RenderPoint(dot + R.RecoveryPhase);

        _timing.TickCpuClock(_modeRasters == C.SpacedRasters);
        return FinishClock(dot);
    }
}
