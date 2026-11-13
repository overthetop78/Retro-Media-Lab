using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal interface IMp3Playback : IDisposable
{
    int Channels { get; }
    int SampleRate { get; }
    ulong SamplePosition { get; }
    bool HasError { get; }
    int ReadSamples(Span<short> destination);
    void SeekSample(ulong position);
}

internal interface ICartridgeAudioMedia
{
    // A found file with invalid MP3 data returns true and a null playback object.
    bool OpenTrack(ushort index, int minimumDigits, CancellationToken cancellation, out IMp3Playback? playback);
}

internal sealed partial class Yx5200 : IDisposable
{
    public const int ContextSize = NativeContextSize + sizeof(ulong);
    private readonly BlipBuffer _output;
    private readonly ICartridgeAudioMedia _media;
    private readonly CancellationToken _cancellation;
    private readonly short[] _audioBuffer = new short[2048];
    private IMp3Playback? _track;
    private int _sampleRate;

    public Yx5200(BlipBuffer output, ICartridgeAudioMedia media, CancellationToken cancellation)
    {
        _output = output; _media = media; _cancellation = cancellation;
    }
    public void Initialize(int sampleRate)
    {
        _sampleRate = sampleRate;
        _output.SetRates(yx5200.trackIndex != 0 ? _track!.SampleRate : 48000, _sampleRate);
    }
    public void Reset() => yx5200_reset();
    public void Write(uint data) => yx5200_write(data);
    private void yx5200_load_track(ushort index, byte playbackLoop)
    {
        yx5200_unload_track();
        for (int digits = 1; digits <= 4; digits++)
        {
            IMp3Playback? playback = null;
            bool transferred = false;
            try
            {
                _cancellation.ThrowIfCancellationRequested();
                if (!_media.OpenTrack(index, digits, _cancellation, out playback)) continue;
                if (playback is not null && playback.Channels > 0 && playback.Channels <= 2)
                {
                    _output.SetRates(playback.SampleRate, _sampleRate);
                    yx5200.trackIndex = index;
                    yx5200.playbackEnabled = 1;
                    yx5200.playbackLoop = playbackLoop;
                    _track = playback;
                    transferred = true;
                }
                break;
            }
            finally
            {
                if (!transferred) playback?.Dispose();
            }
        }
    }
    private void yx5200_unload_track()
    {
        IMp3Playback? playback = _track;
        try { playback?.Dispose(); }
        finally
        {
            _track = null;
            yx5200.playbackEnabled = 0;
            yx5200.trackIndex = 0;
        }
    }

    public void Update(int outputSamples)
    {
        bool completed = false;
        try
        {
            short previousLeft = yx5200.audio[0], previousRight = yx5200.audio[1];
            int samples = _output.ClocksNeeded(outputSamples);
            if (yx5200.playbackEnabled != 0)
            {
                IMp3Playback track = _track!;
                int available = 0;
                int channels = track.Channels;
                int toRead = samples * channels;
                while (available < toRead)
                {
                    _cancellation.ThrowIfCancellationRequested();
                    available += track.ReadSamples(_audioBuffer.AsSpan(available, toRead - available));
                    if (available < toRead)
                    {
                        if (yx5200.playbackLoop != 0 && !track.HasError) track.SeekSample(0);
                        else
                        {
                            yx5200_unload_track();
                            for (int i = 0; i < channels; i++) _audioBuffer[available++] = 0;
                            break;
                        }
                    }
                }
                if (yx5200.playbackVolume != 0 && yx5200.audioEnabled != 0)
                {
                    uint count = 0;
                    for (int i = 0; i < available; i += channels)
                    {
                        short left = unchecked((short)(_audioBuffer[i] * yx5200.playbackVolume / 30));
                        short right = unchecked((short)(_audioBuffer[i + channels - 1] * yx5200.playbackVolume / 30));
                        _output.AddDeltaFast(count++, left - previousLeft, right - previousRight);
                        previousLeft = left; previousRight = right;
                    }
                }
                else
                {
                    _output.AddDeltaFast(0, -previousLeft, -previousRight);
                    previousLeft = previousRight = 0;
                }
            }
            else
            {
                _output.AddDeltaFast(0, -previousLeft, -previousRight);
                previousLeft = previousRight = 0;
            }
            yx5200.audio[0] = previousLeft; yx5200.audio[1] = previousRight;
            _output.EndFrame(unchecked((uint)samples));
            completed = true;
        }
        finally
        {
            if (!completed) yx5200_unload_track();
        }
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YX5200 context buffer is too short.", nameof(state));
        state[..NativeContextSize].Clear();
        WriteT_YX5200(yx5200, state);
        BinaryPrimitives.WriteUInt64LittleEndian(state[NativeContextSize..], _track?.SamplePosition ?? 0);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YX5200 context buffer is too short.", nameof(state));
        bool completed = false;
        try
        {
            yx5200_unload_track();
            ReadT_YX5200(yx5200, state);
            ulong position = BinaryPrimitives.ReadUInt64LittleEndian(state[NativeContextSize..]);
            if (yx5200.rxCounter >= 10) yx5200.rxCounter = 0;
            ushort index = yx5200.trackIndex;
            yx5200.trackIndex = 0;
            if (index > 0 && index <= 2999)
            {
                yx5200_load_track(index, yx5200.playbackLoop);
                _track?.SeekSample(position);
            }
            completed = true;
            return ContextSize;
        }
        finally
        {
            if (!completed) yx5200_unload_track();
        }
    }
    public void Dispose() => yx5200_unload_track();
}
