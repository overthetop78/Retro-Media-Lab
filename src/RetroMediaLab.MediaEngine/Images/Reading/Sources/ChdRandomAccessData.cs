using System.IO;
using RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;
using IMediaRandomAccessData = RetroMediaLab.MediaFileSystems.Interfaces.IMediaRandomAccessData;

namespace RetroMediaLab.MediaEngine.Images.Reading.Sources;

/// <summary>Owns a CHD container and exposes decoded hunks as bounded logical data.</summary>
internal sealed class ChdRandomAccessData : IMediaRandomAccessData, IDisposable
{
    private readonly ChdContainer _container;
    private readonly ScopedFileRandomAccessStream _stream;
    private readonly byte[] _hunk;
    private readonly object _gate = new();
    private int _hunkIndex = -1;
    private bool _disposed;
    public long Length => _container.LogicalBytes;

    private ChdRandomAccessData(ChdContainer container,ScopedFileRandomAccessStream stream)
    { _container = container; _stream = stream; _hunk = new byte[container.HunkBytes]; }

    public static ChdRandomAccessData Open(string path,CancellationToken cancellation)
    {
        ScopedFileRandomAccessStream? stream = null; ChdContainer? container = null; bool transferred = false;
        try
        {
            stream = new(path); stream.BeginAccess();
            container = ChdContainer.Open(stream,leaveOpen:false,cancellation);
            ChdRandomAccessData result = new(container,stream); transferred = true; return result;
        }
        finally
        {
            try { stream?.EndAccess(); }
            finally { if (!transferred) { try { container?.Dispose(); } finally { stream?.Dispose(); } } }
        }
    }
    public ValueTask ReadExactlyAsync(long offset,Memory<byte> destination,CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if (offset < 0 || offset > Length || destination.Length > Length-offset) throw new ArgumentOutOfRangeException(nameof(offset));
            int completed = 0;
            while (completed < destination.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long current = offset+completed;
                int index = checked((int)(current/_hunk.Length)), position = (int)(current%_hunk.Length);
                if (index != _hunkIndex)
                {
                    try { _stream.BeginAccess(); _container.ReadHunk(index,_hunk); _hunkIndex = index; }
                    finally { _stream.EndAccess(); }
                }
                int count = Math.Min(destination.Length-completed,_hunk.Length-position);
                _hunk.AsMemory(position,count).CopyTo(destination[completed..]); completed += count;
            }
        }
        return ValueTask.CompletedTask;
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _container.Dispose(); }
            finally { Array.Clear(_hunk); _hunkIndex = -1; }
        }
    }

}
