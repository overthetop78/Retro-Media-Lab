using System.IO;
using RetroMediaLab.Chips.Audio.Common.Resampling;
using RetroMediaLab.Chips.Controllers.Sega.CD;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;

/// <summary>Owns the inserted image; the reusable controller borrows it until it is detached.</summary>
internal sealed class CdDrive : IDisposable
{
    public const int ContextSize = Cdd.ContextSize;
    private readonly Cdd _controller;
    private readonly CancellationToken _cancellation;
    private MediaEngineDisc? _media;
    private bool _disposed;
    public bool PrecacheImages { get; set; }
    public uint Cycles { get => _controller.Cycles; set => _controller.Cycles = value; }
    public int Loaded => _controller.Loaded == 0 ? 0 : _media?.HasLoopInformation == true ? 2 : 1;
    public int Index { get => _controller.Index; set => _controller.Index = value; }
    public int Lba { get => _controller.Lba; set => _controller.Lba = value; }
    public byte Status { get => _controller.Status; set => _controller.Status = value; }
    public Memory<ushort> Fader => _controller.Fader;
    public int TrackCount => _controller.TrackCount;
    public int LatencyFactor { get => _controller.LatencyFactor; set => _controller.LatencyFactor = value; }
    public int AudioVolume { get => _controller.AudioVolume; set => _controller.AudioVolume = value; }
    public CdDrive(ICddBus bus, BandLimitedBuffer output, CancellationToken cancellation)
    {
        _cancellation = cancellation; _controller = new(bus,output,cancellation);
    }
    public int Load(string path, Span<byte> header)
    {
        ObjectDisposedException.ThrowIf(_disposed,this); _cancellation.ThrowIfCancellationRequested();
        if (header.Length < 0x210) throw new ArgumentException("A 528-byte disc header buffer is required.",nameof(header));
        if (!File.Exists(path)) return 0;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".cue" or ".iso" or ".chd" or ".bin")) return 0;
        MediaEngineDisc? next = null; bool transferred = false;
        try
        {
            next = MediaEngineDisc.OpenAsync(path,_cancellation,PrecacheImages).GetAwaiter().GetResult();
            next.ReadHeader(header[..0x210]);
            Unload(); _controller.Mount(next); _media = next; transferred = true; return 1;
        }
        catch (Exception error) when (error is IOException or NotSupportedException or AggregateException)
        {
            return extension == ".bin" ? 0 : -1;
        }
        finally { if (!transferred) next?.Dispose(); }
    }
    public void Unload()
    {
        MediaEngineDisc? previous = _media;
        try { _controller.Unload(); }
        finally { try { previous?.Dispose(); } finally { _media = null; } }
    }
    /// <summary>Takes ownership after a successful mount. A failed mount leaves ownership with the caller.</summary>
    public void Mount(MediaEngineDisc media)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        Unload(); _controller.Mount(media); _media = media;
    }
    public void Initialize(int sampleRate) => _controller.Initialize(sampleRate);
    public void Reset() => _controller.Reset();
    public void Update() => _controller.Update();
    public void ProcessCommand() => _controller.ProcessCommand();
    public void ReadData(Span<byte> destination, Span<byte> subheader) => _controller.ReadData(destination,subheader);
    public void SeekAudio(int track, int lba) => _controller.SeekAudio(track,lba);
    public void ReadAudio(uint samples) => _controller.ReadAudio(samples);
    public void UpdateAudio(uint samples) => _controller.UpdateAudio(samples);
    public int TrackStart(int track) => _controller.TrackStart(track);
    public int TrackEnd(int track) => _controller.TrackEnd(track);
    public int TrackType(int track) => _controller.TrackType(track);
    public int TrackLoopEnabled(int track) => _controller.TrackLoopEnabled(track);
    public int TrackLoopOffset(int track) => _controller.TrackLoopOffset(track);
    public int SaveContext(Span<byte> state) => _controller.SaveContext(state);
    public int LoadContext(ReadOnlySpan<byte> state, string version) => _controller.LoadContext(state,version);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { Unload(); } finally { _controller.Dispose(); }
    }
}
