using System.Numerics;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

/// <summary>Calculs des opérations documentées, sans accès bus ni avancement du temps.</summary>
/// <remarks>UM008011-0816, pages 145–172 et 188–193. Les sorties « flags » sont
/// uniquement la projection des six indicateurs documentés, pas le registre F complet.
/// Les bits 3/5 et les effets propres aux instructions doivent être établis séparément
/// avant le raccordement au CPU. Aucune valeur matérielle n'est prétendue pour ces bits.</remarks>
internal static class Z80Arithmetic
{
    internal static byte Add8(byte left, byte right, bool carry, out byte flags)
    {
        int sum = left + right + (carry ? 1 : 0);
        byte result = unchecked((byte)sum);
        flags = (byte)(SignZero8(result)
            | ((left ^ right ^ sum) & F.HalfCarry)
            | ((~(left ^ right) & (left ^ result) & 0x80) >> 5)
            | ((sum >> 8) & F.Carry));
        return result;
    }

    internal static byte Subtract8(byte left, byte right, bool carry, out byte flags)
    {
        int difference = left - right - (carry ? 1 : 0);
        byte result = unchecked((byte)difference);
        flags = (byte)(SignZero8(result) | F.Subtract
            | ((left ^ right ^ difference) & F.HalfCarry)
            | (((left ^ right) & (left ^ result) & 0x80) >> 5)
            | (difference < 0 ? F.Carry : 0));
        return result;
    }

    internal static byte Increment8(byte value, byte previousFlags, out byte flags)
    {
        byte result = unchecked((byte)(value + 1));
        flags = (byte)(SignZero8(result) | (previousFlags & F.Carry)
            | ((value ^ result) & F.HalfCarry)
            | (value == 0x7f ? F.ParityOverflow : 0));
        return result;
    }

    internal static byte Decrement8(byte value, byte previousFlags, out byte flags)
    {
        byte result = unchecked((byte)(value - 1));
        flags = (byte)(SignZero8(result) | F.Subtract | (previousFlags & F.Carry)
            | ((value ^ result) & F.HalfCarry)
            | (value == 0x80 ? F.ParityOverflow : 0));
        return result;
    }

    internal static byte And8(byte left, byte right, out byte flags)
    {
        byte result = (byte)(left & right);
        flags = (byte)(SignZeroParity(result) | F.HalfCarry);
        return result;
    }

    internal static byte Or8(byte left, byte right, out byte flags)
    {
        byte result = (byte)(left | right);
        flags = SignZeroParity(result);
        return result;
    }

    internal static byte Xor8(byte left, byte right, out byte flags)
    {
        byte result = (byte)(left ^ right);
        flags = SignZeroParity(result);
        return result;
    }

    /// <summary>ADD HL/IX/IY : S, Z et P/V sont conservés ; N est remis à zéro.</summary>
    internal static ushort Add16(ushort left, ushort right, byte previousFlags, out byte flags)
    {
        int sum = left + right;
        flags = (byte)((previousFlags & (F.Sign | F.Zero | F.ParityOverflow))
            | (((left ^ right ^ sum) >> 8) & F.HalfCarry)
            | ((sum >> 16) & F.Carry));
        return unchecked((ushort)sum);
    }

    internal static ushort AddCarry16(ushort left, ushort right, bool carry, out byte flags)
    {
        int sum = left + right + (carry ? 1 : 0);
        ushort result = unchecked((ushort)sum);
        flags = (byte)(SignZero16(result)
            | (((left ^ right ^ sum) >> 8) & F.HalfCarry)
            | ((~(left ^ right) & (left ^ result) & 0x8000) >> 13)
            | ((sum >> 16) & F.Carry));
        return result;
    }

    internal static ushort SubtractCarry16(ushort left, ushort right, bool carry, out byte flags)
    {
        int difference = left - right - (carry ? 1 : 0);
        ushort result = unchecked((ushort)difference);
        flags = (byte)(SignZero16(result) | F.Subtract
            | (((left ^ right ^ difference) >> 8) & F.HalfCarry)
            | (((left ^ right) & (left ^ result) & 0x8000) >> 13)
            | (difference < 0 ? F.Carry : 0));
        return result;
    }

    /// <summary>Rotations et décalages CB, dont SLL décrit dans Young ; les bits 3/5 sont ajoutés par le décodeur.</summary>
    internal static byte RotateShift(byte value, int kind, byte previousFlags, out byte flags)
    {
        int carry, result;
        switch (kind)
        {
            case 0: carry = value >> 7; result = (value << 1) | carry; break;
            case 1: carry = value & 1; result = (value >> 1) | (carry << 7); break;
            case 2: carry = value >> 7; result = (value << 1) | (previousFlags & F.Carry); break;
            case 3: carry = value & 1; result = (value >> 1) | ((previousFlags & F.Carry) << 7); break;
            case 4: carry = value >> 7; result = value << 1; break;
            case 5: carry = value & 1; result = (value >> 1) | (value & 0x80); break;
            case 6: carry = value >> 7; result = (value << 1) | 1; break;
            case 7: carry = value & 1; result = value >> 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        byte output = unchecked((byte)result);
        flags = (byte)(SignZeroParity(output) | carry);
        return output;
    }

    private static byte SignZero8(byte result) => (byte)((result & F.Sign) | (result == 0 ? F.Zero : 0));
    private static byte SignZero16(ushort result) => (byte)(((result >> 8) & F.Sign) | (result == 0 ? F.Zero : 0));
    private static byte SignZeroParity(byte result) => (byte)(SignZero8(result)
        | ((BitOperations.PopCount((uint)result) & 1) == 0 ? F.ParityOverflow : 0));
}
