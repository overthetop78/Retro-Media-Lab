using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>Native MULS/MULU, following BlastEm translate_m68k_mul with this core's variable cycles.</summary>
internal sealed class M68000X64MultiplyInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsMultiply(O operation) => operation is O.MultiplySigned or O.MultiplyUnsigned;

    // Source word in source. Leaves R15 (memory EA) intact; result is written to Dn.
    public void Emit(O operation, R source, int destination, int multiplier, int ratio, int shift)
    {
        if (!IsMultiply(operation)) throw new ArgumentException("Not a multiply instruction.", nameof(operation));
        bool signed = operation == O.MultiplySigned;
        code.MoveExtend(source, R.R11, W.Word, W.Dword, signed);
        code.Move(R.R11, R.R8, W.Dword);
        if (signed)
        {
            code.Shift(S.Left, 1, R.R8, W.Dword);
            code.Binary(A.Xor, R.R11, R.R8, W.Dword);
        }
        code.BinaryImmediate(A.And, 65535, R.R8, W.Dword);
        // Count bits in a 16-bit word without a data-dependent loop or callback.
        code.Move(R.R8, R.R9, W.Dword); code.Shift(S.Right, 1, R.R9, W.Dword);
        code.BinaryImmediate(A.And, 0x5555, R.R9, W.Dword); code.Binary(A.Subtract, R.R9, R.R8, W.Dword);
        code.Move(R.R8, R.R9, W.Dword); code.Shift(S.Right, 2, R.R9, W.Dword);
        code.BinaryImmediate(A.And, 0x3333, R.R9, W.Dword); code.BinaryImmediate(A.And, 0x3333, R.R8, W.Dword);
        code.Binary(A.Add, R.R9, R.R8, W.Dword);
        code.Move(R.R8, R.R9, W.Dword); code.Shift(S.Right, 4, R.R9, W.Dword); code.Binary(A.Add, R.R9, R.R8, W.Dword);
        code.BinaryImmediate(A.And, 0x0f0f, R.R8, W.Dword);
        code.Move(R.R8, R.R9, W.Dword); code.Shift(S.Right, 8, R.R9, W.Dword); code.Binary(A.Add, R.R9, R.R8, W.Dword);
        code.BinaryImmediate(A.And, 31, R.R8, W.Dword);
        code.Shift(S.Left, 1, R.R8, W.Dword); code.BinaryImmediate(A.Add, 38, R.R8, W.Dword);
        code.MultiplyImmediate(multiplier, R.R8, R.R8, W.Dword);
        code.MultiplyImmediate(ratio, R.R8, R.R8, W.Qword);
        if (shift != 0) code.Shift(S.Right, (byte)shift, R.R8, W.Qword);
        _context.AddState(M68000StateField.cycles, R.R8);
        _context.LoadRegister(destination, R.R10, M68000OperandSize.Word, signed);
        code.MultiplySigned(R.R11, R.R10, W.Dword);
        _context.StoreRegister(destination, R.R10, M68000OperandSize.Long);
        _context.LogicalFlags(R.R10, M68000OperandSize.Long, R.R8);
    }
}

