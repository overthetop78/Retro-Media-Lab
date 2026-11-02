using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;
using System.IO;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;
namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

/// <summary>WD1770 : protocole de registres et transferts cadencés sur les secteurs du SDK.</summary>
/// <remarks>La carte possède la sélection du lecteur et la ligne NMI ; le cœur possède les images.
/// Le composant ne crée ni fichier ni thread. Chaque Tick représente un cycle de son horloge d'entrée.</remarks>
public sealed partial class Wd1770
{
    private readonly Func<int, IDiskImage?> _disk;
    private readonly int _clock;
    private readonly int[] _physicalTracks;
    private readonly long[] _seekSteps;
    private readonly long[] _readBytes;
    private readonly long[] _writtenBytes;
    private readonly IReadOnlyList<int> _stepMilliseconds;
    private int _drive;
    public long SeekSteps(int drive) => _seekSteps[drive];
    public long ReadBytes(int drive) => _readBytes[drive];
    public long WrittenBytes(int drive) => _writtenBytes[drive];
    private byte _command, _track, _sector = 1, _data, _errors;
    private int _delay, _offset, _targetTrack, _motorTicks, _rotation, _idIndex, _direction = 1;
    private byte[] _buffer = [];
    private bool _drq, _irq, _typeOne, _crcError, _forceIndex, _forceReady, _forceNotReady, _wasReady;
    private Phase _phase;
    public int Drive
    {
        get => _drive;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _physicalTracks.Length);
            _drive = value;
        }
    }
    public int Side { get; set; }
    public bool Selected { get; set; } = true;
    public bool DoubleDensity { get; set; } = true;
    public int MechanicalSpeedPercent { get; set; } = 100;
    public bool Irq => _irq;
    public bool Drq => _drq;
    public bool Nmi => _irq || _drq;
    public bool Motor => _motorTicks > 0;
    public bool Active => _phase != Phase.Idle;
    public bool Writing => _phase is Phase.WriteSector or Phase.WriteTrack;
    public bool Reading => _phase is Phase.ReadSector or Phase.ReadAddress or Phase.ReadTrack;
    private IDiskImage? Image => Selected ? _disk(Drive) : null;
    private int ByteTicks => Math.Max(1, _clock / (DoubleDensity ? C.DoubleDensityBytesPerSecond : C.SingleDensityBytesPerSecond));
    private int RevolutionTicks => _clock / C.RevolutionsPerSecond;

    public Wd1770(Func<int, IDiskImage?> disk, int ticksPerSecond = C.CpuClock,
        Wd1770Model model = Wd1770Model.Wd1770, int driveCount = C.DefaultDriveCount)
    {
        ArgumentNullException.ThrowIfNull(disk); ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerSecond, C.DoubleDensityBytesPerSecond);
        ArgumentOutOfRangeException.ThrowIfLessThan(driveCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(driveCount, C.MaximumDriveCount);
        if (!Enum.IsDefined(model)) throw new ArgumentOutOfRangeException(nameof(model));
        _disk = disk;
        _clock = ticksPerSecond;
        _stepMilliseconds = model == Wd1770Model.Wd1772 ? C.Wd1772StepMilliseconds : C.StepMilliseconds;
        _physicalTracks = new int[driveCount];
        _seekSteps = new long[driveCount];
        _readBytes = new long[driveCount];
        _writtenBytes = new long[driveCount];
        Reset();
    }

    /// <summary>Réinitialise l'électronique ; la position mécanique des têtes et les images restent en place.</summary>
    public void Reset()
    {
        _command = _track = _data = _errors = 0; _sector = 1;
        _phase = Phase.Idle; _buffer = []; _delay = _offset = _motorTicks = _rotation = _idIndex = 0;
        _drq = _irq = _crcError = _forceIndex = _forceReady = _forceNotReady = false; _typeOne = true; _wasReady = Image is not null;
    }

    public byte Read(int register)
    {
        switch (register & 3)
        {
            case 0:
                _irq = false;
                var status = (byte)(_errors | (Active ? C.Busy : 0) | (Motor ? C.Motor : 0));
                if (_typeOne)
                {
                    if (_physicalTracks[Drive] == 0) status |= 4;
                    if (Motor && _rotation < _clock / 250) status |= 2;
                    if (Motor) status |= 0x20;
                    if (Image?.IsReadOnly == true) status |= C.WriteProtected;
                }
                else if (_drq) status |= C.DataRequest;
                return status;
            case 1: return _track;
            case 2: return _sector;
            default: if (_drq && Reading) _readBytes[Drive]++; _drq = false; return _data;
        }
    }

    public void Write(int register, byte value)
    {
        switch (register & 3)
        {
            case 0: Command(value); break;
            case 1: if (!Active) _track = value; break;
            case 2: if (!Active) _sector = value; break;
            case 3:
                _data = value;
                if (_drq && Writing && _offset < _buffer.Length) { _buffer[_offset++] = value; _writtenBytes[Drive]++; }
                _drq = false; break;
        }
    }

    /// <summary>Avance le moteur, les délais mécaniques et la fenêtre de service des octets DRQ.</summary>
    public void Tick() => Advance(1);

    /// <summary>Saute les intervalles sans événement tout en conservant les échéances DRQ, moteur et index.</summary>
    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        while (ticks > 0)
        {
            var elapsed = Active ? Math.Min(ticks, Math.Max(1, _delay)) : ticks;
            if (_forceIndex) elapsed = Math.Min(elapsed, RevolutionTicks - _rotation);
            _motorTicks = Math.Max(0, _motorTicks - elapsed);
            _rotation = (int)(((long)_rotation + elapsed) % RevolutionTicks);
            var ready = Image is not null;
            if (_forceIndex && _rotation == 0 || _forceReady && ready && !_wasReady || _forceNotReady && !ready && _wasReady) _irq = true;
            _wasReady = ready;
            ticks -= elapsed;
            if (!Active) continue;
            _delay -= elapsed;
            if (_delay > 0) continue;
            if (_phase == Phase.Positioning) { Positioned(); continue; }
            if (_drq)
            {
                _errors |= C.LostData;
                if (Writing && _offset < _buffer.Length) _buffer[_offset++] = 0;
            }
            if (_offset == _buffer.Length) { TransferCompleted(); continue; }
            if (!Writing) _data = _buffer[_offset++];
            _drq = true;
            _delay = ByteTicks;
            _motorTicks = RevolutionTicks * 10;
        }
    }

    private int MechanicalDelay(int ticks) => MechanicalSpeedPercent == 0 ? 0 : checked((int)((long)ticks * 100 / MechanicalSpeedPercent));
    private void Complete(byte errors = 0)
    {
        _errors |= errors; _phase = Phase.Idle; _drq = false; _irq = true; _buffer = [];
        _motorTicks = Math.Max(_motorTicks, RevolutionTicks * 9);
    }
    private static bool MediaFailure(Exception error) => error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
}
