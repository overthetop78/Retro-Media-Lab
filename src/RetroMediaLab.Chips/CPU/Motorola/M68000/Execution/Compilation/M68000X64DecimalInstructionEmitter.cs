using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using U = RetroMediaLab.Chips.Common.Compilation.X64.X64Unary;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>ABCD/SBCD/NBCD, including the existing core's invalid-digit behavior.</summary>
internal sealed class M68000X64DecimalInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsDecimal(O operation) => operation is O.AddDecimal or O.SubtractDecimal or O.NegateDecimal;
    // Byte source R8, destination R9; returns R10. EAs R14/R15 are preserved.
    public void Emit(O operation)
    {
        if (operation == O.NegateDecimal) { Negate(); return; }
        bool add = operation == O.AddDecimal;
        code.Move(R.R9, R.R10, W.Dword); code.BinaryImmediate(A.And, 15, R.R10, W.Dword);
        code.Move(R.R8, R.R11, W.Dword); code.BinaryImmediate(A.And, 15, R.R11, W.Dword);
        code.Binary(add ? A.Add : A.Subtract, R.R11, R.R10, W.Dword);
        SeedExtend(); code.BinaryImmediate(add ? A.AddCarry : A.SubtractBorrow, 0, R.R10, W.Dword);
        code.MoveImmediate(0, R.R11, W.Dword);
        code.BinaryImmediate(A.Compare, add ? 9 : 15, R.R10, W.Dword); int noCorrection = code.ReserveJump(C.BelowOrEqual);
        code.MoveImmediate(6, R.R11, W.Dword); code.PatchJump(noCorrection, code.Position);
        code.BinaryImmediate(A.And, 240, R.R8, W.Dword); code.BinaryImmediate(A.And, 240, R.R9, W.Dword);
        code.Binary(add ? A.Add : A.Subtract, R.R8, R.R9, W.Dword); code.Binary(A.Add, R.R9, R.R10, W.Dword);
        code.Move(R.R10, R.Rax, W.Dword);
        if (add)
        {
            code.Unary(U.Complement, R.Rax, W.Dword); code.Binary(A.Add, R.R11, R.R10, W.Dword);
            code.MoveImmediate(0, R.R8, W.Dword);
            code.BinaryImmediate(A.Compare, 159, R.R10, W.Dword); code.SetCondition(C.Above, R.R8);
            code.Shift(S.Left, 8, R.R8, W.Dword); Carry(R.R8);
            code.Test(R.R8, R.R8, W.Dword); int noCarry = code.ReserveJump(C.Zero);
            code.BinaryImmediate(A.Subtract, 160, R.R10, W.Dword); code.PatchJump(noCarry, code.Position);
            code.Binary(A.And, R.R10, R.Rax, W.Dword);
        }
        else
        {
            code.BinaryImmediate(A.Compare, 255, R.R10, W.Dword); int withinByte = code.ReserveJump(C.BelowOrEqual);
            code.BinaryImmediate(A.Add, 160, R.R10, W.Dword); code.MoveImmediate(256, R.R8, W.Dword);
            int captured = code.ReserveJump(); code.PatchJump(withinByte, code.Position);
            code.MoveImmediate(0, R.R8, W.Dword); code.Binary(A.Compare, R.R11, R.R10, W.Dword);
            code.SetCondition(C.Carry, R.R8); code.Shift(S.Left, 8, R.R8, W.Dword);
            code.PatchJump(captured, code.Position); Carry(R.R8);
            code.Binary(A.Subtract, R.R11, R.R10, W.Dword);
            code.Move(R.R10, R.R8, W.Dword); code.Unary(U.Complement, R.R8, W.Dword);
            code.Binary(A.And, R.R8, R.Rax, W.Dword);
        }
        FinishFlags();
    }
    private void Negate()
    {
        code.MoveImmediate(0, R.R10, W.Dword); SeedExtend(); code.Binary(A.SubtractBorrow, R.R9, R.R10, W.Dword);
        code.Test(R.R10, R.R10, W.Dword); int nonzero = code.ReserveJump(C.NotZero);
        _context.StoreStateImmediate(M68000StateField.n_flag, 0); _context.StoreStateImmediate(M68000StateField.v_flag, 0);
        _context.StoreStateImmediate(M68000StateField.c_flag, 0); _context.StoreStateImmediate(M68000StateField.x_flag, 0);
        int done = code.ReserveJump(); code.PatchJump(nonzero, code.Position);
        code.Move(R.R10, R.Rax, W.Dword); code.Move(R.R10, R.R8, W.Dword);
        code.Binary(A.Or, R.R9, R.R8, W.Dword); code.BinaryImmediate(A.And, 15, R.R8, W.Dword);
        int normal = code.ReserveJump(C.NotZero);
        code.BinaryImmediate(A.And, 240, R.R10, W.Dword); code.BinaryImmediate(A.Add, 6, R.R10, W.Dword);
        code.PatchJump(normal, code.Position); code.BinaryImmediate(A.Add, 154, R.R10, W.Dword);
        code.BinaryImmediate(A.And, 255, R.R10, W.Dword);
        code.Move(R.R10, R.R8, W.Dword); code.Unary(U.Complement, R.R8, W.Dword); code.Binary(A.And, R.R8, R.Rax, W.Dword);
        _context.StoreStateImmediate(M68000StateField.c_flag, 256); _context.StoreStateImmediate(M68000StateField.x_flag, 256);
        FinishFlags(); code.PatchJump(done, code.Position);
    }
    private void SeedExtend() => code.BitMemoryImmediate(X64Bit.Test, 8, R.Rdx,
        M68000X64ContextEmitter.StateOffset(M68000StateField.x_flag), W.Dword);
    private void Carry(R value)
    {
        _context.StoreState(M68000StateField.c_flag, value); _context.StoreState(M68000StateField.x_flag, value);
    }
    private void FinishFlags()
    {
        _context.StoreState(M68000StateField.v_flag, R.Rax);
        code.BinaryImmediate(A.And, 255, R.R10, W.Dword); _context.StoreState(M68000StateField.n_flag, R.R10);
        _context.LoadState(M68000StateField.not_z_flag, R.R8); code.Binary(A.Or, R.R10, R.R8, W.Dword);
        _context.StoreState(M68000StateField.not_z_flag, R.R8);
    }
}

