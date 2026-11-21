using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public sealed record CpuZ80NativeProfile(long TotalTicks, long CompilationTicks, long ExecutionTicks, long BusTicks, long Entries, long Builds, long BusAccesses,
    long CodeInvalidations, long TailInvalidations, long Evictions, int UniqueBuildAddresses, int CachedBlocks, CpuZ80NativeCodeChange[] Changes);
public sealed record CpuZ80NativeCodeChange(ushort Address, byte Expected, byte Actual, long Count);

internal static class CpuZ80NativeAbi
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate int Read(int address);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Write(int address, int value);
}

public sealed partial class CpuZ80<TBus> : IDisposable where TBus : class, ICpuZ80Bus
{
    private readonly object _nativeGate = new();
    private Dictionary<uint, NativeEntry>? _nativeBlocks;
    private LinkedList<uint>? _nativeAllocationOrder;
    private nint[]? _nativeLinks;
    private MemoryHandle _nativeLinksPin;
    private bool _nativeLinksPinned, _nativeDisposed, _nativeDisposePending;
    private int _nativeDepth;
    private ulong _nativeRatio;
    private int _nativeShift;
    private Z80X64Layout? _nativeLayout;
    private Z80X64MemoryMap? _nativeMap;
    private CpuZ80NativeAbi.Read? _nativeMemoryRead, _nativePortRead, _nativeOpcodeRead;
    private CpuZ80NativeAbi.Write? _nativeMemoryWrite, _nativePortWrite;
    private Func<int, byte>? _nativeDecodeRead;
    private int[][]? _nativeCycleTables;
    private Exception? _nativeError;
    private List<Z80X64Block>? _nativeRetired;
    private NativeTiming? _nativeTiming;
    private sealed class NativeTiming
    {
        internal long Total, Compilation, Execution, Bus, Changed, TailChanged, Evictions;
        internal readonly HashSet<ushort> Addresses = [];
        internal readonly Dictionary<uint,long> Changes=[];
    }
    public void EnableNativeProfiling(bool enabled)
    {
        lock (_nativeGate) { ObjectDisposedException.ThrowIf(_nativeDisposed,this); _nativeTiming=enabled?new():null; }
    }
    public CpuZ80NativeProfile GetNativeProfile()
    {
        lock(_nativeGate) return new(_nativeTiming?.Total??0,_nativeTiming?.Compilation??0,_nativeTiming?.Execution??0,_nativeTiming?.Bus??0,
            NativeBlockEntryCount,NativeBlockBuildCount,NativeBusAccessCount,_nativeTiming?.Changed??0,_nativeTiming?.TailChanged??0,_nativeTiming?.Evictions??0,
            _nativeTiming?.Addresses.Count??0,_nativeBlocks?.Count??0,
            _nativeTiming?.Changes.OrderByDescending(x=>x.Value).Take(32).Select(x=>new CpuZ80NativeCodeChange((ushort)(x.Key>>16),(byte)(x.Key>>8),(byte)x.Key,x.Value)).ToArray()??[]);
    }
    public long NativeBlockBuildCount { get; private set; }
    public long NativeBlockEntryCount { get; private set; }
    public long NativeBusAccessCount { get; private set; }
    public int NativeCachedBlockCount { get { lock (_nativeGate) return _nativeBlocks?.Count ?? 0; } }
    private sealed record NativeEntry(Z80X64Block Block, LinkedListNode<uint> Node, byte Opcode);

    public unsafe void RunNative(uint target)
    {
        lock (_nativeGate)
        {
            ObjectDisposedException.ThrowIf(_nativeDisposed, this);
            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
                throw new PlatformNotSupportedException("The Z80 native route requires Windows x64.");
            if (_state.cycles >= target) return;
            var profiling = _nativeTiming;long runStart=profiling is null?0:Stopwatch.GetTimestamp();
            try
            {
                EnsureNativeRuntime();
                if (_nativeRatio != _cycleRatio || _nativeShift != _cycleShift)
                { ClearNativeBlocks(); _nativeRatio = _cycleRatio; _nativeShift = _cycleShift; }
                _nativeMap!.Refresh(_bus, _readMap);
                int[][] timing = _nativeCycleTables ??= [cc_op, cc_cb, cc_ed, cc_xy, cc_xycb, cc_ex];
                while (_state.cycles < target)
                {
                    if (_state.irq_state != 0 && IFF1 != 0 && _state.after_ei == 0)
                    {
                        TakeInterrupt(); if (_state.cycles >= target) break;
                        _nativeMap.Refresh(_bus, _readMap);
                    }
                    ushort address = unchecked((ushort)PC);
                    byte opcode = _nativeDecodeRead!(address);
                    uint cacheKey = ((uint)address << 8) | opcode;
                    _nativeBlocks ??= []; _nativeAllocationOrder ??= [];
                    if (!_nativeBlocks.TryGetValue(cacheKey, out var entry))
                    {
                        long compileStart=profiling is null?0:Stopwatch.GetTimestamp();
                        var block = Z80X64Block.Compile(address, _nativeDecodeRead!, _nativeLayout!, timing, _cycleRatio, _cycleShift);
                        if(profiling is not null){profiling.Compilation+=Stopwatch.GetTimestamp()-compileStart;profiling.Addresses.Add(address);}
                        bool owned = false;
                        try
                        {
                            if (_nativeBlocks.Count >= 4096)
                            { if(profiling is not null)profiling.Evictions++;RemoveNativeBlock(_nativeAllocationOrder.First!.Value); }
                            var node = _nativeAllocationOrder.AddLast(cacheKey);
                            try
                            {
                                entry = new(block, node, opcode); _nativeBlocks.Add(cacheKey, entry);
                                owned = true; NativeBlockBuildCount++;
                            }
                            finally { if (!owned) _nativeAllocationOrder.Remove(node); }
                        }
                        finally { if (!owned) block.Dispose(); }
                    }
                    _nativeLinks![address] = entry.Block.LinkedEntry;
                    _nativeMap.ClearFault(); Z80X64Exit reason; uint exitBlock;
                    long executionStart=profiling is null?0:Stopwatch.GetTimestamp();
                    _nativeDepth++;
                    try
                    {
                        // Pin a primitive interior field, not the managed-reference
                        // fields of CpuZ80State. Layout offsets come from the CLR.
                        fixed (CpuZ80Pair* state = &_state.pc)
                            reason = entry.Block.Execute((nint)state, _nativeMap.Pointer, target, out exitBlock);
                    }
                    finally
                    {
                        if(profiling is not null)profiling.Execution+=Stopwatch.GetTimestamp()-executionStart;
                        LastFetch = _nativeMap.FetchByte;
                        FinishNativeCall();
                    }
                    NativeBlockEntryCount++;
                    Exception? error = _nativeError; _nativeError = null;
                    if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
                    ObjectDisposedException.ThrowIf(_nativeDisposed, this);
                    if (reason == Z80X64Exit.CodeChanged)
                    {
                        ushort exitAddress = unchecked((ushort)(exitBlock >> 8));
                        byte actualOpcode = _nativeDecodeRead!(exitAddress);
                        if(profiling is not null)
                        {
                            profiling.Changed++;if(unchecked((ushort)PC)!=exitAddress)profiling.TailChanged++;
                            if(_nativeBlocks.TryGetValue(exitBlock,out var lost))
                            {uint key=((uint)exitAddress<<16)|((uint)lost.Opcode<<8)|actualOpcode;profiling.Changes[key]=profiling.Changes.GetValueOrDefault(key)+1;}
                        }
                        // A different first opcode selects another cached native
                        // variant. Later-prefix/suffix changes invalidate this one.
                        if (unchecked((byte)exitBlock) == actualOpcode) RemoveNativeBlock(exitBlock);
                        else if(_nativeBlocks.TryGetValue(exitBlock,out var previous)
                            && _nativeLinks[exitAddress] == previous.Block.LinkedEntry) _nativeLinks[exitAddress] = 0;
                    }
                    else if (reason == Z80X64Exit.Interrupt)
                    {
                        TakeInterrupt(); _nativeMap.Refresh(_bus, _readMap);
                    }
                }
                }
            finally { if(profiling is not null)profiling.Total+=Stopwatch.GetTimestamp()-runStart; }
        }
    }
    private unsafe void EnsureNativeRuntime()
    {
        if (_nativeMap is not null) return;
        bool ready = false;
        try
        {
            _nativeLayout ??= new([
                NativeOffset(ref _state.pc, ref _state.pc), NativeOffset(ref _state.pc, ref _state.sp),
                NativeOffset(ref _state.pc, ref _state.af), NativeOffset(ref _state.pc, ref _state.bc),
                NativeOffset(ref _state.pc, ref _state.de), NativeOffset(ref _state.pc, ref _state.hl),
                NativeOffset(ref _state.pc, ref _state.ix), NativeOffset(ref _state.pc, ref _state.iy),
                NativeOffset(ref _state.pc, ref _state.wz), NativeOffset(ref _state.pc, ref _state.af2),
                NativeOffset(ref _state.pc, ref _state.bc2), NativeOffset(ref _state.pc, ref _state.de2), NativeOffset(ref _state.pc, ref _state.hl2),
                NativeOffset(ref _state.pc, ref _state.r), NativeOffset(ref _state.pc, ref _state.r2),
                NativeOffset(ref _state.pc, ref _state.iff1), NativeOffset(ref _state.pc, ref _state.iff2),
                NativeOffset(ref _state.pc, ref _state.halt), NativeOffset(ref _state.pc, ref _state.im), NativeOffset(ref _state.pc, ref _state.i),
                NativeOffset(ref _state.pc, ref _state.nmi_state), NativeOffset(ref _state.pc, ref _state.nmi_pending),
                NativeOffset(ref _state.pc, ref _state.irq_state), NativeOffset(ref _state.pc, ref _state.after_ei),
                NativeOffset(ref _state.pc, ref _state.cycles), NativeOffset(ref _state.pc, ref EA)]);
            _nativeLinks = new nint[65536]; _nativeLinksPin = _nativeLinks.AsMemory().Pin(); _nativeLinksPinned = true;
            _nativeMemoryRead = address => NativeBus(0, address, 0);
            _nativeMemoryWrite = (address, value) => NativeBus(1, address, value);
            _nativePortRead = address => NativeBus(2, address, 0);
            _nativePortWrite = (address, value) => NativeBus(3, address, value);
            _nativeOpcodeRead = address => NativeBus(4, address, 0);
            _nativeDecodeRead = address => unchecked((byte)ReadOpcode(address));
            _nativeMap = new([
                Marshal.GetFunctionPointerForDelegate(_nativeMemoryRead), Marshal.GetFunctionPointerForDelegate(_nativeMemoryWrite),
                Marshal.GetFunctionPointerForDelegate(_nativePortRead), Marshal.GetFunctionPointerForDelegate(_nativePortWrite),
                Marshal.GetFunctionPointerForDelegate(_nativeOpcodeRead)],
                [SZ, SZ_BIT, SZP, SZHV_inc, SZHV_dec, SZHVC_add, SZHVC_sub], (nint)_nativeLinksPin.Pointer);
            _nativeMap.FetchByte = LastFetch; ready = true;
        }
        finally { if (!ready) ReleaseNativeRuntime(); }
    }
    private static int NativeOffset<T>(ref CpuZ80Pair start, ref T field) where T : unmanaged =>
        checked((int)Unsafe.ByteOffset(ref Unsafe.As<CpuZ80Pair, byte>(ref start), ref Unsafe.As<T, byte>(ref field)));
    private int NativeBus(int operation, int address, int value)
    {
        int result = 0;
        var profiling=_nativeTiming;long started=profiling is null?0:Stopwatch.GetTimestamp();
        try
        {
            LastFetch = _nativeMap!.FetchByte; NativeBusAccessCount++;
            switch (operation)
            {
                case 0: result = RM(address); break;
                case 1: WM(address, value); break;
                case 2: result = IN(address); break;
                case 3: OUT(address, value); break;
                case 4: result = ReadOpcode(address); break;
            }
        }
        catch (Exception error) { _nativeError ??= error; }
        finally
        {
            try
            {
                _nativeMap!.Refresh(_bus, _readMap);
                if (_nativeDisposed) _nativeError ??= new ObjectDisposedException(GetType().Name);
            }
            catch (Exception error) { _nativeError ??= error; }
            if (_nativeError is not null) _nativeMap!.MarkFault(); else _nativeMap!.ClearFault();
            if(profiling is not null)profiling.Bus+=Stopwatch.GetTimestamp()-started;
        }
        return result;
    }
    private void RemoveNativeBlock(uint key)
    {
        if (_nativeBlocks is null || !_nativeBlocks.Remove(key, out var entry)) return;
        ushort address = unchecked((ushort)(key >> 8));
        if (_nativeLinks![address] == entry.Block.LinkedEntry) _nativeLinks[address] = 0;
        _nativeAllocationOrder!.Remove(entry.Node); RetireNative(entry.Block);
    }
    private void RetireNative(Z80X64Block block)
    {
        if (_nativeDepth != 0) (_nativeRetired ??= []).Add(block); else block.Dispose();
    }
    private void ClearNativeBlocks()
    {
        if (_nativeLinks is not null) Array.Clear(_nativeLinks);
        var blocks = _nativeBlocks; _nativeBlocks = null; _nativeAllocationOrder = null;
        if (blocks is null) return;
        List<Exception>? errors = null;
        foreach (var entry in blocks.Values)
            try { RetireNative(entry.Block); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Unable to close every native Z80 block.", errors);
    }
    private void FinishNativeCall()
    {
        if (--_nativeDepth != 0) return;
        var retired = _nativeRetired; _nativeRetired = null; List<Exception>? errors = null;
        try
        {
            if (retired is not null) foreach (var block in retired)
                try { block.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        finally { if (_nativeDisposePending) ReleaseNativeRuntime(); }
        if (errors is not null) throw new AggregateException("Unable to retire every native Z80 block.", errors);
    }
    private void ReleaseNativeRuntime()
    {
        _nativeTiming=null;
        _nativeDisposePending = false;
        try { _nativeMap?.Dispose(); }
        finally
        {
            _nativeMap = null;
            try { if (_nativeLinksPinned) { _nativeLinksPinned = false; _nativeLinksPin.Dispose(); } }
            finally
            {
                _nativeLinks = null; _nativeLinksPin = default;
                _nativeMemoryRead = _nativePortRead = _nativeOpcodeRead = null;
                _nativeMemoryWrite = _nativePortWrite = null; _nativeDecodeRead = null;
            }
        }
    }
    public void Dispose()
    {
        lock (_nativeGate)
        {
            if (_nativeDisposed) return; _nativeDisposed = true;
            try { ClearNativeBlocks(); }
            finally { if (_nativeDepth != 0) _nativeDisposePending = true; else ReleaseNativeRuntime(); }
        }
    }
}
