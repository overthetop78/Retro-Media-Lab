using System.IO;

namespace RetroMediaLab.MediaEngine.Images.Reading.Sources;

/// <summary>A seekable decoder stream whose owner bounds every operating-system file access.</summary>
internal sealed class ScopedFileRandomAccessStream(string path) : Stream
{
    private FileStream? _active;
    private bool _closed;
    private long _position;
    private readonly long _length = new FileInfo(path).Length;
    public void BeginAccess()
    {
        ObjectDisposedException.ThrowIf(_closed,this);
        if (_active is not null) throw new InvalidOperationException("A decoder access is already active.");
        FileStream? next = null; bool transferred = false;
        try
        {
            next = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.RandomAccess);
            next.Position = _position; _active = next; transferred = true;
        }
        finally { if (!transferred) next?.Dispose(); }
    }
    public void EndAccess()
    {
        FileStream? active = _active;
        try { if (active is not null) _position = active.Position; }
        finally { try { active?.Dispose(); } finally { _active = null; } }
    }
    private FileStream Active => _active ?? throw new InvalidOperationException("No decoder access is active.");
    public override bool CanRead => !_closed;
    public override bool CanSeek => !_closed;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position { get=>_active?.Position ?? _position; set { if (_active is not null) _active.Position=value; _position=value; } }
    public override int Read(byte[] buffer,int offset,int count)=>Active.Read(buffer,offset,count);
    public override int Read(Span<byte> buffer)=>Active.Read(buffer);
    public override long Seek(long offset,SeekOrigin origin)=>Active.Seek(offset,origin);
    public override void Flush() { }
    public override void SetLength(long value)=>throw new NotSupportedException();
    public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        try { if (disposing) EndAccess(); }
        finally { _closed=true; base.Dispose(disposing); }
    }
}
