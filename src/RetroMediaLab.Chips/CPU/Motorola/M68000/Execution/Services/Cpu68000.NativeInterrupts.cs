using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public sealed partial class Cpu68000<TBus> where TBus : class, ICpu68000Bus
{
    private const int NativeDelayedCacheCapacity = 512;
    private Dictionary<(uint Address, ushort Opcode), NativeDelayedEntry>? _nativeDelayedEntries;
    private LinkedList<(uint Address, ushort Opcode)>? _nativeDelayedOrder;
    private int _nativeDelayedRatio, _nativeDelayedShift;
    private sealed record NativeDelayedEntry(M68000X64Block Block, LinkedListNode<(uint Address, ushort Opcode)> Node, bool ObservedCode);
    public long NativeDelayedBlockBuildCount { get; private set; }
    public long NativeDelayedBlockEntryCount { get; private set; }
    public long NativeDelayedFallbackInstructionCount { get; private set; }
    public int NativeDelayedCachedBlockCount { get { lock (_nativeGate) return _nativeDelayedEntries?.Count ?? 0; } }

    /// <summary>
    /// Native counterpart of SetIrqDelay. The previous instruction's base cost
    /// is charged before the delayed body; the enclosing runner charges the
    /// final IR's base cost. This body must run even past the caller's budget.
    /// </summary>
    public void SetIrqDelayNative(uint level)
    {
        lock (_nativeGate)
        {
            ObjectDisposedException.ThrowIf(_nativeDisposed, this);
            if (_bus is not ICpu68000CodeBus codeBus || !OperatingSystem.IsWindows()
                || RuntimeInformation.ProcessArchitecture != Architecture.X64 || _cycleRatio < 1)
            {
                if (!_irqLatency && (REG_IR & 0xf000) != 0x2000) NativeDelayedFallbackInstructionCount++;
                SetIrqDelay(level); return;
            }
            if (!_irqLatency)
            {
                if ((REG_IR & 0xf000) != 0x2000)
                {
                    USE_CYCLES(CYC_INSTRUCTION[(int)REG_IR]);
                    _irqLatency = true;
                    try
                    {
                        if (!TryExecuteNativeDelayedInstruction(codeBus))
                        {
                            NativeDelayedFallbackInstructionCount++;
                            REG_IR = m68ki_read_imm_16(); ExecuteInstruction();
                        }
                    }
                    finally { _irqLatency = false; }
                }
                CPU_INT_LEVEL = level << 8;
            }
            m68ki_check_interrupts();
        }
    }

    private unsafe bool TryExecuteNativeDelayedInstruction(ICpu68000CodeBus codeBus)
    {
        uint address = REG_PC;
        if ((address & 1) != 0 || !codeBus.TryGetCodeMemory(address, out var window, out uint logicalBase)) return false;
        uint masked = address & CPU_ADDRESS_MASK;
        if (masked < logicalBase || (ulong)(masked - logicalBase) + 2 > (ulong)window.Length) return false;
        ushort opcode = _prefetch && CPU_PREF_ADDR == address ? (ushort)CPU_PREF_DATA
            : BinaryPrimitives.ReadUInt16LittleEndian(window.Span[(int)(masked - logicalBase)..]);
        var key = (Address: address, Opcode: opcode);
        if (_nativeDelayedRatio != _cycleRatio || _nativeDelayedShift != _cycleShift)
        {
            ClearNativeDelayedCache(); _nativeDelayedRatio = _cycleRatio; _nativeDelayedShift = _cycleShift;
        }
        _nativeMemoryMap ??= new(); InstallNativeBus();
        bool invalidate = false;
        if (_bus is ICpu68000DirectMemoryBus data)
        {
            long version = data.DirectMemoryVersion; _nativeMemoryMap.Update(data);
            invalidate = !_nativeLinkVersionKnown || version != _nativeLinkVersion;
            _nativeLinkVersion = version; _nativeLinkVersionKnown = true;
        }
        if (codeBus is ICpu68000VersionedCodeBus versioned)
        {
            long version = versioned.CodeMemoryVersion;
            invalidate |= !_nativeCodeVersionKnown || version != _nativeCodeVersion;
            _nativeCodeVersion = version; _nativeCodeVersionKnown = true;
        }
        if (invalidate) InvalidateNativeLinks();
        _nativeRefreshMode = (_bus as ICpu68000NativeRefreshBus)?.NativeRefreshMode ?? Cpu68000NativeRefreshMode.Unsupported;
        _nativeMemoryMap.SetRefreshWindow(_nativeRefreshMode, _nativeRefreshMode == Cpu68000NativeRefreshMode.StableDuringNativeExecution
            && _bus.IsRefreshWindowElapsed(_state.cycles, _state.refresh_cycles));
        // Nested steps share the parent's table. Preserve flags from earlier
        // accesses of that outer instruction until its own epilogue consumes them.
        if (_nativeExecutionDepth == 0) _nativeMemoryMap.ResetBusStatus();
        _nativeDelayedEntries ??= []; _nativeDelayedOrder ??= [];
        bool observed = codeBus is ICpu68000ObservedCodeBus observer && observer.RequiresCodeFetchObservation(address);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (_nativeDelayedEntries.TryGetValue(key, out var entry) && (!entry.Block.Matches(window, logicalBase) || entry.ObservedCode != observed))
            { RemoveNativeDelayedEntry(key); entry = null; }
            if (entry is null)
            {
                var block = M68000X64Block.TryCompile(window, logicalBase, address, Configuration, _instructionCycles, _cycleRatio, _cycleShift,
                    maximumInstructions: 1, directMemory: true, protectedWrites: _bus is ICpu68000ProtectedMemoryBus, busCallbacks: true,
                    isIllegalOpcode: static value => InstructionMap[value] == InstructionMap[0x4afc], delayedInstruction: true, initialOpcode: opcode,
                    cacheGuestRegisters: _useNativeGuestRegisters, observeInstructionFetches: observed);
                if (block is null) return false;
                bool owned = false;
                try
                {
                    if (_nativeDelayedEntries.Count >= NativeDelayedCacheCapacity) RemoveNativeDelayedEntry(_nativeDelayedOrder.First!.Value);
                    var node = _nativeDelayedOrder.AddLast(key);
                    try { entry = new(block, node, observed); _nativeDelayedEntries.Add(key, entry); owned = true; NativeDelayedBlockBuildCount++; }
                    finally { if (!owned) _nativeDelayedOrder.Remove(node); }
                }
                finally { if (!owned) block.Dispose(); }
            }
            var map = _nativeMemoryMap ?? throw new InvalidOperationException("Native memory map is not initialized.");
            M68000X64BlockExit reason;
            _nativeExecutionDepth++;
            try
            {
                fixed (uint* registers = _registers)
                fixed (Cpu68000State* state = &_state)
                    reason = entry.Block.ExecuteLinked((nint)registers, (nint)state, map.Pointer, 0, out _);
            }
            finally { FinishNativeExecution(); }
            ThrowNativeBusError(); ObjectDisposedException.ThrowIf(_nativeDisposed, this);
            NativeDelayedBlockEntryCount++;
            if (reason == M68000X64BlockExit.CodeChanged) { RemoveNativeDelayedEntry(key); continue; }
            return reason != M68000X64BlockExit.MemoryDenied;
        }
        return false;
    }

    private void RemoveNativeDelayedEntry((uint Address, ushort Opcode) key)
    {
        if (_nativeDelayedEntries is null || !_nativeDelayedEntries.Remove(key, out var entry)) return;
        _nativeDelayedOrder!.Remove(entry.Node); RetireNativeBlock(entry.Block);
    }
    private void ClearNativeDelayedCache()
    {
        var entries = _nativeDelayedEntries; _nativeDelayedEntries = null; _nativeDelayedOrder = null;
        if (entries is null) return;
        List<Exception>? errors = null;
        foreach (var entry in entries.Values)
            try { RetireNativeBlock(entry.Block); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Unable to close every delayed native instruction.", errors);
    }
}
