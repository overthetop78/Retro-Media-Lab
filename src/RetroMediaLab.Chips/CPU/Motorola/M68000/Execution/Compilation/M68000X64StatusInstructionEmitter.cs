using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>SR/CCR translation using the existing core's split flag representation.</summary>
internal sealed class M68000X64StatusInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    // Result in R10, scratch R8/R9. MOVE from SR is unprivileged on 68000.
    public void ReadStatus()
    {
        _context.LoadState(M68000StateField.t1_flag, R.R10);
        _context.LoadState(M68000StateField.s_flag, R.R8); code.Shift(S.Left, 11, R.R8, W.Dword);
        code.Binary(A.Or, R.R8, R.R10, W.Dword);
        _context.LoadState(M68000StateField.int_mask, R.R8); code.Binary(A.Or, R.R8, R.R10, W.Dword);
        Part(M68000StateField.x_flag, 4, 16); Part(M68000StateField.n_flag, 4, 8);
        Part(M68000StateField.v_flag, 6, 2); Part(M68000StateField.c_flag, 8, 1);
        _context.LoadState(M68000StateField.not_z_flag, R.R8); code.MoveImmediate(0, R.R9, W.Dword);
        code.Test(R.R8, R.R8, W.Dword); code.SetCondition(C.Zero, R.R9); code.Shift(S.Left, 2, R.R9, W.Dword);
        code.Binary(A.Or, R.R9, R.R10, W.Dword);
        void Part(M68000StateField flag, byte shift, int mask)
        {
            _context.LoadState(flag, R.R8); code.Shift(S.Right, shift, R.R8, W.Dword);
            code.BinaryImmediate(A.And, mask, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.R10, W.Dword);
        }
    }
    // Only CCR fields change. Source must remain distinct from R8/R9 scratch.
    public void WriteCondition(R source)
    {
        Part(M68000StateField.x_flag, 16, 4); Part(M68000StateField.n_flag, 8, 4);
        Part(M68000StateField.v_flag, 2, 6); Part(M68000StateField.c_flag, 1, 8);
        code.MoveImmediate(0, R.R9, W.Dword); code.TestImmediate(4, source, W.Dword); code.SetCondition(C.Zero, R.R9);
        _context.StoreState(M68000StateField.not_z_flag, R.R9);
        void Part(M68000StateField flag, int mask, byte shift)
        {
            code.Move(source, R.R8, W.Dword); code.BinaryImmediate(A.And, mask, R.R8, W.Dword);
            code.Shift(S.Left, shift, R.R8, W.Dword); _context.StoreState(flag, R.R8);
        }
    }
}

