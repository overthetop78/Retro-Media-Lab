using System.Buffers;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

/// <summary>Owns pins for borrowed memory; never owns or disposes the backing memory itself.</summary>
/// <remarks>Build or replace mappings outside CPU execution. Dispose only after the last CPU lease returns.</remarks>
public sealed class Sh2ReadPageMap : IDisposable
{
    public const int PageShift = 12, PageSize = 1 << PageShift, PageCount = 1 << (29 - PageShift);
    private readonly nint[] _pages = GC.AllocateArray<nint>(PageCount, pinned: true);
    private readonly Dictionary<Memory<byte>, MemoryHandle> _pins = [];
    private bool _disposed;
    public ReadOnlyMemory<nint> Pages { get { ObjectDisposedException.ThrowIf(_disposed, this); return _pages; } }

    /// <summary>Map a physical range, repeating a power-of-two backing window as Yabause's RAM aliases do.</summary>
    public unsafe void MapRead(uint start, uint length, Memory<byte> backing)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if ((start & (PageSize - 1)) != 0 || length == 0 || (length & (PageSize - 1)) != 0
            || (ulong)start + length > 0x20000000 || backing.Length < PageSize
            || (backing.Length & (backing.Length - 1)) != 0) throw new ArgumentException("Invalid native SH-2 read window.");
        if (!_pins.TryGetValue(backing, out MemoryHandle handle))
        {
            handle = backing.Pin();
            bool transferred = false;
            try
            {
                if (handle.Pointer == null) throw new InvalidOperationException("The SH-2 read window could not be pinned.");
                _pins.Add(backing, handle); transferred = true;
            }
            finally { if (!transferred) handle.Dispose(); }
        }
        int first = (int)(start >> PageShift), count = (int)(length >> PageShift), mask = backing.Length - 1;
        for (int i = 0; i < count; i++) _pages[first + i] = (nint)((byte*)handle.Pointer + ((i * PageSize) & mask));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _pages.AsSpan().Clear();
        List<Exception>? errors = null;
        try
        {
            foreach (MemoryHandle handle in _pins.Values)
            {
                try { handle.Dispose(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { _pins.Clear(); }
        if (errors is not null) throw new AggregateException("Unable to release every SH-2 read window pin.", errors);
    }
}
