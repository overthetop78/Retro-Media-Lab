using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using U = RetroMediaLab.Chips.Common.Compilation.X64.X64Unary;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// DIVS/DIVU native arithmetic and overflow paths, using BlastEm's early DIVU
/// overflow check and the existing core's variable timing. Zero divisors are
/// rejected by preflight before fetch/EA mutations, for the existing trap path.
/// </summary>
internal sealed class M68000X64DivideInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsDivide(O operation) => operation is O.DivideSigned or O.DivideUnsigned;

    // Source in R14. R14/R15 are available because the only destination is Dn.
    public void Emit(O operation, int destination, Cpu68000Configuration configuration, int ratio, int shift)
    {
        bool signed = operation == O.DivideSigned;
        code.MoveExtend(R.R14, R.R15, W.Word, W.Dword, signed);
        _context.LoadRegister(destination, R.R14, M68000OperandSize.Long);
        int overflow = -1;
        if (!signed)
        {
            code.Move(R.R14, R.R8, W.Dword); code.Shift(S.Right, 16, R.R8, W.Dword);
            code.Binary(A.Compare, R.R15, R.R8, W.Dword); overflow = code.ReserveJump(C.NoCarry);
        }
        // Preserve the state pointer around the implicit EDX:EAX dividend.
        // Signed 64-bit division also handles INT32_MIN / -1 without a host fault.
        code.Push(R.Rdx);
        if (signed)
        {
            code.MoveExtend(R.R14, R.Rax, W.Dword, W.Qword, true);
            code.MoveExtend(R.R15, R.R11, W.Dword, W.Qword, true);
            code.SignExtendDividend(W.Qword); code.Unary(U.DivideSigned, R.R11, W.Qword);
        }
        else
        {
            code.Move(R.R14, R.Rax, W.Dword); code.MoveImmediate(0, R.Rdx, W.Dword);
            code.Unary(U.Divide, R.R15, W.Dword);
        }
        code.Move(R.Rdx, R.R9, W.Dword); code.Pop(R.Rdx);
        code.Move(R.Rax, R.R11, W.Dword); code.Move(R.Rax, R.R10, W.Dword);
        code.BinaryImmediate(A.And, 65535, R.R10, W.Dword);
        code.Shift(S.Left, 16, R.R9, W.Dword); code.Binary(A.Or, R.R9, R.R10, W.Dword);
        if (signed) SignedCycles(); else UnsignedCycles();
        AddCycles(configuration.ClockMultiplier, ratio, shift);
        new M68000X64RefreshEmitter(code).EmitSkip(configuration.RefreshSkipAdvance);
        if (signed)
        {
            // Match the established core's explicit INT32_MIN / -1 result.
            code.BinaryImmediate(A.Compare, int.MinValue, R.R14, W.Dword); int ordinary = code.ReserveJump(C.NotZero);
            code.BinaryImmediate(A.Compare, -1, R.R15, W.Dword); int ordinarySign = code.ReserveJump(C.NotZero);
            code.MoveImmediate(0, R.R10, W.Dword); code.MoveImmediate(0, R.R11, W.Dword);
            code.PatchJump(ordinary, code.Position); code.PatchJump(ordinarySign, code.Position);
            code.MoveExtend(R.R11, R.R9, W.Word, W.Dword, true);
            code.Binary(A.Compare, R.R9, R.R11, W.Dword); overflow = code.ReserveJump(C.NotZero);
        }
        _context.LogicalFlags(R.R11, M68000OperandSize.Word, R.R9);
        _context.StoreRegister(destination, R.R10, M68000OperandSize.Long);
        int done = code.ReserveJump();
        code.PatchJump(overflow, code.Position);
        if (!signed)
        {
            code.MoveImmediate(10, R.R8, W.Dword);
            AddCycles(configuration.ClockMultiplier, ratio, shift);
        }
        _context.StoreStateImmediate(M68000StateField.n_flag, 128);
        _context.StoreStateImmediate(M68000StateField.v_flag, 128);
        _context.StoreStateImmediate(M68000StateField.c_flag, 0);
        code.PatchJump(done, code.Position);
    }
    private void AddCycles(int multiplier, int ratio, int shift)
    {
        code.MultiplyImmediate(multiplier, R.R8, R.R8, W.Dword);
        code.MultiplyImmediate(ratio, R.R8, R.R8, W.Qword);
        if (shift != 0) code.Shift(S.Right, (byte)shift, R.R8, W.Qword);
        _context.AddState(M68000StateField.cycles, R.R8);
    }
    private void UnsignedCycles()
    {
        code.MoveImmediate(76, R.R8, W.Dword); code.Move(R.R14, R.R9, W.Dword);
        code.Move(R.R15, R.Rax, W.Dword); code.Shift(S.Left, 16, R.Rax, W.Dword);
        for (int i = 0; i < 15; i++)
        {
            code.Test(R.R9, R.R9, W.Dword); int positive = code.ReserveJump(C.NoSign);
            code.Shift(S.Left, 1, R.R9, W.Dword); code.Binary(A.Subtract, R.Rax, R.R9, W.Dword);
            int next = code.ReserveJump(); code.PatchJump(positive, code.Position);
            code.Shift(S.Left, 1, R.R9, W.Dword); code.BinaryImmediate(A.Add, 4, R.R8, W.Dword);
            code.Binary(A.Compare, R.Rax, R.R9, W.Dword); int below = code.ReserveJump(C.Carry);
            code.Binary(A.Subtract, R.Rax, R.R9, W.Dword); code.BinaryImmediate(A.Subtract, 2, R.R8, W.Dword);
            code.PatchJump(below, code.Position); code.PatchJump(next, code.Position);
        }
    }
    private void SignedCycles()
    {
        code.MoveImmediate(12, R.R8, W.Dword); code.Move(R.R14, R.R9, W.Dword);
        code.Test(R.R9, R.R9, W.Dword); int positive = code.ReserveJump(C.NoSign);
        code.BinaryImmediate(A.Add, 2, R.R8, W.Dword); code.Unary(U.Negate, R.R9, W.Dword);
        code.PatchJump(positive, code.Position); code.Shift(S.ArithmeticRight, 16, R.R9, W.Dword);
        code.Move(R.R15, R.Rax, W.Dword); Absolute(R.Rax);
        code.Binary(A.Compare, R.Rax, R.R9, W.Dword); int large = code.ReserveJump(C.GreaterOrEqual);
        code.BinaryImmediate(A.Add, 110, R.R8, W.Dword);
        code.Test(R.R15, R.R15, W.Dword); int negativeSource = code.ReserveJump(C.Sign);
        code.Test(R.R14, R.R14, W.Dword); int negativeDestination = code.ReserveJump(C.Sign);
        code.BinaryImmediate(A.Subtract, 2, R.R8, W.Dword); int adjusted = code.ReserveJump();
        code.PatchJump(negativeDestination, code.Position); code.BinaryImmediate(A.Add, 2, R.R8, W.Dword);
        code.PatchJump(adjusted, code.Position); code.PatchJump(negativeSource, code.Position);
        code.Move(R.R11, R.R9, W.Dword); Absolute(R.R9);
        code.BinaryImmediate(A.Compare, int.MinValue, R.R14, W.Dword); int normal = code.ReserveJump(C.NotZero);
        code.Unary(U.Negate, R.R9, W.Dword); code.PatchJump(normal, code.Position);
        // Fifteen quotient bits determine the legacy timing; no callback or
        // second division is necessary. Count zeros with fixed-width SWAR.
        code.BinaryImmediate(A.And, 65534, R.R9, W.Dword);
        code.Move(R.R9, R.Rax, W.Dword); code.Shift(S.Right, 1, R.Rax, W.Dword);
        code.BinaryImmediate(A.And, 0x5555, R.Rax, W.Dword); code.Binary(A.Subtract, R.Rax, R.R9, W.Dword);
        code.Move(R.R9, R.Rax, W.Dword); code.Shift(S.Right, 2, R.Rax, W.Dword);
        code.BinaryImmediate(A.And, 0x3333, R.Rax, W.Dword); code.BinaryImmediate(A.And, 0x3333, R.R9, W.Dword); code.Binary(A.Add, R.Rax, R.R9, W.Dword);
        code.Move(R.R9, R.Rax, W.Dword); code.Shift(S.Right, 4, R.Rax, W.Dword); code.Binary(A.Add, R.Rax, R.R9, W.Dword); code.BinaryImmediate(A.And, 0x0f0f, R.R9, W.Dword);
        code.Move(R.R9, R.Rax, W.Dword); code.Shift(S.Right, 8, R.Rax, W.Dword); code.Binary(A.Add, R.Rax, R.R9, W.Dword); code.BinaryImmediate(A.And, 31, R.R9, W.Dword);
        code.Shift(S.Left, 1, R.R9, W.Dword); code.BinaryImmediate(A.Add, 30, R.R8, W.Dword); code.Binary(A.Subtract, R.R9, R.R8, W.Dword);
        int done = code.ReserveJump(); code.PatchJump(large, code.Position);
        code.BinaryImmediate(A.Add, 4, R.R8, W.Dword); code.PatchJump(done, code.Position);
    }
    private void Absolute(R register)
    {
        code.Test(register, register, W.Dword); int positive = code.ReserveJump(C.NoSign);
        code.Unary(U.Negate, register, W.Dword); code.PatchJump(positive, code.Position);
    }
}

