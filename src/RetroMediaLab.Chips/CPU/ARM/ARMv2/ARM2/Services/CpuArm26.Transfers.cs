using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using System.Numerics;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

public sealed partial class CpuArm26
{
    private void SingleTransfer(uint instruction)
    {
        var baseRegister = (int)((instruction >> C.BaseRegisterShift) & C.RegisterMask);
        var destination = (int)((instruction >> C.DestinationRegisterShift) & C.RegisterMask);
        var preIndex = (instruction & C.PreIndex) != 0;
        var writeBack = !preIndex || (instruction & C.WriteBack) != 0;
        var user = Mode == Arm26Mode.User || (!preIndex && (instruction & C.WriteBack) != 0);
        var byteTransfer = (instruction & C.ByteTransfer) != 0;
        var load = (instruction & C.Load) != 0;
        var offset = instruction & C.TransferOffsetMask;
        if ((instruction & C.ImmediateOperand) != 0)
        {
            var carry = (_status & C.Carry) != 0;
            offset = Shift(Operand((int)(instruction & C.RegisterMask)),
                (ArmShiftType)((instruction >> C.ShiftTypeShift) & C.ShiftTypeMask),
                (int)((instruction >> C.ImmediateShiftShift) & C.ImmediateShiftMask), true, ref carry);
        }
        var original = Operand(baseRegister, includeStatus: false);
        var updated = (instruction & C.AddOffset) != 0 ? unchecked(original + offset) : unchecked(original - offset);
        var address = preIndex ? updated : original;
        if (!ValidDataAddress(address)) { AddressException(); return; }
        _nextFetchSequential = false;
        uint value;
        bool success;
        if (load)
        {
            if (byteTransfer)
            {
                success = ReadByte(address, user, out var octet);
                value = octet;
            }
            else
            {
                success = ReadWord(address, false, user, false, out value);
                value = BitOperations.RotateRight(value, (int)(address & C.WordAlignmentMask) * C.ByteBits);
            }
        }
        else
        {
            value = Operand(destination, pcOffset: C.StoredPcOffset);
            success = byteTransfer ? WriteByte(address, (byte)value, user)
                : WriteWord(address, value, false, user);
        }
        // Les ARM2/ARM3 abandonnent le transfert simple sans modifier les registres.
        if (!success) { DataAbort(); return; }
        if (writeBack && baseRegister != C.ProgramCounter) _registers[baseRegister] = updated;
        if (!load) return;
        MergeInternalCycle();
        if (destination == C.ProgramCounter) SetProgramCounter(value);
        else _registers[destination] = value;
    }

    private void BlockTransfer(uint instruction)
    {
        var baseRegister = (int)((instruction >> C.BaseRegisterShift) & C.RegisterMask);
        var list = instruction & C.RegisterListMask;
        var count = list == 0 ? C.RegisterCount : BitOperations.PopCount(list);
        if (list == 0) list = C.EmptyRegisterList;
        var span = (uint)(count * C.WordBytes);
        var increment = (instruction & C.AddOffset) != 0;
        var preIndex = (instruction & C.PreIndex) != 0;
        var writeBack = (instruction & C.WriteBack) != 0;
        var load = (instruction & C.Load) != 0;
        var restoreStatus = (instruction & C.BlockStatusTransfer) != 0;
        var hasPc = (list & C.EmptyRegisterList) != 0;
        var userRegisters = restoreStatus && (!load || !hasPc);
        var original = Operand(baseRegister, includeStatus: false);
        var updated = increment ? unchecked(original + span) : unchecked(original - span);
        var address = increment ? unchecked(original + (preIndex ? C.WordBytes : 0u))
            : unchecked(original - span + (preIndex ? 0u : C.WordBytes));
        if (!ValidDataAddress(address))
        {
            if (writeBack && baseRegister != C.ProgramCounter) _registers[baseRegister] = updated;
            AddressException();
            return;
        }
        var user = Mode == Arm26Mode.User;
        var aborted = false;
        var sequential = false;
        var loadedBase = false;
        uint loadedPc = 0;
        _nextFetchSequential = false;
        for (var register = 0; register < C.RegisterCount; register++)
        {
            if ((list & (1u << register)) == 0) continue;
            var busAddress = address & C.AddressByteMask;
            if (load)
            {
                if (!ReadWord(busAddress, sequential, user, false, out var value)) aborted = true;
                if (!aborted)
                {
                    if (register == C.ProgramCounter) loadedPc = value;
                    else if (userRegisters) SetUserRegister(register, value);
                    else _registers[register] = value;
                    if (register == baseRegister && (!userRegisters || PhysicalBank(register, Mode) == Arm26Mode.User))
                        loadedBase = true;
                }
            }
            else
            {
                var value = userRegisters ? UserRegister(register) : Operand(register, pcOffset: C.StoredPcOffset);
                if (register == baseRegister && writeBack && sequential &&
                    (!userRegisters || PhysicalBank(register, Mode) == Arm26Mode.User)) value = updated;
                if (!WriteWord(busAddress, value, sequential, user)) aborted = true;
            }
            sequential = true;
            address = unchecked(address + C.WordBytes);
        }
        if (baseRegister != C.ProgramCounter)
        {
            if (aborted) _registers[baseRegister] = writeBack ? updated : original;
            else if (writeBack && !loadedBase) _registers[baseRegister] = updated;
        }
        if (load) MergeInternalCycle();
        if (aborted) DataAbort();
        else if (load && hasPc) SetProgramCounter(loadedPc, restoreStatus);
    }

    private void Swap(uint instruction)
    {
        if (model == Arm26Model.Arm2) { UndefinedInstruction(); return; }
        var address = Operand((int)((instruction >> C.BaseRegisterShift) & C.RegisterMask), includeStatus: false);
        if (!ValidDataAddress(address)) { AddressException(); return; }
        var source = Operand((int)(instruction & C.RegisterMask), pcOffset: C.StoredPcOffset);
        var destination = (int)((instruction >> C.DestinationRegisterShift) & C.RegisterMask);
        var user = Mode == Arm26Mode.User;
        var success = false;
        uint value = 0;
        _nextFetchSequential = false;
        CompleteCacheFill();
        _busLocked = true;
        bus.SetBusLock(true);
        try
        {
            if ((instruction & C.ByteTransfer) != 0)
            {
                var read = ReadByte(address, user, out var octet);
                value = octet;
                success = read && WriteByte(address, (byte)source, user);
            }
            else
            {
                var read = ReadWord(address, false, user, false, out value);
                value = BitOperations.RotateRight(value, (int)(address & C.WordAlignmentMask) * C.ByteBits);
                success = read && WriteWord(address, source, false, user);
            }
        }
        finally
        {
            _busLocked = false;
            bus.SetBusLock(false);
        }
        if (!success) { DataAbort(); return; }
        InternalCycle();
        if (destination == C.ProgramCounter) SetProgramCounter(value);
        else _registers[destination] = value;
    }
}
