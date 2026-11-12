using System.Buffers.Binary;
using H = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class EmulatorCore
{
    private readonly Queue<AudioChunk> _audio = new();
    private int _bufferedAudioFrames;
    private long _videoSequence, _audioSequence, _audioFrames;
    public int BufferedAudioFrames => Volatile.Read(ref _bufferedAudioFrames);
    public VideoFrame? LatestVideoFrame { get; private set; }
    public AudioChunk? LatestAudioChunk { get; private set; }

    // TI MP010A, table2-3: nominal decoded color differences, normalized to black=.47.
    // This is host RGB presentation, not an analog NTSC decoder or a measured television palette.
    private static readonly uint[] Palette = BuildPalette();
    private static uint[] BuildPalette()
    {
        (double Y, double Ry, double By)[] levels =
        [
            (0,.47,.47), (0,.47,.47), (.53,.07,.20), (.67,.17,.27),
            (.40,.40,1), (.53,.43,.93), (.47,.83,.30), (.67,0,.70),
            (.53,.93,.27), (.67,.93,.27), (.73,.57,.07), (.80,.57,.17),
            (.46,.13,.23), (.53,.73,.67), (.80,.47,.47), (1,.47,.47)
        ];
        return levels.Select(level =>
        {
            double r = level.Y + level.Ry - .47, b = level.Y + level.By - .47;
            double g = (level.Y - .299 * r - .114 * b) / .587;
            return (Channel(r) << 16) | (Channel(g) << 8) | Channel(b);
        }).ToArray();
        static uint Channel(double value) => (uint)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }

    public void RunFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Board.AdvanceFrame();
        LatestVideoFrame = PresentVideo(++_videoSequence);
        short[] samples = Board.DrainAudio();
        if (samples.Length == 0) return;
        LatestAudioChunk = new(samples, SampleRate, samples.Length, ++_audioSequence,
            TimeSpan.FromSeconds((double)_audioFrames / SampleRate), EmulationAudioChannelLayout.Mono);
        _audioFrames += samples.Length;
        _audio.Enqueue(LatestAudioChunk);
        Interlocked.Add(ref _bufferedAudioFrames, samples.Length);
    }

    private VideoFrame PresentVideo(long sequence)
    {
        // Each published frame owns immutable bytes; Common may still display the preceding frame.
        byte[] pixels = new byte[H.Width * H.Height * 4];
        var indices = Board.ColourIndices.Span;
        var known = Board.PixelKnowledge.Span;
        for (int i = 0; i < indices.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(pixels.AsSpan(i * 4), known[i] ? Palette[indices[i] & 15] : 0U);
        return new(pixels, H.Width, H.Height, H.Width * 4, EmulationPixelFormat.Xrgb8888, 4f / 3,
            sequence, TimeSpan.FromTicks(Board.TimeNanoseconds / 100));
    }
    public bool TryDequeueAudio(out AudioChunk? chunk)
    {
        if (!_audio.TryDequeue(out chunk)) return false;
        Interlocked.Add(ref _bufferedAudioFrames, -chunk.FrameCount);
        return true;
    }
    private void ClearOutput()
    {
        _audio.Clear(); Interlocked.Exchange(ref _bufferedAudioFrames, 0);
        LatestVideoFrame = null; LatestAudioChunk = null;
        _videoSequence = _audioSequence = _audioFrames = 0;
    }
}
