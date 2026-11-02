using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

public sealed partial class CpuArm26
{
    private void Execute(uint instruction)
    {
        switch ((instruction >> C.InstructionClassShift) & C.InstructionClassMask)
        {
            case C.InstructionClassData:
                if ((instruction & C.SwapMask) == C.SwapPattern) Swap(instruction);
                else if ((instruction & C.MultiplyMask) == C.MultiplyPattern) Multiply(instruction);
                else if ((instruction & C.InvalidRegisterShiftMask) == C.InvalidRegisterShiftMask) UndefinedInstruction();
                else DataProcessing(instruction);
                break;
            case C.InstructionClassImmediateData:
                DataProcessing(instruction);
                break;
            case C.InstructionClassImmediateTransfer:
            case C.InstructionClassRegisterTransfer:
                if ((instruction & C.UndefinedTransferMask) == C.UndefinedTransferMask) UndefinedInstruction();
                else SingleTransfer(instruction);
                break;
            case C.InstructionClassBlockTransfer:
                BlockTransfer(instruction);
                break;
            case C.InstructionClassBranch:
                if ((instruction & C.BranchLink) != 0)
                    _registers[C.LinkRegister] = ((InstructionAddress + C.BranchReturnOffset) & C.AddressMask) | _status;
                var displacement = (int)(instruction << C.BranchSignShift) >> C.BranchScaleShift;
                SetProgramCounter(unchecked(InstructionAddress + C.OperandPcOffset + (uint)displacement));
                break;
            default:
                if ((instruction & C.SoftwareInterruptMask) == C.SoftwareInterruptPattern)
                    EnterException(C.SoftwareInterruptVector, Arm26Mode.Supervisor, InstructionAddress + C.ExceptionReturnOffset);
                else CoprocessorInstruction(instruction);
                break;
        }
    }

    private void DataProcessing(uint instruction)
    {
        var operation = (ArmDataOperation)((instruction >> C.OperationShift) & C.OperationMask);
        var destination = (int)((instruction >> C.DestinationRegisterShift) & C.RegisterMask);
        var left = Operand((int)((instruction >> C.BaseRegisterShift) & C.RegisterMask), includeStatus: false);
        var right = DataOperand(instruction, out var carry);
        var overflow = (_status & C.Overflow) != 0;
        var carryIn = (_status & C.Carry) != 0;
        var arithmetic = false;
        uint result;
        switch (operation)
        {
            case ArmDataOperation.And:
            case ArmDataOperation.Test: result = left & right; break;
            case ArmDataOperation.ExclusiveOr:
            case ArmDataOperation.TestEqual: result = left ^ right; break;
            case ArmDataOperation.Subtract:
            case ArmDataOperation.Compare:
                result = AddWithCarry(left, ~right, true, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.ReverseSubtract:
                result = AddWithCarry(right, ~left, true, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.Add:
            case ArmDataOperation.CompareNegative:
                result = AddWithCarry(left, right, false, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.AddCarry:
                result = AddWithCarry(left, right, carryIn, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.SubtractCarry:
                result = AddWithCarry(left, ~right, carryIn, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.ReverseSubtractCarry:
                result = AddWithCarry(right, ~left, carryIn, out carry, out overflow); arithmetic = true; break;
            case ArmDataOperation.Or: result = left | right; break;
            case ArmDataOperation.Move: result = right; break;
            case ArmDataOperation.BitClear: result = left & ~right; break;
            default: result = ~right; break;
        }
        var test = operation is >= ArmDataOperation.Test and <= ArmDataOperation.CompareNegative;
        if (test && destination == C.ProgramCounter)
        {
            SetStatus(result, deferBankReads: true);
            return;
        }
        if (!test && destination == C.ProgramCounter)
        {
            SetProgramCounter(result, (instruction & C.SetFlags) != 0);
            return;
        }
        if (test || (instruction & C.SetFlags) != 0)
        {
            SetNz(result);
            SetCarry(carry);
            if (arithmetic) SetOverflow(overflow);
        }
        if (!test) _registers[destination] = result;
    }

    private void Multiply(uint instruction)
    {
        var destination = (int)((instruction >> C.BaseRegisterShift) & C.RegisterMask);
        var multiplicandRegister = (int)(instruction & C.RegisterMask);
        var multiplier = Operand((int)((instruction >> C.ShiftRegisterShift) & C.RegisterMask), includeStatus: false);
        var value = (instruction & C.MultiplyAccumulate) != 0
            ? Operand((int)((instruction >> C.DestinationRegisterShift) & C.RegisterMask)) : 0;
        var carry = false;
        var iterations = 0;
        do
        {
            // Rd is the Booth accumulator. Reading Rm again is significant
            // when software uses the documented invalid Rd == Rm combination.
            var multiplicand = multiplicandRegister == destination && destination != C.ProgramCounter
                ? value : Operand(multiplicandRegister, pcOffset: C.StoredPcOffset);
            var shifted = multiplicand << (iterations * C.MultiplyBitsPerCycle);
            switch ((ArmMultiplyDigit)(multiplier & C.MultiplyDigitMask))
            {
                case ArmMultiplyDigit.Zero:
                    if (carry) value = unchecked(value + shifted);
                    carry = false;
                    break;
                case ArmMultiplyDigit.One:
                    value = unchecked(value + (carry ? shifted << C.MultiplyDoubleShift : shifted));
                    carry = false;
                    break;
                case ArmMultiplyDigit.Two:
                    value = unchecked(value - (carry ? shifted : shifted << C.MultiplyDoubleShift));
                    carry = true;
                    break;
                case ArmMultiplyDigit.Three:
                    if (!carry) value = unchecked(value - shifted);
                    carry = true;
                    break;
            }
            iterations++;
            multiplier >>= C.MultiplyBitsPerCycle;
        } while ((multiplier != 0 || carry) && iterations < C.MaximumMultiplyCycles);
        if (model == Arm26Model.Arm3)
        {
            for (var cycle = 0; cycle < iterations; cycle++) InternalCycle();
        }
        else
        {
            Cycles += iterations;
            bus.MultiplyCycles((InstructionAddress + C.StoredPcOffset) & C.AddressMask, iterations);
            _nextFetchSequential = false;
        }
        if (destination != C.ProgramCounter) _registers[destination] = value;
        if ((instruction & C.SetFlags) != 0) SetNz(value);
    }
}
