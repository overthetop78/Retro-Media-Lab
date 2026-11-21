using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>ADDX/SUBX/NEGX with native carry input and cumulative guest Z.</summary>
internal sealed class M68000X64ExtendInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool IsExtend(O operation) => operation is O.AddExtend or O.SubtractExtend or O.NegateExtend;

    // R8 source, R9 destination; R10 result. Does not touch R14/R15 holding EAs.
    public void Emit(O operation, M68000OperandSize size)
    {
        _context.LoadState(M68000StateField.not_z_flag, R.R11);
        if (operation == O.NegateExtend) code.MoveImmediate(0, R.R10, W.Dword);
        else code.Move(R.R9, R.R10, W.Dword);
        code.BitMemoryImmediate(X64Bit.Test, 8, R.Rdx,
            M68000X64ContextEmitter.StateOffset(M68000StateField.x_flag), W.Dword);
        code.Binary(operation == O.AddExtend ? A.AddCarry : A.SubtractBorrow,
            operation == O.NegateExtend ? R.R9 : R.R8, R.R10, M68000X64ContextEmitter.Width(size));
        _context.CaptureArithmeticFlags(true);
        code.BinaryStore(A.Or, R.R11, R.Rdx, M68000X64ContextEmitter.StateOffset(M68000StateField.not_z_flag), W.Dword);
        if (operation == O.NegateExtend)
        {
            // The established handler uses source & result for NEGX's V bit,
            // including with X set; do not substitute host SBB overflow here.
            code.Move(R.R9, R.Rax, W.Dword); code.Binary(A.And, R.R10, R.Rax, W.Dword);
            if (size != M68000OperandSize.Byte) code.Shift(S.Right, size == M68000OperandSize.Word ? (byte)8 : (byte)24, R.Rax, W.Dword);
            _context.StoreState(M68000StateField.v_flag, R.Rax);
        }
    }
}

