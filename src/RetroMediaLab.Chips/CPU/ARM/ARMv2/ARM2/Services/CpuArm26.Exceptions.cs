using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

public sealed partial class CpuArm26
{
    private void EnterException(uint vector, Arm26Mode mode, uint returnAddress, bool maskFiq = false)
    {
        var saved = (returnAddress & C.AddressMask) | _status;
        SetMode(mode);
        _registers[C.LinkRegister] = saved;
        _status |= C.IrqDisable;
        if (maskFiq) _status |= C.FiqDisable;
        _address = vector;
        _deferredOperandMode = null;
        RefillPipeline();
    }

    private void UndefinedInstruction()
    {
        // Priorité matérielle des premiers ARM : un abandon déjà présent dans le pipeline
        // peut masquer le piège d’une instruction indéfinie en fin de page.
        var vector = _prefetchAborts[0] || _prefetchAborts[1] ? C.PrefetchAbortVector : C.UndefinedVector;
        EnterException(vector, Arm26Mode.Supervisor, InstructionAddress + C.ExceptionReturnOffset);
    }

    private void DataAbort() => EnterException(C.DataAbortVector, Arm26Mode.Supervisor, InstructionAddress + C.AbortReturnOffset);
    private void AddressException() => EnterException(C.AddressExceptionVector, Arm26Mode.Supervisor, InstructionAddress + C.AbortReturnOffset);
    private static bool ValidDataAddress(uint address) => (address & ~C.AddressByteMask) == 0;
}
