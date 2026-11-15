using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    public bool UseCompiledBlocks { get; set; }
    public bool LinkCompiledBlocks { get; set; } = true;
    public Type? SpecializedExternalBusType { get; set; }
    // The native runtime already supplies the optimized dispatcher; a reference
    // CPU remains available without building a second giant copy of the loop.
    private void ExecuteNativeLoop() => ExecuteLoop();
    private const int MaximumBlockInstructions = 64;
    private const int MaximumCachedBlocks = 4096;
    private sealed class CodeBlock(uint address, ushort[] instructions, uint[] versionAddresses, long[] versions, Action<Sh2<TBus>> execute)
    {
        public uint Address = address;
        public ushort[] Instructions = instructions;
        public uint[] VersionAddresses = versionAddresses;
        public long[] Versions = versions;
        public Action<Sh2<TBus>> Execute = execute;
        public ulong CodeEpoch;
        public int SteadyFetchWait;
        public bool Active = true;
        public CodeBlock? Successor0, Successor1;
    }
    private readonly Dictionary<uint, CodeBlock> _codeBlocks = [];
    private readonly Dictionary<uint, List<CodeBlock>> _inactiveCodeBlocks = [];
    private readonly Queue<uint> _codeBlockOrder = [];
    private readonly CodeBlock?[] _codeBlockLookup = new CodeBlock?[256];
    private int _entryFetchWait;
    private ulong _blockEventLimit;
    private CodeBlock? _executingCodeBlock;
    private ulong _executingCodeEpoch;
    private CodeBlock? _linkSourceCandidate;
    public long CompiledBlockBuildCount { get; private set; }
    public long CompiledBlockBuildTicks { get; private set; }
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
        foreach (CodeBlock block in _codeBlocks.Values) DeactivateCodeBlock(block);
        _codeBlocks.Clear(); _inactiveCodeBlocks.Clear(); _codeBlockOrder.Clear(); Array.Clear(_codeBlockLookup);
        _executingCodeBlock = _linkSourceCandidate = null;
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
            block = ResolveCodeBlock(address, first, block);
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
        for (int i = 1; i < block.Instructions.Length; i++)
            if (_bus.Fetch16(block.Address + (uint)(i * 2)) != block.Instructions[i]) return false;
        return true;
    }
    private void CaptureBlockVersions(CodeBlock block)
    {
        _bus.Fetch16(block.Address, out int steadyWait);
        if (_bus.TryMapFetch(block.Address, out _, out _, out int mappedWait)) steadyWait = mappedWait;
        block.SteadyFetchWait = steadyWait;
        _bus.MarkCode(block.Address, block.Instructions.Length * 2);
        // Fetches may fill or replace physical cache lines: snapshot afterwards.
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
        { CaptureBlockVersions(previous); return previous; }
        if (_inactiveCodeBlocks.TryGetValue(address, out List<CodeBlock>? inactive))
        {
            for (int i = 0; i < inactive.Count; i++)
            {
                CodeBlock old = inactive[i];
                if (!BlockWordsMatch(old, first)) continue;
                inactive.RemoveAt(i);
                if (previous is not null) { DeactivateCodeBlock(previous); inactive.Add(previous); }
                CaptureBlockVersions(old); _codeBlocks[address] = old; return old;
            }
        }
        if (previous is not null)
        {
            DeactivateCodeBlock(previous);
            if (inactive is null) _inactiveCodeBlocks[address] = inactive = [];
            if (inactive.Count == 4) inactive.RemoveAt(0);
            inactive.Add(previous);
        }
        else
        {
            if (_codeBlocks.Count == MaximumCachedBlocks)
            {
                uint evicted = _codeBlockOrder.Dequeue();
                DeactivateCodeBlock(_codeBlocks[evicted]);
                _codeBlocks.Remove(evicted); _inactiveCodeBlocks.Remove(evicted);
                int slot = (int)(evicted >> 1 & 255);
                if (_codeBlockLookup[slot]?.Address == evicted) _codeBlockLookup[slot] = null;
            }
            _codeBlockOrder.Enqueue(address);
        }
        CodeBlock block = CompileBlock(address, first);
        _codeBlocks[address] = block; return block;
    }

    private CodeBlock CompileBlock(uint start, ushort first)
    {
        long buildStart = Stopwatch.GetTimestamp();
        CompiledBlockBuildCount++;
        var words = new List<ushort>(MaximumBlockInstructions);
        int delaySlotIndex = -1;
        for (int i = 0; i < MaximumBlockInstructions; i++)
        {
            uint address = start + (uint)(i * 2);
            if (_bus.CodeVersion(address) < 0) break;
            ushort opcode = i == 0 ? first : _bus.Fetch16(address);
            words.Add(opcode);
            if (!IsCompiledFallThroughInstruction(opcode))
            {
                if (IsCompiledDelayedBranch(opcode) && i + 1 < MaximumBlockInstructions && _bus.CodeVersion(address + 2) >= 0)
                {
                    ushort slotOpcode = _bus.Fetch16(address + 2);
                    if (IsCompiledPureInstruction(slotOpcode)) { delaySlotIndex = words.Count; words.Add(slotOpcode); }
                }
                break;
            }
        }
        string name = $"SH2_{start:X8}_{CompiledBlockBuildCount}";
        // DynamicMethod inherits debugger/JIT options from its associated module.
        // Keep generated native blocks with their optimized helper module; do not
        // alter the Debug configuration of the application or reference interpreter.
        var method = NativeHelperMaps.ContainsKey(GetType())
            ? new DynamicMethod(name, typeof(void), [typeof(Sh2<TBus>)], GetType().Module, true)
            : new DynamicMethod(name, typeof(void), [typeof(Sh2<TBus>)], typeof(Sh2<TBus>), true);
        ILGenerator il = method.GetILGenerator(); Label end = il.DefineLabel();
        int fastCount = 0, maximumWait = _bus.MaximumWaitStates;
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
                uint address = start + (uint)(i * 2);
                // Only side-effect-free fall-through instructions are admitted.
                // Commit the known PC before an access/helper that can observe it.
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, unchecked((int)(address + 4))); il.Emit(OpCodes.Stfld, pcField);
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, unchecked((int)address)); il.Emit(OpCodes.Ldc_I4, i == 0 ? 1 : 0);
                EmitInlinedCall(il, ChargeFastFetch);
                EmitCompiledInstruction(il, words[i]);
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, unchecked((int)(address + 6)));
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
                EmitBoundedInstruction(il, words[firstIndex], start + (uint)(firstIndex * 2), firstIndex == 0, end);
                if (IsCompiledReadInstruction(words[firstIndex]) && firstIndex + 1 < words.Count)
                { il.Emit(OpCodes.Ldarg_0); EmitInlinedCall(il, ContinueBlockAfterRead); il.Emit(OpCodes.Brfalse, end); }
                firstIndex++; continue;
            }
            int lastIndex = firstIndex + 1;
            while (lastIndex < words.Count && lastIndex != delaySlotIndex && IsCompiledPureInstruction(words[lastIndex])) lastIndex++;
            int prefixCycles = lastIndex - firstIndex;
            for (int i = firstIndex == 0 ? 1 : firstIndex; i < lastIndex; i++)
            { _bus.Fetch16(start + (uint)(i * 2), out int wait); prefixCycles += wait; }
            Label bounded = il.DefineLabel(), afterPrefix = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, prefixCycles); il.Emit(OpCodes.Ldc_I4, firstIndex == 0 ? 1 : 0);
            EmitInlinedCall(il, CanRunBlockPrefix); il.Emit(OpCodes.Brfalse, bounded);
            for (int i = firstIndex; i < lastIndex; i++) EmitCompiledInstruction(il, words[i]);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, unchecked((int)(start + (uint)(lastIndex * 2) + 4)));
            il.Emit(OpCodes.Ldc_I4, prefixCycles); il.Emit(OpCodes.Ldc_I4, firstIndex == 0 ? 1 : 0); EmitInlinedCall(il, FinishBlockPrefix);
            il.Emit(OpCodes.Br, afterPrefix); il.MarkLabel(bounded);
            for (int i = firstIndex; i < lastIndex; i++) EmitBoundedInstruction(il, words[i], start + (uint)(i * 2), i == 0, end);
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
        uint firstLine = start & ~15u, lastLine = (start + (uint)(words.Count * 2 - 2)) & ~15u;
        uint[] addresses = new uint[(lastLine - firstLine) / 16 + 1];
        for (int i = 0; i < addresses.Length; i++) addresses[i] = firstLine + (uint)(i * 16);
        var block = new CodeBlock(start, words.ToArray(), addresses, new long[addresses.Length], method.CreateDelegate<Action<Sh2<TBus>>>());
        RuntimeHelpers.PrepareDelegate(block.Execute);
        CaptureBlockVersions(block);
        CompiledBlockBuildTicks += Stopwatch.GetTimestamp() - buildStart;
        return block;
    }
    private void EmitBoundedInstruction(ILGenerator il, ushort opcode, uint address, bool first, Label end)
    {
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, unchecked((int)address));
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
