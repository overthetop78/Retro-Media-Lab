using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

/// <summary>Position physique et impulsions du lecteur ; les images restent empruntées au cœur.</summary>
internal sealed record Intel8271DriveState
{
    public required int OwnerClock { get; init; }
    public required Intel8271State Chip { get; init; }
    public required int? CachedImageDrive { get; init; }
    public required int? WritingImageDrive { get; init; }
    public required uint[] Pulses { get; init; }
    public required uint[] WrittenPulses { get; init; }

    public required int[] Tracks { get; init; }
    public required uint[] Rotation { get; init; }
    public required bool?[] IndexLevels { get; init; }
    public required long TimeFraction { get; init; }
    public required byte PreviousControl { get; init; }
    public required bool SignalsChanged { get; init; }
    public required bool TrackChanged { get; init; }
    public required long OwnerTick { get; init; }
    public required long NextControllerTick { get; init; }
    public required bool ReadDataHigh { get; init; }
    public required int PreviousDrive { get; init; }
    public required bool Motor { get; init; }
    public required ulong Time { get; init; }
    public required ulong LastReadPulse { get; init; }
    public required bool HasReadPulse { get; init; }
    public required int CachedTrack { get; init; }
    public required int CachedHead { get; init; }
    public required int NextPulse { get; init; }
    public required int WritingTrack { get; init; }
    public required int WritingHead { get; init; }
    public required int WritingDrive { get; init; }
    public required ulong WriteStarted { get; init; }
    public required uint WritePhase { get; init; }
}