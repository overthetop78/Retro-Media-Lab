using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

public readonly record struct Wd1770DriveOutputState(bool Motor, bool Step, bool Direction,
    bool WriteGate, bool Active, bool Reading, bool Writing, bool DoubleDensity);

/// <summary>État du contrôleur et des canaux internes, sans lecteur ni image.</summary>
public sealed record Wd1770State
{
    public required int FormatVersion { get; init; }
    public required Wd1770Model Model { get; init; }
    public required int Clock { get; init; }
    public required Wd1770ReadState Reader { get; init; }
    public required Wd1770WriteState Writer { get; init; }
    public required Wd1770DriveOutputState PublishedDriveOutputs { get; init; }
    public required byte[] Id { get; init; }
    public required byte? BusData { get; init; }
    public required long StepPulses { get; init; }
    public required long DataBytesRead { get; init; }
    public required long DataBytesWritten { get; init; }
    public required bool BusSelected { get; init; }
    public required bool BusRead { get; init; }
    public required int BusRegister { get; init; }
    public required byte BusWriteData { get; init; }
    public required byte Command { get; init; }
    public required byte Track { get; init; }
    public required byte Sector { get; init; }
    public required byte Data { get; init; }
    public required byte Errors { get; init; }
    public required byte SeekTarget { get; init; }
    public required byte WriteLatch { get; init; }
    public required bool Drq { get; init; }
    public required bool Irq { get; init; }
    public required bool TypeOne { get; init; }
    public required bool ForceIndex { get; init; }
    public required bool ForceImmediate { get; init; }
    public required bool Index { get; init; }
    public required bool TrackZero { get; init; }
    public required bool WriteProtected { get; init; }
    public required bool ReadData { get; init; }
    public required bool Reset { get; init; }
    public required bool Motor { get; init; }
    public required bool SpinComplete { get; init; }
    public required bool Step { get; init; }
    public required bool Direction { get; init; }
    public required bool Verify { get; init; }
    public required bool WriteLatchFull { get; init; }
    public required bool FormatCrc { get; init; }
    public required bool FormatCrcRequested { get; init; }
    public required int Timer { get; init; }
    public required int MotorIndexes { get; init; }
    public required int IdleIndexes { get; init; }
    public required int SpinIndexes { get; init; }
    public required int SearchIndexes { get; init; }
    public required int RestoreSteps { get; init; }
    public required int IdOffset { get; init; }
    public required int Remaining { get; init; }
    public required int SearchBytes { get; init; }
    public required int WriteRun { get; init; }
    public required int WriteGapBytes { get; init; }
    public required ushort Crc { get; init; }
    public required Phase Phase { get; init; }
}