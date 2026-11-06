using System.Text.Json;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

/// <summary>Enveloppe commune aux modèles AcornEmu ; les données matérielles restent dans leur assemblage.</summary>
internal sealed record AcornEmuSavedState
{
    public required int FormatVersion { get; init; }
    public required MachineModel Model { get; init; }
    public required JsonElement Machine { get; init; }
    public required long VideoSequence { get; init; }
    public required double VideoTimeSeconds { get; init; }
    public required long AudioSequence { get; init; }
    public required long AudioFrames { get; init; }
    public required VideoFrame? Video { get; init; }
    public required AudioChunk? LatestAudio { get; init; }
    public required AudioChunk[] PendingAudio { get; init; }
}
