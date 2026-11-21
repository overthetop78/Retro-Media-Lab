using RetroMediaLab.Chips.Common.Compilation.X64;
using System.Buffers;
using System.Buffers.Binary;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal enum M68000X64BlockExit : uint { Boundary, MemoryDenied, CodeChanged, BusFault }

/// <summary>
/// Native straight-line block with direct mapped instruction fetch. The caller
/// resolves the live bus window before entry and handles interrupts/exceptions
/// outside this block. Only side-effect-free code windows are accepted.
/// </summary>
internal sealed class M68000X64Block : IDisposable
{
    private readonly object _sync = new();
    private readonly ReadOnlyMemory<byte> _window;
    private readonly uint _logicalBase;
    private MemoryHandle _pin;
    private bool _pinned, _disposed;
    private X64CodeBuffer? _code;
    private int _linkedEntryOffset;
    public uint StartAddress { get; }
    public uint EndAddress { get; private set; }
    public int InstructionCount { get; private set; }
    private M68000X64Block(ReadOnlyMemory<byte> window, uint logicalBase, uint start)
    { _window = window; _logicalBase = logicalBase; StartAddress = start; EndAddress = start; }

    public bool Matches(ReadOnlyMemory<byte> window, uint logicalBase) => _window.Equals(window) && _logicalBase == logicalBase;
    private int Offset(uint address)
    {
        uint masked = address & 0x00ffffff;
        if ((address & 1) != 0 || masked < _logicalBase || (ulong)(masked - _logicalBase) + 2 > (ulong)_window.Length)
            throw new WindowBoundary();
        return checked((int)(masked - _logicalBase));
    }
    private ushort Read(uint address) => BinaryPrimitives.ReadUInt16LittleEndian(_window.Span[Offset(address)..]);

    public static unsafe M68000X64Block? TryCompile(ReadOnlyMemory<byte> window, uint logicalBase, uint start,
        Cpu68000Configuration configuration, ReadOnlySpan<ushort> instructionCycles,
        int cycleRatio = 1, int cycleShift = 0, int maximumInstructions = 64, bool directMemory = false, bool linkBlocks = false,
        bool keepHostState = true, bool protectedWrites = false, bool busCallbacks = false, Func<ushort, bool>? isIllegalOpcode = null,
        bool delayedInstruction = false, ushort? initialOpcode = null, bool cacheGuestRegisters = false,
        Func<uint, nint>? getLinkSlot = null, bool immutableCode = false, bool observeInstructionFetches = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (logicalBase > 0xffffff || (logicalBase & 1) != 0) throw new ArgumentOutOfRangeException(nameof(logicalBase));
        if (instructionCycles.Length != 65536) throw new ArgumentException("The existing instruction timing table is required.", nameof(instructionCycles));
        ArgumentOutOfRangeException.ThrowIfLessThan(cycleRatio, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(cycleShift);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cycleShift, 31);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumInstructions, 1);
        if (delayedInstruction && (maximumInstructions != 1 || linkBlocks))
            throw new ArgumentException("A delayed instruction must execute exactly once without native chaining.");
        if (cacheGuestRegisters && (!keepHostState || linkBlocks && !directMemory))
            throw new ArgumentException("Guest register caching requires a host frame and a mapped link table.");
        if (observeInstructionFetches && (!busCallbacks || !directMemory))
            throw new ArgumentException("Observed code requires native bus services.");
        var block = new M68000X64Block(window, logicalBase, start);
        bool published = false;
        try
        {
            block._pin = window.Pin(); block._pinned = true;
            var code = new X64Emitter { CacheExecutionState = keepHostState, CachePrefetch = keepHostState && configuration.Prefetch,
                CacheGuestRegisters = cacheGuestRegisters, ReadOnlyDataBase = (long)block._pin.Pointer,
                ObserveInstructionFetches = observeInstructionFetches };
            var context = new M68000X64ContextEmitter(code);
            var exits = new List<int>();
            var codeChanged = new List<int>(); var memoryDenied = new List<int>();
            var busFaults = new List<int>(); var busBoundaries = new List<int>();
            code.Push(R.R12);
            if (directMemory)
            {
                code.Push(R.R13); code.Push(R.R14); code.Push(R.R15);
                code.Move(R.R8, R.R13, W.Qword);
            }
            if (cacheGuestRegisters)
            {
                code.Push(R.Rbx); code.Push(R.Rbp); code.Push(R.Rsi); code.Push(R.Rdi);
                if (linkBlocks) code.Store(R.R9, R.R13, M68000X64MemoryMap.LinkRootOffset, W.Qword);
                context.InitializeHostState();
            }
            else
            {
                if (linkBlocks) { code.Push(R.Rbx); code.Move(R.R9, R.Rbx, W.Qword); }
                if (keepHostState)
                {
                    code.Push(R.Rbp); code.Push(R.Rsi);
                    if (configuration.Prefetch) code.Push(R.Rdi);
                    context.InitializeHostState();
                }
            }
            // Linked entries share their owner's frame and live CPU context.
            // Only the initial managed entry saves registers/loads the state.
            int frameBytes = code.StackBytes;
            block._linkedEntryOffset = code.Position;
            if (!cacheGuestRegisters) code.MoveImmediate((long)block._pin.Pointer, R.R12, W.Qword);
            if (!delayedInstruction)
            {
                context.LoadState(M68000StateField.stopped, R.R8);
                code.BinaryImmediate(A.Compare, 0, R.R8, W.Dword); exits.Add(code.ReserveJump(C.NotZero));
            }
            uint address = start;
            uint preferredLinkTarget = start;
            var labels = new Dictionary<uint, int>();
            List<uint>? branchTargets = getLinkSlot is null ? null : [];
            bool verifyPc = true;
            for (int index = 0; index < maximumInstructions; index++)
            {
                M68000DecodedInstruction instruction;
                int[] offsets; ushort[] words; int nextOffset = 0, fetchOffset = 0;
                try
                {
                    instruction = M68000InstructionDecoder.Decode(address,
                        a => a == start && initialOpcode.HasValue ? initialOpcode.Value : block.Read(a));
                    if (instruction.Status != M68000DecodeStatus.Decoded)
                    {
                        if (!busCallbacks || isIllegalOpcode?.Invoke(instruction.Opcode) != true) break;
                        instruction = new(M68000DecodeStatus.Decoded, address, unchecked(address + 2), instruction.Opcode,
                            M68000NativeOperation.IllegalInstruction, M68000InstructionVariant.Normal, null, 0,
                            new(M68000OperandMode.Unused), new(M68000OperandMode.Unused));
                    }
                    int count = checked((int)instruction.ByteLength / 2);
                    offsets = new int[count]; words = new ushort[count];
                    for (int word = 0; word < count; word++)
                    {
                        uint position = unchecked(address + (uint)(2 * word));
                        offsets[word] = block.Offset(position); words[word] = block.Read(position);
                    }
                    if (configuration.Prefetch) nextOffset = block.Offset(instruction.NextAddress);
                    if (configuration.Prefetch) fetchOffset = block.Offset(unchecked(address + 2));
                }
                catch (WindowBoundary) { break; }
                var body = new X64Emitter { CacheExecutionState = code.CacheExecutionState, CachePrefetch = code.CachePrefetch,
                    CacheGuestRegisters = cacheGuestRegisters, ReadOnlyDataBase = code.ReadOnlyDataBase,
                    ObserveInstructionFetches = observeInstructionFetches };
                bool systemBody = M68000X64SystemInstructionEmitter.HasBody(instruction);
                bool memoryBody = directMemory && M68000X64MemoryInstructionEmitter.CanEmit(instruction) && (!systemBody || busCallbacks);
                bool controlBody = M68000X64ControlInstructionEmitter.CanEmit(instruction);
                if (!memoryBody && !controlBody && !new M68000X64InstructionEmitter(body, configuration.ClockMultiplier, cycleRatio, cycleShift, configuration).TryEmitBody(instruction)) break;
                labels[address] = code.Position;

                // Boundary checks do not change guest state. A stale opcode or
                // extension returns before fetching any part of that instruction.
                if (!delayedInstruction)
                {
                    context.LoadState(M68000StateField.cycles, R.R8);
                    context.LoadState(M68000StateField.cycle_end, R.R9);
                    code.Binary(A.Compare, R.R9, R.R8, W.Dword); exits.Add(code.ReserveJump(C.NoCarry));
                }
                if (verifyPc)
                {
                    context.LoadState(M68000StateField.pc, R.R8);
                    code.BinaryImmediate(A.Compare, unchecked((int)address), R.R8, W.Dword); exits.Add(code.ReserveJump(C.NotZero));
                }
                // Keep the entry opcode guard: an already prefetched opcode
                // can belong to an earlier mapping even when this ROM is stable.
                // Later ROM words are protected by the owner's version barrier.
                if (!immutableCode || index == 0)
                {
                    if (configuration.Prefetch)
                    {
                        context.LoadState(M68000StateField.pref_addr, R.R8);
                        code.BinaryImmediate(A.Compare, unchecked((int)address), R.R8, W.Dword);
                        int uncached = code.ReserveJump(C.NotZero);
                        context.LoadState(M68000StateField.pref_data, R.R8);
                        int ready = code.ReserveJump();
                        code.PatchJump(uncached, code.Position);
                        context.LoadInstructionWord(offsets[0], R.R8);
                        code.PatchJump(ready, code.Position);
                    }
                    else context.LoadInstructionWord(offsets[0], R.R8);
                    code.BinaryImmediate(A.Compare, instruction.Opcode, R.R8, W.Dword); codeChanged.Add(code.ReserveJump(C.NotZero));
                }
                for (int word = 1; !immutableCode && word < words.Length; word++)
                {
                    context.LoadInstructionWord(offsets[word], R.R8);
                    code.BinaryImmediate(A.Compare, words[word], R.R8, W.Dword); codeChanged.Add(code.ReserveJump(C.NotZero));
                }
                if (observeInstructionFetches && !delayedInstruction && configuration.TrackInstructionAddress)
                    context.StoreStateImmediate(M68000StateField.prev_pc, address);
                context.ObserveCodeFetch(address, busFaults);
                if (memoryBody)
                    new M68000X64MemoryInstructionEmitter(code, configuration.ClockMultiplier, cycleRatio, cycleShift, configuration, protectedWrites, busCallbacks, busFaults).EmitPreflight(instruction, configuration.AddressErrors, memoryDenied);
                if (!observeInstructionFetches && !delayedInstruction && configuration.TrackInstructionAddress)
                    context.StoreStateImmediate(M68000StateField.prev_pc, address);
                context.StoreStateImmediate(M68000StateField.ir, instruction.Opcode);
                if (!delayedInstruction && configuration.RefreshPeriod > 0)
                {
                    context.LoadState(M68000StateField.cycles, R.R8);
                    context.LoadState(M68000StateField.refresh_cycles, R.R9);
                    code.Binary(A.Compare, R.R9, R.R8, W.Dword);
                    int noRefresh = code.ReserveJump(C.Less);
                    code.Move(R.R8, R.R9, W.Dword);
                    code.BinaryImmediate(A.Add, configuration.RefreshPeriod, R.R9, W.Dword);
                    context.StoreState(M68000StateField.refresh_cycles, R.R9);
                    code.BinaryImmediate(A.Add, configuration.RefreshStall, R.R8, W.Dword);
                    context.StoreState(M68000StateField.cycles, R.R8);
                    if (instruction.ByteLength > 2) context.ObserveCodeFetch(unchecked(address + 2), busFaults);
                    code.PatchJump(noRefresh, code.Position);
                }
                bool opcodeOnly = controlBody || instruction.Operation == M68000NativeOperation.Link
                    || systemBody && M68000X64SystemInstructionEmitter.Privileged(instruction.Operation);
                // MOVE reads its source before consuming a destination extension.
                // Devices observe the intermediate PC/prefetch during that read.
                int deferredBytes = instruction.Operation == M68000NativeOperation.Move
                    && instruction.Source.Mode is not (M68000OperandMode.DataRegister or M68000OperandMode.AddressRegister or M68000OperandMode.Immediate or M68000OperandMode.ImmediateWord)
                    ? instruction.Destination.Mode switch
                    {
                        M68000OperandMode.Displacement or M68000OperandMode.IndexedDisplacement or M68000OperandMode.AbsoluteShort => 2,
                        M68000OperandMode.AbsoluteLong => 4, _ => 0
                    } : 0;
                uint fetchedAddress = opcodeOnly ? unchecked(address + 2) : unchecked(instruction.NextAddress - (uint)deferredBytes);
                context.StoreStateImmediate(M68000StateField.pc, fetchedAddress);
                if (configuration.Prefetch)
                {
                    context.StoreStateImmediate(M68000StateField.pref_addr, fetchedAddress);
                    if (immutableCode)
                    {
                        // This fetch precedes all data callbacks in the body.
                        // Its ROM value is protected by the entry revision;
                        // deferred fetches in the body still read live memory.
                        context.StoreStateImmediate(M68000StateField.pref_data, block.Read(fetchedAddress));
                    }
                    else
                    {
                        context.LoadInstructionWord(opcodeOnly ? fetchOffset : nextOffset - deferredBytes, R.R8);
                        context.StoreState(M68000StateField.pref_data, R.R8);
                    }
                }
                if (controlBody) new M68000X64ControlInstructionEmitter(code).EmitBody(instruction, configuration, cycleRatio, cycleShift, nextOffset);
                else if (memoryBody) new M68000X64MemoryInstructionEmitter(code, configuration.ClockMultiplier, cycleRatio, cycleShift, configuration, protectedWrites, busCallbacks, busFaults).EmitBody(instruction, nextOffset);
                else code.Append(body.ToArray());
                int cost = unchecked((int)((long)instructionCycles[instruction.Opcode] * cycleRatio >> cycleShift));
                // Same per-instruction rounding as USE_CYCLES, not a sum rounded
                // once at the end of a block.
                if (!delayedInstruction) context.AddStateImmediate(M68000StateField.cycles, cost);
                if (busCallbacks && memoryBody && M68000X64MemoryInstructionEmitter.MayCallBus(instruction))
                {
                    code.Load(R.R13, M68000X64MemoryMap.BusChangedOffset, R.R8, W.Dword);
                    code.Test(R.R8, R.R8, W.Dword);
                    busBoundaries.Add(code.ReserveJump(C.NotZero));
                }
                if (!delayedInstruction && controlBody && labels.TryGetValue(M68000X64ControlInstructionEmitter.Target(instruction), out int target))
                {
                    context.LoadState(M68000StateField.pc, R.R8);
                    code.BinaryImmediate(A.Compare, unchecked((int)M68000X64ControlInstructionEmitter.Target(instruction)), R.R8, W.Dword);
                    code.Jump(target, C.Zero);
                }
                if (controlBody && branchTargets is not null)
                {
                    uint branchTarget = M68000X64ControlInstructionEmitter.Target(instruction);
                    if (branchTarget <= 0xffffff && (branchTarget & 1) == 0 && !branchTargets.Contains(branchTarget))
                        branchTargets.Add(branchTarget);
                }
                address = instruction.NextAddress;
                preferredLinkTarget = controlBody && instruction.Condition == 0
                    || instruction.Operation == M68000NativeOperation.BranchSubroutine
                    ? M68000X64ControlInstructionEmitter.Target(instruction) : address;
                verifyPc = controlBody || M68000X64DivideInstructionEmitter.IsDivide(instruction.Operation);
                block.InstructionCount++; block.EndAddress = address;
                if (systemBody || instruction.Operation is M68000NativeOperation.BranchSubroutine or M68000NativeOperation.ReturnSubroutine
                    or M68000NativeOperation.Jump or M68000NativeOperation.JumpSubroutine
                    || instruction.Operation == M68000NativeOperation.BranchCondition && instruction.Condition == 0) break;
            }
            if (block.InstructionCount == 0) return null;
            foreach (int exit in exits) code.PatchJump(exit, code.Position);
            if (linkBlocks)
            {
                var noLink = new List<int>();
                R linkRoot = cacheGuestRegisters ? R.R14 : R.Rbx;
                if (getLinkSlot is null) LoadLinkRoot();
                context.LoadState(M68000StateField.cycles, R.R8);
                context.LoadState(M68000StateField.cycle_end, R.R9);
                code.Binary(A.Compare, R.R9, R.R8, W.Dword); noLink.Add(code.ReserveJump(C.NoCarry));
                context.LoadState(M68000StateField.pc, R.R8);
                if (getLinkSlot is not null)
                {
                    // Known fallthrough/branch targets use one stable pointer
                    // slot, as opposed to rediscovering their page and offset
                    // on every visit. Unknown jumps retain the general map.
                    var targets = new List<uint>();
                    if (preferredLinkTarget <= 0xffffff && (preferredLinkTarget & 1) == 0) targets.Add(preferredLinkTarget);
                    if (address <= 0xffffff && (address & 1) == 0 && !targets.Contains(address)) targets.Add(address);
                    foreach (uint branchTarget in branchTargets!.AsEnumerable().Reverse().Take(4))
                        if (!targets.Contains(branchTarget)) targets.Add(branchTarget);
                    foreach (uint destination in targets)
                    {
                        nint slot = getLinkSlot(destination);
                        if (slot == 0) continue;
                        code.BinaryImmediate(A.Compare, unchecked((int)destination), R.R8, W.Dword);
                        int other = code.ReserveJump(C.NotZero);
                        code.MoveImmediate(slot, R.R10, W.Qword);
                        code.Load(R.R10, 0, R.R11, W.Qword);
                        code.Test(R.R11, R.R11, W.Qword); noLink.Add(code.ReserveJump(C.Zero));
                        code.JumpRegister(R.R11);
                        code.PatchJump(other, code.Position);
                    }
                    LoadLinkRoot();
                }
                code.TestImmediate(1, R.R8, W.Dword); noLink.Add(code.ReserveJump(C.NotZero));
                code.BinaryImmediate(A.Compare, 0xffffff, R.R8, W.Dword); noLink.Add(code.ReserveJump(C.Above));
                code.Move(R.R8, R.R9, W.Dword); code.Shift(X64Shift.Right, 12, R.R9, W.Dword);
                code.LoadIndexed(linkRoot, R.R9, 3, 0, R.R10, W.Qword);
                code.Test(R.R10, R.R10, W.Qword); noLink.Add(code.ReserveJump(C.Zero));
                code.BinaryImmediate(A.And, 4095, R.R8, W.Dword); code.Shift(X64Shift.Right, 1, R.R8, W.Dword);
                code.LoadIndexed(R.R10, R.R8, 3, 0, R.R11, W.Qword);
                code.Test(R.R11, R.R11, W.Qword); noLink.Add(code.ReserveJump(C.Zero));
                // Every target in this table belongs to the same core and has
                // the identical frame layout. Its linked entry reloads only
                // its code-window pointer; context and saved frame stay live.
                code.JumpRegister(R.R11);
                foreach (int jump in noLink) code.PatchJump(jump, code.Position);
                void LoadLinkRoot()
                {
                    if (cacheGuestRegisters) code.Load(R.R13, M68000X64MemoryMap.LinkRootOffset, linkRoot, W.Qword);
                    code.Test(linkRoot, linkRoot, W.Qword); noLink.Add(code.ReserveJump(C.Zero));
                }
            }
            int busBoundaryExit = code.Position;
            EmitExit(M68000X64BlockExit.Boundary);
            foreach (int exit in codeChanged) code.PatchJump(exit, code.Position);
            EmitExit(M68000X64BlockExit.CodeChanged);
            foreach (int exit in memoryDenied) code.PatchJump(exit, code.Position);
            EmitExit(M68000X64BlockExit.MemoryDenied);
            foreach (int exit in busFaults) code.PatchJump(exit, code.Position);
            EmitExit(M68000X64BlockExit.BusFault);
            // Keep the usual no-change branch untaken. A single cold exit
            // applies the callback's deferred cycle delta for any instruction.
            if (busBoundaries.Count != 0)
            {
                int target = delayedInstruction ? busBoundaryExit : code.Position;
                foreach (int exit in busBoundaries) code.PatchJump(exit, target);
                if (!delayedInstruction)
                {
                    code.TestImmediate(2, R.R8, W.Dword); code.Jump(busBoundaryExit, C.Zero);
                    code.Load(R.R13, M68000X64MemoryMap.BusCycleAdjustmentOffset, R.R8, W.Dword);
                    context.AddState(M68000StateField.cycles, R.R8);
                    code.Jump(busBoundaryExit);
                }
            }
            void EmitExit(M68000X64BlockExit reason)
            {
                // Identify the block which exits, including through a native
                // chain, so stale code invalidates the correct cache entry.
                code.MoveImmediate(unchecked((long)(((ulong)start << 32) | (uint)reason)), R.Rax, W.Qword);
                context.FlushHostState();
                RestoreFrame(); code.Return();
            }
            void RestoreFrame()
            {
                code.StackBytes = frameBytes;
                if (cacheGuestRegisters)
                {
                    code.Pop(R.Rdi); code.Pop(R.Rsi); code.Pop(R.Rbp); code.Pop(R.Rbx);
                }
                else
                {
                    if (keepHostState)
                    {
                        if (configuration.Prefetch) code.Pop(R.Rdi);
                        code.Pop(R.Rsi); code.Pop(R.Rbp);
                    }
                    if (linkBlocks) code.Pop(R.Rbx);
                }
                if (directMemory) { code.Pop(R.R15); code.Pop(R.R14); code.Pop(R.R13); }
                code.Pop(R.R12);
            }
            block._code = X64CodeBuffer.Create(code.ToArray());
            published = true; return block;
        }
        finally { if (!published) block.Dispose(); }
    }

    internal nint EntryPoint
    {
        get { lock (_sync) { ObjectDisposedException.ThrowIf(_disposed, this); return _code!.DangerousGetHandle(); } }
    }
    internal nint LinkedEntryPoint
    {
        get { lock (_sync) { ObjectDisposedException.ThrowIf(_disposed, this); return _code!.DangerousGetHandle() + _linkedEntryOffset; } }
    }
    public M68000X64BlockExit Execute(nint registers, nint state, nint memory = 0) => ExecuteLinked(registers, state, memory, 0, out _);

    internal M68000X64BlockExit ExecuteLinked(nint registers, nint state, nint memory, nint links, out uint exitBlock)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ulong result = _code!.ExecuteLinked(registers, state, memory, links);
            exitBlock = (uint)(result >> 32);
            return (M68000X64BlockExit)unchecked((uint)result);
        }
    }
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            try { _code?.Dispose(); }
            finally { if (_pinned) { _pin.Dispose(); _pinned = false; } }
        }
    }
    private sealed class WindowBoundary : Exception { }
}

