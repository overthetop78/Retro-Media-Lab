using System.IO;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;
using ChipConstants = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Wd1770DriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Câblage des lecteurs aux broches WD1770/1772. Les médias restent empruntés à la machine.</summary>
internal sealed class Wd1770Drive
{
    private static readonly IReadOnlyDictionary<string, int> FmAttributes = new Dictionary<string, int>
        { [C.FmPhysicalLayout] = C.PhysicalLayoutEnabled };
    private static readonly IReadOnlyDictionary<string, int> MfmAttributes = new Dictionary<string, int>
        { [C.MfmPhysicalLayout] = C.PhysicalLayoutEnabled };
    private readonly Func<int, IEmulationDiskImage?> _images;
    private readonly int _ownerClock;
    private readonly uint _siliconClock;
    private readonly int _clocksPerOwnerTick;
    private readonly int _ownerClockRemainder;
    private readonly uint _nanosecondsPerClock;
    private readonly uint _nanosecondRemainder;
    private readonly int[] _tracks;
    private readonly uint[] _rotation;
    private readonly long[] _seeks;
    private readonly long[] _reads;
    private readonly long[] _writes;
    private long _ownerFraction;
    private ulong _time;
    private ulong _nanosecondFraction;
    private ulong _tickBaseHalfClock;
    private ulong _readUntil;
    private bool _carryPulse;
    private bool _indexPin;
    private bool _readDataPin;
    private int _drive;
    private int _side;
    private bool _selected = true;
    private bool? _motorOverride;
    private bool _changed = true;
    private bool _previousStep;
    private bool _previousGate;
    private bool _previousMotor;
    private bool _previousReading;
    private bool _previousWriting;
    private int _previousDrive;
    private int _previousSide;
    private bool _previousDensity;
    private long _lastReads;
    private long _lastWrites;
    private IEmulationDiskImage? _cachedImage;
    private int _cachedTrack = -1;
    private int _cachedSide = -1;
    private bool _cachedDensity;
    private uint[] _pulses = [];
    private int _nextPulse;
    private IEmulationDiskImage? _writingImage;
    private int _writingDrive;
    private int _writingTrack;
    private int _writingSide;
    private bool _writingDensity;
    private ulong _writeStarted;
    private uint _writePhase;
    private readonly List<uint> _writtenPulses = [];

    internal Wd1770 Chip { get; }
    internal int Drive
    {
        get => _drive;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _tracks.Length);
            if (_drive == value) return;
            Synchronize();
            _drive = value;
            _changed = true;
        }
    }
    internal int Side { get => _side; set { if (_side != value) { _side = value; _changed = true; } } }
    internal bool Selected { get => _selected; set { if (_selected != value) { _selected = value; _changed = true; } } }
    internal bool? MotorOverride { get => _motorOverride; set { if (_motorOverride != value) { _motorOverride = value; _changed = true; } } }
    internal bool DoubleDensity
    {
        get => Chip.DoubleDensity;
        set { if (Chip.DoubleDensity != value) { Chip.DoubleDensity = value; _changed = true; } }
    }
    internal int MechanicalSpeedPercent { get; set; } = C.NormalSpeedPercent;
    internal bool Active => Chip.Active;
    internal bool Reading => Chip.Reading;
    internal bool Writing => Chip.Writing;
    internal bool Motor => Chip.Motor;
    internal bool Drq => Chip.Drq;
    internal bool Irq => Chip.Irq;
    internal bool Nmi => Chip.Nmi;
    internal long SeekSteps(int drive) => _seeks[drive];
    internal long ReadBytes(int drive) => _reads[drive];
    internal long WrittenBytes(int drive) => _writes[drive];
    private bool DriveMotor => Selected && (MotorOverride ?? Chip.Motor);
    private string EncodingId => DoubleDensity ? C.MfmEncoding : C.FmEncoding;
    private uint CellNanoseconds => DoubleDensity ? C.MfmCellNanoseconds : C.FmCellNanoseconds;
    private IReadOnlyDictionary<string, int> Attributes => DoubleDensity ? MfmAttributes : FmAttributes;

    internal Wd1770Drive(Func<int, IEmulationDiskImage?> images, int ticksPerSecond = C.OwnerClock,
        Wd1770Model model = Wd1770Model.Wd1770, int driveCount = C.DefaultDriveCount)
    {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfLessThan(driveCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(driveCount, C.MaximumDriveCount);
        _images = images;
        _ownerClock = ticksPerSecond;
        Chip = new(model);
        _siliconClock = (uint)Chip.ClockFrequency;
        _clocksPerOwnerTick = Chip.ClockFrequency / _ownerClock;
        _ownerClockRemainder = Chip.ClockFrequency % _ownerClock;
        _nanosecondsPerClock = C.NanosecondsPerSecond / (uint)Chip.ClockFrequency;
        _nanosecondRemainder = C.NanosecondsPerSecond % (uint)Chip.ClockFrequency;
        Chip.DriveSignalsChanged += () => _changed = true;
        Chip.WriteDataChanged += CaptureWriteData;
        _tracks = new int[driveCount];
        _rotation = new uint[driveCount];
        _seeks = new long[driveCount];
        _reads = new long[driveCount];
        _writes = new long[driveCount];
        Synchronize();
    }

    internal byte Read(int register)
    {
        Synchronize();
        var value = Chip.Read(register);
        CaptureCounts();
        return value;
    }
    internal void Write(int register, byte value)
    {
        Synchronize();
        Chip.Write(register, value);
        CaptureCounts();
        Synchronize();
    }
    internal void Reset() { Chip.Reset(); _changed = true; Synchronize(); }
    internal void SetReset(bool active) { Chip.SetReset(active); _changed = true; Synchronize(); }
    internal void MediaChanged(int drive)
    {
        if (_writingImage is not null && _writingDrive == drive) PublishWindow(false);
        _changed = true;
        _cachedTrack = -1;
        _pulses = [];
        _readUntil = 0;
        Synchronize();
    }
    internal void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        for (var tick = 0; tick < ticks; tick++) Tick();
    }
    internal void Tick()
    {
        _ownerFraction += _ownerClockRemainder;
        var clocks = _clocksPerOwnerTick;
        if (_ownerFraction >= _ownerClock) { clocks++; _ownerFraction -= _ownerClock; }
        if (_changed) Synchronize();
        if (!Active && !_previousMotor)
        {
            if (_indexPin) { _indexPin = false; Chip.SetIndex(false); }
            if (_readDataPin) { _readDataPin = false; Chip.SetReadData(false); }
            Chip.Advance(clocks);
            _time += (ulong)clocks * _nanosecondsPerClock;
            if (_nanosecondRemainder != 0)
            {
                _nanosecondFraction += (ulong)clocks * _nanosecondRemainder;
                _time += _nanosecondFraction / _siliconClock;
                _nanosecondFraction %= _siliconClock;
            }
            return;
        }
        // La facilité de vitesse reste dans le câblage. Les cycles internes de la puce ne sont pas supprimés.
        var accelerated = MechanicalSpeedPercent != C.NormalSpeedPercent && Active && !Drq && !Irq;
        if (accelerated)
            clocks = MechanicalSpeedPercent == 0 ? clocks * C.MaximumClockMultiplier
                : Math.Max(1, clocks * MechanicalSpeedPercent / C.NormalSpeedPercent);
        _tickBaseHalfClock = Chip.HalfClocks;
        for (var clock = 0; clock < clocks; clock++)
        {
            TickSilicon();
            if (accelerated && (Drq || Irq)) break;
        }
    }

    private void TickSilicon()
    {
        if (_changed) Synchronize();
        if (_previousMotor)
        {
            if (_writingImage is null && _cachedTrack < 0) EnsureTrack();
            var indexLevel = _cachedImage is not null && _rotation[_drive] < C.IndexNanoseconds;
            if (indexLevel != _indexPin) { _indexPin = indexLevel; Chip.SetIndex(indexLevel); }
            if (_carryPulse) { ReadPulse(); _carryPulse = false; }
            while (_writingImage is null && _nextPulse < _pulses.Length && _pulses[_nextPulse] <= _rotation[_drive])
            { ReadPulse(); _nextPulse++; }
        }
        else if (_indexPin) { _indexPin = false; Chip.SetIndex(false); }
        var readLevel = _previousMotor && _time < _readUntil;
        if (readLevel != _readDataPin) { _readDataPin = readLevel; Chip.SetReadData(readLevel); }
        if (_changed) Synchronize(); // L'index peut modifier MO/WG avant le front CLK.
        Chip.Tick();
        _tickBaseHalfClock += ChipConstants.HalfClocksPerClock;
        var elapsed = _nanosecondsPerClock;
        if (_nanosecondRemainder != 0)
        {
            _nanosecondFraction += _nanosecondRemainder;
            if (_nanosecondFraction >= _siliconClock)
            {
                elapsed++;
                _nanosecondFraction -= _siliconClock;
            }
        }
        _time += elapsed;
        if (_previousMotor)
        {
            _rotation[_drive] += elapsed;
            if (_rotation[_drive] >= C.RevolutionNanoseconds)
            {
                _carryPulse = _pulses.Length != 0 && _pulses[^1] == C.RevolutionNanoseconds;
                _rotation[_drive] -= C.RevolutionNanoseconds;
                _nextPulse = 0;
            }
        }
        if (_changed || _writingImage is not null && _time - _writeStarted >= C.RevolutionNanoseconds)
            Synchronize();
    }

    internal void Synchronize()
    {
        CaptureCounts();
        var changed = _changed || _previousDrive != Drive || _previousSide != Side || _previousDensity != DoubleDensity;
        var step = Chip.Step;
        var gate = Chip.WriteGate;
        var motor = DriveMotor;
        if (!changed && step == _previousStep && gate == _previousGate && motor == _previousMotor
            && Reading == _previousReading && Writing == _previousWriting)
        {
            if (_writingImage is not null && _time - _writeStarted >= C.RevolutionNanoseconds) PublishWindow(true);
            return;
        }
        _changed = false;
        if (changed)
        {
            _cachedTrack = -1;
            _readUntil = 0;
            _carryPulse = false;
        }
        if (step && !_previousStep && Selected)
        {
            _tracks[Drive] = Math.Clamp(_tracks[Drive] + (Chip.Direction ? 1 : -1), 0, byte.MaxValue);
            _seeks[Drive]++;
            _cachedTrack = -1;
            _images(Drive)?.Mechanics.Seek(_tracks[Drive], Side);
        }
        Chip.SetTrackZero(Selected && _tracks[Drive] == 0);
        Chip.SetWriteProtected(Selected && _images(Drive)?.IsReadOnly == true);
        for (var drive = 0; drive < _tracks.Length; drive++)
        {
            _images(drive)?.Mechanics.SetMotor(motor && drive == Drive);
            _images(drive)?.Mechanics.SetActivity(Selected && drive == Drive && Reading, Selected && drive == Drive && Writing);
        }
        if (_writingImage is not null && (!gate || !Selected || Drive != _writingDrive || Side != _writingSide || DoubleDensity != _writingDensity))
            PublishWindow(false);
        if (gate && Selected && _writingImage is null && _images(Drive) is { } image)
        {
            _writingImage = image;
            _writingDrive = Drive;
            _writingTrack = _tracks[Drive];
            _writingSide = Side;
            _writingDensity = DoubleDensity;
            _writeStarted = _time;
            _writePhase = _rotation[Drive];
            _writtenPulses.Clear();
        }
        _previousDrive = Drive;
        _previousSide = Side;
        _previousDensity = DoubleDensity;
        _previousStep = step;
        _previousGate = gate;
        _previousMotor = motor;
        _previousReading = Reading;
        _previousWriting = Writing;
    }
    private void CaptureCounts()
    {
        _reads[Drive] += Chip.DataBytesRead - _lastReads;
        _writes[Drive] += Chip.DataBytesWritten - _lastWrites;
        _lastReads = Chip.DataBytesRead;
        _lastWrites = Chip.DataBytesWritten;
    }
    private void ReadPulse() => _readUntil = _time + (DoubleDensity ? C.MfmReadPulseNanoseconds : C.FmReadPulseNanoseconds);
    private void EnsureTrack()
    {
        if (_cachedTrack == _tracks[_drive] && _cachedSide == _side && _cachedDensity == _previousDensity) return;
        var image = _images(_drive);
        _cachedImage = image;
        _cachedTrack = _tracks[Drive];
        _cachedSide = Side;
        _cachedDensity = DoubleDensity;
        _pulses = [];
        _nextPulse = 0;
        if (image is null) return;
        try
        {
            var revolution = image.ReadTrack(_cachedTrack, Side, EncodingId, CellNanoseconds,
                C.RevolutionNanoseconds, Attributes).Revolutions[0];
            uint position = 0;
            _pulses = revolution.FluxIntervalsNanoseconds.Select(value => position = checked(position + value)).ToArray();
            while (_nextPulse < _pulses.Length && _pulses[_nextPulse] <= _rotation[Drive]) _nextPulse++;
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException) { _pulses = []; }
    }
    private void CaptureWriteData(ulong halfClock, bool high)
    {
        if (!high || _writingImage is null) return;
        var when = _time + ((halfClock - _tickBaseHalfClock) * C.HalfNanosecondsPerSecond + _nanosecondFraction) / _siliconClock;
        _writtenPulses.Add(checked((uint)(when - _writeStarted)));
    }
    private void PublishWindow(bool keepWriting)
    {
        if (_writingImage is not { } image) return;
        var duration = checked((uint)(_time - _writeStarted));
        var edge = _writtenPulses.Count != 0 && _writtenPulses[^1] == duration;
        _writingImage = null;
        var encoding = _writingDensity ? C.MfmEncoding : C.FmEncoding;
        var cell = _writingDensity ? C.MfmCellNanoseconds : C.FmCellNanoseconds;
        var attributes = _writingDensity ? MfmAttributes : FmAttributes;
        try
        {
            if (duration != 0) image.WriteTrackWindow(_writingTrack, _writingSide, encoding, cell,
                C.RevolutionNanoseconds, _writePhase, duration,
                _writtenPulses.Where(value => !keepWriting || value < duration).ToArray(), attributes, !keepWriting);
            if (keepWriting)
            {
                _writingImage = image;
                _writeStarted = _time;
                _writePhase = _rotation[_writingDrive];
            }
        }
        finally
        {
            _writtenPulses.Clear();
            if (_writingImage is not null && edge) _writtenPulses.Add(0);
            _cachedTrack = -1;
        }
    }
}
