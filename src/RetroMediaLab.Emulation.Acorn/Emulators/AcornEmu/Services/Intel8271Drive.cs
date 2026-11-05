using System.IO;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Intel8271DriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Câblage des lecteurs du module sur les broches du 8271 ; le SDK reste propriétaire de l'image.</summary>
internal sealed class Intel8271Drive
{
    private static readonly IReadOnlyDictionary<string, int> TrackAttributes =
        new Dictionary<string, int> { [C.PhysicalLayoutAttribute] = C.PhysicalLayoutEnabled };
    private readonly IIntel8271 _chip;
    private readonly Func<int, IEmulationDiskImage?> _images;
    private readonly Func<byte, int> _headSelect;
    private readonly int _ownerClock;
    private readonly uint _nanosecondsPerTick;
    private readonly int _nanosecondRemainder;
    private readonly uint _cellNanoseconds;
    private readonly uint _dataWindowThresholdNanoseconds;
    private long _timeFraction;
    private readonly int[] _tracks = new int[H.DriveCount];
    private readonly uint[] _rotation = new uint[H.DriveCount];
    private byte _previousControl;
    private bool _signalsChanged = true;
    private int _previousDrive;
    private bool _motor;
    private ulong _time;
    private ulong _lastReadPulse;
    private bool _hasReadPulse;
    private IEmulationDiskImage? _cachedImage;
    private int _cachedTrack = -1;
    private int _cachedHead = -1;
    private uint[] _pulses = [];
    private int _nextPulse;
    private IEmulationDiskImage? _writingImage;
    private int _writingTrack;
    private int _writingHead;
    private int _writingDrive;
    private ulong _writeStarted;
    private uint _writePhase;
    private readonly List<uint> _writtenPulses = [];

    internal Intel8271Drive(IIntel8271 chip, Func<int, IEmulationDiskImage?> images,
        int ownerClock = H.DefaultOwnerClock, int chipClock = H.MiniClock, Func<byte, int>? headSelect = null)
    {
        ArgumentNullException.ThrowIfNull(chip);
        ArgumentNullException.ThrowIfNull(images);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ownerClock);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chipClock);
        _chip = chip;
        _images = images;
        _ownerClock = ownerClock;
        _nanosecondsPerTick = checked((uint)(C.NanosecondsPerSecond / ownerClock));
        _nanosecondRemainder = checked((int)(C.NanosecondsPerSecond % ownerClock));
        _headSelect = headSelect ?? (static _ => 0);
        _cellNanoseconds = checked((uint)((long)H.ChipCyclesPerHalfBit * C.NanosecondsPerSecond / chipClock));
        _dataWindowThresholdNanoseconds = checked((uint)((long)C.StandardSingleShotWindowNanoseconds
            * H.StandardClock / chipClock));
        _chip.WriteDataChanged += WriteDataChanged;
        _chip.DriveSignalsChanged += () => _signalsChanged = true;
    }

    internal void Tick()
    {
        var elapsed = _nanosecondsPerTick;
        _timeFraction += _nanosecondRemainder;
        if (_timeFraction >= _ownerClock)
        {
            _timeFraction -= _ownerClock;
            elapsed++;
        }
        _time += elapsed;
        Synchronize();
        _chip.SetUnseparatedData(false);
        if (_motor)
        {
            var drive = _previousDrive;
            if (_writingImage is null) EnsureTrack(drive);
            _rotation[drive] += elapsed;
            if (_rotation[drive] >= C.RevolutionNanoseconds)
            {
                if (_writingImage is null && _pulses.Length != 0 && _pulses[^1] == C.RevolutionNanoseconds)
                    ReadPulse();
                _rotation[drive] -= C.RevolutionNanoseconds;
                _nextPulse = 0;
            }
            _chip.SetIndex(drive, _rotation[drive] < C.IndexNanoseconds);
            while (_writingImage is null && _nextPulse < _pulses.Length && _pulses[_nextPulse] <= _rotation[drive])
            {
                ReadPulse();
                _nextPulse++;
            }
        }
        _chip.Tick();
        Synchronize();
    }

    internal void Synchronize()
    {
        if (!_signalsChanged)
        {
            if (_writingImage is not null && _time - _writeStarted >= C.RevolutionNanoseconds)
            {
                PublishWindow(continueWriting: true);
                _motor = _chip.Motor;
            }
            return;
        }
        var control = _chip.DriveControl;
        var drive = _chip.ActiveDrive;
        var head = _headSelect(control);
        _signalsChanged = false;
        if ((_previousControl & H.OutputStep) == 0 && (control & H.OutputStep) != 0
            && (control & H.DriveSelectMask) != 0)
        {
            _tracks[drive] = Math.Clamp(_tracks[drive] + ((control & H.OutputDirection) != 0 ? 1 : -1), 0, byte.MaxValue);
            _cachedImage = null;
            _images(drive)?.Mechanics.Seek(_tracks[drive], head);
        }
        for (var index = 0; index < H.DriveCount; index++)
        {
            var image = _images(index);
            _chip.SetReady(index, image is not null);
            _chip.SetWriteProtected(index, image?.IsReadOnly == true);
            _chip.SetTrackZero(index, _tracks[index] == 0);
            if (!_chip.Motor || drive != index) _chip.SetIndex(index, false);
            image?.Mechanics.SetMotor(_chip.Motor && drive == index);
            image?.Mechanics.SetActivity(drive == index && _chip.Reading, drive == index && _chip.Writing);
        }
        var enabled = (control & H.OutputWriteEnable) != 0;
        if (_writingImage is not null && (!enabled || drive != _writingDrive || head != _writingHead)) EndWrite();
        if (enabled && _writingImage is null && _images(drive) is { } writable)
        {
            _writingImage = writable;
            _writingDrive = drive;
            _writingTrack = _tracks[drive];
            _writingHead = head;
            _writeStarted = _time;
            _writePhase = _rotation[drive];
            _writtenPulses.Clear();
        }
        if (_writingImage is not null && _time - _writeStarted >= C.RevolutionNanoseconds)
            PublishWindow(continueWriting: true);
        _previousControl = control;
        _previousDrive = drive;
        _motor = _chip.Motor;
    }

    internal void Reset()
    {
        _signalsChanged = true;
        EndWrite();
        _previousControl = _chip.DriveControl;
        _cachedImage = null;
        _pulses = [];
        _hasReadPulse = false;
        for (var drive = 0; drive < H.DriveCount; drive++) _chip.SetWriteFault(drive, false);
        Synchronize();
    }

    internal void MediaChanged(int drive)
    {
        _signalsChanged = true;
        if (_writingImage is not null && _writingDrive == drive) EndWrite();
        _cachedImage = null;
        _pulses = [];
        _hasReadPulse = false;
        _chip.SetWriteFault(drive, false);
        Synchronize();
    }

    private void EnsureTrack(int drive)
    {
        var image = _images(drive);
        var head = _headSelect(_chip.DriveControl);
        if (ReferenceEquals(image, _cachedImage) && _tracks[drive] == _cachedTrack && head == _cachedHead) return;
        _cachedImage = image;
        _cachedTrack = _tracks[drive];
        _cachedHead = head;
        _pulses = [];
        _nextPulse = 0;
        if (image is null) return;
        try
        {
            var revolution = image.ReadTrack(_cachedTrack, head, C.EncodingId, _cellNanoseconds,
                C.RevolutionNanoseconds, TrackAttributes).Revolutions[0];
            uint position = 0;
            _pulses = revolution.FluxIntervalsNanoseconds.Select(interval => position = checked(position + interval)).ToArray();
            while (_nextPulse < _pulses.Length && _pulses[_nextPulse] <= _rotation[drive]) _nextPulse++;
            image.Mechanics.Seek(_cachedTrack, head);
        }
        catch (Exception error) when (error is IOException or ArgumentException or NotSupportedException)
        { _pulses = []; }
    }

    private void ReadPulse()
    {
        // Le monostable externe distingue les intervalles courts et longs ; une frontière
        // exactement égale à la période entière classerait mal les raccordements déphasés.
        _chip.SetDataWindow(!_hasReadPulse || _time - _lastReadPulse >= _dataWindowThresholdNanoseconds);
        _chip.SetUnseparatedData(true);
        _lastReadPulse = _time;
        _hasReadPulse = true;
    }

    private void WriteDataChanged(bool high)
    {
        if (!high || _writingImage is null) return;
        _writtenPulses.Add(checked((uint)(_time - _writeStarted)));
    }

    private void EndWrite() => PublishWindow(continueWriting: false);

    private void PublishWindow(bool continueWriting)
    {
        if (_writingImage is not { } image) return;
        var duration = checked((uint)(_time - _writeStarted));
        var edgePulse = _writtenPulses.Count != 0 && _writtenPulses[^1] == duration;
        _writingImage = null;
        try
        {
            if (duration != 0) image.WriteTrackWindow(_writingTrack, _writingHead, C.EncodingId,
                _cellNanoseconds, C.RevolutionNanoseconds, _writePhase, duration,
                _writtenPulses.Where(offset => !continueWriting || offset < duration).ToArray(),
                TrackAttributes, includeEndingInstant: !continueWriting);
            if (continueWriting)
            {
                _writingImage = image;
                _writeStarted = _time;
                _writePhase = _rotation[_writingDrive];
            }
        }
        catch (Exception error) when (error is IOException or ArgumentException or NotSupportedException or UnauthorizedAccessException)
        { _signalsChanged = true; _chip.SetWriteFault(_writingDrive, true); }
        finally
        {
            _writtenPulses.Clear();
            if (_writingImage is not null && edgePulse) _writtenPulses.Add(0);
            _cachedImage = null;
        }
    }
}
