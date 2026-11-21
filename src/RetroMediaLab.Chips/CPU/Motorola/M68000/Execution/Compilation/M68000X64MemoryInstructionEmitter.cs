using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;
using M = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000OperandMode;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// Direct data accesses using the native bank-table strategy of the reference
/// backend. Guards run before guest-state changes. R13 is the read/write table;
/// R14/R15 preserve the source value and destination EA. The block saves them.
/// </summary>
internal sealed partial class M68000X64MemoryInstructionEmitter(X64Emitter code, int multiplier = 1, int ratio = 1, int shift = 0,
    Cpu68000Configuration? configuration = null, bool protectedWrites = false, bool busCallbacks = false, List<int>? busFaults = null)
{
    private readonly M68000X64ContextEmitter _context = new(code);
    private readonly Cpu68000Configuration _configuration = configuration ?? new(multiplier);
    private static bool Memory(M68000DecodedOperand operand) => operand.Mode is M.Indirect or M.PostIncrement or M.PreDecrement
        or M.Displacement or M.IndexedDisplacement or M.AbsoluteShort or M.AbsoluteLong or M.PcDisplacement or M.PcIndexedDisplacement;
    private static bool Register(M68000DecodedOperand operand) => operand.Mode is M.DataRegister or M.AddressRegister;
    private static bool Value(M68000DecodedOperand operand) => Memory(operand) || Register(operand) || operand.Mode == M.Immediate;
    private static int Index(M68000DecodedOperand operand) => operand.Register + (operand.Mode == M.AddressRegister ? 8 : 0);
    private static int Step(M68000DecodedOperand operand, M68000OperandSize size) =>
        size == M68000OperandSize.Byte ? operand.Register == 7 ? 2 : 1 : size == M68000OperandSize.Word ? 2 : 4;
    public static bool CanEmit(in M68000DecodedInstruction instruction)
    {
        if (M68000X64SystemInstructionEmitter.HasBody(instruction)) return true;
        if (CanEmitTransfer(instruction)) return true;
        if (M68000X64RotateInstructionEmitter.CanEmitRegister(instruction)) return true;
        if (M68000X64ShiftInstructionEmitter.CanEmit(instruction)) return true;
        if (instruction.Status == M68000DecodeStatus.Decoded && instruction.Operation == O.SetCondition) return Memory(instruction.Destination);
        if (instruction.Status == M68000DecodeStatus.Decoded && instruction.Operation is O.BranchSubroutine or O.ReturnSubroutine) return true;
        if (instruction.Status != M68000DecodeStatus.Decoded || instruction.Size is not { } size || size > M68000OperandSize.Long) return false;
        var operation = instruction.Operation;
        if (operation == O.NegateDecimal) return Memory(instruction.Destination);
        if (operation is O.AddDecimal or O.SubtractDecimal) return Memory(instruction.Source) && Memory(instruction.Destination);
        if (operation == O.TestAndSet || M68000X64ShiftInstructionEmitter.IsShift(operation)) return Memory(instruction.Destination);
        if (M68000X64RotateInstructionEmitter.IsRotate(operation)) return Memory(instruction.Destination);
        if (operation == O.NegateExtend) return Memory(instruction.Destination);
        if (operation is O.AddExtend or O.SubtractExtend)
            return Memory(instruction.Source) && Memory(instruction.Destination);
        if (operation == O.MoveFromStatus) return Memory(instruction.Destination);
        if (operation == O.MoveCondition) return Memory(instruction.Source);
        if (M68000X64DivideInstructionEmitter.IsDivide(operation))
            return Value(instruction.Source) && instruction.Destination.Mode == M.DataRegister;
        if (M68000X64MultiplyInstructionEmitter.IsMultiply(operation))
            return Value(instruction.Source) && instruction.Destination.Mode == M.DataRegister;
        if (M68000X64BitInstructionEmitter.IsBit(operation))
            return instruction.Source.Mode is M.DataRegister or M.ImmediateWord && Memory(instruction.Destination);
        if (operation == O.Test) return Memory(instruction.Source);
        if (operation is O.Clear or O.Complement or O.Negate) return Memory(instruction.Destination);
        if (operation is not (O.Move or O.Add or O.Subtract or O.Compare or O.And or O.Or or O.ExclusiveOr)) return false;
        if (!Value(instruction.Source) || !(Memory(instruction.Destination) || Register(instruction.Destination))) return false;
        if (instruction.Destination.Mode == M.AddressRegister && operation is not (O.Move or O.Add or O.Subtract or O.Compare)) return false;
        return Memory(instruction.Source) || Memory(instruction.Destination);
    }
    private void AdjustedRegister(int index, R destination, M68000OperandSize size, bool signed, int[]? deltas)
    {
        int delta = deltas?[index] ?? 0;
        if (delta == 0) { _context.LoadRegister(index, destination, size, signed); return; }
        _context.LoadRegister(index, destination, M68000OperandSize.Long);
        code.BinaryImmediate(A.Add, delta, destination, W.Dword);
        if (size != M68000OperandSize.Long)
            code.MoveExtend(destination, destination, size == M68000OperandSize.Word ? W.Word : W.Byte, W.Dword, signed);
    }
    // Computes an unmasked 32-bit guest address in R8, using R9 as scratch.
    // Deltas model source EA side effects during preflight without changing state.
    private void Address(M68000DecodedOperand operand, M68000OperandSize size, uint instructionAddress, bool apply, int[]? deltas = null)
    {
        switch (operand.Mode)
        {
            case M.AbsoluteShort:
            case M.AbsoluteLong:
                code.MoveImmediate(operand.Immediate, R.R8, W.Dword); return;
            case M.PcDisplacement:
                code.MoveImmediate(unchecked(instructionAddress + 2 + (uint)operand.Displacement), R.R8, W.Dword); return;
            case M.PcIndexedDisplacement:
                code.MoveImmediate(unchecked(instructionAddress + 2), R.R8, W.Dword); break;
            default:
                AdjustedRegister(8 + operand.Register, R.R8, M68000OperandSize.Long, false, deltas); break;
        }
        if (operand.Mode == M.PreDecrement)
        {
            code.BinaryImmediate(A.Subtract, Step(operand, size), R.R8, W.Dword);
            if (apply) _context.StoreRegister(8 + operand.Register, R.R8, M68000OperandSize.Long);
        }
        if (operand.Mode == M.PostIncrement && apply)
        {
            code.Move(R.R8, R.R9, W.Dword); code.BinaryImmediate(A.Add, Step(operand, size), R.R9, W.Dword);
            _context.StoreRegister(8 + operand.Register, R.R9, M68000OperandSize.Long);
        }
        if (operand.Mode is M.IndexedDisplacement or M.PcIndexedDisplacement)
        {
            int index = operand.IndexDescriptor >> 1;
            AdjustedRegister(index, R.R9, (operand.IndexDescriptor & 1) != 0 ? M68000OperandSize.Long : M68000OperandSize.Word, true, deltas);
            code.Binary(A.Add, R.R9, R.R8, W.Dword);
        }
        if (operand.Mode is M.Displacement or M.IndexedDisplacement or M.PcIndexedDisplacement)
            code.BinaryImmediate(A.Add, operand.Displacement, R.R8, W.Dword);
    }
    private void Guard(R address, M68000OperandSize size, bool write, bool addressErrors, List<int> denied, bool requireDirect = false)
    {
        if (size != M68000OperandSize.Byte && addressErrors && (!busCallbacks || requireDirect))
        {
            _context.LoadState(M68000StateField.aerr_enabled, R.R9);
            code.Test(R.R9, R.R9, W.Dword); int disabled = code.ReserveJump(C.Zero);
            code.TestImmediate(1, address, W.Dword); denied.Add(code.ReserveJump(C.NotZero));
            code.PatchJump(disabled, code.Position);
        }
        int pages = size == M68000OperandSize.Long ? 2 : 1;
        for (int part = 0; part < pages; part++)
        {
            code.Move(address, R.R9, W.Dword);
            if (part != 0) code.BinaryImmediate(A.Add, 2, R.R9, W.Dword);
            code.Shift(X64Shift.Right, 16, R.R9, W.Dword); code.BinaryImmediate(A.And, 255, R.R9, W.Dword);
            code.LoadIndexed(R.R13, R.R9, 3, write ? 2048 : 0, R.R10, W.Qword);
            code.Test(R.R10, R.R10, W.Qword);
            if (!busCallbacks || requireDirect) denied.Add(code.ReserveJump(C.Zero));
            else
            {
                int mapped = code.ReserveJump(C.NotZero);
                int function = (write ? 2 : 0) + (size == M68000OperandSize.Byte ? 0 : 1);
                code.Load(R.R13, M68000X64MemoryMap.BusFunctionsOffset + function * 8, R.R10, W.Qword);
                code.Test(R.R10, R.R10, W.Qword); denied.Add(code.ReserveJump(C.Zero));
                code.PatchJump(mapped, code.Position);
            }
        }
    }
    public void EmitPreflight(in M68000DecodedInstruction instruction, bool addressErrors, List<int> denied)
    {
        if (!CanEmit(instruction)) throw new ArgumentException("Unsupported native memory instruction.", nameof(instruction));
        if (busCallbacks)
        {
            // The native access itself now handles both direct RAM and bus
            // handlers, including address faults at the actual access point.
            // Do not compute and probe both EAs a second time before it.
            bool countedShift = (M68000X64ShiftInstructionEmitter.CanEmit(instruction)
                || M68000X64RotateInstructionEmitter.CanEmitRegister(instruction)) && instruction.Source.Mode == M.DataRegister;
            bool longMovem = instruction.Operation == O.MoveMultiple && instruction.Size == M68000OperandSize.Long;
            if (_configuration.RefreshSkipAdvance != 0 && (countedShift || longMovem || M68000X64DivideInstructionEmitter.IsDivide(instruction.Operation)))
                new M68000X64RefreshEmitter(code).Guard(denied);
            return;
        }
        code.Test(R.R13, R.R13, W.Qword); denied.Add(code.ReserveJump(C.Zero));
        if (CanEmitTransfer(instruction)) { PreflightTransfer(instruction, addressErrors, denied); return; }
        if (M68000X64RotateInstructionEmitter.CanEmitRegister(instruction))
        {
            if (_configuration.RefreshSkipAdvance != 0 && instruction.Source.Mode == M.DataRegister)
                new M68000X64RefreshEmitter(code).Guard(denied);
            return;
        }
        if (M68000X64ShiftInstructionEmitter.CanEmit(instruction))
        {
            if (_configuration.RefreshSkipAdvance != 0 && instruction.Source.Mode == M.DataRegister)
                new M68000X64RefreshEmitter(code).Guard(denied);
            return;
        }
        if (instruction.Operation == O.SetCondition)
        {
            Address(instruction.Destination, M68000OperandSize.Byte, instruction.Address, false);
            Guard(R.R8, M68000OperandSize.Byte, true, addressErrors, denied); return;
        }
        if (instruction.Operation is O.BranchSubroutine or O.ReturnSubroutine)
        {
            _context.LoadRegister(15, R.R8, M68000OperandSize.Long);
            bool push = instruction.Operation == O.BranchSubroutine;
            if (push) code.BinaryImmediate(A.Subtract, 4, R.R8, W.Dword);
            Guard(R.R8, M68000OperandSize.Long, push, addressErrors, denied);
            return;
        }
        var size = instruction.Size!.Value; int[] deltas = new int[16];
        if (Memory(instruction.Source))
        {
            Address(instruction.Source, size, instruction.Address, false, deltas);
            Guard(R.R8, size, false, addressErrors, denied, M68000X64DivideInstructionEmitter.IsDivide(instruction.Operation));
            if (instruction.Source.Mode is M.PostIncrement or M.PreDecrement)
                deltas[8 + instruction.Source.Register] += Step(instruction.Source, size) * (instruction.Source.Mode == M.PostIncrement ? 1 : -1);
        }
        if (Memory(instruction.Destination))
        {
            uint destinationBase = unchecked(instruction.Address + (instruction.Source.Mode == M.ImmediateWord ? 2u : 0u));
            Address(instruction.Destination, size, destinationBase, false, deltas);
            if (instruction.Operation is not (O.Move or O.MoveFromStatus)) Guard(R.R8, size, false, addressErrors, denied);
            if (instruction.Operation is not (O.Compare or O.BitTest)
                && (instruction.Operation != O.TestAndSet || _configuration.WriteTasMemory)) Guard(R.R8, size, true, addressErrors, denied);
        }
        if (M68000X64DivideInstructionEmitter.IsDivide(instruction.Operation))
        {
            if (_configuration.RefreshSkipAdvance != 0) new M68000X64RefreshEmitter(code).Guard(denied);
            // Read without applying EA changes: a zero divisor must reach the
            // existing trap handler with the complete pre-instruction state.
            if (Memory(instruction.Source))
            {
                Address(instruction.Source, size, instruction.Address, false);
                code.Move(R.R8, R.R15, W.Dword); Read(size); code.Move(R.R10, R.R14, W.Dword);
            }
            else LoadSource(instruction.Source, size, instruction.Address, false);
            code.Test(R.R14, R.R14, W.Word); denied.Add(code.ReserveJump(C.Zero));
        }
    }
    internal static bool MayCallBus(in M68000DecodedInstruction instruction) => Memory(instruction.Source) || Memory(instruction.Destination)
        || M68000X64SystemInstructionEmitter.HasBody(instruction)
        || M68000X64DivideInstructionEmitter.IsDivide(instruction.Operation)
        || instruction.Operation is O.BranchSubroutine or O.ReturnSubroutine or O.JumpSubroutine or O.PushEffectiveAddress or O.Link or O.Unlink;
    // R15 holds the full EA. Lookup produces a page pointer in R11 and offset in R9.
    private void Lookup(bool write, int advance, bool bytes, bool mapped = false)
    {
        code.Move(R.R15, R.R9, W.Dword);
        if (advance != 0) code.BinaryImmediate(A.Add, advance, R.R9, W.Dword);
        code.Move(R.R9, R.Rax, W.Dword); code.Shift(X64Shift.Right, 16, R.Rax, W.Dword);
        code.BinaryImmediate(A.And, 255, R.Rax, W.Dword);
        code.LoadIndexed(R.R13, R.Rax, 3, mapped ? M68000X64MemoryMap.MappedReadOffset : write ? 2048 : 0, R.R11, W.Qword);
        code.BinaryImmediate(A.And, bytes ? 65535 : 65534, R.R9, W.Dword);
        if (bytes) code.BinaryImmediate(A.Xor, 1, R.R9, W.Dword);
    }
    private void Read(M68000OperandSize size, bool mapped = false)
    {
        ReadPart(size == M68000OperandSize.Byte, 0, R.R10, mapped);
        if (size != M68000OperandSize.Long) return;
        code.Shift(X64Shift.Left, 16, R.R10, W.Dword); ReadPart(false, 2, R.R8, mapped);
        code.Binary(A.Or, R.R8, R.R10, W.Dword);
    }
    private List<int> SlowAccesses(bool bytes)
    {
        var slow = new List<int>();
        code.Test(R.R11, R.R11, W.Qword); slow.Add(code.ReserveJump(C.Zero));
        if (!bytes && _configuration.AddressErrors)
        {
            code.TestImmediate(1, R.R15, W.Dword); int even = code.ReserveJump(C.Zero);
            _context.LoadState(M68000StateField.aerr_enabled, R.Rax);
            code.Test(R.Rax, R.Rax, W.Dword); slow.Add(code.ReserveJump(C.NotZero));
            code.PatchJump(even, code.Position);
            // RAX must still be the bank index for protected stores.
            code.Move(R.R15, R.Rax, W.Dword); code.Shift(X64Shift.Right, 16, R.Rax, W.Dword);
            code.BinaryImmediate(A.And, 255, R.Rax, W.Dword);
        }
        return slow;
    }
    private void BusAccess(int function, int advance, R value = R.R10, R result = R.R10) =>
        new M68000X64BusEmitter(code, busFaults ?? throw new InvalidOperationException("Native bus exits are required.")).Emit(function, advance, value, result);
    private void ReadPart(bool bytes, int advance, R result, bool mapped = false)
    {
        Lookup(false, advance, bytes, mapped && busCallbacks);
        var slow = busCallbacks ? mapped ? new List<int>() : SlowAccesses(bytes) : null;
        if (busCallbacks && mapped){code.Test(R.R11,R.R11,W.Qword);slow!.Add(code.ReserveJump(C.Zero));}
        code.MoveImmediate(0, result, W.Dword);
        code.LoadIndexed(R.R11, R.R9, 0, 0, result, bytes ? W.Byte : W.Word);
        if (slow is null) return;
        int done = code.ReserveJump(); foreach (int jump in slow) code.PatchJump(jump, code.Position);
        BusAccess(mapped ? bytes ? 4 : 5 : bytes ? 0 : 1, advance, result: result); code.PatchJump(done, code.Position);
    }
    private void WritePart(bool bytes, int advance, R value)
    {
        Lookup(true, advance, bytes);
        var slow = busCallbacks ? SlowAccesses(bytes) : null;
        // SlowAccesses uses the original EA parity; an added word has the same
        // parity. Restore the advanced bank index before its protection lookup.
        if (busCallbacks && !bytes && _configuration.AddressErrors && advance != 0)
        {
            code.Move(R.R15, R.Rax, W.Dword); code.BinaryImmediate(A.Add, advance, R.Rax, W.Dword);
            code.Shift(X64Shift.Right, 16, R.Rax, W.Dword); code.BinaryImmediate(A.And, 255, R.Rax, W.Dword);
        }
        StorePart(value, bytes ? W.Byte : W.Word, bytes);
        if (slow is null) return;
        int done = code.ReserveJump(); foreach (int jump in slow) code.PatchJump(jump, code.Position);
        BusAccess(bytes ? 2 : 3, advance, value); code.PatchJump(done, code.Position);
    }
    private void Write(M68000OperandSize size, bool lowFirst = false)
    {
        if (size == M68000OperandSize.Long)
        {
            code.Move(R.R10, R.R8, W.Dword); code.Shift(X64Shift.Right, 16, R.R8, W.Dword);
            if (lowFirst) { WritePart(false, 2, R.R10); WritePart(false, 0, R.R8); }
            else { WritePart(false, 0, R.R8); WritePart(false, 2, R.R10); }
        }
        else
        {
            WritePart(size == M68000OperandSize.Byte, 0, R.R10);
        }
    }
    private void StorePart(R value, W width, bool bytes)
    {
        int ignored = -1;
        if (protectedWrites)
        {
            // Lookup leaves the bank index in RAX, pointer in R11, and host
            // offset in R9. Protection uses the guest offset before byte XOR.
            code.LoadIndexed(R.R13, R.Rax, 3, M68000X64MemoryMap.WriteMinimumOffset, R.Rax, W.Dword);
            if (bytes) code.BinaryImmediate(A.Xor, 1, R.R9, W.Dword);
            code.Binary(A.Compare, R.Rax, R.R9, W.Dword); ignored = code.ReserveJump(C.Carry);
            if (bytes) code.BinaryImmediate(A.Xor, 1, R.R9, W.Dword);
        }
        code.StoreIndexed(value, R.R11, R.R9, 0, 0, width);
        if (ignored >= 0) code.PatchJump(ignored, code.Position);
    }
    private void LoadSource(M68000DecodedOperand operand, M68000OperandSize size, uint address, bool signed)
    {
        if (Memory(operand))
        {
            Address(operand, size, address, true); code.Move(R.R8, R.R15, W.Dword); Read(size, operand.Mode is M.PcDisplacement or M.PcIndexedDisplacement);
            code.Move(R.R10, R.R14, W.Dword);
            if (signed && size == M68000OperandSize.Word) code.MoveExtend(R.R14, R.R14, W.Word, W.Dword, true);
        }
        else if (Register(operand)) _context.LoadRegister(Index(operand), R.R14, size, signed);
        else
        {
            uint value = size == M68000OperandSize.Byte ? operand.Immediate & 255 : size == M68000OperandSize.Word ? operand.Immediate & 65535 : operand.Immediate;
            if (signed && size == M68000OperandSize.Word) value = unchecked((uint)(short)value);
            code.MoveImmediate(value, R.R14, W.Dword);
        }
    }
    internal void LoadSystemSource(in M68000DecodedInstruction instruction, bool signed) =>
        LoadSource(instruction.Source, M68000OperandSize.Word, instruction.Address, signed);
    internal void PopSystem(M68000OperandSize size)
    {
        _context.LoadRegister(15, R.R15, M68000OperandSize.Long);
        code.Move(R.R15, R.R8, W.Dword); code.BinaryImmediate(A.Add, size == M68000OperandSize.Word ? 2 : 4, R.R8, W.Dword);
        _context.StoreRegister(15, R.R8, M68000OperandSize.Long); Read(size);
    }
    public void EmitBody(in M68000DecodedInstruction instruction, int nextOffset = 0)
    {
        if (!CanEmit(instruction)) throw new ArgumentException("Unsupported native memory instruction.", nameof(instruction));
        if (M68000X64SystemInstructionEmitter.HasBody(instruction))
        {
            new M68000X64SystemInstructionEmitter(code, _configuration, ratio, shift,
                busFaults ?? throw new InvalidOperationException("Native system exits are required.")).Emit(instruction, this, nextOffset); return;
        }
        if (CanEmitTransfer(instruction)) { EmitTransfer(instruction, nextOffset); return; }
        if (M68000X64RotateInstructionEmitter.CanEmitRegister(instruction))
        {
            new M68000X64RotateInstructionEmitter(code).EmitRegister(instruction, _configuration, ratio, shift); return;
        }
        if (M68000X64ShiftInstructionEmitter.CanEmit(instruction))
        {
            new M68000X64ShiftInstructionEmitter(code).Emit(instruction, _configuration, ratio, shift); return;
        }
        if (instruction.Operation == O.SetCondition)
        {
            new M68000X64ControlInstructionEmitter(code).EmitCondition(instruction.Condition);
            code.Move(R.R8, R.R14, W.Dword); code.Unary(X64Unary.Negate, R.R14, W.Byte);
            Address(instruction.Destination, M68000OperandSize.Byte, instruction.Address, true);
            code.Move(R.R8, R.R15, W.Dword); code.Move(R.R14, R.R10, W.Dword); Write(M68000OperandSize.Byte); return;
        }
        if (instruction.Operation is O.BranchSubroutine or O.ReturnSubroutine)
        {
            // BlastEm m68k_core.c/translate_m68k_bsr and translate_m68k_rts:
            // push the decoded return PC, or pop PC, then transfer control.
            // The block supplies this core's fetch and instruction cycle table.
            _context.LoadRegister(15, R.R15, M68000OperandSize.Long);
            if (instruction.Operation == O.BranchSubroutine)
            {
                code.BinaryImmediate(A.Subtract, 4, R.R15, W.Dword);
                _context.StoreRegister(15, R.R15, M68000OperandSize.Long);
                code.MoveImmediate(instruction.NextAddress, R.R10, W.Dword); Write(M68000OperandSize.Long);
                _context.StoreStateImmediate(M68000StateField.pc, unchecked(instruction.Address + 2 + instruction.Source.Immediate));
            }
            else
            {
                code.Move(R.R15, R.R8, W.Dword); code.BinaryImmediate(A.Add, 4, R.R8, W.Dword);
                _context.StoreRegister(15, R.R8, M68000OperandSize.Long);
                Read(M68000OperandSize.Long); _context.StoreState(M68000StateField.pc, R.R10);
            }
            return;
        }
        var size = instruction.Size!.Value; var operation = instruction.Operation;
        if (M68000X64DivideInstructionEmitter.IsDivide(operation))
        {
            int divided = -1;
            if (busCallbacks)
            {
                LoadSource(instruction.Source, M68000OperandSize.Word, instruction.Address, false);
                code.Test(R.R14, R.R14, W.Word); int nonzero = code.ReserveJump(C.NotZero);
                _context.StoreStateImmediate(M68000StateField.c_flag, 0);
                code.MoveImmediate(5, R.R14, W.Dword);
                new M68000X64BusEmitter(code, busFaults!).EmitControl(Cpu68000NativeControl.Trap);
                divided = code.ReserveJump(); code.PatchJump(nonzero, code.Position);
            }
            // Preflight already loaded the side-effect-free source in R14.
            // Fetch/refresh touch R8/R9 only. Apply the EA update without a
            // second source read before consuming the retained divisor.
            else if (Memory(instruction.Source)) Address(instruction.Source, M68000OperandSize.Word, instruction.Address, true);
            new M68000X64DivideInstructionEmitter(code).Emit(operation, instruction.Destination.Register, _configuration, ratio, shift);
            if (divided >= 0) code.PatchJump(divided, code.Position);
            return;
        }
        if (operation == O.MoveFromStatus)
        {
            new M68000X64StatusInstructionEmitter(code).ReadStatus();
            Address(instruction.Destination, size, instruction.Address, true); code.Move(R.R8, R.R15, W.Dword);
            Write(size); return;
        }
        if (operation == O.MoveCondition)
        {
            LoadSource(instruction.Source, size, instruction.Address, false);
            new M68000X64StatusInstructionEmitter(code).WriteCondition(R.R14); return;
        }
        if (M68000X64MultiplyInstructionEmitter.IsMultiply(operation))
        {
            LoadSource(instruction.Source, M68000OperandSize.Word, instruction.Address, false);
            new M68000X64MultiplyInstructionEmitter(code).Emit(operation, R.R14, instruction.Destination.Register, multiplier, ratio, shift);
            return;
        }
        if (M68000X64BitInstructionEmitter.IsBit(operation))
        {
            LoadSource(instruction.Source, M68000OperandSize.Long, instruction.Address, false);
            uint destinationBase = unchecked(instruction.Address + (instruction.Source.Mode == M.ImmediateWord ? 2u : 0u));
            Address(instruction.Destination, size, destinationBase, true); code.Move(R.R8, R.R15, W.Dword);
            Read(size, instruction.Destination.Mode is M.PcDisplacement or M.PcIndexedDisplacement);
            new M68000X64BitInstructionEmitter(code).Emit(operation, size, R.R14, multiplier, ratio, shift);
            if (operation != O.BitTest) Write(size);
            return;
        }
        bool unary = operation is O.Clear or O.Complement or O.Negate or O.NegateExtend or O.TestAndSet or O.NegateDecimal
            || M68000X64RotateInstructionEmitter.IsRotate(operation) || M68000X64ShiftInstructionEmitter.IsShift(operation);
        bool addressDestination = instruction.Destination.Mode == M.AddressRegister;
        bool signedSource = addressDestination && size == M68000OperandSize.Word && instruction.Variant != M68000InstructionVariant.Quick;
        if (!unary) LoadSource(instruction.Source, size, instruction.Address, signedSource);
        if (operation == O.Move && Memory(instruction.Source) && instruction.Destination.Mode is M.Displacement or M.IndexedDisplacement or M.AbsoluteShort or M.AbsoluteLong)
            CompleteInstructionFetch(instruction.NextAddress, nextOffset);
        if (operation == O.Test)
        {
            _context.LogicalFlags(R.R14, size, R.R11); return;
        }
        if (Memory(instruction.Destination))
        {
            Address(instruction.Destination, size, instruction.Address, true); code.Move(R.R8, R.R15, W.Dword);
            if (operation is not (O.Move or O.Clear)) { Read(size); code.Move(R.R10, R.R9, W.Dword); }
        }
        else if (operation != O.Move)
            _context.LoadRegister(Index(instruction.Destination), R.R9, addressDestination ? M68000OperandSize.Long : size);
        var resultSize = addressDestination ? M68000OperandSize.Long : size;
        if (M68000X64DecimalInstructionEmitter.IsDecimal(operation))
        {
            code.Move(R.R14, R.R8, W.Dword);
            new M68000X64DecimalInstructionEmitter(code).Emit(operation); Write(M68000OperandSize.Byte); return;
        }
        if (operation == O.TestAndSet)
        {
            _context.LogicalFlags(R.R9, M68000OperandSize.Byte, R.R11);
            if (_configuration.WriteTasMemory)
            {
                code.Move(R.R9, R.R10, W.Dword); code.BinaryImmediate(A.Or, 128, R.R10, W.Dword); Write(M68000OperandSize.Byte);
            }
            return;
        }
        if (M68000X64ShiftInstructionEmitter.IsShift(operation))
        {
            code.Move(R.R9, R.R10, W.Dword);
            var shifts = new M68000X64ShiftInstructionEmitter(code);
            shifts.EmitMemoryResult(operation); Write(size); shifts.EmitMemoryFlags(operation); return;
        }
        if (M68000X64RotateInstructionEmitter.IsRotate(operation))
        {
            code.Move(R.R9, R.R10, W.Dword);
            var rotates = new M68000X64RotateInstructionEmitter(code);
            rotates.EmitMemoryResult(operation); Write(size); rotates.EmitMemoryFlags(operation); return;
        }
        if (M68000X64ExtendInstructionEmitter.IsExtend(operation))
        {
            code.Move(R.R14, R.R8, W.Dword);
            new M68000X64ExtendInstructionEmitter(code).Emit(operation, size);
            if (Memory(instruction.Destination)) Write(size);
            else _context.StoreRegister(Index(instruction.Destination), R.R10, size);
            return;
        }
        if (operation == O.Move) code.Move(R.R14, R.R10, W.Dword);
        else if (operation == O.Clear) code.MoveImmediate(0, R.R10, W.Dword);
        else if (operation == O.Complement)
        {
            code.Move(R.R9, R.R10, W.Dword); code.Unary(X64Unary.Complement, R.R10, W.Dword);
            if (size != M68000OperandSize.Long) code.BinaryImmediate(A.And, size == M68000OperandSize.Byte ? 255 : 65535, R.R10, W.Dword);
        }
        else if (operation == O.Negate)
        {
            code.Move(R.R9, R.R10, W.Dword);
            code.Unary(X64Unary.Negate, R.R10, M68000X64ContextEmitter.Width(resultSize));
        }
        else
        {
            code.Move(R.R14, R.R8, W.Dword); code.Move(R.R9, R.R10, W.Dword);
            code.Binary(operation switch { O.Add => A.Add, O.Subtract or O.Compare => A.Subtract, O.And => A.And, O.Or => A.Or, _ => A.Xor }, R.R8, R.R10, M68000X64ContextEmitter.Width(resultSize));
        }
        if (Memory(instruction.Destination) && operation is O.Clear or O.Complement)
        {
            Write(size); _context.LogicalFlags(R.R10, resultSize, R.R11); return;
        }
        if (!addressDestination || operation == O.Compare)
        {
            if (operation is O.Add or O.Subtract or O.Compare or O.Negate)
                _context.CaptureArithmeticFlags(operation != O.Compare);
            else _context.LogicalFlags(R.R10, resultSize, R.R11);
        }
        if (operation == O.Compare) return;
        if (Memory(instruction.Destination)) Write(size, operation == O.Move && instruction.Destination.Mode == M.PreDecrement);
        else _context.StoreRegister(Index(instruction.Destination), R.R10, resultSize);
    }
    private void CompleteInstructionFetch(uint address, int offset)
    {
        _context.ObserveCodeFetch(_configuration.Prefetch ? address : unchecked(address - 2), busFaults!);
        _context.StoreStateImmediate(M68000StateField.pc, address);
        if (!_configuration.Prefetch) return;
        _context.StoreStateImmediate(M68000StateField.pref_addr, address);
        _context.LoadInstructionWord(offset, R.R8);
        _context.StoreState(M68000StateField.pref_data, R.R8);
    }
}

