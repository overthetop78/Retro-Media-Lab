using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

public sealed partial class CpuArm26
{
    public uint GetRegister(int register)
    {
        if ((uint)register >= C.RegisterCount) throw new ArgumentOutOfRangeException(nameof(register));
        return register == C.ProgramCounter ? ((_address + C.OperandPcOffset) & C.AddressMask) | _status : _registers[register];
    }

    private uint Operand(int register, bool includeStatus = true, int pcOffset = C.OperandPcOffset)
    {
        if (register == C.ProgramCounter)
            return ((InstructionAddress + (uint)pcOffset) & C.AddressMask) | (includeStatus ? _status : 0);
        if (register >= C.FiqFirstBankedRegister && PhysicalBank(register, _operandMode) != PhysicalBank(register, Mode))
            return _banks[(int)PhysicalBank(register, _operandMode), register - C.FiqFirstBankedRegister];
        return _registers[register];
    }

    private static Arm26Mode PhysicalBank(int register, Arm26Mode mode) =>
        register < C.FiqFirstBankedRegister || (register < C.StackRegister && mode != Arm26Mode.Fiq)
            ? Arm26Mode.User : mode;

    private uint UserRegister(int register)
    {
        if (register == C.ProgramCounter) return Operand(register, pcOffset: C.StoredPcOffset);
        return register < C.FiqFirstBankedRegister || PhysicalBank(register, Mode) == Arm26Mode.User
            ? _registers[register] : _banks[(int)Arm26Mode.User, register - C.FiqFirstBankedRegister];
    }

    private void SetUserRegister(int register, uint value)
    {
        if (register < C.FiqFirstBankedRegister || PhysicalBank(register, Mode) == Arm26Mode.User)
            _registers[register] = value;
        else _banks[(int)Arm26Mode.User, register - C.FiqFirstBankedRegister] = value;
    }

    private void SetStatus(uint value, bool deferBankReads = false)
    {
        var previous = Mode;
        var allowed = previous == Arm26Mode.User ? C.FlagsMask : C.StatusMask;
        var next = (_status & ~allowed) | (value & allowed);
        if ((next & C.ModeMask) != (_status & C.ModeMask))
        {
            for (var register = C.FiqFirstBankedRegister; register < C.ProgramCounter; register++)
                _banks[(int)PhysicalBank(register, previous), register - C.FiqFirstBankedRegister] = _registers[register];
            _status = next;
            for (var register = C.FiqFirstBankedRegister; register < C.ProgramCounter; register++)
                _registers[register] = _banks[(int)PhysicalBank(register, Mode), register - C.FiqFirstBankedRegister];
            if (deferBankReads && model == Arm26Model.Arm2) _deferredOperandMode = previous;
        }
        else _status = next;
    }

    private void SetMode(Arm26Mode mode)
    {
        // Une exception peut changer de mode depuis User, indépendamment des droits du programme.
        var previous = Mode;
        if (previous == mode) return;
        for (var register = C.FiqFirstBankedRegister; register < C.ProgramCounter; register++)
            _banks[(int)PhysicalBank(register, previous), register - C.FiqFirstBankedRegister] = _registers[register];
        _status = (_status & ~C.ModeMask) | (uint)mode;
        for (var register = C.FiqFirstBankedRegister; register < C.ProgramCounter; register++)
            _registers[register] = _banks[(int)PhysicalBank(register, mode), register - C.FiqFirstBankedRegister];
    }

    private void SetProgramCounter(uint value, bool restoreStatus = false)
    {
        if (restoreStatus) SetStatus(value);
        _address = value & C.AddressMask;
        _deferredOperandMode = null;
        RefillPipeline();
    }

    private void SetNz(uint result)
    {
        _status = (_status & ~(C.Negative | C.Zero)) | (result & C.Negative) | (result == 0 ? C.Zero : 0);
    }

    private void SetCarry(bool value) => _status = value ? _status | C.Carry : _status & ~C.Carry;
    private void SetOverflow(bool value) => _status = value ? _status | C.Overflow : _status & ~C.Overflow;
}
