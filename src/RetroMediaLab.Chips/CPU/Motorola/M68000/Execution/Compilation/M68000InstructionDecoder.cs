namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal enum M68000DecodeStatus : byte { Decoded, Invalid, UnsupportedFamily }
internal enum M68000NativeOperation : byte
{
    Invalid, Move, Add, Subtract, SetCondition, DecrementBranch,
    BranchCondition, BranchSubroutine, ALineTrap, FLineTrap,
    MovePeripheral, BitTest, BitChange, BitClear, BitSet, Or, And, ExclusiveOr, Compare,
    OrCcr, OrSr, AndCcr, AndSr, ExclusiveOrCcr, ExclusiveOrSr,
    AddExtend, SubtractExtend, AddDecimal, SubtractDecimal, Exchange,
    MultiplySigned, MultiplyUnsigned, DivideSigned, DivideUnsigned,
    ArithmeticShiftRight, ArithmeticShiftLeft, LogicalShiftRight, LogicalShiftLeft,
    RotateExtendRight, RotateExtendLeft, RotateRight, RotateLeft,
    LoadEffectiveAddress, CheckBounds, MoveMultiple, MoveFromStatus, MoveCondition, MoveStatus,
    NegateExtend, Clear, Negate, Complement, Swap, Extend, NegateDecimal, PushEffectiveAddress,
    IllegalInstruction, TestAndSet, Test, Jump, JumpSubroutine, Trap, Link, Unlink,
    MoveUserStack, Reset, NoOperation, Stop, ReturnException, ReturnSubroutine, TrapOverflow, ReturnCondition
}
internal enum M68000InstructionVariant : byte { Normal, Quick, Immediate, Byte, Word, Long }

internal readonly record struct M68000DecodedInstruction(
    M68000DecodeStatus Status, uint Address, uint NextAddress, ushort Opcode,
    M68000NativeOperation Operation, M68000InstructionVariant Variant,
    M68000OperandSize? Size, byte Condition,
    M68000DecodedOperand Source, M68000DecodedOperand Destination)
{
    public uint ByteLength => unchecked(NextAddress - Address);
}

/// <summary>
/// Front end for the experimental native backend. MOVE, quick arithmetic and branch
/// families follow BlastEm 68kinst.c/m68k_decode, tree ec73825ece2a134beb7825a5fd6fd21a398ff12e.
/// UnsupportedFamily means the port is incomplete, not an illegal guest instruction.
/// The existing interpreter's InstructionMap remains unchanged.
/// </summary>
internal static partial class M68000InstructionDecoder
{
    public static M68000DecodedInstruction Decode(uint start, Func<uint, ushort> fetch)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        ushort opcode = fetch(start);
        if ((opcode >> 12) == 0) return DecodeImmediate(start, opcode, fetch);
        if ((opcode >> 12) == 4) return DecodeMiscellaneous(start, opcode, fetch);
        if ((opcode >> 12) is 8 or 9 or 11 or 12 or 13 or 14) return DecodeArithmetic(start, opcode, fetch);
        uint address = unchecked(start + 2);
        int family = opcode >> 12;
        byte reg = (byte)(opcode >> 9 & 7);
        byte condition = 0;
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
        bool Operand(byte ea, M68000OperandSize operandSize, out M68000DecodedOperand value)
        {
            uint next = M68000OperandDecoder.Decode(ea, operandSize, address, fetch, out value);
            if (next == uint.MaxValue) return false;
            address = next;
            return true;
        }
        M68000DecodedInstruction Result(M68000DecodeStatus status) => new(status, start,
            status == M68000DecodeStatus.Decoded ? address : unchecked(start + 2), opcode,
            status == M68000DecodeStatus.Decoded ? operation : M68000NativeOperation.Invalid,
            variant, size, condition,
            status == M68000DecodeStatus.Invalid ? source with { Immediate = opcode } : source, destination);

        switch (family)
        {
            case 1:
            case 2:
            case 3:
                operation = M68000NativeOperation.Move;
                size = family == 1 ? M68000OperandSize.Byte
                    : family == 3 ? M68000OperandSize.Word : M68000OperandSize.Long;
                if (!Operand((byte)(opcode & 63), size.Value, out source)
                    || (family == 1 && source.Mode == M68000OperandMode.AddressRegister))
                    return Result(M68000DecodeStatus.Invalid);
                // MOVE's destination fields are reversed relative to the source EA.
                byte destinationEa = (byte)(((opcode >> 6 & 7) << 3) | reg);
                if (!Operand(destinationEa, size.Value, out destination)
                    || destination.Mode > M68000OperandMode.AbsoluteLong
                    || (family == 1 && destination.Mode == M68000OperandMode.AddressRegister))
                    return Result(M68000DecodeStatus.Invalid);
                break;
            case 5:
                int sizeBits = opcode >> 6 & 3;
                if (sizeBits == 3)
                {
                    condition = (byte)(opcode >> 8 & 15);
                    if ((opcode >> 3 & 7) == 1)
                    {
                        operation = M68000NativeOperation.DecrementBranch;
                        source = new(M68000OperandMode.Immediate, Immediate: unchecked((uint)(short)Word()));
                        destination = new(M68000OperandMode.DataRegister, (byte)(opcode & 7));
                    }
                    else
                    {
                        operation = M68000NativeOperation.SetCondition;
                        if (!Operand((byte)(opcode & 63), M68000OperandSize.Byte, out destination)
                            || destination.Mode == M68000OperandMode.AddressRegister
                            || destination.Mode > M68000OperandMode.AbsoluteLong)
                            return Result(M68000DecodeStatus.Invalid);
                    }
                }
                else
                {
                    operation = (opcode & 0x100) == 0 ? M68000NativeOperation.Add : M68000NativeOperation.Subtract;
                    variant = M68000InstructionVariant.Quick;
                    size = (M68000OperandSize)sizeBits;
                    source = new(M68000OperandMode.Immediate, Immediate: reg == 0 ? 8u : reg);
                    if (!Operand((byte)(opcode & 63), size.Value, out destination)
                        || destination.Mode > M68000OperandMode.AbsoluteLong
                        || (size == M68000OperandSize.Byte && destination.Mode == M68000OperandMode.AddressRegister))
                        return Result(M68000DecodeStatus.Invalid);
                }
                break;
            case 6:
                condition = (byte)(opcode >> 8 & 15);
                operation = condition == 1 ? M68000NativeOperation.BranchSubroutine : M68000NativeOperation.BranchCondition;
                uint displacement;
                if ((opcode & 255) == 0)
                {
                    variant = M68000InstructionVariant.Word;
                    displacement = unchecked((uint)(short)Word());
                }
                else
                {
                    // 0xff is -1 on 68000, not the long branch extension of 68020.
                    variant = M68000InstructionVariant.Byte;
                    displacement = unchecked((uint)(sbyte)opcode);
                }
                source = new(M68000OperandMode.Immediate, Immediate: displacement);
                break;
            case 7:
                if ((opcode & 0x100) != 0) return Result(M68000DecodeStatus.Invalid);
                operation = M68000NativeOperation.Move;
                variant = M68000InstructionVariant.Quick;
                size = M68000OperandSize.Long;
                source = new(M68000OperandMode.Immediate, Immediate: unchecked((uint)(sbyte)opcode));
                destination = new(M68000OperandMode.DataRegister, reg);
                break;
            case 10:
                operation = M68000NativeOperation.ALineTrap;
                break;
            case 15:
                operation = M68000NativeOperation.FLineTrap;
                break;
            default:
                return Result(M68000DecodeStatus.UnsupportedFamily);
        }
        return Result(M68000DecodeStatus.Decoded);
    }
}
