using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Enums;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class BbcMicroMachine
{
    private BbcInterruptSources _interruptSources;

    private void ConnectInterrupts()
    {
        _via.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.SystemVia, asserted);
        if (_userVia is not null)
            _userVia.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.UserVia, asserted);
        _acia.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.Serial, asserted);
        _winchester.IrqChanged = asserted => SetInterrupt(BbcInterruptSources.Winchester, asserted);

        SetInterrupt(BbcInterruptSources.SystemVia, _via.Irq);
        SetInterrupt(BbcInterruptSources.UserVia, _userVia?.Irq == true);
        SetInterrupt(BbcInterruptSources.Serial, _acia.Irq);
        SetInterrupt(BbcInterruptSources.Winchester, _winchester.Irq);
    }

    private void SetInterrupt(BbcInterruptSources source, bool asserted)
    {
        if (asserted) _interruptSources |= source;
        else _interruptSources &= ~source;
    }
}
