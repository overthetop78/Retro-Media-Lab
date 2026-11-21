using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// Bcc/DBcc translation follows BlastEm m68k_core_x86.c. The adapter retains
/// the existing core's split cycle additions and conditional extension prefetch.
/// No stack operation, peripheral callback or skipped guest cycle is introduced.
/// </summary>
internal sealed class M68000X64ControlInstructionEmitter(X64Emitter code)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool CanEmit(in M68000DecodedInstruction instruction) =>
        instruction.Status == M68000DecodeStatus.Decoded && instruction.Operation is O.BranchCondition or O.DecrementBranch;
    public static uint Target(in M68000DecodedInstruction instruction) => unchecked(instruction.Address + 2 + instruction.Source.Immediate);

    // Produce a boolean in R8 from this core's noncanonical flag payloads.
    public void EmitCondition(byte condition)
    {
        if (condition < 2) { code.MoveImmediate(condition == 0 ? 1 : 0, R.R8, W.Dword); return; }
        M68000StateField flag = condition switch
        {
            2 or 3 or 4 or 5 => M68000StateField.c_flag,
            6 or 7 => M68000StateField.not_z_flag,
            8 or 9 => M68000StateField.v_flag,
            _ => M68000StateField.n_flag
        };
        _context.LoadState(flag, R.R8);
        if (condition >= 12)
        {
            _context.LoadState(M68000StateField.v_flag, R.R9);
            code.Binary(A.Xor, R.R9, R.R8, W.Dword);
        }
        if (condition is not (6 or 7)) code.BinaryImmediate(A.And, condition <= 5 ? 256 : 128, R.R8, W.Dword);
        code.MoveImmediate(0, R.R9, W.Dword);
        code.Test(R.R8, R.R8, W.Dword);
        bool invert = condition is 2 or 4 or 7 or 8 or 10 or 12 or 14;
        code.SetCondition(invert ? C.Zero : C.NotZero, R.R9);
        code.Move(R.R9, R.R8, W.Dword);
        if (condition is 2 or 3 or 14 or 15)
        {
            _context.LoadState(M68000StateField.not_z_flag, R.R9);
            code.MoveImmediate(0, R.R10, W.Dword);
            code.Test(R.R9, R.R9, W.Dword);
            code.SetCondition(condition is 2 or 14 ? C.NotZero : C.Zero, R.R10);
            code.Binary(condition is 2 or 14 ? A.And : A.Or, R.R10, R.R8, W.Dword);
        }
    }

    public void EmitBody(in M68000DecodedInstruction instruction, Cpu68000Configuration configuration,
        int cycleRatio, int cycleShift, int nextOffset)
    {
        if (!CanEmit(instruction)) throw new ArgumentException("Unsupported native control instruction.", nameof(instruction));
        bool decrement = instruction.Operation == O.DecrementBranch;
        bool extension = instruction.ByteLength == 4;
        EmitCondition(instruction.Condition);
        code.Test(R.R8, R.R8, W.Dword);
        int skip = code.ReserveJump(decrement ? C.NotZero : C.Zero);
        int expired = -1;
        if (decrement)
        {
            _context.LoadRegister(instruction.Destination.Register, R.R8, M68000OperandSize.Word);
            code.BinaryImmediate(A.Subtract, 1, R.R8, W.Word);
            _context.StoreRegister(instruction.Destination.Register, R.R8, M68000OperandSize.Word);
            code.BinaryImmediate(A.Compare, 65535, R.R8, W.Word);
            expired = code.ReserveJump(C.Zero);
        }
        if (extension && configuration.Prefetch)
        {
            _context.StoreStateImmediate(M68000StateField.pref_addr, instruction.NextAddress);
            _context.LoadInstructionWord(nextOffset, R.R8);
            _context.StoreState(M68000StateField.pref_data, R.R8);
        }
        _context.StoreStateImmediate(M68000StateField.pc, Target(instruction));
        if (decrement)
        {
            AddCycles(-2); _context.StoreStateImmediate(M68000StateField.poll_detected, 0);
        }
        int done = code.ReserveJump();
        if (decrement)
        {
            code.PatchJump(expired, code.Position); AddCycles(2);
        }
        code.PatchJump(skip, code.Position);
        _context.StoreStateImmediate(M68000StateField.pc, instruction.NextAddress);
        if (!decrement) AddCycles(extension ? 2 : -2);
        code.PatchJump(done, code.Position);

        void AddCycles(int adjustment)
        {
            int cost = unchecked((int)((long)(adjustment * configuration.ClockMultiplier) * cycleRatio >> cycleShift));
            _context.AddStateImmediate(M68000StateField.cycles, cost);
        }
    }
}

