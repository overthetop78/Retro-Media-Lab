using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// BlastEm translate_shift register paths, adapted to Cpu68000's cycle additions
/// and exact noncanonical flag payloads. Rotates retain their existing handler.
/// </summary>
internal sealed class M68000X64ShiftInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsShift(O op) => op is O.ArithmeticShiftRight or O.ArithmeticShiftLeft or O.LogicalShiftRight or O.LogicalShiftLeft;
    public static bool CanEmit(in M68000DecodedInstruction instruction) => IsShift(instruction.Operation)
        && instruction.Destination.Mode == M68000OperandMode.DataRegister
        && instruction.Source.Mode is M68000OperandMode.DataRegister or M68000OperandMode.Immediate;

    // Memory shifts are fixed at one word-sized step. Their complete timing
    // is already in the instruction table, unlike register count additions.
    public void EmitMemoryResult(O operation)
    {
        code.Shift(operation is O.ArithmeticShiftLeft or O.LogicalShiftLeft ? S.Left
            : operation == O.ArithmeticShiftRight ? S.ArithmeticRight : S.Right, 1, R.R10, W.Word);
        code.PushFlags(); code.Pop(R.R14);
    }
    public void EmitMemoryFlags(O operation)
    {
        code.Push(R.R14); code.PopFlags();
        _context.CaptureArithmeticFlags(true);
        if (operation != O.ArithmeticShiftLeft) _context.StoreStateImmediate(M68000StateField.v_flag, 0);
    }

    public void Emit(in M68000DecodedInstruction instruction, Cpu68000Configuration configuration, int ratio, int cycleShift)
    {
        if (!CanEmit(instruction)) throw new ArgumentException("Unsupported native shift.", nameof(instruction));
        var op = instruction.Operation; var size = instruction.Size!.Value;
        int bits = size == M68000OperandSize.Byte ? 8 : size == M68000OperandSize.Word ? 16 : 32;
        int mask = bits == 32 ? -1 : (1 << bits) - 1;
        bool immediate = instruction.Source.Mode == M68000OperandMode.Immediate;
        bool left = op is O.ArithmeticShiftLeft or O.LogicalShiftLeft;
        bool arithmetic = op is O.ArithmeticShiftRight or O.ArithmeticShiftLeft;
        if (immediate) code.MoveImmediate(instruction.Source.Immediate, R.R11, W.Dword);
        else
        {
            _context.LoadRegister(instruction.Source.Register, R.R11, M68000OperandSize.Long);
            code.BinaryImmediate(A.And, 63, R.R11, W.Dword);
        }
        var done = new List<int>(); int zero = -1;
        if (!immediate)
        {
            code.Test(R.R11, R.R11, W.Dword); zero = code.ReserveJump(C.Zero);
        }
        // Same split USE_CYCLES rounding as the handler.
        code.MultiplyImmediate(2 * configuration.ClockMultiplier, R.R11, R.R8, W.Dword);
        code.MultiplyImmediate(ratio, R.R8, R.R8, W.Qword);
        if (cycleShift != 0) code.Shift(S.Right, (byte)cycleShift, R.R8, W.Qword);
        _context.AddState(M68000StateField.cycles, R.R8);
        if (!immediate) new M68000X64RefreshEmitter(code).EmitSkip(configuration.RefreshSkipAdvance);
        _context.LoadRegister(instruction.Destination.Register, R.R8, size);
        code.Move(R.R8, R.R10, W.Dword);
        int large = -1;
        if (!immediate)
        {
            code.BinaryImmediate(A.Compare, bits, R.R11, W.Dword);
            // LSR/LSL byte/word retain their normal carry payload at count == width.
            bool inclusive = !arithmetic && bits < 32;
            large = code.ReserveJump(inclusive ? C.Above : C.NoCarry);
        }
        if (left) Shift(S.Left, R.R10, R.R11);
        else if (arithmetic)
        {
            if (bits < 32) code.MoveExtend(R.R10, R.R10, bits == 8 ? W.Byte : W.Word, W.Dword, true);
            Shift(S.ArithmeticRight, R.R10, R.R11);
        }
        else Shift(S.Right, R.R10, R.R11);
        if (bits < 32) code.BinaryImmediate(A.And, mask, R.R10, W.Dword);
        _context.StoreRegister(instruction.Destination.Register, R.R10, size);
        _context.LogicalFlags(R.R10, size, R.R9);
        if (op == O.LogicalShiftRight) _context.StoreStateImmediate(M68000StateField.n_flag, 0);
        code.Move(R.R8, R.R9, W.Dword);
        if (!left)
        {
            if (immediate || bits == 8)
            {
                code.MoveImmediate(9, R.Rax, W.Dword); code.Binary(A.Subtract, R.R11, R.Rax, W.Dword);
                Shift(S.Left, R.R9, R.Rax);
            }
            else
            {
                code.Move(R.R11, R.Rax, W.Dword); code.BinaryImmediate(A.Subtract, 1, R.Rax, W.Dword);
                Shift(S.Right, R.R9, R.Rax); code.Shift(S.Left, 8, R.R9, W.Dword);
            }
        }
        else if (immediate && bits > 8)
        {
            code.MoveImmediate(bits - 8, R.Rax, W.Dword); code.Binary(A.Subtract, R.R11, R.Rax, W.Dword);
            Shift(S.Right, R.R9, R.Rax);
        }
        else if (bits < 32)
        {
            Shift(S.Left, R.R9, R.R11);
            if (bits == 16) code.Shift(S.Right, 8, R.R9, W.Dword);
        }
        else
        {
            code.MoveImmediate(32, R.Rax, W.Dword); code.Binary(A.Subtract, R.R11, R.Rax, W.Dword);
            Shift(S.Right, R.R9, R.Rax); code.Shift(S.Left, 8, R.R9, W.Dword);
        }
        _context.StoreState(M68000StateField.c_flag, R.R9); _context.StoreState(M68000StateField.x_flag, R.R9);
        if (op == O.ArithmeticShiftLeft)
        {
            // All bits crossing the sign position must agree for V to remain clear.
            if (immediate && bits == 8 && instruction.Source.Immediate == 8)
            {
                code.Test(R.R8, R.R8, W.Dword); int clear = code.ReserveJump(C.Zero);
                _context.StoreStateImmediate(M68000StateField.v_flag, 128); code.PatchJump(clear, code.Position);
            }
            else
            {
                code.MoveImmediate(bits - 1, R.R9, W.Dword); code.Binary(A.Subtract, R.R11, R.R9, W.Dword);
                code.MoveImmediate(mask, R.Rax, W.Dword); Shift(S.Left, R.Rax, R.R9);
                if (bits < 32) code.BinaryImmediate(A.And, mask, R.Rax, W.Dword);
                code.Move(R.R8, R.R9, W.Dword); code.Binary(A.And, R.Rax, R.R9, W.Dword);
                code.Test(R.R9, R.R9, W.Dword); int allZero = code.ReserveJump(C.Zero);
                code.Binary(A.Compare, R.Rax, R.R9, W.Dword); int allOne = code.ReserveJump(C.Zero);
                _context.StoreStateImmediate(M68000StateField.v_flag, 128);
                code.PatchJump(allZero, code.Position); code.PatchJump(allOne, code.Position);
            }
        }
        done.Add(code.ReserveJump());
        if (!immediate)
        {
            code.PatchJump(large, code.Position);
            if (op == O.ArithmeticShiftRight)
            {
                code.TestImmediate(unchecked(1 << (bits - 1)), R.R8, W.Dword); int positive = code.ReserveJump(C.Zero);
                code.MoveImmediate(mask, R.R10, W.Dword); _context.StoreRegister(instruction.Destination.Register, R.R10, size);
                _context.StoreStateImmediate(M68000StateField.n_flag, 128);
                _context.StoreStateImmediate(M68000StateField.not_z_flag, uint.MaxValue);
                _context.StoreStateImmediate(M68000StateField.v_flag, 0);
                _context.StoreStateImmediate(M68000StateField.c_flag, 256); _context.StoreStateImmediate(M68000StateField.x_flag, 256);
                done.Add(code.ReserveJump()); code.PatchJump(positive, code.Position);
            }
            code.MoveImmediate(0, R.R10, W.Dword); _context.StoreRegister(instruction.Destination.Register, R.R10, size);
            _context.LogicalFlags(R.R10, size, R.R9);
            code.MoveImmediate(0, R.R9, W.Dword);
            if (op != O.ArithmeticShiftRight)
            {
                code.BinaryImmediate(A.Compare, bits, R.R11, W.Dword); int tooFar = code.ReserveJump(C.NotZero);
                code.Move(R.R8, R.R9, W.Dword);
                if (left) { code.BinaryImmediate(A.And, 1, R.R9, W.Dword); code.Shift(S.Left, 8, R.R9, W.Dword); }
                else { code.Shift(S.Right, 23, R.R9, W.Dword); code.BinaryImmediate(A.And, 256, R.R9, W.Dword); }
                code.PatchJump(tooFar, code.Position);
            }
            _context.StoreState(M68000StateField.c_flag, R.R9); _context.StoreState(M68000StateField.x_flag, R.R9);
            if (op == O.ArithmeticShiftLeft)
            {
                code.Test(R.R8, R.R8, W.Dword); int clear = code.ReserveJump(C.Zero);
                _context.StoreStateImmediate(M68000StateField.v_flag, 128); code.PatchJump(clear, code.Position);
            }
            done.Add(code.ReserveJump());
            code.PatchJump(zero, code.Position);
            _context.LoadRegister(instruction.Destination.Register, R.R10, size);
            _context.LogicalFlags(R.R10, size, R.R9); _context.StoreStateImmediate(M68000StateField.c_flag, 0);
        }
        foreach (int jump in done) code.PatchJump(jump, code.Position);
    }
    private void Shift(S operation, R value, R count)
    {
        // RCX owns the guest register-file pointer outside this short sequence.
        code.Push(R.Rcx); code.Move(count, R.Rcx, W.Dword); code.ShiftByCl(operation, value, W.Dword); code.Pop(R.Rcx);
    }
}

