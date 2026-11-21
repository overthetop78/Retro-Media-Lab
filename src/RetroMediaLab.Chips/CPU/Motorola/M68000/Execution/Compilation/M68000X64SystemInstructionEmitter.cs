using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;
using K = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Cpu68000NativeControl;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// Native system-instruction sequencing. Shared status/exception services use
/// the same core and bus; no opcode is redispatched to an instruction handler.
/// </summary>
internal sealed class M68000X64SystemInstructionEmitter(X64Emitter code, Cpu68000Configuration configuration,
    int ratio, int shift, List<int> faults)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    public static bool HasBody(in M68000DecodedInstruction instruction) => instruction.Status == M68000DecodeStatus.Decoded
        && instruction.Operation is O.MoveStatus or O.OrSr or O.AndSr or O.ExclusiveOrSr or O.MoveUserStack or O.Reset or O.Stop
            or O.ReturnException or O.ReturnCondition or O.TrapOverflow or O.Trap or O.ALineTrap or O.FLineTrap or O.IllegalInstruction or O.CheckBounds;
    public static bool Privileged(O operation) => operation is O.MoveStatus or O.OrSr or O.AndSr or O.ExclusiveOrSr
        or O.MoveUserStack or O.Reset or O.Stop or O.ReturnException;
    private void Control(K operation) => new M68000X64BusEmitter(code, faults).EmitControl(operation);
    private void Trap(uint vector)
    {
        code.MoveImmediate(vector, R.R14, W.Dword); Control(K.Trap);
    }
    private void AddCycles(int clocks) => _context.AddStateImmediate(M68000StateField.cycles,
        unchecked((int)((long)(clocks * configuration.ClockMultiplier) * ratio >> shift)));
    public void Emit(in M68000DecodedInstruction instruction, M68000X64MemoryInstructionEmitter memory, int nextOffset)
    {
        var operation = instruction.Operation;
        int denied = -1, completed = -1;
        if (Privileged(operation))
        {
            _context.LoadState(M68000StateField.s_flag, R.R8); code.Test(R.R8, R.R8, W.Dword);
            denied = code.ReserveJump(C.Zero);
            // Privilege is checked before extension consumption in the existing
            // core. The block has fetched only the opcode for this path.
            if (instruction.NextAddress != unchecked(instruction.Address + 2))
            {
                _context.StoreStateImmediate(M68000StateField.pc, instruction.NextAddress);
                if (configuration.Prefetch)
                {
                    _context.StoreStateImmediate(M68000StateField.pref_addr, instruction.NextAddress);
                    _context.LoadInstructionWord(nextOffset, R.R8);
                    _context.StoreState(M68000StateField.pref_data, R.R8);
                }
            }
        }
        switch (operation)
        {
            case O.MoveStatus:
                memory.LoadSystemSource(instruction, false); Control(K.SetStatus); break;
            case O.OrSr:
            case O.AndSr:
            case O.ExclusiveOrSr:
                new M68000X64StatusInstructionEmitter(code).ReadStatus();
                code.BinaryImmediate(operation == O.OrSr ? A.Or : operation == O.AndSr ? A.And : A.Xor,
                    unchecked((int)instruction.Source.Immediate), R.R10, W.Dword);
                code.Move(R.R10, R.R14, W.Dword); Control(K.SetStatus); break;
            case O.MoveUserStack:
                if (instruction.Source.Mode == M68000OperandMode.AddressRegister)
                {
                    _context.LoadRegister(8 + instruction.Source.Register, R.R14, M68000OperandSize.Long); Control(K.WriteUserStack);
                }
                else
                {
                    Control(K.ReadUserStack); _context.StoreRegister(8 + instruction.Destination.Register, R.R10, M68000OperandSize.Long);
                }
                break;
            case O.Reset:
                AddCycles(132); _context.AddStateImmediate(M68000StateField.refresh_cycles, configuration.ResetRefreshAdvance); break;
            case O.Stop:
                code.MoveImmediate(instruction.Source.Immediate, R.R14, W.Dword);
                code.BinaryMemoryImmediate(A.Or, 1, R.Rdx, M68000X64ContextEmitter.StateOffset(M68000StateField.stopped), W.Dword);
                Control(K.SetStatus);
                _context.LoadState(M68000StateField.stopped, R.R8); code.Test(R.R8, R.R8, W.Dword);
                int running = code.ReserveJump(C.Zero);
                _context.LoadState(M68000StateField.cycle_end, R.R8);
                code.BinaryImmediate(A.Subtract, 4 * configuration.ClockMultiplier, R.R8, W.Dword);
                _context.StoreState(M68000StateField.cycles, R.R8); code.PatchJump(running, code.Position); break;
            case O.ReturnException:
                memory.PopSystem(M68000OperandSize.Word); code.Move(R.R10, R.R14, W.Dword);
                memory.PopSystem(M68000OperandSize.Long); _context.StoreState(M68000StateField.pc, R.R10);
                Control(K.SetStatus);
                if (configuration.AddressErrors) _context.StoreStateImmediate(M68000StateField.instr_mode, 0);
                _context.StoreStateImmediate(M68000StateField.run_mode, 0); break;
            case O.ReturnCondition:
                memory.PopSystem(M68000OperandSize.Word);
                new M68000X64StatusInstructionEmitter(code).WriteCondition(R.R10);
                memory.PopSystem(M68000OperandSize.Long); _context.StoreState(M68000StateField.pc, R.R10); break;
            case O.Trap: Trap(32 + instruction.Source.Immediate); break;
            case O.TrapOverflow:
                _context.LoadState(M68000StateField.v_flag, R.R8); code.TestImmediate(128, R.R8, W.Dword);
                int clear = code.ReserveJump(C.Zero); Trap(7); int trapped = code.ReserveJump();
                code.PatchJump(clear, code.Position); AddCycles(4); code.PatchJump(trapped, code.Position); break;
            case O.ALineTrap: Control(K.ALine); break;
            case O.FLineTrap: Control(K.FLine); break;
            case O.IllegalInstruction: Control(K.IllegalInstruction); break;
            case O.CheckBounds:
                memory.LoadSystemSource(instruction, true);
                _context.LoadRegister(instruction.Destination.Register, R.R8, M68000OperandSize.Word, true);
                code.Move(R.R8, R.R9, W.Dword); code.BinaryImmediate(A.And, 65535, R.R9, W.Dword);
                _context.StoreState(M68000StateField.not_z_flag, R.R9);
                _context.StoreStateImmediate(M68000StateField.v_flag, 0); _context.StoreStateImmediate(M68000StateField.c_flag, 0);
                code.Test(R.R8, R.R8, W.Dword); int negative = code.ReserveJump(C.Sign);
                code.Binary(A.Compare, R.R14, R.R8, W.Dword); int above = code.ReserveJump(C.Greater);
                AddCycles(10); int bounded = code.ReserveJump();
                code.PatchJump(negative, code.Position); _context.StoreStateImmediate(M68000StateField.n_flag, 128); AddCycles(2);
                code.PatchJump(above, code.Position); Trap(6); code.PatchJump(bounded, code.Position); break;
            default: throw new ArgumentException("Unsupported native system instruction.", nameof(instruction));
        }
        if (denied >= 0)
        {
            completed = code.ReserveJump(); code.PatchJump(denied, code.Position);
            Control(K.PrivilegeViolation); code.PatchJump(completed, code.Position);
        }
    }
}

