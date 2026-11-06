using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Enums;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class BbcMicroMachine
{
    private BbcInterruptSources _interruptSources;
    private BbcNmiSources _nmiSources;

    private void ConnectInterrupts()
    {
        _via.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.SystemVia, asserted);
        if (_userVia is not null)
            _userVia.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.UserVia, asserted);
        _acia.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.Serial, asserted);
        _winchester.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.Winchester, asserted);
        _disks.NmiChanged = asserted => SetNmi(BbcNmiSources.Disk, asserted);
        _econet.NmiChanged = asserted => SetNmi(BbcNmiSources.Network, asserted);

        SetInterrupt(BbcInterruptSources.SystemVia, _via.Irq);
        SetInterrupt(BbcInterruptSources.UserVia, _userVia?.Irq == true);
        SetInterrupt(BbcInterruptSources.Serial, _acia.Irq);
        SetInterrupt(BbcInterruptSources.Winchester, _winchester.Irq);
        SetNmi(BbcNmiSources.Disk, _disks.Nmi);
        SetNmi(BbcNmiSources.Network, _econet.Nmi);
    }

    private void SetInterrupt(BbcInterruptSources source, bool asserted)
    {
        if (asserted) _interruptSources |= source;
        else _interruptSources &= ~source;
    }

    private void SetNmi(BbcNmiSources source, bool asserted)
    {
        if (asserted) _nmiSources |= source;
        else _nmiSources &= ~source;
    }
}
