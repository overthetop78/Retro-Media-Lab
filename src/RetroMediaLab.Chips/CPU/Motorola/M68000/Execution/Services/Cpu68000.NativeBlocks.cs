using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

public sealed record Cpu68000NativeFallback(ushort Opcode, string Handler, long Attempts);
public sealed record Cpu68000NativeProfile(long BoundaryExits, long MemoryDeniedExits, long CodeChangedExits,
    long NoCodeWindowFallbacks, long MissingBodyFallbacks, Cpu68000NativeFallback[] Fallbacks,
    long TotalTicks = 0, long NativeSelectionTicks = 0, long CompilationTicks = 0,
    long NativeExecutionTicks = 0, long FallbackTicks = 0);

public sealed partial class Cpu68000<TBus> : IDisposable where TBus : class, ICpu68000Bus
{
    private readonly object _nativeGate = new();
    private Dictionary<uint, NativeEntry>? _nativeEntries;
    private NativeEntry?[]?[]? _nativePages;
    private LinkedList<uint>? _nativeAllocationOrder;
    private M68000X64MemoryMap? _nativeMemoryMap;
    private M68000X64BlockLinks? _nativeLinks;
    private long _nativeLinkVersion;
    private bool _nativeLinkVersionKnown;
    private long _nativeCodeVersion;
    private bool _nativeCodeVersionKnown;
    private ulong _nativePublicationEpoch = 1;
    private Cpu68000NativeRefreshMode _nativeRefreshMode;
    private bool _nativeDisposed;
    private int _nativeRatio, _nativeShift;
    private NativeProfile? _nativeProfile;
    private const int NativeCacheCapacity = 4096;
    private bool _useNativeGuestRegisters;
    private bool _useNativeStableCode;
    /// <summary>Uses owner-certified ROM version barriers. Writable code retains per-instruction guards.</summary>
    public bool UseNativeStableCode
    {
        get { lock (_nativeGate) return _useNativeStableCode; }
        set
        {
            lock (_nativeGate)
            {
                ObjectDisposedException.ThrowIf(_nativeDisposed, this);
                if (_useNativeStableCode == value) return;
                if (_nativeExecutionDepth != 0 || _nativeRunDepth != 0)
                    throw new InvalidOperationException("The native code policy cannot change during execution.");
                ClearNativeCache();
                _useNativeStableCode = value;
            }
        }
    }
    /// <summary>Selects the guest-register ABI. Existing native blocks remain the default.</summary>
    public bool UseNativeGuestRegisters
    {
        get { lock (_nativeGate) return _useNativeGuestRegisters; }
        set
        {
            lock (_nativeGate)
            {
                ObjectDisposedException.ThrowIf(_nativeDisposed, this);
                if (_useNativeGuestRegisters == value) return;
                if (_nativeExecutionDepth != 0 || _nativeRunDepth != 0)
                    throw new InvalidOperationException("The native register ABI cannot change during execution.");
                ClearNativeCache();
                _useNativeGuestRegisters = value;
            }
        }
    }
    public long NativeBlockBuildCount { get; private set; }
    public long NativeStableBlockBuildCount { get; private set; }
    public long NativeStableBlockReuseCount { get; private set; }
    public long NativeCompilationTicks { get; private set; }
    public long NativeBlockEntryCount { get; private set; }
    public long NativeFallbackInstructionCount { get; private set; }
    public int NativeCachedBlockCount { get { lock (_nativeGate) return _nativeEntries?.Values.Count(entry => entry.Block is not null) ?? 0; } }

    private sealed class NativeEntry(ReadOnlyMemory<byte> window, uint logicalBase, ushort opcode,
        M68000X64Block? block, LinkedListNode<uint> node)
    {
        internal readonly ReadOnlyMemory<byte> Window = window;
        internal readonly uint Base = logicalBase;
        internal readonly ushort Opcode = opcode;
        internal readonly M68000X64Block? Block = block;
        internal readonly LinkedListNode<uint> Node = node;
        internal readonly nint EntryPoint = block?.LinkedEntryPoint ?? 0;
        internal long CodeVersion;
        internal bool HasCodeVersion;
        internal bool StableContent;
        internal bool ObservedCode;
        internal long ContentVersion;
        internal ulong PublishedEpoch;
    }
    private sealed class NativeProfile
    {
        internal readonly long[] Fallbacks = new long[65536];
        internal long Boundary, MemoryDenied, CodeChanged, NoCodeWindow, MissingBody;
        internal long TotalTicks, NativeSelectionTicks, CompilationTicks, NativeExecutionTicks, FallbackTicks;
    }
    /// <summary>Separate diagnostic pass only. Disabled by default; enabling resets the profile.</summary>
    public void EnableNativeProfiling(bool enabled)
    {
        lock (_nativeGate)
        {
            ObjectDisposedException.ThrowIf(_nativeDisposed, this);
            _nativeProfile = enabled ? new() : null;
        }
    }
    public Cpu68000NativeProfile GetNativeProfile()
    {
        lock (_nativeGate)
        {
            if (_nativeProfile is not { } profile) return new(0, 0, 0, 0, 0, []);
            var rows = new List<Cpu68000NativeFallback>();
            var handlers = DynamicInstructionHandlers;
            for (int opcode = 0; opcode < profile.Fallbacks.Length; opcode++)
                if (profile.Fallbacks[opcode] != 0)
                    rows.Add(new((ushort)opcode, handlers[InstructionMap[opcode]].Name, profile.Fallbacks[opcode]));
            return new(profile.Boundary, profile.MemoryDenied, profile.CodeChanged, profile.NoCodeWindow, profile.MissingBody,
                rows.OrderByDescending(row => row.Attempts).ToArray(), profile.TotalTicks, profile.NativeSelectionTicks,
                profile.CompilationTicks, profile.NativeExecutionTicks, profile.FallbackTicks);
        }
    }

    /// <summary>
    /// Experimental native block runner. Missing native bodies execute through
    /// the existing instruction handler. This is not yet a full-native backend.
    /// Run remains the original interpreter and is never redirected here.
    /// </summary>
    public void RunNative(uint cycles)
    {
        lock (_nativeGate)
        {
            ObjectDisposedException.ThrowIf(_nativeDisposed, this);
            if (_bus is not ICpu68000CodeBus codeBus || !OperatingSystem.IsWindows()
                || RuntimeInformation.ProcessArchitecture != Architecture.X64 || _cycleRatio < 1)
            { Run(cycles); return; }
            if (unchecked((uint)_state.cycles) >= cycles) return;
            m68ki_check_interrupts();
            if (CPU_STOPPED != 0) { _state.cycles = unchecked((int)cycles); return; }
            if (_nativeRatio != _cycleRatio || _nativeShift != _cycleShift)
            {
                ClearNativeCache(); _nativeRatio = _cycleRatio; _nativeShift = _cycleShift;
            }
            uint previousTarget = _state.cycle_end;
            _nativeRunDepth++;
            _state.cycle_end = cycles;

            bool addressFault = false;
            var timing = _nativeProfile;
            long runStart = timing is null ? 0 : Stopwatch.GetTimestamp();
            try
            {
                _nativeRefreshMode = (_bus as ICpu68000NativeRefreshBus)?.NativeRefreshMode ?? Cpu68000NativeRefreshMode.Unsupported;
                while (true)
                {
                    try
                    {
                        if (addressFault) { addressFault = false; m68ki_exception_address_error(); }
                        while (unchecked((uint)_state.cycles) < cycles)
                        {
                            long nativeStart = timing is null ? 0 : Stopwatch.GetTimestamp();
                            bool executed = TryExecuteNativeBlock(codeBus);
                            if (timing is not null) timing.NativeSelectionTicks += Stopwatch.GetTimestamp() - nativeStart;
                            if (executed) continue;
                            long fallbackStart = timing is null ? 0 : Stopwatch.GetTimestamp();
                            FetchDynamicInstruction();
                            if (_nativeProfile is { } profile) profile.Fallbacks[(ushort)REG_IR]++;
                            ExecuteInstruction(); FinishDynamicInstruction();
                            NativeFallbackInstructionCount++;
                            if (timing is not null) timing.FallbackTicks += Stopwatch.GetTimestamp() - fallbackStart;
                        }
                        break;
                    }
                    catch (AddressFault) { addressFault = true; }
                }
            }
            finally
            {
                if (timing is not null) timing.TotalTicks += Stopwatch.GetTimestamp() - runStart;
                _nativeRunDepth--;
                _state.cycle_end = _nativeRunDepth == 0 || _nativeDisposed ? 0 : previousTarget;
            }
        }
    }

    private unsafe bool TryExecuteNativeBlock(ICpu68000CodeBus codeBus)
    {
        uint address = REG_PC;
        if ((address & 1) != 0)
        { if (_nativeProfile is { } profile) profile.NoCodeWindow++; return false; }
        var versionedCode = codeBus as ICpu68000VersionedCodeBus;
        long codeVersion = versionedCode?.CodeMemoryVersion ?? 0;
        var entry = FindNativeEntry(address);
        ReadOnlyMemory<byte> window; uint logicalBase;
        if (entry is not null && versionedCode is not null && entry.HasCodeVersion && entry.CodeVersion == codeVersion)
        {
            // The bus guarantees the window identity/policy until its revision
            // changes. Byte mutations remain covered by native opcode guards.
            window = entry.Window; logicalBase = entry.Base;
        }
        else
        {
            if (!codeBus.TryGetCodeMemory(address, out window, out logicalBase))
            { if (_nativeProfile is { } profile) profile.NoCodeWindow++; return false; }
            uint masked = address & CPU_ADDRESS_MASK;
            if (masked < logicalBase || (ulong)(masked - logicalBase) + 2 > (ulong)window.Length)
            { if (_nativeProfile is { } profile) profile.NoCodeWindow++; return false; }
            if (entry is not null)
            {
                bool sameWindow = entry.Window.Equals(window) && entry.Base == logicalBase;
                bool sameStableContent = !entry.StableContent || codeBus is ICpu68000StableCodeBus stable
                    && stable.IsCodeContentStable(address, window, logicalBase)
                    && stable.GetStableCodeContentVersion(address, window, logicalBase) == entry.ContentVersion;
                bool sameObservation = entry.ObservedCode == (codeBus is ICpu68000ObservedCodeBus observed && observed.RequiresCodeFetchObservation(address));
                if (!sameWindow || !sameStableContent || !sameObservation) { RemoveNativeEntry(address); entry = null; }
                else
                {
                    // Revalidate mapping/policy, but do not regenerate immutable
                    // ROM bodies merely because an unrelated bank changed.
                    if (entry.StableContent && entry.CodeVersion != codeVersion) NativeStableBlockReuseCount++;
                    entry.CodeVersion = codeVersion; entry.HasCodeVersion = versionedCode is not null;
                }
            }
        }
        ushort opcode = entry?.Opcode ?? 0;
        if (entry is null || entry.Block is null)
        {
            // Keep invalid host words on their existing error path. This is a
            // cold selection check, not extra work on each compiled-block hit.
            if (_prefetch && CPU_PREF_ADDR == address && CPU_PREF_DATA > ushort.MaxValue) return false;
            opcode = _prefetch && CPU_PREF_ADDR == address ? (ushort)CPU_PREF_DATA
                : BinaryPrimitives.ReadUInt16LittleEndian(window.Span[(int)((address & CPU_ADDRESS_MASK) - logicalBase)..]);
            // Unsupported starts have no native guard. Their live opcode must
            // still be read even if only the bytes changed, not the mapping.
            if (entry is not null && entry.Opcode != opcode) { RemoveNativeEntry(address); entry = null; }
        }
        bool invalidateLinks = versionedCode is not null && (!_nativeCodeVersionKnown || codeVersion != _nativeCodeVersion);
        _nativeCodeVersion = codeVersion; _nativeCodeVersionKnown = versionedCode is not null;
        _nativeMemoryMap ??= new(); InstallNativeBus();
        if (_bus is ICpu68000DirectMemoryBus dataBus)
        {
            long version = dataBus.DirectMemoryVersion;
            _nativeMemoryMap.Update(dataBus);
            if (!_nativeLinkVersionKnown || version != _nativeLinkVersion)
            {
                invalidateLinks = true;
                _nativeLinkVersion = version; _nativeLinkVersionKnown = true;
            }
        }
        // Bus callbacks also work when no data bank has a direct projection.
        _nativeMemoryMap.SetRefreshWindow(_nativeRefreshMode, _nativeRefreshMode == Cpu68000NativeRefreshMode.StableDuringNativeExecution
            && _bus.IsRefreshWindowElapsed(_state.cycles, _state.refresh_cycles));
        if (versionedCode is not null) _nativeLinks ??= new();
        _nativeMemoryMap.ResetBusStatus();
        if (invalidateLinks) InvalidateNativeLinks();
        _nativeEntries ??= []; _nativeAllocationOrder ??= new();
        if (entry is null)
        {
            bool stableContent = _useNativeStableCode && codeBus is ICpu68000StableCodeBus stable
                && stable.IsCodeContentStable(address, window, logicalBase);
            long contentVersion = stableContent
                ? ((ICpu68000StableCodeBus)codeBus).GetStableCodeContentVersion(address, window, logicalBase) : 0;
            long compileStart = Stopwatch.GetTimestamp();
            var block = M68000X64Block.TryCompile(window, logicalBase, address, Configuration, _instructionCycles, _cycleRatio, _cycleShift,
                directMemory: _nativeMemoryMap is not null, linkBlocks: _nativeLinks is not null,
                protectedWrites: _bus is ICpu68000ProtectedMemoryBus, busCallbacks: true,
                isIllegalOpcode: static opcode => InstructionMap[opcode] == InstructionMap[0x4afc], initialOpcode: opcode,
                cacheGuestRegisters: _useNativeGuestRegisters,
                getLinkSlot: _useNativeGuestRegisters && _nativeLinks is not null ? _nativeLinks.GetSlot : null,
                immutableCode: stableContent,
                observeInstructionFetches: codeBus is ICpu68000ObservedCodeBus observedCode && observedCode.RequiresCodeFetchObservation(address));
            long compileTicks = Stopwatch.GetTimestamp() - compileStart;
            NativeCompilationTicks += compileTicks;
            if (_nativeProfile is { } compilation) compilation.CompilationTicks += compileTicks;
            bool cached = false;
            try
            {
                if (_nativeEntries.Count >= NativeCacheCapacity) RemoveNativeEntry(_nativeAllocationOrder.First!.Value);
                var node = _nativeAllocationOrder.AddLast(address);
                try
                {
                    entry = new(window, logicalBase, opcode, block, node)
                    {
                        CodeVersion = codeVersion, HasCodeVersion = versionedCode is not null,
                        StableContent = stableContent, ContentVersion = contentVersion,
                        ObservedCode = codeBus is ICpu68000ObservedCodeBus observation && observation.RequiresCodeFetchObservation(address)
                    };
                    IndexNativeEntry(address, entry);
                    _nativeEntries.Add(address, entry); cached = true;
                    if (block is not null)
                    {
                        NativeBlockBuildCount++;
                        if (stableContent) NativeStableBlockBuildCount++;
                    }
                }
                finally { if (!cached) { IndexNativeEntry(address, null); _nativeAllocationOrder.Remove(node); } }
            }
            finally { if (!cached) block?.Dispose(); }
        }
        if (entry.Block is null) { if (_nativeProfile is { } profile) profile.MissingBody++; return false; }
        if (_nativeLinks is not null && entry.PublishedEpoch != _nativePublicationEpoch)
        {
            _nativeLinks.Publish(address, entry.EntryPoint); entry.PublishedEpoch = _nativePublicationEpoch;
        }
        int beforeCycles = _state.cycles;
        M68000X64BlockExit reason;
        uint exitBlock;
        long executeStart = _nativeProfile is null ? 0 : Stopwatch.GetTimestamp();
        var executionMap = _nativeMemoryMap ?? throw new InvalidOperationException("Native memory map is not initialized.");
        _nativeExecutionDepth++;
        try
        {
            fixed (uint* registers = _registers)
            fixed (Cpu68000State* state = &_state)
                reason = entry.Block.ExecuteLinked((nint)registers, (nint)state, executionMap.Pointer,
                    _nativeLinks?.Pointer ?? 0, out exitBlock);
        }
        finally { FinishNativeExecution(); }
        ThrowNativeBusError();
        ObjectDisposedException.ThrowIf(_nativeDisposed, this);
        if (_nativeProfile is { } execution) execution.NativeExecutionTicks += Stopwatch.GetTimestamp() - executeStart;
        NativeBlockEntryCount++;
        if (_nativeProfile is { } exits)
        {
            switch (reason)
            {
                case M68000X64BlockExit.Boundary: exits.Boundary++; break;
                case M68000X64BlockExit.MemoryDenied: exits.MemoryDenied++; break;
                case M68000X64BlockExit.CodeChanged: exits.CodeChanged++; break;
            }
        }
        if (reason == M68000X64BlockExit.CodeChanged)
        {
            RemoveNativeEntry(exitBlock);
            // Guards exit before the stale instruction mutates guest state.
            // The next selection recompiles against its live/prefetched opcode.
            return true;
        }
        if (REG_PC != address || _state.cycles != beforeCycles) return true;
        // A device/data-window refusal retains the block; retrying this address
        // must not rebuild identical code on every interpreter fallback.
        return false;
    }

    private void RemoveNativeEntry(uint address)
    {
        if (_nativeEntries is null || !_nativeEntries.Remove(address, out var entry)) return;
        _nativeLinks?.Remove(address);
        IndexNativeEntry(address, null);
        _nativeAllocationOrder!.Remove(entry.Node);
        RetireNativeBlock(entry.Block);
    }
    private NativeEntry? FindNativeEntry(uint address) => address <= CPU_ADDRESS_MASK
        ? _nativePages?[address >> 12]?[(address & 4095) >> 1]
        : _nativeEntries?.GetValueOrDefault(address);

    private void IndexNativeEntry(uint address, NativeEntry? entry)
    {
        if (address > CPU_ADDRESS_MASK) return;
        if (entry is null)
        {
            if (_nativePages?[address >> 12] is { } page) page[(address & 4095) >> 1] = null;
            return;
        }
        _nativePages ??= new NativeEntry?[]?[4096];
        var target = _nativePages[address >> 12] ??= new NativeEntry?[2048];
        target[(address & 4095) >> 1] = entry;
    }
    private void InvalidateNativeLinks()
    {
        _nativeLinks?.Clear();
        if (_nativePublicationEpoch == ulong.MaxValue)
        {
            if (_nativeEntries is not null) foreach (var entry in _nativeEntries.Values) entry.PublishedEpoch = 0;
            _nativePublicationEpoch = 1;
        }
        else _nativePublicationEpoch++;
    }
    private void ClearNativeCache()
    {
        List<Exception>? errors = null;
        try { ClearNativeDelayedCache(); } catch (Exception error) { (errors ??= []).Add(error); }
        InvalidateNativeLinks(); _nativeLinkVersionKnown = false; _nativeCodeVersionKnown = false;
        var entries = _nativeEntries; _nativeEntries = null; _nativePages = null; _nativeAllocationOrder = null;
        if (entries is not null) foreach (var entry in entries.Values)
        {
            try { RetireNativeBlock(entry.Block); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Unable to close every 68000 native block.", errors);
    }
    public void Dispose()
    {
        lock (_nativeGate)
        {
            if (_nativeDisposed) return;
            _nativeDisposed = true;
            try { ClearNativeCache(); }
            finally
            {
                if (_nativeExecutionDepth != 0) _nativeDisposePending = true;
                else ReleaseNativeResources();
            }
        }
    }
}
