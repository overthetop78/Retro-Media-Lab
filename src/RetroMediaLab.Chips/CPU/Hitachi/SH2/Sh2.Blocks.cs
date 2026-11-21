using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    public bool UseCompiledBlocks { get; set; }
    public bool UseX64RegisterBlocks { get; set; }
    public bool UseSharedCompiledBodies { get; set; }
    public long CompiledBodyBuildCount { get; private set; }
    public long SharedCompiledBodyReuseCount { get; private set; }
    public long X64RegisterBlockExecutions { get; private set; }
    public bool LinkCompiledBlocks { get; set; } = true;
    public Type? SpecializedExternalBusType { get; set; }
    // The native runtime already supplies the optimized dispatcher; a reference
    // CPU remains available without building a second giant copy of the loop.
    private void ExecuteNativeLoop()
    { if (UseFullX64Blocks) ExecuteFullX64Loop(); else ExecuteLoop(); }
    private const int MaximumBlockInstructions = 64;
    private int _compiledBlockCacheCapacity = 4096;
    /// <summary>Maximum resident code addresses. Changing it releases existing compiled blocks.</summary>
    public int CompiledBlockCacheCapacity
    {
        get => _compiledBlockCacheCapacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            if (value == _compiledBlockCacheCapacity) return;
            ClearCodeCache();
            _compiledBlockCacheCapacity = value;
        }
    }
    private sealed class CodeBlock(uint address, ushort[] instructions, int[] fetchWaits, uint[] versionAddresses, long[] versions, Action<Sh2<TBus>> execute)
    {
        public uint Address = address;
        public ushort[] Instructions = instructions;
        public int[] FetchWaits = fetchWaits;
        public uint[] VersionAddresses = versionAddresses;
        public long[] Versions = versions;
        public Action<Sh2<TBus>> Execute = execute;
        public ulong CodeEpoch;
        public int SteadyFetchWait;
        public bool Active = true;
        public CodeBlock? Successor0, Successor1;
        public Sh2X64RegisterBlock? X64;
        public Sh2X64Block? FullNative;
        public int NativeTailClocks;
        public int X64Count;
    }
    private readonly Dictionary<uint, CodeBlock> _codeBlocks = [];
    private readonly Dictionary<uint, List<CodeBlock>> _inactiveCodeBlocks = [];
    private readonly Queue<uint> _codeBlockOrder = [];
    private readonly Dictionary<string, Action<Sh2<TBus>>> _compiledBodies = [];
    private readonly CodeBlock?[] _codeBlockLookup = new CodeBlock?[256];
    private int _entryFetchWait;
    private ulong _blockEventLimit;
    private CodeBlock? _executingCodeBlock;
    private ulong _executingCodeEpoch;
    private CodeBlock? _linkSourceCandidate;
    public long CompiledBlockBuildCount { get; private set; }
    public long CompiledBlockBuildTicks { get; private set; }
    public long CompiledBlockFallbackCount { get; private set; }
    public uint? CompiledBlockFallbackAddress { get; private set; }
    public ushort? CompiledBlockFallbackOpcode { get; private set; }
    public string? CompiledBlockFallbackReason { get; private set; }
    private static readonly MethodInfo BeginBlockInstruction = typeof(Sh2<TBus>).GetMethod(nameof(BeginCompiledInstruction), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo EndBlockInstruction = typeof(Sh2<TBus>).GetMethod(nameof(CompleteInstruction), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo CanRunBlockPrefix = typeof(Sh2<TBus>).GetMethod(nameof(CanRunCompiledPurePrefix), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo FinishBlockPrefix = typeof(Sh2<TBus>).GetMethod(nameof(FinishCompiledPurePrefix), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ContinueBlockAfterRead = typeof(Sh2<TBus>).GetMethod(nameof(CanContinueCompiledAfterRead), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ResolveBlockSuccessor = typeof(Sh2<TBus>).GetMethod(nameof(ResolveCompiledSuccessor), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo CanRunFastPayload = typeof(Sh2<TBus>).GetMethod(nameof(CanRunCompiledFastPayload), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ChargeFastFetch = typeof(Sh2<TBus>).GetMethod(nameof(ChargeCompiledFastFetch), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo FinishFastLinear = typeof(Sh2<TBus>).GetMethod(nameof(FinishCompiledFastLinear), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo FinishFastRead = typeof(Sh2<TBus>).GetMethod(nameof(FinishCompiledFastRead), BindingFlags.NonPublic | BindingFlags.Instance)!;
    public void ClearCodeCache()
    {
        if (_compilationTiming is { } profile) { profile.CacheClears++; profile.Evicted.Clear(); }
        foreach (CodeBlock block in _codeBlocks.Values) ReleaseCodeBlock(block);
        foreach (var variants in _inactiveCodeBlocks.Values)
            foreach (CodeBlock block in variants) ReleaseCodeBlock(block);
        _codeBlocks.Clear(); _inactiveCodeBlocks.Clear(); _codeBlockOrder.Clear(); _compiledBodies.Clear(); Array.Clear(_codeBlockLookup);
        _executingCodeBlock = _linkSourceCandidate = null;
        ClearX64PayloadHost();
    }

    private void ReleaseCodeBlock(CodeBlock block)
    {
        DeactivateCodeBlock(block);
        try { block.X64?.Dispose(); }
        finally
        {
            block.X64 = null;
            try { if (block.FullNative is { } payload) RetireNativeBlock(payload); }
            finally { block.FullNative = null; }
        }
    }

    private void ExecuteX64ArithmeticPrefix()
    {
        CodeBlock block = _executingCodeBlock!;
        if (UseX64RegisterBlocks && block.X64 is not null)
        {
            block.X64.Execute(R);
            X64RegisterBlockExecutions++;
        }
        else
            for (int i = 0; i < block.X64Count; i++) ExecuteOpcode(block.Instructions[i]);
    }
    private bool TryExecuteCompiledBlock()
    {
        if (_branch != 0 || ID != 0 || Sleeping || _yield) { _linkSourceCandidate = null; return false; }
        uint address = ProgramCounter;
        if (_bus.CodeVersion(address) < 0) { _linkSourceCandidate = null; return false; }
        ushort first = _bus.Fetch16(address, out _entryFetchWait);
        int slot = (int)(address >> 1 & 255);
        CodeBlock? block = _codeBlockLookup[slot];
        if (block is null || block.Address != address) _codeBlocks.TryGetValue(address, out block);
        ulong epoch = _bus.CodeEpoch(address);
        if (block is null || !block.Active || block.Instructions[0] != first
            || !(epoch != 0 && epoch == block.CodeEpoch) && !BlockVersionsMatch(block))
        {
            try { block = ResolveCodeBlock(address, first, block); }
            catch (InvalidProgramException error)
            {
                CompiledBlockFallbackCount++;
                CompiledBlockFallbackAddress = address;
                CompiledBlockFallbackOpcode = first;
                CompiledBlockFallbackReason = error.Message;
                UseCompiledBlocks = false;
                ClearCodeCache();
                _linkSourceCandidate = null;
                return false;
            }
        }
        _codeBlockLookup[slot] = block;
        block.CodeEpoch = _bus.CodeEpoch(address);
        if (_linkSourceCandidate is { Active: true } source) LinkCodeBlock(source, block);
        _linkSourceCandidate = null;
        SetExecutingCodeBlock(block);
        ulong before = Cycles;
        block.Execute(this);
        return Cycles != before;
    }

    private bool BlockVersionsMatch(CodeBlock block)
    {
        for (int i = 0; i < block.Versions.Length; i++)
            if (_bus.CodeVersion(block.VersionAddresses[i]) != block.Versions[i]) return false;
        return true;
    }
    private bool BlockWordsMatch(CodeBlock block, ushort first)
    {
        if (block.Instructions[0] != first) return false;
        // Later fetch waits are embedded in fused prefixes. Identical code
        // bytes cannot reuse those costs after a cache or mapping timing change.
        for (int i = 1; i < block.Instructions.Length; i++)
            if (!TryReadMappedCode(block.Address + (uint)(i * 2), out ushort opcode, out int wait)
                || opcode != block.Instructions[i] || wait != block.FetchWaits[i]) return false;
        return true;
    }
    // Translation and validation must not fetch future instructions through the
    // emulated bus: a fetch can fill/evict SH7095 cache lines and change timings.
    // An unmapped next instruction ends the block; execution fetches it normally.
    private bool TryReadMappedCode(uint address, out ushort opcode, out int wait)
    {
        if (_bus.TryMapFetch(address, out var memory, out uint start, out wait)
            && address >= start && (ulong)(address - start) + 2 <= (ulong)memory.Length)
        {
            opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(memory.Span.Slice((int)(address - start), 2));
            return true;
        }
        opcode = 0; wait = 0; return false;
    }
    private void CaptureBlockVersions(CodeBlock block)
    {
        int steadyWait = _entryFetchWait;
        if (TryReadMappedCode(block.Address, out _, out int mappedWait)) steadyWait = mappedWait;
        block.SteadyFetchWait = steadyWait;
        _bus.MarkCode(block.Address, block.Instructions.Length * 2);
        // Translation has not filled or replaced any hardware cache line.
        for (int i = 0; i < block.Versions.Length; i++) block.Versions[i] = _bus.CodeVersion(block.VersionAddresses[i]);
        block.CodeEpoch = _bus.CodeEpoch(block.Address);
        block.Active = true;
    }
    private static void DeactivateCodeBlock(CodeBlock block)
    { block.Active = false; block.Successor0 = block.Successor1 = null; }
    private static void LinkCodeBlock(CodeBlock source, CodeBlock target)
    {
        if (source.Successor0 == target) return;
        source.Successor1 = source.Successor0; source.Successor0 = target;
    }
    private void SetExecutingCodeBlock(CodeBlock block)
    {
        _blockEventLimit = _deferClockUpdates ? _busDeadline : ulong.MaxValue;
        _executingCodeBlock = block; _executingCodeEpoch = block.CodeEpoch;
    }
    private Action<Sh2<TBus>>? ResolveCompiledSuccessor()
    {
        _linkSourceCandidate = null;
        if (!LinkCompiledBlocks || Cycles >= _executionEnd || Cycles >= _busDeadline || InterruptReady
            || _branch != 0 || ID != 0 || Sleeping || _yield || _executingCodeBlock is not { Active: true } source)
            return null;
        uint address = ProgramCounter;
        CodeBlock? target = source.Successor0?.Address == address ? source.Successor0
            : source.Successor1?.Address == address ? source.Successor1 : null;
        if (target is { Active: true })
        {
            ulong epoch = _bus.CodeEpoch(address);
            if (epoch != 0 && epoch == target.CodeEpoch)
            {
                _entryFetchWait = target.SteadyFetchWait; SetExecutingCodeBlock(target);
                return target.Execute;
            }
            if (BlockVersionsMatch(target))
            {
                ushort first = _bus.Fetch16(address, out _entryFetchWait);
                if (first == target.Instructions[0])
                {
                    target.CodeEpoch = _bus.CodeEpoch(address); SetExecutingCodeBlock(target);
                    return target.Execute;
                }
            }
        }
        // Only the outer dispatcher performs a cold lookup or compiles code.
        _linkSourceCandidate = source;
        return null;
    }
    private CodeBlock ResolveCodeBlock(uint address, ushort first, CodeBlock? previous)
    {
        if (previous is not null && BlockWordsMatch(previous, first))
        { if (_compilationTiming is { } profile) profile.SameBodyReuses++; CaptureBlockVersions(previous); return previous; }
        if (_inactiveCodeBlocks.TryGetValue(address, out List<CodeBlock>? inactive))
        {
            for (int i = 0; i < inactive.Count; i++)
            {
                CodeBlock old = inactive[i];
                if (!BlockWordsMatch(old, first)) continue;
                if (_compilationTiming is { } profile) profile.VariantReuses++;
                inactive.RemoveAt(i);
                if (previous is not null) { DeactivateCodeBlock(previous); inactive.Add(previous); }
                CaptureBlockVersions(old); _codeBlocks[address] = old; return old;
            }
        }
        if (previous is not null)
        {
            ProfileRebuild(previous, first);
            DeactivateCodeBlock(previous);
            if (inactive is null) _inactiveCodeBlocks[address] = inactive = [];
            if (inactive.Count == 4) { if (_compilationTiming is { } profile) profile.VariantEvictions++; ReleaseCodeBlock(inactive[0]); inactive.RemoveAt(0); }
            inactive.Add(previous);
        }
        else
        {
            if (_codeBlocks.Count == _compiledBlockCacheCapacity)
            {
                uint evicted = _codeBlockOrder.Dequeue();
                if (_compilationTiming is { } profile) { profile.Evictions++; profile.Evicted.Add(evicted); }
                ReleaseCodeBlock(_codeBlocks[evicted]);
                if (_inactiveCodeBlocks.TryGetValue(evicted, out var variants))
                    foreach (CodeBlock variant in variants) ReleaseCodeBlock(variant);
                _codeBlocks.Remove(evicted); _inactiveCodeBlocks.Remove(evicted);
                int slot = (int)(evicted >> 1 & 255);
                if (_codeBlockLookup[slot]?.Address == evicted) _codeBlockLookup[slot] = null;
            }
            _codeBlockOrder.Enqueue(address);
            if (_compilationTiming is { } timing && timing.Evicted.Remove(address)) timing.RecompiledAfterEviction++;
        }
        CodeBlock block = CompileBlock(address, first);
        _codeBlocks[address] = block; return block;
    }

    private CodeBlock CompileBlock(uint start, ushort first)
    {
        long buildStart = Stopwatch.GetTimestamp();
        CompiledBlockBuildCount++;
        var profile = _compilationTiming;
        if (profile is not null) { profile.Attempts++; profile.Builds[start] = profile.Builds.GetValueOrDefault(start) + 1; }
        var words = new List<ushort>(MaximumBlockInstructions);
        var fetchWaits = new List<int>(MaximumBlockInstructions);
        int delaySlotIndex = -1;
        for (int i = 0; i < MaximumBlockInstructions; i++)
        {
            uint address = start + (uint)(i * 2);
            if (_bus.CodeVersion(address) < 0) break;
            ushort opcode; int fetchWait;
            if (i == 0) { opcode = first; TryReadMappedCode(address, out _, out fetchWait); }
            else if (!TryReadMappedCode(address, out opcode, out fetchWait)) break;
            words.Add(opcode);
            fetchWaits.Add(fetchWait);
            if (!IsCompiledFallThroughInstruction(opcode))
            {
                if (IsCompiledDelayedBranch(opcode) && i + 1 < MaximumBlockInstructions && _bus.CodeVersion(address + 2) >= 0)
                {
                    if (TryReadMappedCode(address + 2, out ushort slotOpcode, out int slotWait) && IsCompiledPureInstruction(slotOpcode))
                    { delaySlotIndex = words.Count; words.Add(slotOpcode); fetchWaits.Add(slotWait); }
                }
                break;
            }
        }
        Sh2X64RegisterBlock? native = null;
        ProfileInstructionSequence(words, fetchWaits);
        long scanEnd = profile is null ? 0 : Stopwatch.GetTimestamp();
        if (profile is not null) { profile.Scan += scanEnd - buildStart; profile.Instructions += words.Count; }
        int nativeCount = 0;
        try
        {
        if (UseX64RegisterBlocks && _bus.TryMapFetch(start, out var mapped, out uint mappedStart, out _)
            && start >= mappedStart)
        {
            int limit = (int)Math.Min((ulong)words.Count, ((ulong)mapped.Length - Math.Min((ulong)(start - mappedStart), (ulong)mapped.Length)) / 2);
            while (nativeCount < limit && nativeCount != delaySlotIndex && Sh2X64RegisterBlock.Supports(words[nativeCount])) nativeCount++;
            for (; nativeCount >= 2; nativeCount--)
            {
                native = Sh2X64RegisterBlock.TryCompile(words.Take(nativeCount).ToArray());
                if (native is not null) break;
            }
            if (native is null) nativeCount = 0;
        }
        string name = $"SH2_{start:X8}_{CompiledBlockBuildCount}";
        long prefixEnd = profile is null ? 0 : Stopwatch.GetTimestamp();
        if (profile is not null) profile.Prefix += prefixEnd - scanEnd;
        int maximumWait = _bus.MaximumWaitStates;
        string? bodyKey = UseSharedCompiledBodies
            ? $"{delaySlotIndex}:{nativeCount}:{maximumWait}|{string.Join(',', words)}|{string.Join(',', fetchWaits)}" : null;
        Action<Sh2<TBus>> execute;
        if (bodyKey is not null && _compiledBodies.TryGetValue(bodyKey, out var shared))
        {
            execute = shared; SharedCompiledBodyReuseCount++;
        }
        else
        {
            execute = CompileBlockBody(name, start, words, fetchWaits, delaySlotIndex, nativeCount, maximumWait, bodyKey is not null, profile);
            if (bodyKey is not null)
            {
                // Bound retained templates even when self-modifying code creates many variants.
                if (_compiledBodies.Count >= _compiledBlockCacheCapacity) _compiledBodies.Clear();
                _compiledBodies.Add(bodyKey, execute);
            }
        }
        long bodyEnd = profile is null ? 0 : Stopwatch.GetTimestamp();
        uint firstLine = start & ~15u, lastLine = (start + (uint)(words.Count * 2 - 2)) & ~15u;
        uint[] addresses = new uint[(lastLine - firstLine) / 16 + 1];
        for (int i = 0; i < addresses.Length; i++) addresses[i] = firstLine + (uint)(i * 16);
        var block = new CodeBlock(start, words.ToArray(), fetchWaits.ToArray(), addresses, new long[addresses.Length], execute) { X64 = native, X64Count = nativeCount };
        CaptureBlockVersions(block);
        if (profile is not null) { long publishEnd = Stopwatch.GetTimestamp(); profile.Publish += publishEnd - bodyEnd; profile.Total += publishEnd - buildStart; }
        CompiledBlockBuildTicks += Stopwatch.GetTimestamp() - buildStart;
        native = null;
        return block;
        }
        finally { native?.Dispose(); }
    }
    private Action<Sh2<TBus>> CompileBlockBody(string name, uint start, List<ushort> words, List<int> fetchWaits,
        int delaySlotIndex, int nativeCount, int maximumWait, bool relocatable, CompilationTiming? profile)
    {
        CompiledBodyBuildCount++;
        long bodyStart = profile is null ? 0 : Stopwatch.GetTimestamp();
        // DynamicMethod inherits debugger/JIT options from its associated module.
        // Keep generated native blocks with their optimized helper module; do not
        // alter the Debug configuration of the application or reference interpreter.
        var method = NativeHelperMaps.ContainsKey(GetType())
            ? new DynamicMethod(name, typeof(void), [typeof(Sh2<TBus>)], GetType().Module, true)
            : new DynamicMethod(name, typeof(void), [typeof(Sh2<TBus>)], typeof(Sh2<TBus>), true);
        ILGenerator il = method.GetILGenerator(); Label end = il.DefineLabel();
        LocalBuilder? baseAddress = null;
        if (relocatable)
        {
            baseAddress = il.DeclareLocal(typeof(uint));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(Sh2<TBus>).GetField(nameof(_executingCodeBlock), BindingFlags.Instance | BindingFlags.NonPublic)!);
            il.Emit(OpCodes.Ldfld, typeof(CodeBlock).GetField(nameof(CodeBlock.Address))!);
            il.Emit(OpCodes.Stloc, baseAddress);
        }
        int fastCount = 0;
        while (fastCount < words.Count && fastCount != delaySlotIndex && IsCompiledFallThroughInstruction(words[fastCount])) fastCount++;
        bool emitFast = maximumWait >= 0 && fastCount > 0 && (1L + 2L * maximumWait) * fastCount <= int.MaxValue;
        Label boundedPayload = il.DefineLabel(), afterFastPayload = il.DefineLabel();
        if (emitFast)
        {
            int[] remainingCosts = new int[fastCount + 1];
            for (int i = fastCount - 1; i >= 0; i--)
                remainingCosts[i] = remainingCosts[i + 1] + 1 + maximumWait + (IsCompiledReadInstruction(words[i]) ? maximumWait : 0);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, remainingCosts[0]);
            EmitInlinedCall(il, CanRunFastPayload); il.Emit(OpCodes.Brfalse, boundedPayload);
            FieldInfo pcField = typeof(Sh2<TBus>).GetField(nameof(PC), BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (int i = 0; i < fastCount; i++)
            {
                if (i == 0 && nativeCount > 0)
                {
                    for (int n = 0; n < nativeCount; n++)
                    {
                        il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, start + (uint)(n * 2) + 4, start, baseAddress); il.Emit(OpCodes.Stfld, pcField);
                        il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, start + (uint)(n * 2), start, baseAddress); il.Emit(OpCodes.Ldc_I4, n == 0 ? 1 : 0);
                        EmitInlinedCall(il, ChargeFastFetch);
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, typeof(Sh2<TBus>).GetMethod(nameof(ExecuteX64ArithmeticPrefix), BindingFlags.Instance | BindingFlags.NonPublic)!);
                    il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, start + (uint)(nativeCount * 2) + 4, start, baseAddress);
                    EmitInlinedCall(il, FinishFastLinear); il.Emit(OpCodes.Brfalse, end);
                    i = nativeCount - 1;
                    continue;
                }
                uint address = start + (uint)(i * 2);
                // Only side-effect-free fall-through instructions are admitted.
                // Commit the known PC before an access/helper that can observe it.
                il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, address + 4, start, baseAddress); il.Emit(OpCodes.Stfld, pcField);
                il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, address, start, baseAddress); il.Emit(OpCodes.Ldc_I4, i == 0 ? 1 : 0);
                EmitInlinedCall(il, ChargeFastFetch);
                EmitCompiledInstruction(il, words[i]);
                il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, address + 6, start, baseAddress);
                if (IsCompiledReadInstruction(words[i]))
                {
                    il.Emit(OpCodes.Ldc_I4, remainingCosts[i + 1]); EmitInlinedCall(il, FinishFastRead);
                }
                else EmitInlinedCall(il, FinishFastLinear);
                il.Emit(OpCodes.Brfalse, end);
            }
            il.Emit(OpCodes.Br, afterFastPayload);
        }
        il.MarkLabel(boundedPayload);
        for (int firstIndex = 0; firstIndex < words.Count;)
        {
            if (emitFast && firstIndex == fastCount) il.MarkLabel(afterFastPayload);
            if (firstIndex == delaySlotIndex || !IsCompiledPureInstruction(words[firstIndex]))
            {
                EmitBoundedInstruction(il, words[firstIndex], start + (uint)(firstIndex * 2), firstIndex == 0, end, start, baseAddress);
                if (IsCompiledReadInstruction(words[firstIndex]) && firstIndex + 1 < words.Count)
                { il.Emit(OpCodes.Ldarg_0); EmitInlinedCall(il, ContinueBlockAfterRead); il.Emit(OpCodes.Brfalse, end); }
                firstIndex++; continue;
            }
            int lastIndex = firstIndex + 1;
            while (lastIndex < words.Count && lastIndex != delaySlotIndex && IsCompiledPureInstruction(words[lastIndex])) lastIndex++;
            int prefixCycles = lastIndex - firstIndex;
            for (int i = firstIndex == 0 ? 1 : firstIndex; i < lastIndex; i++)
                prefixCycles += fetchWaits[i];
            Label bounded = il.DefineLabel(), afterPrefix = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, prefixCycles); il.Emit(OpCodes.Ldc_I4, firstIndex == 0 ? 1 : 0);
            EmitInlinedCall(il, CanRunBlockPrefix); il.Emit(OpCodes.Brfalse, bounded);
            int remainingStart = firstIndex;
            if (firstIndex == 0 && nativeCount > 0)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Sh2<TBus>).GetMethod(nameof(ExecuteX64ArithmeticPrefix), BindingFlags.Instance | BindingFlags.NonPublic)!);
                remainingStart = nativeCount;
            }
            for (int i = remainingStart; i < lastIndex; i++) EmitCompiledInstruction(il, words[i]);
            il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, start + (uint)(lastIndex * 2) + 4, start, baseAddress);
            il.Emit(OpCodes.Ldc_I4, prefixCycles); il.Emit(OpCodes.Ldc_I4, firstIndex == 0 ? 1 : 0); EmitInlinedCall(il, FinishBlockPrefix);
            il.Emit(OpCodes.Br, afterPrefix); il.MarkLabel(bounded);
            for (int i = firstIndex; i < lastIndex; i++) EmitBoundedInstruction(il, words[i], start + (uint)(i * 2), i == 0, end, start, baseAddress);
            il.MarkLabel(afterPrefix); firstIndex = lastIndex;
        }
        if (emitFast && fastCount == words.Count) il.MarkLabel(afterFastPayload);
        il.MarkLabel(end);
        LocalBuilder successor = il.DeclareLocal(typeof(Action<Sh2<TBus>>)); Label exit = il.DefineLabel();
        il.Emit(OpCodes.Ldarg_0); EmitInlinedCall(il, ResolveBlockSuccessor); il.Emit(OpCodes.Stloc, successor);
        il.Emit(OpCodes.Ldloc, successor); il.Emit(OpCodes.Brfalse, exit);
        il.Emit(OpCodes.Ldloc, successor); il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Tailcall); il.Emit(OpCodes.Callvirt, typeof(Action<Sh2<TBus>>).GetMethod("Invoke")!); il.Emit(OpCodes.Ret);
        il.MarkLabel(exit); il.Emit(OpCodes.Ret);
        long emitEnd = profile is null ? 0 : Stopwatch.GetTimestamp();
        if (profile is not null) { profile.Emit += emitEnd - bodyStart; profile.IlBytes += il.ILOffset; profile.MaximumIlBytes = Math.Max(profile.MaximumIlBytes, il.ILOffset); }
        var execute = method.CreateDelegate<Action<Sh2<TBus>>>();
        long delegateEnd = profile is null ? 0 : Stopwatch.GetTimestamp();
        if (profile is not null) profile.Delegate += delegateEnd - emitEnd;
        RuntimeHelpers.PrepareDelegate(execute);
        if (profile is not null) profile.Prepare += Stopwatch.GetTimestamp() - delegateEnd;
        return execute;
    }
    private static void EmitCompiledAddress(ILGenerator il, uint address, uint start, LocalBuilder? baseAddress)
    {
        if (baseAddress is null) { il.Emit(OpCodes.Ldc_I4, unchecked((int)address)); return; }
        il.Emit(OpCodes.Ldloc, baseAddress);
        if (address != start) { il.Emit(OpCodes.Ldc_I4, unchecked((int)(address - start))); il.Emit(OpCodes.Add); }
    }
    private void EmitBoundedInstruction(ILGenerator il, ushort opcode, uint address, bool first, Label end, uint start, LocalBuilder? baseAddress)
    {
        il.Emit(OpCodes.Ldarg_0); EmitCompiledAddress(il, address, start, baseAddress);
        il.Emit(OpCodes.Ldc_I4, opcode); il.Emit(OpCodes.Ldc_I4, first ? 1 : 0);
        EmitInlinedCall(il, BeginBlockInstruction); il.Emit(OpCodes.Brfalse, end);
        EmitCompiledInstruction(il, opcode);
        il.Emit(OpCodes.Ldarg_0); EmitInlinedCall(il, EndBlockInstruction); il.Emit(OpCodes.Brfalse, end);
    }
    private bool CanRunCompiledPurePrefix(int clocks, bool first)
    {
        if (!_deferClockUpdates || InterruptReady) return false;
        ulong end = Cycles + (uint)(clocks + (first ? _entryFetchWait : 0));
        // No temporal callback is allowed inside this fused arithmetic prefix.
        return end <= _executionEnd && end < _busDeadline;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FinishCompiledPurePrefix(uint nextPc, int clocks, bool first)
    {
        PC = nextPc; ID = 0;
        step(clocks + (first ? _entryFetchWait : 0));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanContinueCompiledAfterRead()
    {
        // MMIO may schedule an event earlier than the limit captured at entry.
        _blockEventLimit = _deferClockUpdates ? _busDeadline : ulong.MaxValue;
        if (InterruptReady || _executingCodeBlock is not { } block) return false;
        ulong epoch = _bus.CodeEpoch(block.Address);
        if (epoch != 0 && epoch == _executingCodeEpoch) return true;
        if (!BlockVersionsMatch(block)) return false;
        _executingCodeEpoch = block.CodeEpoch = epoch;
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanRunCompiledFastPayload(int maximumClocks)
    {
        if (!_deferClockUpdates || _branch != 0 || ID != 0 || Sleeping || _yield || InterruptReady) return false;
        ulong end = Cycles + (uint)maximumClocks;
        return end <= _executionEnd && end < _busDeadline;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ChargeCompiledFastFetch(uint address, bool first)
    {
        int wait = _entryFetchWait;
        if (!first) _bus.Fetch16(address, out wait);
        // The bound gates the path; only actual bus clocks are charged.
        step(wait + 1);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FinishCompiledFastLinear(uint nextPc)
    {
        PC = nextPc; ID = 0;
        if (!_yield) return true;
        _yield = false;
        if (Cycles < _executionEnd) step((int)(_executionEnd - Cycles));
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FinishCompiledFastRead(uint nextPc, int remainingMaximumClocks)
    {
        bool completed = FinishCompiledFastLinear(nextPc);
        _blockEventLimit = _deferClockUpdates ? _busDeadline : ulong.MaxValue;
        if (!completed || !CanContinueCompiledAfterRead()) return false;
        return remainingMaximumClocks == 0 || CanRunCompiledFastPayload(remainingMaximumClocks);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool BeginCompiledInstruction(uint expectedAddress, ushort expectedOpcode, bool first)
    {
        if (Cycles >= _executionEnd || Cycles >= _blockEventLimit) return false;
        if (_irqLevel != 0 && InterruptReady && _branch == 0 && ID == 0) return false;
        ID = 0;
        int wait = _entryFetchWait;
        if (!first && _bus.Fetch16(expectedAddress, out wait) != expectedOpcode) return false;
        step(wait + 1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompleteInstruction()
    {
        switch (_branch)
        {
            case 0: PC = unchecked(PC + 2); break;
            case 1: PC = unchecked(PC + 2); _branch = 2; break;
            case 2: PC = PPC; _branch = 0; break;
        }
        if (!_yield) return true;
        _yield = false; if (Cycles < _executionEnd) step((int)(_executionEnd - Cycles));
        return false;
    }
}
