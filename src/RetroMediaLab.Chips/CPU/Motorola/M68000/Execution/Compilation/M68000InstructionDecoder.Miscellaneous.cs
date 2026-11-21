namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal static partial class M68000InstructionDecoder
{
    // BlastEm 68kinst.c MISC case, without M68010/M68020 branches.
    private static M68000DecodedInstruction DecodeMiscellaneous(uint start, ushort opcode, Func<uint, ushort> fetch)
    {
        uint address = unchecked(start + 2);
        var operation = M68000NativeOperation.Invalid;
        M68000OperandSize? size = null;
        M68000DecodedOperand source = new(M68000OperandMode.Unused);
        M68000DecodedOperand destination = new(M68000OperandMode.Unused);
        byte lowReg = (byte)(opcode & 7), highReg = (byte)(opcode >> 9 & 7);
        int mode = opcode >> 3 & 7, sizeBits = opcode >> 6 & 3;
        ushort Word()
        {
            ushort word = fetch(address);
            address = unchecked(address + 2);
            return word;
        }
        bool Operand(M68000OperandSize operandSize, out M68000DecodedOperand operand)
        {
            uint next = M68000OperandDecoder.Decode((byte)(opcode & 63), operandSize, address, fetch, out operand);
            if (next == uint.MaxValue) return false;
            address = next;
            return true;
        }
        static bool Alterable(M68000DecodedOperand operand) => operand.Mode != M68000OperandMode.AddressRegister
            && operand.Mode <= M68000OperandMode.AbsoluteLong;
        static bool Control(M68000DecodedOperand operand) => operand.Mode is M68000OperandMode.Indirect
            or M68000OperandMode.Displacement or M68000OperandMode.IndexedDisplacement
            or M68000OperandMode.AbsoluteShort or M68000OperandMode.AbsoluteLong
            or M68000OperandMode.PcDisplacement or M68000OperandMode.PcIndexedDisplacement;
        M68000DecodedInstruction Result(bool valid) => new(
            valid ? M68000DecodeStatus.Decoded : M68000DecodeStatus.Invalid,
            start, valid ? address : unchecked(start + 2), opcode,
            valid ? operation : M68000NativeOperation.Invalid, M68000InstructionVariant.Normal, size, 0,
            valid ? source : source with { Immediate = opcode }, destination);

        if ((opcode & 0x1c0) == 0x1c0)
        {
            operation = M68000NativeOperation.LoadEffectiveAddress;
            size = M68000OperandSize.Long;
            destination = new(M68000OperandMode.AddressRegister, highReg);
            return Result(Operand(size.Value, out source) && Control(source));
        }
        if ((opcode & 0x100) != 0)
        {
            if ((opcode & 0x180) != 0x180) return Result(false);
            operation = M68000NativeOperation.CheckBounds;
            size = M68000OperandSize.Word;
            destination = new(M68000OperandMode.DataRegister, highReg);
            return Result(Operand(size.Value, out source) && source.Mode != M68000OperandMode.AddressRegister);
        }
        if ((opcode & 0xb80) == 0x880 && mode is not (0 or 1))
        {
            operation = M68000NativeOperation.MoveMultiple;
            size = (opcode & 0x40) != 0 ? M68000OperandSize.Long : M68000OperandSize.Word;
            var registers = new M68000DecodedOperand(M68000OperandMode.DataRegister, Immediate: Word());
            if ((opcode & 0x400) != 0)
            {
                destination = registers;
                if (!Operand(size.Value, out source) || source.Mode is M68000OperandMode.PreDecrement or M68000OperandMode.Immediate)
                    return Result(false);
                // BlastEm's PC-relative convention uses instruction address + 2;
                // MOVEM has an extra register-mask word before its EA extension.
                if (source.Mode is M68000OperandMode.PcDisplacement or M68000OperandMode.PcIndexedDisplacement)
                    source = source with { Displacement = unchecked(source.Displacement + 2) };
            }
            else
            {
                source = registers;
                if (!Operand(size.Value, out destination) || !Alterable(destination)
                    || destination.Mode is M68000OperandMode.DataRegister or M68000OperandMode.PostIncrement)
                    return Result(false);
            }
            return Result(true);
        }

        int group = opcode >> 9 & 7;
        switch (group)
        {
            case 0:
                operation = sizeBits == 3 ? M68000NativeOperation.MoveFromStatus : M68000NativeOperation.NegateExtend;
                size = sizeBits == 3 ? M68000OperandSize.Word : (M68000OperandSize)sizeBits;
                return Result(Operand(size.Value, out destination) && Alterable(destination));
            case 1:
                if (sizeBits == 3) return Result(false); // MOVE from CCR requires 68010.
                operation = M68000NativeOperation.Clear;
                size = (M68000OperandSize)sizeBits;
                return Result(Operand(size.Value, out destination) && Alterable(destination));
            case 2:
            case 3:
                size = sizeBits == 3 ? M68000OperandSize.Word : (M68000OperandSize)sizeBits;
                if (sizeBits == 3)
                {
                    operation = group == 2 ? M68000NativeOperation.MoveCondition : M68000NativeOperation.MoveStatus;
                    return Result(Operand(size.Value, out source) && source.Mode != M68000OperandMode.AddressRegister);
                }
                operation = group == 2 ? M68000NativeOperation.Negate : M68000NativeOperation.Complement;
                return Result(Operand(size.Value, out destination) && Alterable(destination));
            case 4:
                switch (opcode >> 3 & 63)
                {
                    case 1: // LINK.L is 68020.
                    case 9: // BKPT is 68010.
                    case 0x38:
                        return Result(false);
                    case 8:
                        operation = M68000NativeOperation.Swap;
                        size = M68000OperandSize.Word; // BlastEm encodes the swapped half size.
                        source = new(M68000OperandMode.DataRegister, lowReg);
                        return Result(true);
                    case 0x10:
                    case 0x18:
                        operation = M68000NativeOperation.Extend;
                        size = (opcode >> 3 & 63) == 0x10 ? M68000OperandSize.Word : M68000OperandSize.Long;
                        destination = new(M68000OperandMode.DataRegister, lowReg);
                        return Result(true);
                    default:
                        if ((opcode & 0x1c0) == 0)
                        {
                            operation = M68000NativeOperation.NegateDecimal;
                            size = M68000OperandSize.Byte;
                            return Result(Operand(size.Value, out destination) && Alterable(destination));
                        }
                        if ((opcode & 0x1c0) == 0x40)
                        {
                            operation = M68000NativeOperation.PushEffectiveAddress;
                            size = M68000OperandSize.Long;
                            return Result(Operand(size.Value, out source) && Control(source));
                        }
                        return Result(false);
                }
            case 5:
                if ((opcode & 255) == 0xfa) return Result(false); // CPU32 BGND.
                if ((opcode & 255) == 0xfc)
                {
                    operation = M68000NativeOperation.IllegalInstruction;
                    return Result(true);
                }
                if (sizeBits == 3)
                {
                    operation = M68000NativeOperation.TestAndSet;
                    size = M68000OperandSize.Byte;
                    return Result(Operand(size.Value, out destination) && Alterable(destination));
                }
                operation = M68000NativeOperation.Test;
                size = (M68000OperandSize)sizeBits;
                return Result(Operand(size.Value, out source) && Alterable(source));
            case 6:
                return Result(false); // 68020 long MUL/DIV.
            case 7:
                if ((opcode & 0x80) != 0)
                {
                    operation = (opcode & 0x40) != 0 ? M68000NativeOperation.Jump : M68000NativeOperation.JumpSubroutine;
                    // No immediate extension is fetched for BlastEm's unsized control EA.
                    if ((opcode & 63) == 0x3c) return Result(false);
                    return Result(Operand(M68000OperandSize.Word, out source) && Control(source));
                }
                if ((opcode & 0x40) == 0) return Result(false);
                switch (mode)
                {
                    case 0:
                    case 1:
                        operation = M68000NativeOperation.Trap;
                        source = new(M68000OperandMode.Immediate, Immediate: (uint)(opcode & 15));
                        return Result(true);
                    case 2:
                        operation = M68000NativeOperation.Link;
                        size = M68000OperandSize.Word;
                        source = new(M68000OperandMode.AddressRegister, lowReg);
                        destination = new(M68000OperandMode.Immediate, Immediate: unchecked((uint)(short)Word()));
                        return Result(true);
                    case 3:
                        operation = M68000NativeOperation.Unlink;
                        destination = new(M68000OperandMode.AddressRegister, lowReg);
                        return Result(true);
                    case 4:
                    case 5:
                        operation = M68000NativeOperation.MoveUserStack;
                        if ((opcode & 8) != 0) destination = new(M68000OperandMode.AddressRegister, lowReg);
                        else source = new(M68000OperandMode.AddressRegister, lowReg);
                        return Result(true);
                    case 6:
                        operation = lowReg switch
                        {
                            0 => M68000NativeOperation.Reset,
                            1 => M68000NativeOperation.NoOperation,
                            2 => M68000NativeOperation.Stop,
                            3 => M68000NativeOperation.ReturnException,
                            5 => M68000NativeOperation.ReturnSubroutine,
                            6 => M68000NativeOperation.TrapOverflow,
                            7 => M68000NativeOperation.ReturnCondition,
                            _ => M68000NativeOperation.Invalid
                        };
                        if (operation == M68000NativeOperation.Stop)
                            source = new(M68000OperandMode.Immediate, Immediate: Word());
                        return Result(operation != M68000NativeOperation.Invalid);
                    default:
                        return Result(false); // MOVEC is 68010.
                }
            default:
                throw new InvalidOperationException("Not a miscellaneous opcode group.");
        }
    }
}
