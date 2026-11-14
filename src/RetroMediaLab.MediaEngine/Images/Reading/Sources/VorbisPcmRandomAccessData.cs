using System.IO;
using RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;
using IMediaRandomAccessData = RetroMediaLab.MediaFileSystems.Interfaces.IMediaRandomAccessData;

namespace RetroMediaLab.MediaEngine.Images.Reading.Sources;

/// <summary>Decodes seekable Ogg/Vorbis into signed little-endian PCM without retaining an open file.</summary>
internal sealed class VorbisPcmRandomAccessData : IMediaRandomAccessData, IDisposable
{
    private readonly VorbisDecoder _decoder;
    private readonly ScopedFileRandomAccessStream _stream;
    private readonly byte[] _frame;
    private readonly object _gate = new();
    private bool _disposed;
    public int Channels => _decoder.Channels;
    public int SampleRate => _decoder.SampleRate;
    public long Length { get; }
    private VorbisPcmRandomAccessData(VorbisDecoder decoder,ScopedFileRandomAccessStream stream)
    { _decoder=decoder; _stream=stream; _frame=new byte[decoder.Channels*sizeof(short)]; Length=checked(decoder.TotalFrames*_frame.Length); }

    public static VorbisPcmRandomAccessData Open(string path,CancellationToken cancellation)
    {
        ScopedFileRandomAccessStream? stream=null; VorbisDecoder? decoder=null; bool transferred=false;
        try
        {
            stream=new(path); stream.BeginAccess(); decoder=VorbisDecoder.Open(stream,leaveOpen:false,cancellation);
            VorbisPcmRandomAccessData result=new(decoder,stream); transferred=true; return result;
        }
        finally
        {
            try { stream?.EndAccess(); }
            finally { if (!transferred) { try { decoder?.Dispose(); } finally { stream?.Dispose(); } } }
        }
    }
    public ValueTask ReadExactlyAsync(long offset,Memory<byte> destination,CancellationToken cancellationToken=default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if (offset<0 || offset>Length || destination.Length>Length-offset) throw new ArgumentOutOfRangeException(nameof(offset));
            cancellationToken.ThrowIfCancellationRequested();
            if (destination.IsEmpty) return ValueTask.CompletedTask;
            try
            {
                _stream.BeginAccess();
                long frame=offset/_frame.Length;
                if (_decoder.PcmPosition!=frame) _decoder.SeekPcm(frame);
                int skip=(int)(offset%_frame.Length), completed=0;
                while (completed<destination.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int remaining=destination.Length-completed;
                    if (skip==0 && remaining>=_frame.Length)
                    {
                        int available=remaining-remaining%_frame.Length;
                        int read=_decoder.ReadPcm(destination.Span.Slice(completed,available));
                        if (read<=0) throw new EndOfStreamException("The Vorbis PCM ended inside the requested range.");
                        completed+=read;
                    }
                    else
                    {
                        int read=_decoder.ReadPcm(_frame);
                        if (read!=_frame.Length) throw new EndOfStreamException("The Vorbis PCM ended inside a sample frame.");
                        int count=Math.Min(remaining,_frame.Length-skip);
                        _frame.AsMemory(skip,count).CopyTo(destination[completed..]); completed+=count; skip=0;
                    }
                }
            }
            finally { _stream.EndAccess(); }
        }
        return ValueTask.CompletedTask;
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed=true;
            try { _decoder.Dispose(); }
            finally { try { _stream.Dispose(); } finally { Array.Clear(_frame); } }
        }
    }
}
