using System.Text.Json;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class EmulatorCore
{
    public byte[] SaveState()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return JsonSerializer.SerializeToUtf8Bytes(new EmulatorState(1, Board.CaptureState(),
            _videoSequence, _audioSequence, _audioFrames, _audio.ToArray(), LatestAudioChunk, (PeripheralCategory[])_controllers.Clone()));
    }
    public void LoadState(ReadOnlySpan<byte> state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EmulatorState saved;
        try { saved = JsonSerializer.Deserialize<EmulatorState>(state) ?? throw new ArgumentException("Empty machine state."); }
        catch (JsonException error) { throw new ArgumentException("Invalid machine state.", nameof(state), error); }
        if (saved.Version != 1 || saved.Board is null || saved.VideoSequence < 0 || saved.AudioSequence < 0
            || saved.AudioFrames < 0 || saved.Audio is null || saved.Controllers is not { Length: 2 })
            throw new ArgumentException("Invalid machine state.", nameof(state));
        foreach (var controller in saved.Controllers) ValidateController(controller);
        long buffered = 0, previousSequence = 0;
        foreach (var chunk in saved.Audio)
        {
            if (!ValidChunk(chunk) || chunk.Sequence <= previousSequence || chunk.Sequence > saved.AudioSequence)
                throw new ArgumentException("Invalid audio queue.", nameof(state));
            previousSequence = chunk.Sequence; buffered += chunk.FrameCount;
        }
        if (buffered > int.MaxValue || saved.LatestAudio is { } last && (!ValidChunk(last) || last.Sequence != saved.AudioSequence))
            throw new ArgumentException("Invalid audio state.", nameof(state));
        // Board validates every child on a staging machine before touching the live machine.
        Board.RestoreState(saved.Board);
        _videoSequence = saved.VideoSequence; _audioSequence = saved.AudioSequence; _audioFrames = saved.AudioFrames;
        saved.Controllers.CopyTo(_controllers, 0);
        _audio.Clear(); foreach (var chunk in saved.Audio) _audio.Enqueue(chunk);
        Interlocked.Exchange(ref _bufferedAudioFrames, (int)buffered);
        LatestAudioChunk = saved.LatestAudio;
        LatestVideoFrame = _videoSequence == 0 ? null : PresentVideo(_videoSequence);
    }
    private bool ValidChunk(AudioChunk? chunk) => chunk is not null && chunk.HasValidLength
        && chunk.ChannelLayout == EmulationAudioChannelLayout.Mono && chunk.SampleRate == SampleRate
        && chunk.Sequence > 0 && chunk.Timestamp >= TimeSpan.Zero;
}
