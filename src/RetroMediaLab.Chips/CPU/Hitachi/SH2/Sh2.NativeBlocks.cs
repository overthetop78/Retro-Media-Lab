using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Reflection;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private Sh2X64MemoryAccess? _nativeMemoryAccess;
    private Sh2X64MemoryAccess? _nativeMemoryCore;
    private bool _nativeMemoryUsesLiveState;
    private Sh2X64Loop? _nativeLoopCore;
    private nint _nativeMemoryPointer;
    private int _nativeExecutionDepth;
    private readonly List<Sh2X64Block> _retiredNativeBlocks = [];
    private static readonly MethodInfo NativeLoopCoreMethod = typeof(Sh2<TBus>).GetMethod(nameof(RunFullX64LoopCore), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo NativeMemoryCoreMethod = typeof(Sh2<TBus>).GetMethod(nameof(AccessX64MemoryCore), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private ExceptionDispatchInfo? _nativeMemoryFailure;
    private readonly Dictionary<ushort, Sh2X64Block> _fullNativeOpcodes = [];
    private bool _useFullX64Blocks;
    /// <summary>Select on the owning thread outside Execute; UseCompiledBlocks enables execution.</summary>
    public bool UseFullX64Blocks
    {
        get => _useFullX64Blocks;
        set
        {
            if (value == _useFullX64Blocks) return;
            if (value && (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
                || !OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64))
                throw new PlatformNotSupportedException("The SH-2 native backend requires Windows x64.");
            ClearCodeCache(); _useFullX64Blocks = value;
        }
    }
    public long FullNativeInstructions { get; private set; }
    public long FullNativeOpcodeBuilds { get; private set; }
    public long FullNativePureRuns { get; private set; }
    private bool _useMultiInstructionX64Blocks;
    /// <summary>Execute memory and control instructions in the same native block. Select outside Execute.</summary>
    public bool UseMultiInstructionX64Blocks
    {
        get => _useMultiInstructionX64Blocks;
        set { if (value == _useMultiInstructionX64Blocks) return; ClearCodeCache(); _useMultiInstructionX64Blocks = value; }
    }
    public long FullNativeBlockRuns { get; private set; }
    private bool _useLinkedX64Regions;
    /// <summary>Link basic blocks inside one mapped native region. Select outside Execute.</summary>
    public bool UseLinkedX64Regions
    {
        get => _useLinkedX64Regions;
        set { if (value == _useLinkedX64Regions) return; ClearCodeCache(); _useLinkedX64Regions = value; }
    }
    public long FullNativeLinkedHops { get; private set; }
    private void ClearX64PayloadHost()
    {
        try { foreach (var block in _fullNativeOpcodes.Values) RetireNativeBlock(block); }
        finally { _fullNativeOpcodes.Clear(); _nativeMemoryAccess = null; _nativeMemoryPointer = 0; _nativeMemoryFailure = null; }
    }

    private void RetireNativeBlock(Sh2X64Block block)
    { if (_nativeExecutionDepth != 0) _retiredNativeBlocks.Add(block); else block.Dispose(); }

    private Sh2X64MemoryAccess NativeMemoryCallback()
    {
        bool live = _activeLiveState != 0; if (_nativeMemoryCore is null || _nativeMemoryUsesLiveState != live) BindNativeMemoryCore(live);
        var callback = _nativeMemoryAccess ??= AccessX64Memory;
        if (_nativeMemoryPointer == 0) _nativeMemoryPointer = Marshal.GetFunctionPointerForDelegate(callback);
        return callback;
    }

    private void BindNativeMemoryCore(bool live)
    {
        _nativeMemoryCore = ResolveCompiledHelper(live ? LiveMemoryCoreMethod : NativeMemoryCoreMethod).CreateDelegate<Sh2X64MemoryAccess>(this);
        _nativeMemoryUsesLiveState = live;
    }
    private unsafe void ExecuteFullX64Loop()
    {
        if (_useLiveX64Context) { ExecuteWithLiveX64Context(); return; }
        Span<uint> state = stackalloc uint[Sh2X64Context.Words];
        fixed (uint* context = state) ExecuteFullX64Frame((nint)context);
    }

    private unsafe void ExecuteFullX64Frame(nint pointer)
    {
        var access = NativeMemoryCallback(); nint function = _nativeMemoryPointer;
        Span<ulong> timing = stackalloc ulong[_useNativeTimingProfile ? Sh2NativeTimingLayout.Words : 0]; timing.Clear();
        Span<nint> descriptor = stackalloc nint[10]; descriptor.Clear(); descriptor[0] = function;
        var copy = _nativeMemoryCore!;
        copy(pointer, 0, 0, 0x240);
        var run = _activeLiveState != 0 ? (_liveLoopCore ??= ResolveCompiledHelper(LiveLoopCoreMethod).CreateDelegate<Sh2X64Loop>(this)) : (_nativeLoopCore ??= ResolveCompiledHelper(NativeLoopCoreMethod).CreateDelegate<Sh2X64Loop>(this));
        _nativeExecutionDepth++; _nativeMemoryFailure = null;
        try
        {
            fixed (uint* registers = R)
            
            fixed (ulong* diagnostic = timing)
            fixed (nint* services = descriptor)
            {
                descriptor[1] = (nint)diagnostic;
                if (_nativeReadCache.Cache.IsEmpty && _nativeReadMap.Pages.IsEmpty) run((nint)registers, pointer, (nint)services);
                else RunWithNativeReadCache(run, (nint)registers, pointer, (nint)services);
            }
            _nativeMemoryFailure?.Throw();
        }
        finally
        {
            try { copy(pointer, 0, 0, 0x241); }
            finally
            {
                _executingCodeBlock = null;
                CollectNativeTiming(timing);
                _nativeMemoryFailure = null; GC.KeepAlive(access); GC.KeepAlive(run); GC.KeepAlive(copy);
                if (--_nativeExecutionDepth == 0 && _retiredNativeBlocks.Count != 0)
                { try { foreach (var block in _retiredNativeBlocks) block.Dispose(); } finally { _retiredNativeBlocks.Clear(); } }
            }
        }
    }

    private static ulong Native64(ReadOnlySpan<uint> state, int slot) => state[slot] | (ulong)state[slot + 1] << 32;
    private static void Native64(Span<uint> state, int slot, ulong value) { state[slot] = (uint)value; state[slot + 1] = (uint)(value >> 32); }

    private unsafe void AdvanceNativeFrame(nint pointer, int clocks)
    {
        Span<uint> state = new((void*)pointer, Sh2X64Context.Words);
        ulong cycles = Native64(state, Sh2X64Context.CyclesLow) + (uint)clocks;
        Native64(state, Sh2X64Context.CyclesLow, cycles);
        if (_deferClockUpdates)
        {
            state[Sh2X64Context.PendingClocks] += (uint)clocks;
            if (cycles < Native64(state, Sh2X64Context.BusDeadlineLow)) return;
            AccessX64Memory(pointer, 0, 0, 0x210);
        }
        else AccessX64Memory(pointer, 0, (uint)clocks, 0x211);
    }

    // Kept at the unmanaged ABI boundary by NativeRuntimeBuilder. The owning
    // core lease pins inputs and retains every retired target until return.
    private unsafe void InvokeLeasedNativePayload(Sh2X64Block block, nint registers, nint state, nint access)
        => ((delegate* unmanaged<nint, nint, nint, nint, ulong>)block.EntryPointer)(registers, state, access, 0);

    private unsafe void RunFullX64LoopCore(nint registers, nint pointer, nint access)
    {
        Span<uint> state = new((void*)pointer, Sh2X64Context.Words);
        ulong end = Native64(state, Sh2X64Context.EndLow);
        while (Native64(state, Sh2X64Context.CyclesLow) < end)
        {
            uint level = state[Sh2X64Context.IrqLevel];
            bool interrupt = level == 16 || level > (state[Sh2X64Context.Status] >> 4 & 15);
            if (level != 0 && interrupt && state[Sh2X64Context.Branch] == 0 && state[Sh2X64Context.IrqDelay] == 0)
            { AccessX64Memory(pointer, 0, 0, 0x230); if (state[Sh2X64Context.CallbackError] != 0) return; }
            if (state[Sh2X64Context.Sleeping] != 0)
            { ulong current = Native64(state, Sh2X64Context.CyclesLow); if (end > current) AdvanceNativeFrame(pointer, (int)(end - current)); return; }
            state[Sh2X64Context.IrqDelay] = 0;
            if ((state[Sh2X64Context.Pc] & 1) != 0)
            { AccessX64Memory(pointer, 0, 0, 0x231); if (state[Sh2X64Context.CallbackError] != 0) return; continue; }
            uint address = unchecked(state[Sh2X64Context.Pc] - 4);
            ushort opcode; int wait;
            bool mappedEntry = _bus.TryMapFetch(address, out var memory, out uint start, out wait)
                && address >= start && (ulong)(address - start) + 2 <= (ulong)memory.Length;
            if (mappedEntry)
                opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(memory.Span.Slice((int)(address - start), 2));
            else
            {
                opcode = (ushort)AccessX64Memory(pointer, address, 0, 0x220);
                if (state[Sh2X64Context.CallbackError] != 0) return;
                wait = (int)state[Sh2X64Context.FetchWait];
            }
            level = state[Sh2X64Context.IrqLevel];
            interrupt = level == 16 || level > (state[Sh2X64Context.Status] >> 4 & 15);
            if (_useMultiInstructionX64Blocks && state[Sh2X64Context.Pc] == unchecked(address + 4)
                && _bus.CodeVersion(address) >= 0)
            {
                _entryFetchWait = wait;
                CodeBlock? block = _codeBlocks.GetValueOrDefault(address);
                ulong epoch = _bus.CodeEpoch(address);
                bool valid = block is { FullNative.ExecutesInstructions: true, Active: true }
                    && block.Instructions[0] == opcode
                    && (block.FullNative.LinkedEntryFetchWait < 0
                        || (mappedEntry ? wait : NativeMappedEntryWait(address, opcode)) == block.FullNative.LinkedEntryFetchWait)
                    && (epoch != 0 && epoch == block.CodeEpoch || BlockWordsMatch(block, opcode));
                if (valid && (epoch == 0 || epoch != block!.CodeEpoch)) CaptureBlockVersions(block!);
                if (!valid) block = CompileNativeRange(address, opcode, block, true);
                if (block is { FullNative.ExecutesInstructions: true, Active: true })
                {
                    state[Sh2X64Context.EntryPc] = state[Sh2X64Context.Pc];
                    state[Sh2X64Context.Executed] = state[Sh2X64Context.CodeChanged] = 0;
                    state[Sh2X64Context.LinkedHops] = 0;
                    state[Sh2X64Context.FetchWait] = (uint)wait;
                    _executingCodeBlock = block; _executingCodeEpoch = block.CodeEpoch;
                    InvokeLeasedNativePayload(block.FullNative, registers, pointer, access);
                    _executingCodeBlock = null;
                    FullNativeInstructions += state[Sh2X64Context.Executed]; FullNativeBlockRuns++;
                    FullNativeLinkedHops += state[Sh2X64Context.LinkedHops];
                    if (state[Sh2X64Context.CallbackError] != 0) return;
                    continue;
                }
            }
            if (_deferClockUpdates && state[Sh2X64Context.Branch] == 0 && state[Sh2X64Context.Yield] == 0
                && !interrupt && IsCompiledPureInstruction(opcode) && _bus.CodeVersion(address) >= 0)
            {
                _entryFetchWait = wait;
                CodeBlock? block = _codeBlocks.GetValueOrDefault(address); ulong epoch = _bus.CodeEpoch(address);
                bool valid = block is { FullNative: not null, Active: true } && block.Instructions[0] == opcode
                    && (epoch != 0 && epoch == block.CodeEpoch || BlockWordsMatch(block, opcode));
                if (valid && (epoch == 0 || epoch != block!.CodeEpoch)) CaptureBlockVersions(block!);
                if (!valid) block = CompileNativeRange(address, opcode, block, false);
                if (block is { FullNative: not null, Active: true })
                {
                    block.CodeEpoch = epoch; int clocks = checked(wait + 1 + block.NativeTailClocks);
                    ulong until = Native64(state, Sh2X64Context.CyclesLow) + (uint)clocks;
                    if (until <= end && until < Native64(state, Sh2X64Context.BusDeadlineLow))
                    {
                        InvokeLeasedNativePayload(block.FullNative, registers, pointer, access);
                        state[Sh2X64Context.Pc] = unchecked(state[Sh2X64Context.Pc] + (uint)(block.Instructions.Length * 2));
                        AdvanceNativeFrame(pointer, clocks); FullNativeInstructions += block.Instructions.Length; FullNativePureRuns++; continue;
                    }
                }
            }
            AdvanceNativeFrame(pointer, wait + 1); if (state[Sh2X64Context.CallbackError] != 0) return;
            ushort key = Sh2OpcodeTable.Types[opcode] == 142 ? (ushort)0xffff : opcode;
            if (!_fullNativeOpcodes.TryGetValue(key, out var instruction)) instruction = CompileNativeOpcode(key);
            InvokeLeasedNativePayload(instruction, registers, pointer, access); FullNativeInstructions++;
            if (state[Sh2X64Context.CallbackError] != 0) return;
            switch (state[Sh2X64Context.Branch])
            {
                case 0: state[Sh2X64Context.Pc] = unchecked(state[Sh2X64Context.Pc] + 2); break;
                case 1: state[Sh2X64Context.Pc] = unchecked(state[Sh2X64Context.Pc] + 2); state[Sh2X64Context.Branch] = 2; break;
                case 2: state[Sh2X64Context.Pc] = state[Sh2X64Context.PreviousPc]; state[Sh2X64Context.Branch] = 0; break;
            }
            if (state[Sh2X64Context.Yield] == 0) continue;
            state[Sh2X64Context.Yield] = 0;
            ulong elapsed = Native64(state, Sh2X64Context.CyclesLow); if (elapsed < end) AdvanceNativeFrame(pointer, (int)(end - elapsed)); return;
        }
    }

    private Sh2X64Block CompileNativeOpcode(ushort opcode)
    {
        var block = Sh2X64Block.TryCompile([opcode], _useNativeTimingProfile, _useNativeCachedReads, _useLiveX64Context ? _liveContextOffsets : null, _useNativeMappedReads)
            ?? throw new InvalidOperationException($"SH-2 native opcode {opcode:X4} is unsupported; no interpreted fallback is allowed.");
        bool transferred = false;
        try { _fullNativeOpcodes.Add(opcode, block); FullNativeOpcodeBuilds++; transferred = true; return block; }
        finally { if (!transferred) block.Dispose(); }
    }

    private CodeBlock? CompileNativeRange(uint address, ushort first, CodeBlock? previous, bool execution)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        int linkedEntryWait = 0;
        bool linked = execution && _useLinkedX64Regions
            && TryReadMappedCode(address, out ushort mappedFirst, out linkedEntryWait) && mappedFirst == first;
        var words = new List<ushort> { first }; var waits = new List<int> { _entryFetchWait };
        int tailClocks = 0;
        for (int i = 1; i < MaximumBlockInstructions; i++)
        {
            ushort preceding = words[^1];
            bool terminal = execution && !linked && !IsNativeLinearInstruction(preceding);
            if (terminal && !IsCompiledDelayedBranch(preceding)) break;
            uint next = unchecked(address + (uint)(i * 2));
            if (next < address) break;
            if (_bus.CodeVersion(next) < 0 || !TryReadMappedCode(next, out ushort word, out int wait)
                || !execution && !IsCompiledPureInstruction(word)) break;
            words.Add(word); waits.Add(wait); tailClocks = checked(tailClocks + wait + 1);
            if (terminal) break; // Exactly one mapped delay slot, including illegal-slot encodings.
        }
        if (!execution && words.Count < 2) return null;
        var payload = (execution ? Sh2X64Block.TryCompileExecution(words.ToArray(), waits.ToArray(), _deferClockUpdates, linked, linkedEntryWait, _useNativeTimingProfile, _useNativeCachedReads, _useLiveX64Context ? _liveContextOffsets : null, NativeIdlePlan(address, words), _useNativeMappedReads)
            : Sh2X64Block.TryCompile(words.ToArray(), _useNativeTimingProfile, _useNativeCachedReads, _useLiveX64Context ? _liveContextOffsets : null, _useNativeMappedReads))
            ?? throw new InvalidOperationException("SH-2 native arithmetic range contains an unsupported instruction.");
        bool transferred = false;
        try
        {
            uint line = address & ~15u, last = unchecked(address + (uint)(words.Count * 2 - 2)) & ~15u;
            uint[] addresses = new uint[(last - line) / 16 + 1];
            for (int i = 0; i < addresses.Length; i++) addresses[i] = line + (uint)(i * 16);
            var block = new CodeBlock(address, words.ToArray(), waits.ToArray(), addresses, new long[addresses.Length], static _ => { })
                { FullNative = payload, NativeTailClocks = tailClocks };
            CaptureBlockVersions(block);
            if (previous is not null) ReleaseCodeBlock(previous);
            else
            {
                if (_codeBlocks.Count == _compiledBlockCacheCapacity)
                {
                    uint evicted = _codeBlockOrder.Dequeue(); ReleaseCodeBlock(_codeBlocks[evicted]); _codeBlocks.Remove(evicted);
                }
                _codeBlockOrder.Enqueue(address);
            }
            _codeBlocks[address] = block;
            CompiledBlockBuildCount++;
            CompiledBlockBuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
            transferred = true; return block;
        }
        finally { if (!transferred) payload.Dispose(); }
    }

    private static bool IsNativeLinearInstruction(ushort opcode) => !IsCompiledDelayedBranch(opcode)
        && CompiledInstructions[Sh2OpcodeTable.Types[opcode]].Name is not
            (nameof(BT) or nameof(BF) or nameof(TRAPA) or nameof(SLEEP) or nameof(ILLEGAL));
    /// <summary>Executes a translated payload; scheduling/entry fetch are supplied by the block runner.</summary>
    internal unsafe void ExecuteX64Payload(Sh2X64Block block)
    {
        Span<uint> state = stackalloc uint[Sh2X64Context.Words]; CaptureX64State(state);
        Span<ulong> timing = stackalloc ulong[block.TimingInstrumented ? Sh2NativeTimingLayout.Words : 0]; timing.Clear();
        Span<nint> descriptor = stackalloc nint[10]; descriptor.Clear();
        Sh2X64MemoryAccess? access = null;
        nint function = 0;
        _nativeMemoryFailure = null;
        try
        {
            if (block.RequiresMemory)
            {
                access = NativeMemoryCallback();
                function = _nativeMemoryPointer;
            }
            fixed (ulong* diagnostic = timing)
            fixed (nint* services = descriptor)
            {
                descriptor[0] = function; descriptor[1] = (nint)diagnostic;
                if (_nativeReadCache.Cache.IsEmpty && _nativeReadMap.Pages.IsEmpty) block.Execute(R, state, block.RequiresMemory ? (nint)services : 0);
                else PayloadWithNativeReadCache(block, state, (nint)services);
            }
            RestoreX64State(state);
            _nativeMemoryFailure?.Throw();
        }
        finally { CollectNativeTiming(timing); _nativeMemoryFailure = null; GC.KeepAlive(access); }
    }

    private unsafe uint AccessX64Memory(nint pointer, uint address, uint value, uint kind)
    {

        try
        {
            bool live = pointer != 0 && pointer == _activeLiveState; if (_nativeMemoryCore is null || _nativeMemoryUsesLiveState != live) BindNativeMemoryCore(live);
            return _nativeMemoryCore!(pointer, address, value, kind);
        }
        catch (Exception error)
        {
            _nativeMemoryFailure = ExceptionDispatchInfo.Capture(error);
            if (pointer != 0 && pointer == _activeLiveState) _liveError = 1; else { Span<uint> state = new((void*)pointer, Sh2X64Context.Words); CaptureX64State(state); state[Sh2X64Context.CallbackError] = 1; }
            return 0;
        }
    }

    private unsafe uint AccessX64MemoryCore(nint pointer, uint address, uint value, uint kind)
    {
        Span<uint> state = new((void*)pointer, Sh2X64Context.Words);
        if (kind == 0x240) { CaptureX64State(state); return 0; }
        RestoreX64State(state); if (kind == 0x241) return 0;
        uint entry = state[Sh2X64Context.EntryPc], executed = state[Sh2X64Context.Executed], changed = state[Sh2X64Context.CodeChanged];
        uint hops = state[Sh2X64Context.LinkedHops], fetchWait = state[Sh2X64Context.FetchWait];
        bool guarding = _executingCodeBlock is not null;
        uint result = 0; int wait = 0;
        switch (kind)
        {
            case 1: result = readByte(address); break;
            case 2: result = readWord(address); break;
            case 4: result = readLong(address); break;
            case 0x101: writeByte(address, value); break;
            case 0x102: writeWord(address, value); break;
            case 0x104: writeLong(address, value); break;
            case 0x200: step((int)value); break;
            case 0x210: SynchronizeBusClock(); break;
            case 0x211: _bus.AdvanceCycles((int)value); break;
            case 0x220: result = _bus.Fetch16(address, out wait); break;
            case 0x230: interrupt((uint)_irqLevel, (uint)_irqVector); Sleeping = false; step(5); break;
            case 0x231: addressErrorCPU(); step(5); break;
            case 0x250: if (Cycles < _executionEnd) step((int)(_executionEnd - Cycles)); break;
            case >= 0x300 and <= 0x30f: NativeIdleDt(kind & 15); break;
            case 0x400ffe: case 0x400ffd: NativeIdleBra(kind & 0xfff); break;
            default: throw new InvalidOperationException("Unsupported native SH-2 bus access.");
        }
        if (guarding && !NativeExecutingCodeMatches((int)fetchWait)) changed = 1;
        CaptureX64State(state); state[Sh2X64Context.EntryPc] = entry;
        state[Sh2X64Context.Executed] = executed; state[Sh2X64Context.CodeChanged] = changed;
        state[Sh2X64Context.LinkedHops] = hops;
        state[Sh2X64Context.FetchWait] = kind == 0x220 ? (uint)wait : fetchWait; return result;
    }

    private bool NativeExecutingCodeMatches(int entryFetchWait)
    {
        if (_executingCodeBlock is not { Active: true } block) return false;
        ulong epoch = _bus.CodeEpoch(block.Address);
        if (epoch != 0 && epoch == _executingCodeEpoch) return true;
        int linkedWait = block.FullNative?.LinkedEntryFetchWait ?? -1;
        int expectedWait = linkedWait >= 0 ? linkedWait : entryFetchWait;
        if (!TryReadMappedCode(block.Address, out ushort first, out int wait) || wait != expectedWait
            || !BlockWordsMatch(block, first)) return false;
        CaptureBlockVersions(block); _executingCodeEpoch = block.CodeEpoch;
        return true;
    }

    private int NativeMappedEntryWait(uint address, ushort expected) =>
        TryReadMappedCode(address, out ushort word, out int wait) && word == expected ? wait : -1;

    private void NativeIdleDt(uint register)
    {
        if (!AccelerateIdleLoops || R[register] <= 1 || _bus.Fetch16(PC - 2) != 0x8bfd) return;
        int clocks = 3 + _bus.WaitStates(PC - 4, false) + _bus.WaitStates(PC - 2, false);
        uint loops = IdleIterations(R[register] - 1, clocks);
        if (loops == 0) return;
        R[register] -= loops; PollingEpoch += loops; PPC = PC; step((int)(loops * clocks));
    }

    private void NativeIdleBra(uint displacement)
    {
        if (!AccelerateIdleLoops || _bus.Fetch16(PC - 2) != 0x0009
            || displacement == 0xffd && _bus.Fetch16(PC - 6) != 0x0009) return;
        int clocks = 3 + _bus.WaitStates(PC - 4, false) + _bus.WaitStates(PC - 2, false);
        if (displacement == 0xffd) clocks += 1 + _bus.WaitStates(PC - 6, false);
        uint loops = IdleIterations(uint.MaxValue, clocks);
        if (loops != 0) step((int)(loops * clocks));
    }

    internal void CaptureX64State(Span<uint> state)
    {
        if (state.Length < Sh2X64Context.Words) throw new ArgumentException("Native SH-2 context is too short.", nameof(state));
        state.Clear();
        state[Sh2X64Context.Status] = SR; state[Sh2X64Context.GlobalBase] = GBR;
        state[Sh2X64Context.VectorBase] = VBR; state[Sh2X64Context.Procedure] = PR;
        state[Sh2X64Context.MacLow] = MACL; state[Sh2X64Context.MacHigh] = MACH;
        state[Sh2X64Context.Pc] = PC; state[Sh2X64Context.PreviousPc] = PPC;
        state[Sh2X64Context.IrqDelay] = ID; state[Sh2X64Context.Branch] = (uint)_branch;
        state[Sh2X64Context.PollingEpoch] = PollingEpoch;
        state[Sh2X64Context.CyclesLow] = (uint)Cycles; state[Sh2X64Context.CyclesHigh] = (uint)(Cycles >> 32);
        state[Sh2X64Context.EndLow] = (uint)_executionEnd; state[Sh2X64Context.EndHigh] = (uint)(_executionEnd >> 32);
        state[Sh2X64Context.BusDeadlineLow] = (uint)_busDeadline; state[Sh2X64Context.BusDeadlineHigh] = (uint)(_busDeadline >> 32);
        state[Sh2X64Context.PendingClocks] = (uint)_pendingBusClocks;
        state[Sh2X64Context.Sleeping] = Sleeping ? 1u : 0; state[Sh2X64Context.Yield] = _yield ? 1u : 0;
        state[Sh2X64Context.IrqLevel] = (uint)_irqLevel; state[Sh2X64Context.IrqVector] = (uint)_irqVector;
        state[Sh2X64Context.ExceptionType] = ET;
        state[Sh2X64Context.IdleEnabled] = AccelerateIdleLoops ? 1u : 0;
    }

    internal void RestoreX64State(ReadOnlySpan<uint> state)
    {
        if (state.Length < Sh2X64Context.Words) throw new ArgumentException("Native SH-2 context is too short.", nameof(state));
        SR = state[Sh2X64Context.Status]; GBR = state[Sh2X64Context.GlobalBase]; VBR = state[Sh2X64Context.VectorBase];
        PR = state[Sh2X64Context.Procedure]; MACL = state[Sh2X64Context.MacLow]; MACH = state[Sh2X64Context.MacHigh];
        PC = state[Sh2X64Context.Pc]; PPC = state[Sh2X64Context.PreviousPc]; ID = state[Sh2X64Context.IrqDelay];
        _branch = (int)state[Sh2X64Context.Branch]; PollingEpoch = state[Sh2X64Context.PollingEpoch];
        Cycles = state[Sh2X64Context.CyclesLow] | (ulong)state[Sh2X64Context.CyclesHigh] << 32;
        _executionEnd = state[Sh2X64Context.EndLow] | (ulong)state[Sh2X64Context.EndHigh] << 32;
        _busDeadline = state[Sh2X64Context.BusDeadlineLow] | (ulong)state[Sh2X64Context.BusDeadlineHigh] << 32;
        _pendingBusClocks = (int)state[Sh2X64Context.PendingClocks];
        Sleeping = state[Sh2X64Context.Sleeping] != 0; _yield = state[Sh2X64Context.Yield] != 0;
        _irqLevel = (int)state[Sh2X64Context.IrqLevel]; _irqVector = (int)state[Sh2X64Context.IrqVector];
        ET = state[Sh2X64Context.ExceptionType];
    }
}




