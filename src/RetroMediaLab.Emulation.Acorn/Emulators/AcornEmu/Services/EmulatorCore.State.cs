using System.Text.Json;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.EmulatorConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    /// <summary>Le même parcours sérialise tous les modèles ; il est appelé sur le thread du cœur.</summary>
    public byte[] SaveState()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var machine = _machine ?? throw new InvalidOperationException();
        var stateType = machine.StateType ?? throw new NotSupportedException();
        return JsonSerializer.SerializeToUtf8Bytes(new AcornEmuSavedState
        {
            FormatVersion = C.StateFormatVersion,
            Model = _definition.Model,
            Machine = JsonSerializer.SerializeToElement(machine.CaptureState(), stateType),
            VideoSequence = _sequence,
            VideoTimeSeconds = _videoTimeSeconds,
            AudioSequence = _audioSequence,
            AudioFrames = _audioFrames,
            Video = LatestVideoFrame,
            LatestAudio = LatestAudioChunk,
            PendingAudio = _audio.ToArray()
        });
    }

    /// <summary>Réinjecte l'instantané ; aucun RunFrame, reset ou accès média n'est exécuté.</summary>
    public void LoadState(ReadOnlySpan<byte> state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var machine = _machine ?? throw new InvalidOperationException();
        var stateType = machine.StateType ?? throw new NotSupportedException();
        var saved = JsonSerializer.Deserialize<AcornEmuSavedState>(state) ?? throw new ArgumentException(nameof(state));
        if (saved.FormatVersion != C.StateFormatVersion || saved.Model != _definition.Model
            || saved.PendingAudio is null || saved.PendingAudio.Any(chunk => chunk is null || !chunk.HasValidLength))
            throw new ArgumentException(nameof(state));
        var material = saved.Machine.Deserialize(stateType) ?? throw new ArgumentException(nameof(state));
        machine.RestoreState(material);
        _sequence = saved.VideoSequence;
        _videoTimeSeconds = saved.VideoTimeSeconds;
        _audioSequence = saved.AudioSequence;
        _audioFrames = saved.AudioFrames;
        LatestVideoFrame = saved.Video;
        ClearAudio();
        foreach (var chunk in saved.PendingAudio) _audio.Enqueue(chunk);
        Volatile.Write(ref _bufferedAudioFrames, saved.PendingAudio.Sum(chunk => chunk.FrameCount));
        LatestAudioChunk = saved.LatestAudio;
    }
}
