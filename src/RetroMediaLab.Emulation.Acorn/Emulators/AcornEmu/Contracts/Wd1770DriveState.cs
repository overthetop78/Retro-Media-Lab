using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

/// <summary>Position physique et impulsions du lecteur ; les images restent empruntées au cœur.</summary>
internal sealed record Wd1770DriveState
{
    public required int OwnerClock { get; init; }
    public required Wd1770State Chip { get; init; }
    public required int? CachedImageDrive { get; init; }
    public required int? WritingImageDrive { get; init; }
    public required uint[] Pulses { get; init; }
    public required uint[] WrittenPulses { get; init; }
    public required int MechanicalSpeedPercent { get; init; }
    public required int[] Tracks { get; init; }
    public required uint[] Rotation { get; init; }
    public required long[] Seeks { get; init; }
    public required long[] Reads { get; init; }
    public required long[] Writes { get; init; }
    public required long OwnerFraction { get; init; }
    public required ulong Time { get; init; }
    public required ulong NanosecondFraction { get; init; }
    public required ulong TickBaseHalfClock { get; init; }
    public required ulong ReadUntil { get; init; }
    public required bool CarryPulse { get; init; }
    public required bool IndexPin { get; init; }
    public required bool ReadDataPin { get; init; }
    public required int Drive { get; init; }
    public required int Side { get; init; }
    public required bool Selected { get; init; }
    public required bool? MotorOverride { get; init; }
    public required bool Changed { get; init; }
    public required bool PreviousStep { get; init; }
    public required bool PreviousGate { get; init; }
    public required bool PreviousMotor { get; init; }
    public required bool PreviousReading { get; init; }
    public required bool PreviousWriting { get; init; }
    public required int PreviousDrive { get; init; }
    public required int PreviousSide { get; init; }
    public required bool PreviousDensity { get; init; }
    public required long LastReads { get; init; }
    public required long LastWrites { get; init; }
    public required int CachedTrack { get; init; }
    public required int CachedSide { get; init; }
    public required bool CachedDensity { get; init; }
    public required int NextPulse { get; init; }
    public required int WritingDrive { get; init; }
    public required int WritingTrack { get; init; }
    public required int WritingSide { get; init; }
    public required bool WritingDensity { get; init; }
    public required ulong WriteStarted { get; init; }
    public required uint WritePhase { get; init; }
}