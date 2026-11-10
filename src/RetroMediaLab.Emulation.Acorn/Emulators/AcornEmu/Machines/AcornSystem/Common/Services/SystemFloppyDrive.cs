using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Intel8271DriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Drive-side wires of 400,004. Images are borrowed; READY and separation belong to the card.</summary>
internal sealed partial class SystemFloppyDrive : IDisposable
{
    private sealed class Drive(int heads)
    {
        internal readonly int Heads = heads;
        internal IEmulationDiskImage? Image;
        internal int Track;
        internal int Head;
        internal uint Rotation;
        internal long Time;
        internal bool Selected, Motor, Writing;
        internal uint[] Pulses = [];
        internal int Pulse;
        internal long WriteStart;
        internal uint WritePhase;
        internal readonly List<uint> Written = [];
    }

    private readonly SystemFloppyBoard _board;
    private readonly Drive[] _drives;
    private long _time, _pulseEnd = long.MaxValue;
    private long _next = long.MaxValue;
    private byte _levels = byte.MaxValue, _known;
    private bool _disposed;
    private bool? _index, _trackZero, _protectedDisk;
    // Preserve the existing relay's half-microsecond pulse; flux assertions are no longer rounded to its tick.
    private const int ReadPulseNanoseconds = 500;
    private const uint CellNanoseconds = 4000;

    internal SystemFloppyDrive(SystemFloppyBoard board, IReadOnlyList<int>? heads = null)
    {
        if (heads is not null && (heads.Count != 2 || heads.Any(count => count is not (1 or 2))))
            throw new ArgumentException("Each of the two physical drives must have one or two heads.", nameof(heads));
        _drives = [new(heads?[0] ?? 1), new(heads?[1] ?? 1)];
        _board = board;
        board.ConnectDriveTiming(this);
        try
        {
            board.DriveLinesChanged = LinesChanged;
            LinesChanged(board.DriveLevels, board.DriveKnown, byte.MaxValue);
        }
        catch { Dispose(); throw; }
    }

    internal long NextEventNanoseconds => _next;
    internal Action<long>? NextEventChanged { get; set; }
    internal bool Motor(int index) => _drives[index].Motor;
    internal bool Selected(int index) => _drives[index].Selected;
    internal bool Writing(int index) => _drives[index].Writing;

    private void UpdateNextEvent()
    {
        var next = _pulseEnd;
        foreach (var drive in _drives)
        {
            if (!drive.Motor || drive.Image is null) continue;
            var delta = C.RevolutionNanoseconds - drive.Rotation;
            if (drive.Selected)
            {
                if (drive.Rotation < C.IndexNanoseconds)
                    delta = Math.Min(delta, C.IndexNanoseconds - drive.Rotation);
                if (!drive.Writing && drive.Pulse < drive.Pulses.Length)
                    delta = Math.Min(delta, drive.Pulses[drive.Pulse] - drive.Rotation);
            }
            next = Math.Min(next, checked(drive.Time + delta));
        }
        if (_next == next) return;
        _next = next;
        NextEventChanged?.Invoke(next);
    }

    internal void AdvanceTo(long time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, _time);
        _time = time;
        if (time < _next) return;
        var readWasAsserted = _pulseEnd != long.MaxValue;
        if (_pulseEnd <= time)
        {
            _pulseEnd = long.MaxValue;
        }
        foreach (var drive in _drives)
        {
            MaterializeRotation(drive);
            if (!drive.Motor || drive.Image is null) continue;
            if (drive.Selected && !drive.Writing)
                while (drive.Pulse < drive.Pulses.Length && drive.Pulses[drive.Pulse] <= drive.Rotation)
                {
                    _pulseEnd = checked(time + ReadPulseNanoseconds);
                    drive.Pulse++;
                }
            if (drive.Rotation >= C.RevolutionNanoseconds)
            {
                drive.Rotation -= C.RevolutionNanoseconds;
                drive.Pulse = 0;
                if (drive.Writing) PublishWrite(drive, true);
            }
        }
        // Resolve releases and new pulses at this same instant before publishing
        // the common wire. Touching/overlapping pulses have no high-level gap.
        var readAsserted = _pulseEnd != long.MaxValue;
        if (readWasAsserted != readAsserted) _board.SetReadDataLevel(!readAsserted);
        ReleaseReadDataIfInactive();
        PublishSensors();
        UpdateNextEvent();
    }

    private void MaterializeRotation(Drive drive)
    {
        if (drive.Motor && drive.Image is not null)
            drive.Rotation = checked(drive.Rotation + (uint)(_time - drive.Time));
        drive.Time = _time;
    }

    private bool Asserted(SystemFloppyDriveLines line) => (_known & (byte)line) != 0 && (_levels & (byte)line) == 0;

    private void LinesChanged(byte levels, byte known, byte changed)
    {
        AdvanceTo(_board.TimeNanoseconds);
        var previous = _levels;
        _levels = levels; _known = known;
        var step = (changed & (byte)SystemFloppyDriveLines.Step) != 0 && Asserted(SystemFloppyDriveLines.Step)
            && (previous & (byte)SystemFloppyDriveLines.Step) != 0;
        var writePulse = (changed & (byte)SystemFloppyDriveLines.WriteData) != 0 && Asserted(SystemFloppyDriveLines.WriteData);
        for (var index = 0; index < _drives.Length; index++)
        {
            var drive = _drives[index];
            MaterializeRotation(drive);
            var wasSelected = drive.Selected;
            drive.Selected = Asserted(index == 0 ? SystemFloppyDriveLines.Select0 : SystemFloppyDriveLines.Select1);
            drive.Motor = Asserted(SystemFloppyDriveLines.Motor);
            if (drive.Selected && !wasSelected)
                while (drive.Pulse < drive.Pulses.Length && drive.Pulses[drive.Pulse] <= drive.Rotation) drive.Pulse++;
            // Handbook p8: buffered FAULT/OP0 is surface select in System5.
            // The physical drive model decides whether a second head exists.
            var head = drive.Heads == 2 && Asserted(SystemFloppyDriveLines.FaultReset) ? 1 : 0;
            var stepping = step && drive.Selected;
            if (stepping || head != drive.Head)
            {
                if (drive.Writing) PublishWrite(drive, false);
                if (stepping)
                    drive.Track = Math.Clamp(drive.Track + (Asserted(SystemFloppyDriveLines.Direction) ? 1 : -1), 0, byte.MaxValue);
                drive.Head = head;
                drive.Image?.Mechanics.Seek(drive.Track, drive.Head);
                LoadTrack(drive);
            }
            UpdateWriteGate(drive);
            if (writePulse && drive.Writing) drive.Written.Add(checked((uint)(_time - drive.WriteStart)));
            UpdateMechanics(drive);
        }
        ReleaseReadDataIfInactive();
        PublishSensors();
        UpdateNextEvent();
    }

    private void UpdateWriteGate(Drive drive)
    {
        var writing = drive.Selected && drive.Motor && Asserted(SystemFloppyDriveLines.WriteEnable)
            && drive.Image is { IsReadOnly: false };
        if (drive.Writing && !writing) PublishWrite(drive, false);
        if (writing && !drive.Writing)
        {
            drive.Writing = true; drive.WriteStart = _time; drive.WritePhase = drive.Rotation; drive.Written.Clear();
        }
    }

    private static void UpdateMechanics(Drive drive)
    {
        drive.Image?.Mechanics.SetMotor(drive.Motor);
        drive.Image?.Mechanics.SetActivity(drive.Selected && drive.Motor && !drive.Writing, drive.Writing);
    }

    private void PublishSensors()
    {
        var index = false; var trackZero = false; var protectedDisk = false;
        foreach (var drive in _drives)
        {
            if (!drive.Selected) continue;
            index |= drive.Image is null || (drive.Motor && drive.Rotation < C.IndexNanoseconds);
            trackZero |= drive.Track == 0;
            protectedDisk |= drive.Image?.IsReadOnly == true;
        }
        if (_index != index)
        {
            _index = index;
            _board.SetIndexLevel(!index);
        }
        if (_trackZero != trackZero || _protectedDisk != protectedDisk)
        {
            _trackZero = trackZero; _protectedDisk = protectedDisk;
            _board.SetDriveSensorLevels(!trackZero, !protectedDisk);
        }
    }

    private void ReleaseReadDataIfInactive()
    {
        if (_pulseEnd == long.MaxValue) return;
        foreach (var drive in _drives)
            if (drive.Selected && drive.Motor && !drive.Writing && drive.Image is not null) return;
        _pulseEnd = long.MaxValue;
        _board.SetReadDataLevel(true);
    }

    internal void Attach(int index, IEmulationDiskImage? image)
    {
        AdvanceTo(_board.TimeNanoseconds);
        var drive = _drives[index];
        MaterializeRotation(drive);
        if (drive.Writing) PublishWrite(drive, false);
        drive.Image?.Mechanics.SetMotor(false);
        drive.Image?.Mechanics.SetActivity(false, false);
        drive.Image = image;
        LoadTrack(drive);
        // The inserted medium immediately sees the levels already present on
        // the connector. No new write-data pulse or control edge is required.
        UpdateWriteGate(drive);
        UpdateMechanics(drive);
        ReleaseReadDataIfInactive();
        PublishSensors();
        UpdateNextEvent();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            AdvanceTo(_board.TimeNanoseconds);
            foreach (var drive in _drives) if (drive.Writing) PublishWrite(drive, false);
        }
        finally
        {
            try
            {
                if (_pulseEnd != long.MaxValue)
                {
                    // This relay no longer drives NRDATA. A pending pulse must
                    // not remain latched in the borrowed controller or mask the
                    // first transition of a subsequently connected relay.
                    _pulseEnd = long.MaxValue;
                    _board.SetReadDataLevel(true);
                }
            }
            finally
            {
                _board.DisconnectDriveTiming(this);
                _board.DriveLinesChanged = null;
                foreach (var drive in _drives)
                {
                    drive.Image?.Mechanics.SetMotor(false);
                    drive.Image?.Mechanics.SetActivity(false, false);
                    drive.Image = null;
                }
            }
        }
    }
}
