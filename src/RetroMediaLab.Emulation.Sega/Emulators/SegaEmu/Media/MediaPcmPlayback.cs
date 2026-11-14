using System.IO;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.MediaEngine.Images.Reading.Audio;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;

internal sealed class MediaPcmPlayback : IPcmPlayback
{
    private readonly PcmPlaybackSession _session;
    private readonly CancellationToken _cancellation;
    public int Channels => _session.Channels;
    public int SampleRate => _session.SampleRate;
    public ulong SamplePosition => (ulong)_session.FramePosition;
    public bool HasError { get; private set; }
    private MediaPcmPlayback(PcmPlaybackSession session, CancellationToken cancellation) { _session = session; _cancellation = cancellation; }
    public static async Task<MediaPcmPlayback> OpenAsync(string path, CancellationToken cancellation)
    {
        PcmPlaybackSession session = await PcmPlaybackSession.OpenAsync(path,cancellation).ConfigureAwait(false);
        bool transferred = false;
        try { MediaPcmPlayback playback = new(session,cancellation); transferred = true; return playback; }
        finally { if (!transferred) session.Dispose(); }
    }
    public int ReadSamples(Span<short> destination)
    {
        try { return _session.ReadSamples(destination,_cancellation); }
        catch (IOException) { HasError = true; return 0; }
    }
    public void SeekSample(ulong position) => _session.SeekFrame(checked((long)position));
    public void Dispose() => _session.Dispose();
}
