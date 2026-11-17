namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal static partial class M68000InstructionDecoder
{
    // BlastEm 68kinst.c, BIT_MOVEP_IMMED case, 68000 configuration.
    private static M68000DecodedInstruction DecodeImmediate(uint start, ushort opcode, Func<uint, ushort> fetch)
    {
        uint address = unchecked(start + 2);
        var operation = M68000NativeOperation.Invalid;
        var variant = M68000InstructionVariant.Normal;
        M68000OperandSize? size = null;
        M68000DecodedOperand source = new(M68000OperandMode.Unused);
        M68000DecodedOperand destination = new(M68000OperandMode.Unused);
        ushort Word()
        {
            ushort word = fetch(address);
            address = unchecked(address + 2);
            return word;
        }
        bool Destination(M68000OperandSize operandSize)
        {
            uint next = M68000OperandDecoder.Decode((byte)(opcode & 63), operandSize, address, fetch, out destination);
            if (next == uint.MaxValue) return false;
            address = next;
            return true;
        }
        M68000DecodedInstruction Result(bool valid) => new(
            valid ? M68000DecodeStatus.Decoded : M68000DecodeStatus.Invalid,
            start, valid ? address : unchecked(start + 2), opcode,
            valid ? operation : M68000NativeOperation.Invalid, variant, size, 0,
            valid ? source : source with { Immediate = opcode }, destination);

        if ((opcode & 0x138) == 0x108)
        {
            operation = M68000NativeOperation.MovePeripheral;
            size = (opcode & 0x40) != 0 ? M68000OperandSize.Long : M68000OperandSize.Word;
            // Keep the extension as stored by BlastEm; address calculation sign-extends its low word.
            var memory = new M68000DecodedOperand(M68000OperandMode.Displacement,
                (byte)(opcode & 7), Displacement: Word());
            var register = new M68000DecodedOperand(M68000OperandMode.DataRegister, (byte)(opcode >> 9 & 7));
            if ((opcode & 0x80) != 0) { source = register; destination = memory; }
            else { source = memory; destination = register; }
            return Result(true);
        }
        bool registerBit = (opcode & 0x100) != 0;
        bool immediateBit = (opcode & 0xf00) == 0x800;
        if (registerBit || immediateBit)
        {
            operation = (opcode >> 6 & 3) switch
            {
                0 => M68000NativeOperation.BitTest,
                1 => M68000NativeOperation.BitChange,
                2 => M68000NativeOperation.BitClear,
                _ => M68000NativeOperation.BitSet
            };
            source = registerBit
                ? new(M68000OperandMode.DataRegister, (byte)(opcode >> 9 & 7))
                : new(M68000OperandMode.ImmediateWord, Immediate: (uint)(Word() & 255));
            size = M68000OperandSize.Byte;
            if (!Destination(size.Value) || destination.Mode == M68000OperandMode.AddressRegister
                || (!registerBit && destination.Mode == M68000OperandMode.Immediate)
                || (operation != M68000NativeOperation.BitTest && destination.Mode > M68000OperandMode.AbsoluteLong))
                return Result(false);
            if (destination.Mode == M68000OperandMode.DataRegister) size = M68000OperandSize.Long;
            return Result(true);
        }

        // CMP2/CAS/etc. and MOVES are not 68000 instructions.
        if ((opcode & 0xc0) == 0xc0) return Result(false);
        int group = opcode >> 9 & 7;
        if (group == 7) return Result(false);
        if (group is 0 or 1 or 5 && (opcode & 255) is 0x3c or 0x7c)
        {
            bool sr = (opcode & 255) == 0x7c;
            operation = (group, sr) switch
            {
                (0, false) => M68000NativeOperation.OrCcr,
                (0, true) => M68000NativeOperation.OrSr,
                (1, false) => M68000NativeOperation.AndCcr,
                (1, true) => M68000NativeOperation.AndSr,
                (5, false) => M68000NativeOperation.ExclusiveOrCcr,
                _ => M68000NativeOperation.ExclusiveOrSr
            };
            size = sr ? M68000OperandSize.Word : M68000OperandSize.Byte;
            uint value = Word();
            if (!sr) value &= 255;
            source = new(M68000OperandMode.Immediate, Immediate: value);
            return Result(true);
        }
        operation = group switch
        {
            0 => M68000NativeOperation.Or,
            1 => M68000NativeOperation.And,
            2 => M68000NativeOperation.Subtract,
            3 => M68000NativeOperation.Add,
            5 => M68000NativeOperation.ExclusiveOr,
            6 => M68000NativeOperation.Compare,
            _ => M68000NativeOperation.Invalid
        };
        if (operation == M68000NativeOperation.Invalid) return Result(false);
        variant = M68000InstructionVariant.Immediate;
        size = (M68000OperandSize)(opcode >> 6 & 3);
        uint immediate = Word();
        if (size == M68000OperandSize.Byte) immediate &= 255;
        else if (size == M68000OperandSize.Long) immediate = immediate << 16 | Word();
        source = new(M68000OperandMode.Immediate, Immediate: immediate);
        if (!Destination(size.Value) || destination.Mode == M68000OperandMode.AddressRegister
            || destination.Mode > M68000OperandMode.AbsoluteLong) return Result(false);
        return Result(true);
    }
}
