using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using System.Numerics;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;

public sealed partial class CpuArm26
{
    private uint DataOperand(uint instruction, out bool carry)
    {
        carry = (_status & C.Carry) != 0;
        if ((instruction & C.ImmediateOperand) != 0)
        {
            var rotation = (int)((instruction >> C.RotateShift) & C.RotateMask) * C.RotateUnit;
            var immediate = BitOperations.RotateRight(instruction & C.ByteMask, rotation);
            if (rotation != 0) carry = (immediate & C.Negative) != 0;
            return immediate;
        }
        var registerShift = (instruction & C.RegisterShift) != 0;
        var amount = registerShift
            ? (int)(Operand((int)((instruction >> C.ShiftRegisterShift) & C.RegisterMask)) & C.ByteMask)
            : (int)((instruction >> C.ImmediateShiftShift) & C.ImmediateShiftMask);
        var value = Operand((int)(instruction & C.RegisterMask),
            pcOffset: registerShift ? C.StoredPcOffset : C.OperandPcOffset);
        if (registerShift) MergeInternalCycle();
        return Shift(value, (ArmShiftType)((instruction >> C.ShiftTypeShift) & C.ShiftTypeMask),
            amount, !registerShift, ref carry);
    }

    private static uint Shift(uint value, ArmShiftType type, int amount, bool immediate, ref bool carry)
    {
        if (amount == 0)
        {
            if (!immediate || type == ArmShiftType.LogicalLeft) return value;
            if (type == ArmShiftType.RotateRight)
            {
                var result = (value >> 1) | (carry ? C.Negative : 0);
                carry = (value & 1) != 0;
                return result;
            }
            amount = C.WordBits;
        }
        switch (type)
        {
            case ArmShiftType.LogicalLeft:
                carry = amount <= C.WordBits && ((value >> (C.WordBits - amount)) & 1) != 0;
                return amount >= C.WordBits ? 0 : value << amount;
            case ArmShiftType.LogicalRight:
                carry = amount <= C.WordBits && ((value >> (amount - 1)) & 1) != 0;
                return amount >= C.WordBits ? 0 : value >> amount;
            case ArmShiftType.ArithmeticRight:
                if (amount >= C.WordBits)
                {
                    carry = (value & C.Negative) != 0;
                    return carry ? uint.MaxValue : 0;
                }
                carry = ((value >> (amount - 1)) & 1) != 0;
                return (uint)((int)value >> amount);
            default:
                var rotated = BitOperations.RotateRight(value, amount);
                carry = (rotated & C.Negative) != 0;
                return rotated;
        }
    }

    private static uint AddWithCarry(uint left, uint right, bool carryIn, out bool carry, out bool overflow)
    {
        var sum = (ulong)left + right + (carryIn ? 1UL : 0UL);
        var result = (uint)sum;
        carry = sum > uint.MaxValue;
        overflow = (~(left ^ right) & (left ^ result) & C.Negative) != 0;
        return result;
    }
}
