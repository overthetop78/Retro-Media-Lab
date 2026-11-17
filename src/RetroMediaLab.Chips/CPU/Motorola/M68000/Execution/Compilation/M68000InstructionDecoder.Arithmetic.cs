namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal static partial class M68000InstructionDecoder
{
    // Port of the OR_DIV_SBCD, SUB_SUBX, CMP_XOR, AND_MUL_ABCD_EXG,
    // ADD_ADDX and SHIFT_ROTATE cases of BlastEm's 68000 decoder.
    private static M68000DecodedInstruction DecodeArithmetic(uint start, ushort opcode, Func<uint, ushort> fetch)
    {
        uint address = unchecked(start + 2);
        int family = opcode >> 12, sizeBits = opcode >> 6 & 3;
        byte highReg = (byte)(opcode >> 9 & 7), lowReg = (byte)(opcode & 7);
        bool reverse = (opcode & 0x100) != 0;
        var operation = M68000NativeOperation.Invalid;
        var variant = M68000InstructionVariant.Normal;
        M68000OperandSize? size = null;
        M68000DecodedOperand source = new(M68000OperandMode.Unused);
        M68000DecodedOperand destination = new(M68000OperandMode.Unused);

        bool Operand(M68000OperandSize operandSize, out M68000DecodedOperand operand)
        {
            uint next = M68000OperandDecoder.Decode((byte)(opcode & 63), operandSize, address, fetch, out operand);
            if (next == uint.MaxValue) return false;
            address = next;
            return true;
        }
        static bool MemoryDestination(M68000DecodedOperand operand) =>
            operand.Mode >= M68000OperandMode.Indirect && operand.Mode <= M68000OperandMode.AbsoluteLong;
        M68000DecodedInstruction Result(bool valid) => new(
            valid ? M68000DecodeStatus.Decoded : M68000DecodeStatus.Invalid,
            start, valid ? address : unchecked(start + 2), opcode,
            valid ? operation : M68000NativeOperation.Invalid, variant, size, 0,
            valid ? source : source with { Immediate = opcode }, destination);

        switch (family)
        {
            case 8:
                int opmode = opcode >> 6 & 7;
                if (sizeBits == 3 || (reverse && (opcode & 0x30) == 0))
                {
                    if (opmode is 3 or 7)
                    {
                        operation = opmode == 3 ? M68000NativeOperation.DivideUnsigned : M68000NativeOperation.DivideSigned;
                        size = M68000OperandSize.Word;
                        destination = new(M68000OperandMode.DataRegister, highReg);
                        if (!Operand(size.Value, out source) || source.Mode == M68000OperandMode.AddressRegister)
                            return Result(false);
                    }
                    else if (opmode == 4)
                    {
                        operation = M68000NativeOperation.SubtractDecimal;
                        size = M68000OperandSize.Byte;
                        var mode = (opcode & 8) != 0 ? M68000OperandMode.PreDecrement : M68000OperandMode.DataRegister;
                        source = new(mode, lowReg); destination = new(mode, highReg);
                    }
                    else return Result(false); // PACK/UNPK are not 68000 instructions.
                }
                else
                {
                    operation = M68000NativeOperation.Or;
                    size = (M68000OperandSize)sizeBits;
                    if (reverse)
                    {
                        source = new(M68000OperandMode.DataRegister, highReg);
                        if (!Operand(size.Value, out destination) || !MemoryDestination(destination)) return Result(false);
                    }
                    else
                    {
                        destination = new(M68000OperandMode.DataRegister, highReg);
                        if (!Operand(size.Value, out source) || source.Mode == M68000OperandMode.AddressRegister)
                            return Result(false);
                    }
                }
                break;
            case 9:
            case 13:
                operation = family == 9 ? M68000NativeOperation.Subtract : M68000NativeOperation.Add;
                if (reverse)
                {
                    if ((opcode & 0x30) != 0 || sizeBits == 3)
                    {
                        if (sizeBits == 3)
                        {
                            size = M68000OperandSize.Long;
                            destination = new(M68000OperandMode.AddressRegister, highReg);
                            if (!Operand(size.Value, out source)) return Result(false);
                        }
                        else
                        {
                            size = (M68000OperandSize)sizeBits;
                            source = new(M68000OperandMode.DataRegister, highReg);
                            if (!Operand(size.Value, out destination) || !MemoryDestination(destination)) return Result(false);
                        }
                    }
                    else
                    {
                        operation = family == 9 ? M68000NativeOperation.SubtractExtend : M68000NativeOperation.AddExtend;
                        size = (M68000OperandSize)sizeBits;
                        var mode = (opcode & 8) != 0 ? M68000OperandMode.PreDecrement : M68000OperandMode.DataRegister;
                        source = new(mode, lowReg); destination = new(mode, highReg);
                    }
                }
                else
                {
                    size = sizeBits == 3 ? M68000OperandSize.Word : (M68000OperandSize)sizeBits;
                    destination = new(sizeBits == 3 ? M68000OperandMode.AddressRegister : M68000OperandMode.DataRegister, highReg);
                    if (!Operand(size.Value, out source)
                        || (source.Mode == M68000OperandMode.AddressRegister && size == M68000OperandSize.Byte))
                        return Result(false);
                }
                break;
            case 11:
                operation = M68000NativeOperation.Compare;
                if (reverse && sizeBits != 3)
                {
                    size = (M68000OperandSize)sizeBits;
                    if (!Operand(size.Value, out destination)) return Result(false);
                    if (destination.Mode == M68000OperandMode.AddressRegister)
                    {
                        source = new(M68000OperandMode.PostIncrement, destination.Register);
                        destination = new(M68000OperandMode.PostIncrement, highReg);
                    }
                    else
                    {
                        if (destination.Mode > M68000OperandMode.AbsoluteLong) return Result(false);
                        operation = M68000NativeOperation.ExclusiveOr;
                        source = new(M68000OperandMode.DataRegister, highReg);
                    }
                }
                else
                {
                    size = sizeBits == 3 ? reverse ? M68000OperandSize.Long : M68000OperandSize.Word
                        : (M68000OperandSize)sizeBits;
                    destination = new(sizeBits == 3 ? M68000OperandMode.AddressRegister : M68000OperandMode.DataRegister, highReg);
                    if (!Operand(size.Value, out source)
                        || (source.Mode == M68000OperandMode.AddressRegister && size == M68000OperandSize.Byte))
                        return Result(false);
                }
                break;
            case 12:
                if (sizeBits == 3)
                {
                    operation = reverse ? M68000NativeOperation.MultiplySigned : M68000NativeOperation.MultiplyUnsigned;
                    size = M68000OperandSize.Word;
                    destination = new(M68000OperandMode.DataRegister, highReg);
                    if (!Operand(size.Value, out source) || source.Mode == M68000OperandMode.AddressRegister) return Result(false);
                }
                else if (reverse && (opcode & 0xf0) == 0)
                {
                    operation = M68000NativeOperation.AddDecimal;
                    size = M68000OperandSize.Byte;
                    var mode = (opcode & 8) != 0 ? M68000OperandMode.PreDecrement : M68000OperandMode.DataRegister;
                    source = new(mode, lowReg); destination = new(mode, highReg);
                }
                else if (reverse && (opcode & 0x30) == 0)
                {
                    operation = M68000NativeOperation.Exchange;
                    size = M68000OperandSize.Long;
                    if ((opcode & 8) != 0)
                    {
                        source = new((opcode & 0x80) != 0 ? M68000OperandMode.DataRegister : M68000OperandMode.AddressRegister, highReg);
                        destination = new(M68000OperandMode.AddressRegister, lowReg);
                    }
                    else if ((opcode & 0x40) != 0)
                    {
                        source = new(M68000OperandMode.DataRegister, highReg);
                        destination = new(M68000OperandMode.DataRegister, lowReg);
                    }
                    else return Result(false);
                }
                else
                {
                    operation = M68000NativeOperation.And;
                    size = (M68000OperandSize)sizeBits;
                    if (reverse)
                    {
                        source = new(M68000OperandMode.DataRegister, highReg);
                        if (!Operand(size.Value, out destination) || !MemoryDestination(destination)) return Result(false);
                    }
                    else
                    {
                        destination = new(M68000OperandMode.DataRegister, highReg);
                        if (!Operand(size.Value, out source) || source.Mode == M68000OperandMode.AddressRegister) return Result(false);
                    }
                }
                break;
            case 14:
                int shift;
                if ((opcode & 0x8c0) == 0xc0)
                {
                    shift = opcode >> 8 & 7;
                    size = M68000OperandSize.Word;
                    if (!Operand(size.Value, out destination) || !MemoryDestination(destination)) return Result(false);
                }
                else if (sizeBits != 3)
                {
                    shift = (opcode >> 2 & 6) | (opcode >> 8 & 1);
                    size = (M68000OperandSize)sizeBits;
                    if ((opcode & 0x20) != 0) source = new(M68000OperandMode.DataRegister, highReg);
                    else
                    {
                        variant = M68000InstructionVariant.Quick;
                        source = new(M68000OperandMode.Immediate, Immediate: highReg == 0 ? 8u : highReg);
                    }
                    destination = new(M68000OperandMode.DataRegister, lowReg);
                }
                else return Result(false); // No 68020 bitfield instructions.
                operation = shift switch
                {
                    0 => M68000NativeOperation.ArithmeticShiftRight,
                    1 => M68000NativeOperation.ArithmeticShiftLeft,
                    2 => M68000NativeOperation.LogicalShiftRight,
                    3 => M68000NativeOperation.LogicalShiftLeft,
                    4 => M68000NativeOperation.RotateExtendRight,
                    5 => M68000NativeOperation.RotateExtendLeft,
                    6 => M68000NativeOperation.RotateRight,
                    _ => M68000NativeOperation.RotateLeft
                };
                break;
            default:
                throw new InvalidOperationException("Not an arithmetic opcode family.");
        }
        return Result(true);
    }
}
