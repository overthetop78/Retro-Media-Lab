namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal enum M68000OperandMode : byte
{
    DataRegister, AddressRegister, Indirect, PostIncrement, PreDecrement,
    Displacement, EncodedIndex, EncodedExtended, IndexedDisplacement,
    AbsoluteShort, AbsoluteLong, PcDisplacement, PcIndexedDisplacement,
    Immediate, ImmediateWord, Unused
}

internal enum M68000OperandSize : byte { Byte, Word, Long }

internal readonly record struct M68000DecodedOperand(M68000OperandMode Mode,
    byte Register = 0, byte IndexDescriptor = 0, int Displacement = 0, uint Immediate = 0);

/// <summary>68000 effective operand decoding from BlastEm 68kinst.c/m68k_decode_op_ex,
/// tree ec73825ece2a134beb7825a5fd6fd21a398ff12e. Instruction-specific legality is separate.</summary>
internal static class M68000OperandDecoder
{
    public static uint Decode(byte effectiveAddress, M68000OperandSize size, uint extensionAddress,
        Func<uint, ushort> fetch, out M68000DecodedOperand operand)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        if (effectiveAddress > 63) throw new ArgumentOutOfRangeException(nameof(effectiveAddress));
        if (size > M68000OperandSize.Long) throw new ArgumentOutOfRangeException(nameof(size));
        byte mode = (byte)(effectiveAddress >> 3), reg = (byte)(effectiveAddress & 7);
        uint address = extensionAddress;
        ushort Word() { ushort value = fetch(address); address = unchecked(address + 2); return value; }
        operand = new((M68000OperandMode)mode, reg);
        switch (mode)
        {
            case <= 4:
                return address;
            case 5:
                operand = new(M68000OperandMode.Displacement, reg, Displacement: unchecked((short)Word()));
                return address;
            case 6:
            {
                ushort extension = Word();
                operand = new(M68000OperandMode.IndexedDisplacement, reg,
                    (byte)(extension >> 11), unchecked((sbyte)extension));
                return address;
            }
            case 7:
                switch (reg)
                {
                    case 0:
                        operand = new(M68000OperandMode.AbsoluteShort, Immediate: unchecked((uint)(short)Word()));
                        return address;
                    case 1:
                        operand = new(M68000OperandMode.AbsoluteLong, Immediate: (uint)Word() << 16 | Word());
                        return address;
                    case 2:
                        operand = new(M68000OperandMode.PcDisplacement, Displacement: unchecked((short)Word()));
                        return address;
                    case 3:
                    {
                        ushort extension = Word();
                        operand = new(M68000OperandMode.PcIndexedDisplacement,
                            IndexDescriptor: (byte)(extension >> 11), Displacement: unchecked((sbyte)extension));
                        return address;
                    }
                    case 4:
                    {
                        uint value = Word();
                        if (size == M68000OperandSize.Byte) value &= 255;
                        else if (size == M68000OperandSize.Long) value = value << 16 | Word();
                        operand = new(M68000OperandMode.Immediate, Immediate: value);
                        return address;
                    }
                    default:
                        return uint.MaxValue;
                }
            default:
                return uint.MaxValue;
        }
    }
}
