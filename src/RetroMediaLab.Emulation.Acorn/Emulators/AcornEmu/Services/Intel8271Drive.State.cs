using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Intel8271Drive
{
    internal Intel8271DriveState CaptureState() => new()
    {
        OwnerClock = _ownerClock,
        Chip = _chip.CaptureState(),
        CachedImageDrive = FindStateImage(_cachedImage),
        WritingImageDrive = FindStateImage(_writingImage),
        Pulses = (uint[])_pulses.Clone(),
        WrittenPulses = _writtenPulses.ToArray(),

        Tracks = (int[])_tracks.Clone(),
        Rotation = (uint[])_rotation.Clone(),
        IndexLevels = (bool?[])_indexLevels.Clone(),
        TimeFraction = _timeFraction,
        PreviousControl = _previousControl,
        SignalsChanged = _signalsChanged,
        TrackChanged = _trackChanged,
        OwnerTick = _ownerTick,
        NextControllerTick = _nextControllerTick,
        ReadDataHigh = _readDataHigh,
        PreviousDrive = _previousDrive,
        Motor = _motor,
        Time = _time,
        LastReadPulse = _lastReadPulse,
        HasReadPulse = _hasReadPulse,
        CachedTrack = _cachedTrack,
        CachedHead = _cachedHead,
        NextPulse = _nextPulse,
        WritingTrack = _writingTrack,
        WritingHead = _writingHead,
        WritingDrive = _writingDrive,
        WriteStarted = _writeStarted,
        WritePhase = _writePhase,
    };

    internal void RestoreState(Intel8271DriveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.OwnerClock != _ownerClock || state.Pulses is null || state.WrittenPulses is null
            || state.Tracks?.Length != _tracks.Length
            || state.Rotation?.Length != _rotation.Length
            || state.IndexLevels?.Length != _indexLevels.Length
            ) throw new ArgumentException(nameof(state));
        var cached = ResolveStateImage(state.CachedImageDrive);
        var writing = ResolveStateImage(state.WritingImageDrive);
        _chip.RestoreState(state.Chip);
        _cachedImage = cached;
        _writingImage = writing;
        _pulses = (uint[])state.Pulses.Clone();
        _writtenPulses.Clear();
        _writtenPulses.AddRange(state.WrittenPulses);

        state.Tracks.CopyTo(_tracks, 0);
        state.Rotation.CopyTo(_rotation, 0);
        state.IndexLevels.CopyTo(_indexLevels, 0);
        _timeFraction = state.TimeFraction;
        _previousControl = state.PreviousControl;
        _signalsChanged = state.SignalsChanged;
        _trackChanged = state.TrackChanged;
        _ownerTick = state.OwnerTick;
        _nextControllerTick = state.NextControllerTick;
        _readDataHigh = state.ReadDataHigh;
        _previousDrive = state.PreviousDrive;
        _motor = state.Motor;
        _time = state.Time;
        _lastReadPulse = state.LastReadPulse;
        _hasReadPulse = state.HasReadPulse;
        _cachedTrack = state.CachedTrack;
        _cachedHead = state.CachedHead;
        _nextPulse = state.NextPulse;
        _writingTrack = state.WritingTrack;
        _writingHead = state.WritingHead;
        _writingDrive = state.WritingDrive;
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