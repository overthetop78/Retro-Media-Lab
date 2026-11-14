using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;

namespace RetroMediaLab.MediaEngine.Images.Reading.Audio;

/// <summary>Sample-positioned playback over the PCM already decoded by MediaEngine's audio reader.</summary>
public sealed class PcmPlaybackSession : IDisposable
{
    private readonly MediaImageDocument _document;
    private readonly MediaDataRange _data;
    private readonly byte[] _scratch = new byte[8192];
    private bool _disposed;
    public int Channels { get; }
    public int SampleRate { get; }
    public long FramePosition { get; private set; }
    public long FrameCount => _data.Length / (Channels * sizeof(short));

    private PcmPlaybackSession(MediaImageDocument document)
    {
        _document = document;
        if (document.Representation is not SequentialMediaImageRepresentation { Segments: { Count: > 0 } segments })
            throw new InvalidDataException("The audio source does not provide PCM segments.");
        Channels = int.Parse(document.Metadata["channels"], CultureInfo.InvariantCulture);
        SampleRate = int.Parse(document.Metadata["sampleRate"], CultureInfo.InvariantCulture);
        if (Channels <= 0 || SampleRate <= 0 || document.Metadata["bitsPerSample"] != "16")
            throw new InvalidDataException("Signed 16-bit interleaved PCM is required.");
        _data = segments[0].DataRange ?? throw new InvalidDataException("PCM data is unavailable.");
        if (_data.Source is null || _data.Length % (Channels * sizeof(short)) != 0)
            throw new InvalidDataException("The PCM range is incomplete.");
    }
    public static async Task<PcmPlaybackSession> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        MediaImageDocument document = await MediaRecognitionComposition.CreateDefault().ReadingService
            .ReadAsync(new MediaSourceDescriptor(path,[],RequestedFormatId: TapeImageFormatIds.Audio),cancellationToken).ConfigureAwait(false);
        bool transferred = false;
        try { PcmPlaybackSession session = new(document); transferred = true; return session; }
        finally { if (!transferred) Release(document); }
    }
    public int ReadSamples(Span<short> destination, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed,this); cancellationToken.ThrowIfCancellationRequested();
        int frames = (int)Math.Min(destination.Length / Channels, FrameCount - FramePosition);
        int samples = checked(frames * Channels), completed = 0;
        while (completed < samples)
        {
            int count = Math.Min(samples - completed,_scratch.Length / sizeof(short));
            long offset = checked(_data.SourceOffset + FramePosition * Channels * sizeof(short) + completed * sizeof(short));
            _data.Source!.ReadExactlyAsync(offset,_scratch.AsMemory(0,count * sizeof(short)),cancellationToken).GetAwaiter().GetResult();
            for (int i = 0; i < count; i++) destination[completed + i] = BinaryPrimitives.ReadInt16LittleEndian(_scratch.AsSpan(i * 2,2));
            completed += count;
        }
        FramePosition += frames; return samples;
    }
    public void SeekFrame(long position)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if (position < 0 || position > FrameCount) throw new ArgumentOutOfRangeException(nameof(position));
        FramePosition = position;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Release(_document);
    }
    private static void Release(MediaImageDocument document)
    {
        Exception? failure = null;
        try
        {
            if (document.Representation is SequentialMediaImageRepresentation { Segments: { } segments })
            {
                HashSet<object> released = new(ReferenceEqualityComparer.Instance);
                foreach (SequentialMediaSegment segment in segments)
                    if (segment.DataRange?.Source is IDisposable resource && released.Add(resource))
                    {
                        try { resource.Dispose(); } catch (Exception error) { failure ??= error; }
                    }
            }
        }
        finally { document.Source.Drive?.Reset(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
