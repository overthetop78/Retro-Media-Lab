using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private void PreparePull()
    {
        Read(_pc);
        Read((ushort)(C.StackBase | _s));
    }

    private ushort InterruptVector(ushort requested, bool hardware = false)
    {
        if ((!cmos || hardware) && _nmiPending)
        {
            _nmiPending = _nmiAccepted = false;
            return C.NmiVector;
        }
        return requested;
    }
}
