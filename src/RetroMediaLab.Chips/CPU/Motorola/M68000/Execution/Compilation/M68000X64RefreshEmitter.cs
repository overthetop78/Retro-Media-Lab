using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal sealed class M68000X64RefreshEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public void Guard(List<int> denied)
    {
        code.Load(R.R13, 4096, R.R8, W.Dword);
        code.Test(R.R8, R.R8, W.Dword); denied.Add(code.ReserveJump(C.Zero));
    }
    // R13 is the direct-memory table and its refresh metadata. Clobbers R8/R9.
    public void EmitSkip(int advance)
    {
        if (advance == 0) return;
        code.Load(R.R13, 4096, R.R8, W.Dword);
        code.BinaryImmediate(A.Compare, (int)Cpu68000NativeRefreshMode.StableDuringNativeExecution, R.R8, W.Dword);
        int local = code.ReserveJump(C.NotZero);
        code.Load(R.R13, 4104, R.R8, W.Dword); code.Test(R.R8, R.R8, W.Dword);
        int noExternal = code.ReserveJump(C.Zero); int advanceExternal = code.ReserveJump();
        code.PatchJump(local, code.Position);
        _context.LoadState(M68000StateField.cycles, R.R8); _context.LoadState(M68000StateField.refresh_cycles, R.R9);
        code.Binary(A.Compare, R.R9, R.R8, W.Dword); int noLocal = code.ReserveJump(C.Less);
        code.PatchJump(advanceExternal, code.Position);
        _context.AddStateImmediate(M68000StateField.refresh_cycles, advance);
        code.PatchJump(noExternal, code.Position); code.PatchJump(noLocal, code.Position);
    }
}

