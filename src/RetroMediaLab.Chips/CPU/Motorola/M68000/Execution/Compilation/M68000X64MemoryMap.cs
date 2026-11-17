using System.Buffers;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>Owns pins for a native 256-page read/write pointer table; borrows all data.</summary>
internal sealed class M68000X64MemoryMap : IDisposable
{
    internal const int WriteMinimumOffset = 514 * sizeof(long);
    internal const int BusFunctionsOffset = 770 * sizeof(long);
    internal const int BusFaultOffset = 774 * sizeof(long);
    internal const int BusChangedOffset = 775 * sizeof(long);
    internal const int ControlFunctionOffset = 776 * sizeof(long);
    internal const int MappedReadOffset = 777 * sizeof(long);
    internal const int MappedFunctionsOffset = 1033 * sizeof(long);
    internal const int BusCycleAdjustmentOffset = 1035 * sizeof(long);
    internal const int LinkRootOffset = 1036 * sizeof(long);
    private readonly nint[] _table = new nint[1037];
    private readonly MemoryHandle[] _pins = new MemoryHandle[768];
    private readonly bool[] _pinned = new bool[768];
    private readonly ReadOnlyMemory<byte>[] _windows = new ReadOnlyMemory<byte>[768];
    private MemoryHandle _tablePin;
    private bool _disposed, _hasVersion;
    private long _version, _mappedVersion;
    public M68000X64MemoryMap()
    {
        if (IntPtr.Size != 8) throw new PlatformNotSupportedException("Native memory tables require x64.");
        _tablePin = _table.AsMemory().Pin();
    }
    public unsafe nint Pointer
    {
        get { ObjectDisposedException.ThrowIf(_disposed, this); return (nint)_tablePin.Pointer; }
    }
    public unsafe void Update(ICpu68000DirectMemoryBus bus)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(bus);
        long version = bus.DirectMemoryVersion;
        var mapped = bus as ICpu68000VersionedCodeBus;
        long mappedVersion = mapped?.CodeMemoryVersion ?? 0;
        if (_hasVersion && version == _version && mappedVersion == _mappedVersion) return;
        _hasVersion = false;
        try
        {
            for (int page = 0; page < 256; page++)
            {
                bus.GetDirectMemory(page, out var read, out var write);
                _table[514 + page] = 0;
                if (write.Length < 65536 && bus is ICpu68000ProtectedMemoryBus protectedBus)
                {
                    protectedBus.GetProtectedWriteMemory(page, out write, out int minimumOffset);
                    if ((uint)minimumOffset > 65536) throw new InvalidOperationException("Invalid native write-protection offset.");
                    if (write.Length >= 65536) _table[514 + page] = minimumOffset;
                }
                SetWindow(page, read); SetWindow(256 + page, write);
                ReadOnlyMemory<byte> instructions = default;
                if (mapped is not null && mapped.TryGetCodeMemory((uint)page << 16, out var code, out uint address)
                    && !(bus is ICpu68000ObservedCodeBus observed && observed.RequiresCodeFetchObservation((uint)page << 16))
                    && address == (uint)page << 16 && code.Length >= 65536) instructions = code;
                SetWindow(512 + page, instructions);
            }
            _version = version; _mappedVersion = mappedVersion; _hasVersion = true;
        }
        catch
        {
            ClearWindows();
            throw;
        }
    }
    private unsafe void SetWindow(int slot, ReadOnlyMemory<byte> memory)
    {
        memory = memory.Length >= 65536 ? memory[..65536] : default;
        if (_windows[slot].Equals(memory)) return;
        if (_pinned[slot]) { _pinned[slot] = false; _pins[slot].Dispose(); }
        int pointerSlot = slot < 512 ? slot : 777 + slot - 512;
        _pins[slot] = default; _windows[slot] = default; _table[pointerSlot] = 0;
        if (memory.IsEmpty) return;
        var pin = memory.Pin(); _pins[slot] = pin; _pinned[slot] = true;
        _windows[slot] = memory; _table[pointerSlot] = (nint)pin.Pointer;
    }
    public void SetRefreshWindow(Cpu68000NativeRefreshMode mode, bool elapsed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _table[512] = (nint)mode; _table[513] = elapsed ? 1 : 0;
    }
    public void SetBusFunctions(nint read8, nint read16, nint write8, nint write16, nint control = 0, nint mapped8 = 0, nint mapped16 = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _table[770] = read8; _table[771] = read16; _table[772] = write8; _table[773] = write16;
        _table[776] = control;
        _table[1033] = mapped8; _table[1034] = mapped16;
    }
    public void ResetBusStatus() { _table[774] = 0; _table[775] = 0; _table[1035] = 0; }
    public void MarkBusFault() => _table[774] = 1;
    public void ClearBusFault() => _table[774] = 0;
    public void MarkBusChanged() => _table[775] |= 1;
    public int BusCycleAdjustment => unchecked((int)_table[1035]);
    public void SetBusCycleAdjustment(int adjustment) { _table[1035] = adjustment; _table[775] |= 2; }
    private void ClearWindows()
    {
        List<Exception>? errors = null;
        try
        {
            for (int index = 0; index < _pins.Length; index++)
            {
                if (!_pinned[index]) continue;
                _pinned[index] = false;
                try { _pins[index].Dispose(); }
                catch (Exception error) { (errors ??= []).Add(error); }
                finally { _pins[index] = default; }
            }
        }
        finally
        {
            Array.Clear(_table, 0, 512); Array.Clear(_table, 514, 256);
            Array.Clear(_table, 777, 256);
            Array.Clear(_windows);
        }
        if (errors is not null) throw new AggregateException("Unable to unpin every 68000 data window.", errors);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { ClearWindows(); }
        finally { _tablePin.Dispose(); }
    }
}
