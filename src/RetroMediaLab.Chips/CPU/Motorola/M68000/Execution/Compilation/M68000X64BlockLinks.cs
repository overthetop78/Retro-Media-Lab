using System.Buffers;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>
/// Paged guest-to-native entry lookup, following the native-address map used by
/// BlastEm. Entries borrow executable buffers. The core serializes execution,
/// remapping and disposal, and removes pointers before releasing those buffers.
/// Pointers are linked-entry points sharing the core's identical native frame;
/// a full managed-entry prologue must not be published in this table.
/// </summary>
internal sealed class M68000X64BlockLinks : IDisposable
{
    private sealed class Page
    {
        internal readonly nint[] Entries = new nint[2048];
        internal MemoryHandle Pin;
        internal long Generation;
        internal Page(long generation) { Pin = Entries.AsMemory().Pin(); Generation = generation; }
    }
    private readonly nint[] _table = new nint[4096];
    private readonly Page?[] _pages = new Page?[4096];
    private MemoryHandle _tablePin;
    private long _generation = 1;
    private bool _disposed;
    private bool _hasDirectSlots;
    public M68000X64BlockLinks() => _tablePin = _table.AsMemory().Pin();
    public unsafe nint Pointer
    {
        get { ObjectDisposedException.ThrowIf(_disposed, this); return (nint)_tablePin.Pointer; }
    }
    public unsafe void Publish(uint address, nint entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (address > 0xffffff || (address & 1) != 0) return;
        if (entry == 0) throw new ArgumentNullException(nameof(entry));
        int index = (int)(address >> 12);
        var page = _pages[index] ??= new(_generation);
        if (page.Generation != _generation)
        {
            Array.Clear(page.Entries); page.Generation = _generation;
        }
        page.Entries[(address & 4095) >> 1] = entry;
        _table[index] = (nint)page.Pin.Pointer;
    }
    /// <summary>
    /// Borrows a stable slot for a known branch destination. Pages remain pinned
    /// until this map is destroyed, even when the destination code is evicted.
    /// </summary>
    public unsafe nint GetSlot(uint address)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (address > 0xffffff || (address & 1) != 0) return 0;
        _hasDirectSlots = true;
        int index = (int)(address >> 12);
        var page = _pages[index] ??= new(_generation);
        if (page.Generation != _generation)
        {
            Array.Clear(page.Entries); page.Generation = _generation;
        }
        _table[index] = (nint)page.Pin.Pointer;
        return (nint)page.Pin.Pointer + (int)((address & 4095) >> 1) * IntPtr.Size;
    }
    public void Remove(uint address)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (address > 0xffffff || (address & 1) != 0) return;
        if (_pages[address >> 12] is { } page && page.Generation == _generation)
            page.Entries[(address & 4095) >> 1] = 0;
    }
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Array.Clear(_table);
        // Native direct slots bypass the root table. Clearing only the root
        // would let them keep jumping to stale or released executable buffers.
        if (_hasDirectSlots)
            foreach (var page in _pages) if (page is not null) Array.Clear(page.Entries);
        // Entries in inactive pages are cleared lazily before republication.
        if (_generation == long.MaxValue)
        {
            foreach (var page in _pages) if (page is not null) { Array.Clear(page.Entries); page.Generation = 0; }
            _generation = 1;
        }
        else _generation++;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Array.Clear(_table);
        List<Exception>? errors = null;
        try
        {
            foreach (var page in _pages)
            {
                if (page is null) continue;
                Array.Clear(page.Entries);
                try { page.Pin.Dispose(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { Array.Clear(_pages); _tablePin.Dispose(); }
        if (errors is not null) throw new AggregateException("Unable to unpin all native entry pages.", errors);
    }
}
