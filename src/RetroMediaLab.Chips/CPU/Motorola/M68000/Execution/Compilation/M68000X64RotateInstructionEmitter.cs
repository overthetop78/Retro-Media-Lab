using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>Native rotate bodies; guest counts and X remain independent of x86's count masking.</summary>
internal sealed class M68000X64RotateInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsRotate(O op) => op is O.RotateLeft or O.RotateRight or O.RotateExtendLeft or O.RotateExtendRight;
    public static bool CanEmitRegister(in M68000DecodedInstruction instruction) => IsRotate(instruction.Operation)
        && instruction.Destination.Mode == M68000OperandMode.DataRegister
        && instruction.Source.Mode is M68000OperandMode.DataRegister or M68000OperandMode.Immediate;

    public void EmitRegister(in M68000DecodedInstruction instruction, Cpu68000Configuration configuration, int ratio, int shift)
    {
        var size = instruction.Size!.Value;
        bool immediate = instruction.Source.Mode == M68000OperandMode.Immediate;
        if (immediate) code.MoveImmediate(instruction.Source.Immediate, R.R11, W.Dword);
        else
        {
            _context.LoadRegister(instruction.Source.Register, R.R11, M68000OperandSize.Long);
            code.BinaryImmediate(A.And, 63, R.R11, W.Dword);
        }
        code.Test(R.R11, R.R11, W.Dword); int zero = code.ReserveJump(C.Zero);
        code.MultiplyImmediate(2 * configuration.ClockMultiplier, R.R11, R.R8, W.Dword);
        code.MultiplyImmediate(ratio, R.R8, R.R8, W.Qword);
        if (shift != 0) code.Shift(S.Right, (byte)shift, R.R8, W.Qword);
        _context.AddState(M68000StateField.cycles, R.R8);
        if (!immediate) new M68000X64RefreshEmitter(code).EmitSkip(configuration.RefreshSkipAdvance);
        _context.LoadRegister(instruction.Destination.Register, R.R10, size);
        EmitBody(instruction.Operation, size, immediate ? (int)instruction.Source.Immediate : null);
        _context.StoreRegister(instruction.Destination.Register, R.R10, size);
        int done = code.ReserveJump(); code.PatchJump(zero, code.Position);
        _context.LoadRegister(instruction.Destination.Register, R.R10, size);
        _context.LogicalFlags(R.R10, size, R.R8);
        if (Extended(instruction.Operation))
        {
            _context.LoadState(M68000StateField.x_flag, R.R9);
            _context.StoreState(M68000StateField.c_flag, R.R9);
        }
        code.PatchJump(done, code.Position);
    }
    public void EmitMemoryResult(O operation)
    {
        bool extended = Extended(operation), left = operation is O.RotateLeft or O.RotateExtendLeft;
        if (extended) SeedCarry();
        code.Shift(extended ? left ? S.RotateCarryLeft : S.RotateCarryRight : left ? S.RotateLeft : S.RotateRight,
            1, R.R10, W.Word);
        code.PushFlags(); code.Pop(R.R14);
    }
    public void EmitMemoryFlags(O operation)
    {
        _context.LogicalFlags(R.R10, M68000OperandSize.Word, R.R8);
        code.Move(R.R14, R.R9, W.Dword); code.Shift(S.Left, 8, R.R9, W.Dword); code.BinaryImmediate(A.And, 256, R.R9, W.Dword);
        _context.StoreState(M68000StateField.c_flag, R.R9);
        if (Extended(operation)) _context.StoreState(M68000StateField.x_flag, R.R9);
    }
    private static bool Extended(O operation) => operation is O.RotateExtendLeft or O.RotateExtendRight;
    private void SeedCarry() => code.BitMemoryImmediate(X64Bit.Test, 8, R.Rdx,
        M68000X64ContextEmitter.StateOffset(M68000StateField.x_flag), W.Dword);
    private void EmitBody(O operation, M68000OperandSize size, int? immediate)
    {
        int bits = size == M68000OperandSize.Byte ? 8 : size == M68000OperandSize.Word ? 16 : 32;
        W width = M68000X64ContextEmitter.Width(size);
        bool extended = Extended(operation), left = operation is O.RotateLeft or O.RotateExtendLeft;
        S rotate = extended ? left ? S.RotateCarryLeft : S.RotateCarryRight : left ? S.RotateLeft : S.RotateRight;
        if (extended)
        {
            if (immediate.HasValue) code.MoveImmediate(immediate.Value % (bits + 1), R.R9, W.Dword);
            else
            {
                // n <= 63. This reciprocal gives exact floor(n / (bits+1))
                // across that whole domain, with no host DIV or lookup table.
                int divisor = bits + 1, reciprocal = (65536 + divisor - 1) / divisor;
                code.MultiplyImmediate(reciprocal, R.R11, R.R8, W.Dword);
                code.Shift(S.Right, 16, R.R8, W.Dword);
                code.MultiplyImmediate(divisor, R.R8, R.R8, W.Dword);
                code.Move(R.R11, R.R9, W.Dword); code.Binary(A.Subtract, R.R8, R.R9, W.Dword);
            }
            int thirtyTwo = -1;
            if (bits == 32)
            {
                code.BinaryImmediate(A.Compare, 32, R.R9, W.Dword); thirtyTwo = code.ReserveJump(C.Zero);
            }
            code.Push(R.Rcx); code.Move(R.R9, R.Rcx, W.Dword); SeedCarry();
            code.ShiftByCl(rotate, R.R10, width); code.Pop(R.Rcx);
            if (bits == 32)
            {
                int rotated = code.ReserveJump(); code.PatchJump(thirtyTwo, code.Position);
                SeedCarry(); code.Shift(rotate, 1, R.R10, width); code.Shift(rotate, 31, R.R10, width);
                code.PatchJump(rotated, code.Position);
            }
            code.MoveImmediate(0, R.R9, W.Dword); code.SetCondition(C.Carry, R.R9); code.Shift(S.Left, 8, R.R9, W.Dword);
        }
        else
        {
            code.Push(R.Rcx); code.Move(R.R11, R.Rcx, W.Dword);
            code.ShiftByCl(rotate, R.R10, width); code.Pop(R.Rcx);
            // For nonzero guest counts, the result determines C even when the
            // x86 masked count is zero (e.g. a guest rotation by 32).
            code.Move(R.R10, R.R9, W.Dword);
            if (!left) code.Shift(S.Right, (byte)(bits - 1), R.R9, W.Dword);
            code.BinaryImmediate(A.And, 1, R.R9, W.Dword); code.Shift(S.Left, 8, R.R9, W.Dword);
        }
        _context.LogicalFlags(R.R10, size, R.R8);
        _context.StoreState(M68000StateField.c_flag, R.R9);
        if (extended) _context.StoreState(M68000StateField.x_flag, R.R9);
    }
}

