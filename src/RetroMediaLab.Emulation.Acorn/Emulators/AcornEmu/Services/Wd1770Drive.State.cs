using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Wd1770Drive
{
    internal Wd1770DriveState CaptureState() => new()
    {
        OwnerClock = _ownerClock,
        Chip = Chip.CaptureState(),
        CachedImageDrive = FindStateImage(_cachedImage),
        WritingImageDrive = FindStateImage(_writingImage),
        Pulses = (uint[])_pulses.Clone(),
        WrittenPulses = _writtenPulses.ToArray(),
        MechanicalSpeedPercent = MechanicalSpeedPercent,
        Tracks = (int[])_tracks.Clone(),
        Rotation = (uint[])_rotation.Clone(),
        Seeks = (long[])_seeks.Clone(),
        Reads = (long[])_reads.Clone(),
        Writes = (long[])_writes.Clone(),
        OwnerFraction = _ownerFraction,
        Time = _time,
        NanosecondFraction = _nanosecondFraction,
        TickBaseHalfClock = _tickBaseHalfClock,
        ReadUntil = _readUntil,
        CarryPulse = _carryPulse,
        IndexPin = _indexPin,
        ReadDataPin = _readDataPin,
        Drive = _drive,
        Side = _side,
        Selected = _selected,
        MotorOverride = _motorOverride,
        Changed = _changed,
        PreviousStep = _previousStep,
        PreviousGate = _previousGate,
        PreviousMotor = _previousMotor,
        PreviousReading = _previousReading,
        PreviousWriting = _previousWriting,
        PreviousDrive = _previousDrive,
        PreviousSide = _previousSide,
        PreviousDensity = _previousDensity,
        LastReads = _lastReads,
        LastWrites = _lastWrites,
        CachedTrack = _cachedTrack,
        CachedSide = _cachedSide,
        CachedDensity = _cachedDensity,
        NextPulse = _nextPulse,
        WritingDrive = _writingDrive,
        WritingTrack = _writingTrack,
        WritingSide = _writingSide,
        WritingDensity = _writingDensity,
        WriteStarted = _writeStarted,
        WritePhase = _writePhase,
    };

    internal void RestoreState(Wd1770DriveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.OwnerClock != _ownerClock || state.Pulses is null || state.WrittenPulses is null
            || state.Tracks?.Length != _tracks.Length
            || state.Rotation?.Length != _rotation.Length
            || state.Seeks?.Length != _seeks.Length
            || state.Reads?.Length != _reads.Length
            || state.Writes?.Length != _writes.Length
            ) throw new ArgumentException(nameof(state));
        var cached = ResolveStateImage(state.CachedImageDrive);
        var writing = ResolveStateImage(state.WritingImageDrive);
        Chip.RestoreState(state.Chip);
        _cachedImage = cached;
        _writingImage = writing;
        _pulses = (uint[])state.Pulses.Clone();
        _writtenPulses.Clear();
        _writtenPulses.AddRange(state.WrittenPulses);
        MechanicalSpeedPercent = state.MechanicalSpeedPercent;
        state.Tracks.CopyTo(_tracks, 0);
        state.Rotation.CopyTo(_rotation, 0);
        state.Seeks.CopyTo(_seeks, 0);
        state.Reads.CopyTo(_reads, 0);
        state.Writes.CopyTo(_writes, 0);
        _ownerFraction = state.OwnerFraction;
        _time = state.Time;
        _nanosecondFraction = state.NanosecondFraction;
        _tickBaseHalfClock = state.TickBaseHalfClock;
        _readUntil = state.ReadUntil;
        _carryPulse = state.CarryPulse;
        _indexPin = state.IndexPin;
        _readDataPin = state.ReadDataPin;
        _drive = state.Drive;
        _side = state.Side;
        _selected = state.Selected;
        _motorOverride = state.MotorOverride;
        _changed = state.Changed;
        _previousStep = state.PreviousStep;
        _previousGate = state.PreviousGate;
        _previousMotor = state.PreviousMotor;
        _previousReading = state.PreviousReading;
        _previousWriting = state.PreviousWriting;
        _previousDrive = state.PreviousDrive;
        _previousSide = state.PreviousSide;
        _previousDensity = state.PreviousDensity;
        _lastReads = state.LastReads;
        _lastWrites = state.LastWrites;
        _cachedTrack = state.CachedTrack;
        _cachedSide = state.CachedSide;
        _cachedDensity = state.CachedDensity;
        _nextPulse = state.NextPulse;
        _writingDrive = state.WritingDrive;
        _writingTrack = state.WritingTrack;
        _writingSide = state.WritingSide;
        _writingDensity = state.WritingDensity;
        _writeStarted = state.WriteStarted;
        _writePhase = state.WritePhase;
    }

    private int? FindStateImage(IEmulationDiskImage? image)
    {
        if (image is null) return null;
        for (var drive = 0; drive < _tracks.Length; drive++)
            if (ReferenceEquals(_images(drive), image)) return drive;
        throw new InvalidOperationException();
    }

    private IEmulationDiskImage? ResolveStateImage(int? drive)
    {
        if (drive is null) return null;
        if ((uint)drive.Value >= _tracks.Length) throw new ArgumentException(nameof(drive));
        return _images(drive.Value) ?? throw new InvalidOperationException();
    }
}