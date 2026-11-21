using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// Emits guest instruction bodies using the decoded BlastEm operation model,
/// adapted to the existing core's register/flag representation. The block runner
/// must perform fetch/extensions, cycle accounting and exception boundaries.
/// An unsupported body returns false before writing any bytes.
/// </summary>
internal sealed class M68000X64InstructionEmitter(X64Emitter code, int multiplier = 1, int ratio = 1, int shift = 0,
    Cpu68000Configuration? configuration = null)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    private readonly Cpu68000Configuration _configuration = configuration ?? new(multiplier);
    private static bool Register(M68000DecodedOperand operand) =>
        operand.Mode is M68000OperandMode.DataRegister or M68000OperandMode.AddressRegister && operand.Register < 8;
    private static bool Value(M68000DecodedOperand operand) => Register(operand) || operand.Mode == M68000OperandMode.Immediate;
    private static int Index(M68000DecodedOperand operand) => operand.Register + (operand.Mode == M68000OperandMode.AddressRegister ? 8 : 0);
    private void Load(M68000DecodedOperand operand, M68000OperandSize size, R host, bool signed = false)
    {
        if (operand.Mode is M68000OperandMode.Immediate or M68000OperandMode.ImmediateWord)
        {
            uint value = size == M68000OperandSize.Byte ? operand.Immediate & 255
                : size == M68000OperandSize.Word ? operand.Immediate & 65535 : operand.Immediate;
            if (signed && size == M68000OperandSize.Word) value = unchecked((uint)(short)value);
            if (signed && size == M68000OperandSize.Byte) value = unchecked((uint)(sbyte)value);
            code.MoveImmediate(value, host, W.Dword);
        }
        else _context.LoadRegister(Index(operand), host, size, signed);
    }
    private void Mask(R host, M68000OperandSize size)
    {
        if (size != M68000OperandSize.Long)
            code.BinaryImmediate(A.And, size == M68000OperandSize.Byte ? 255 : 65535, host, W.Dword);
    }
    public bool TryEmitBody(in M68000DecodedInstruction instruction)
    {
        if (instruction.Status != M68000DecodeStatus.Decoded) return false;
        O operation = instruction.Operation;
        if (operation == O.NoOperation) return true;
        if (operation == O.SetCondition && instruction.Destination.Mode == M68000OperandMode.DataRegister)
        {
            new M68000X64ControlInstructionEmitter(code).EmitCondition(instruction.Condition);
            code.Move(R.R8, R.R10, W.Dword); code.Unary(X64Unary.Negate, R.R10, W.Byte);
            _context.StoreRegister(instruction.Destination.Register, R.R10, M68000OperandSize.Byte);
            // ST/SF have their own fixed timing entries; other true register
            // conditions add two clocks in the existing handlers.
            if (instruction.Condition >= 2)
            {
                code.MultiplyImmediate(2 * multiplier, R.R8, R.R8, W.Dword);
                code.MultiplyImmediate(ratio, R.R8, R.R8, W.Qword);
                if (shift != 0) code.Shift(X64Shift.Right, (byte)shift, R.R8, W.Qword);
                _context.AddState(M68000StateField.cycles, R.R8);
            }
            return true;
        }
        if (M68000X64RotateInstructionEmitter.CanEmitRegister(instruction))
        {
            if (_configuration.RefreshSkipAdvance != 0 && instruction.Source.Mode == M68000OperandMode.DataRegister) return false;
            new M68000X64RotateInstructionEmitter(code).EmitRegister(instruction, _configuration, ratio, shift); return true;
        }
        if (M68000X64ShiftInstructionEmitter.CanEmit(instruction))
        {
            if (_configuration.RefreshSkipAdvance != 0 && instruction.Source.Mode == M68000OperandMode.DataRegister) return false;
            new M68000X64ShiftInstructionEmitter(code).Emit(instruction, _configuration, ratio, shift); return true;
        }
        if (instruction.Size is not { } size || size > M68000OperandSize.Long) return false;
        var source = instruction.Source; var destination = instruction.Destination;
        if (M68000X64DecimalInstructionEmitter.IsDecimal(operation))
        {
            if (destination.Mode != M68000OperandMode.DataRegister
                || operation != O.NegateDecimal && source.Mode != M68000OperandMode.DataRegister) return false;
            if (operation != O.NegateDecimal) Load(source, M68000OperandSize.Byte, R.R8);
            Load(destination, M68000OperandSize.Byte, R.R9);
            new M68000X64DecimalInstructionEmitter(code).Emit(operation);
            _context.StoreRegister(destination.Register, R.R10, M68000OperandSize.Byte); return true;
        }
        if (operation is O.OrCcr or O.AndCcr or O.ExclusiveOrCcr)
        {
            var status = new M68000X64StatusInstructionEmitter(code); status.ReadStatus();
            code.BinaryImmediate(operation == O.OrCcr ? A.Or : operation == O.AndCcr ? A.And : A.Xor,
                unchecked((int)source.Immediate), R.R10, W.Dword);
            status.WriteCondition(R.R10); return true;
        }
        if (operation == O.TestAndSet && destination.Mode == M68000OperandMode.DataRegister)
        {
            Load(destination, M68000OperandSize.Byte, R.R10);
            _context.LogicalFlags(R.R10, M68000OperandSize.Byte, R.R11);
            code.BinaryImmediate(A.Or, 128, R.R10, W.Dword);
            _context.StoreRegister(destination.Register, R.R10, M68000OperandSize.Byte); return true;
        }
        if (M68000X64ExtendInstructionEmitter.IsExtend(operation))
        {
            if (destination.Mode != M68000OperandMode.DataRegister
                || operation != O.NegateExtend && source.Mode != M68000OperandMode.DataRegister) return false;
            if (operation != O.NegateExtend) Load(source, size, R.R8);
            Load(destination, size, R.R9);
            new M68000X64ExtendInstructionEmitter(code).Emit(operation, size);
            _context.StoreRegister(destination.Register, R.R10, size); return true;
        }
        if (operation == O.MoveFromStatus && destination.Mode == M68000OperandMode.DataRegister)
        {
            new M68000X64StatusInstructionEmitter(code).ReadStatus();
            _context.StoreRegister(destination.Register, R.R10, M68000OperandSize.Word); return true;
        }
        if (operation == O.MoveCondition && Value(source))
        {
            Load(source, M68000OperandSize.Word, R.R10);
            new M68000X64StatusInstructionEmitter(code).WriteCondition(R.R10); return true;
        }
        if (M68000X64MultiplyInstructionEmitter.IsMultiply(operation))
        {
            if (!Value(source) || destination.Mode != M68000OperandMode.DataRegister) return false;
            Load(source, M68000OperandSize.Word, R.R8);
            new M68000X64MultiplyInstructionEmitter(code).Emit(operation, R.R8, destination.Register, multiplier, ratio, shift);
            return true;
        }
        if (M68000X64BitInstructionEmitter.IsBit(operation))
        {
            if (source.Mode is not (M68000OperandMode.DataRegister or M68000OperandMode.ImmediateWord)
                || destination.Mode != M68000OperandMode.DataRegister
                    && !(operation == O.BitTest && destination.Mode == M68000OperandMode.Immediate)) return false;
            Load(source, M68000OperandSize.Long, R.R11);
            Load(destination, size, R.R10);
            new M68000X64BitInstructionEmitter(code).Emit(operation, size, R.R11, multiplier, ratio, shift);
            if (operation != O.BitTest) _context.StoreRegister(Index(destination), R.R10, size);
            return true;
        }
        if (operation == O.Test)
        {
            if (source.Mode != M68000OperandMode.DataRegister) return false;
            Load(source, size, R.R10);
            _context.LogicalFlags(R.R10, size, R.R11); return true;
        }
        bool unary = operation is O.Clear or O.Complement or O.Negate or O.Extend;
        if (operation == O.Swap)
        {
            if (!Register(source) || source.Mode != M68000OperandMode.DataRegister) return false;
        }
        else if (operation == O.Exchange)
        {
            if (!Register(source) || !Register(destination)) return false;
        }
        else if (unary)
        {
            if (!Register(destination) || destination.Mode != M68000OperandMode.DataRegister) return false;
        }
        else
        {
            if (operation is not (O.Move or O.Add or O.Subtract or O.Compare or O.And or O.Or or O.ExclusiveOr)
                || !Value(source) || !Register(destination)) return false;
            if (destination.Mode == M68000OperandMode.AddressRegister && operation is not (O.Move or O.Add or O.Subtract or O.Compare))
                return false;
        }

        if (operation == O.Exchange)
        {
            Load(source, M68000OperandSize.Long, R.R8); Load(destination, M68000OperandSize.Long, R.R9);
            _context.StoreRegister(Index(source), R.R9, M68000OperandSize.Long);
            _context.StoreRegister(Index(destination), R.R8, M68000OperandSize.Long); return true;
        }
        if (operation == O.Swap)
        {
            Load(source, M68000OperandSize.Long, R.R10); code.SwapLongWords(R.R10);
            _context.StoreRegister(Index(source), R.R10, M68000OperandSize.Long);
            _context.LogicalFlags(R.R10, M68000OperandSize.Long, R.R11); return true;
        }
        if (operation == O.Extend)
        {
            Load(destination, size == M68000OperandSize.Word ? M68000OperandSize.Byte : M68000OperandSize.Word, R.R10, true);
            _context.StoreRegister(Index(destination), R.R10, size);
            _context.LogicalFlags(R.R10, size, R.R11); return true;
        }
        if (operation == O.Clear)
        {
            code.MoveImmediate(0, R.R10, W.Dword); _context.StoreRegister(Index(destination), R.R10, size);
            _context.LogicalFlags(R.R10, size, R.R11); return true;
        }
        if (operation == O.Complement)
        {
            Load(destination, size, R.R10); code.Unary(X64Unary.Complement, R.R10, W.Dword); Mask(R.R10, size);
            _context.StoreRegister(Index(destination), R.R10, size);
            _context.LogicalFlags(R.R10, size, R.R11); return true;
        }
        if (operation == O.Negate)
        {
            Load(destination, size, R.R10);
            code.Unary(X64Unary.Negate, R.R10, M68000X64ContextEmitter.Width(size));
            _context.CaptureArithmeticFlags(true);
            _context.StoreRegister(Index(destination), R.R10, size); return true;
        }

        bool addressDestination = destination.Mode == M68000OperandMode.AddressRegister;
        bool signedSource = addressDestination && size == M68000OperandSize.Word
            && instruction.Variant != M68000InstructionVariant.Quick;
        Load(source, size, R.R8, signedSource);
        if (operation == O.Move)
        {
            _context.StoreRegister(Index(destination), R.R8, addressDestination ? M68000OperandSize.Long : size);
            if (!addressDestination) _context.LogicalFlags(R.R8, size, R.R11);
            return true;
        }
        var resultSize = addressDestination ? M68000OperandSize.Long : size;
        Load(destination, resultSize, R.R9); code.Move(R.R9, R.R10, W.Dword);
        A arithmetic = operation switch
        {
            O.Add => A.Add, O.Subtract or O.Compare => A.Subtract,
            O.And => A.And, O.Or => A.Or, _ => A.Xor
        };
        code.Binary(arithmetic, R.R8, R.R10, M68000X64ContextEmitter.Width(resultSize));
        if (operation != O.Compare) _context.StoreRegister(Index(destination), R.R10, resultSize);
        if (addressDestination && operation != O.Compare) return true;
        if (operation is O.Add or O.Subtract or O.Compare)
            _context.CaptureArithmeticFlags(operation != O.Compare);
        else _context.LogicalFlags(R.R10, size, R.R11);
        if (operation == O.Add && instruction.Variant == M68000InstructionVariant.Quick)
            _context.StoreStateImmediate(M68000StateField.poll_detected, 0);
        return true;
    }
}

