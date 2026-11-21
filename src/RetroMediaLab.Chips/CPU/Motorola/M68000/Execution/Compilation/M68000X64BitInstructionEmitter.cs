using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>BlastEm translate_m68k_bit using the host carry for the tested bit.</summary>
internal sealed class M68000X64BitInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsBit(O operation) => operation is O.BitTest or O.BitChange or O.BitClear or O.BitSet;

    // Value/result in R10, bit number in bit. Clobbers R8/R9/R11, leaves R15 EA intact.
    public void Emit(O operation, M68000OperandSize size, R bit, int multiplier, int ratio, int shift)
    {
        if (!IsBit(operation)) throw new ArgumentException("Not a bit instruction.", nameof(operation));
        code.Move(bit, R.R9, W.Dword);
        code.BinaryImmediate(A.And, size == M68000OperandSize.Long ? 31 : 7, R.R9, W.Dword);
        if (size == M68000OperandSize.Long && operation != O.BitTest)
        {
            code.BinaryImmediate(A.Compare, 16, R.R9, W.Dword); int lower = code.ReserveJump(C.Carry);
            int extra = unchecked((int)((long)(2 * multiplier) * ratio >> shift));
            _context.AddStateImmediate(M68000StateField.cycles, extra);
            code.PatchJump(lower, code.Position);
        }
        code.BitRegister(operation switch { O.BitTest => X64Bit.Test, O.BitChange => X64Bit.Complement, O.BitClear => X64Bit.Reset, _ => X64Bit.Set }, R.R9, R.R10, W.Dword);
        _context.ZeroFlagFromCarry();
    }
}

