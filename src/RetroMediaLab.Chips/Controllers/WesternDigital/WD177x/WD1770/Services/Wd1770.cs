using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

/// <summary>Contrôleur physique WD1770/1772 : registres, séquenceur et broches, sans image ni mécanique de lecteur.</summary>
public sealed partial class Wd1770 : Interfaces.IWd1770
{
    private readonly Wd1770Model _model;
    private readonly int _clock;
    private readonly IReadOnlyList<int> _stepMilliseconds;
    private readonly Wd1770ReadChannel _reader = new();
    private readonly Wd1770WriteChannel _writer = new();
    private readonly byte[] _id = new byte[C.IdBytes];
    private byte _command;
    private byte _track;
    private byte _sector;
    private byte _data;
    private byte _errors;
    private byte _seekTarget;
    private byte _writeLatch;
    private bool _drq;
    private bool _irq;
    private bool _typeOne;
    private bool _forceIndex;
    private bool _forceImmediate;
    private bool _index;
    private bool _trackZero;
    private bool _writeProtected;
    private bool _readData;
    private bool _reset;
    private bool _motor;
    private bool _spinComplete;
    private bool _step;
    private bool _direction;
    private bool _verify;
    private bool _writeLatchFull;
    private bool _formatCrc;
    private bool _formatCrcRequested;
    private int _timer;
    private int _motorIndexes;
    private int _idleIndexes;
    private int _spinIndexes;
    private int _searchIndexes;
    private int _restoreSteps;
    private int _idOffset;
    private int _remaining;
    private int _searchBytes;
    private int _writeRun;
    private int _writeGapBytes;
    private ushort _crc;
    private Phase _phase;

    public int ClockFrequency => _clock;
    public ulong HalfClocks => _writer.HalfClocks;
    public long StepPulses { get; private set; }
    public long DataBytesRead { get; private set; }
    public long DataBytesWritten { get; private set; }
    public bool Irq => _irq;
    public bool Drq => _drq;
    public bool Nmi => _irq || _drq;
    public bool Motor => _motor;
    public bool Step => _step;
    public bool Direction => _direction;
    public bool WriteGate => _writer.Gate;
    public bool WriteData => _writer.WriteData;
    public bool Active => _phase != Phase.Idle;
    private bool CommandWrites => (_command & C.CommandGroupMask) == C.WriteSectorCommand
        || (_command & C.CommandMask) == C.WriteTrackCommand;
    public bool Writing => Active && CommandWrites;
    public bool Reading => Active && !_typeOne && !CommandWrites;
    public bool DoubleDensity
    {
        get => _reader.DoubleDensity;
        set { _reader.DoubleDensity = value; _writer.DoubleDensity = value; }
    }
    public event Action<ulong, bool>? WriteDataChanged;
    public event Action? DriveSignalsChanged;
    private int ByteClocks => (DoubleDensity ? C.MfmCellClocks : C.FmCellClocks) * C.CellsPerWord;
    private int CellClocks => DoubleDensity ? C.MfmCellClocks : C.FmCellClocks;

    public Wd1770(Wd1770Model model = Wd1770Model.Wd1770, int clockFrequency = C.SiliconClock)
    {
        if (!Enum.IsDefined(model)) throw new ArgumentOutOfRangeException(nameof(model));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        _model = model;
        _clock = clockFrequency;
        _stepMilliseconds = model == Wd1770Model.Wd1772 ? C.Wd1772StepMilliseconds : C.StepMilliseconds;
        _reader.ByteReceived += ReceiveByte;
        _writer.WriteDataChanged += (time, high) => WriteDataChanged?.Invoke(time, high);
        Reset();
    }

    public void Reset()
    {
        _command = _track = _data = _errors = 0;
        _sector = C.InitialSector;
        _phase = Phase.Idle;
        _timer = 0;
        _drq = _irq = _forceIndex = _forceImmediate = _step = _direction = false;
        _motor = _spinComplete = _verify = _writeLatchFull = false;
        _typeOne = true;
        _motorIndexes = _idleIndexes = _spinIndexes = _searchIndexes = 0;
        _reader.Reset();
        _writer.Reset();
        ResetBus();
        DriveSignalsChanged?.Invoke();
    }

    /// <summary>Niveaux normalisés actifs des entrées du lecteur ; CLK est avancé par Tick/Advance.</summary>
    public void SetReadData(bool active) => _readData = active;
    public void SetTrackZero(bool active) => _trackZero = active;
    public void SetWriteProtected(bool active) => _writeProtected = active;
    public void SetReset(bool active)
    {
        if (active && !_reset) Reset();
        _reset = active;
    }
    public void SetIndex(bool active)
    {
        var rising = active && !_index;
        _index = active;
        if (!rising || _reset) return;
        if (_forceIndex) _irq = true;
        if (_motor && ++_motorIndexes >= C.SpinUpIndexes) _spinComplete = true;
        if (_phase == Phase.Idle)
        {
            if (_motor && ++_idleIndexes >= C.MotorOffIndexes)
            {
                _motor = false;
                DriveSignalsChanged?.Invoke();
            }
            return;
        }
        if (_phase == Phase.SpinUp)
        {
            if (++_spinIndexes == C.SpinUpIndexes) Dispatch();
            return;
        }
        if (_phase == Phase.WaitReadTrack) { _reader.Reset(readTrack: true); _phase = Phase.ReadTrack; return; }
        if (_phase == Phase.ReadTrack) { Complete(); return; }
        if (_phase == Phase.WaitWriteTrack)
        {
            _writer.SetGate(true);
            _phase = Phase.WriteTrack;
            QueueWriteWord();
            DriveSignalsChanged?.Invoke();
            return;
        }
        if (_phase == Phase.WriteTrack) { Complete(); return; }
        if (_phase is Phase.SearchId or Phase.ReadId or Phase.SearchData)
            if (++_searchIndexes >= (_verify ? C.VerifyIndexes : C.SearchIndexes)) Complete(C.NotFound);
    }

    public void Tick()
    {
        _writer.Tick();
        if (_reset) return;
        if (_phase == Phase.WriteTrack && _formatCrc && !_formatCrcRequested
            && _writer.RemainingCells <= C.CellsPerWord)
        {
            _formatCrcRequested = true;
            RequestWriteData();
        }
        if (_timer > 0 && --_timer == 0) TimerFinished();
        if (_phase is Phase.SearchId or Phase.ReadId or Phase.SearchData or Phase.ReadData or Phase.ReadDataCrc or Phase.ReadTrack)
            _reader.Tick(_readData);
        if (_timer == 0 && _writer.CanLoad && _phase is Phase.WriteLead or Phase.WriteMark or Phase.WriteData
            or Phase.WriteCrc or Phase.WriteTail or Phase.WriteFinish or Phase.WriteTrack)
            QueueWriteWord();
    }
    public void Advance(int clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_phase == Phase.Idle) { _writer.AdvanceIdle(clocks); return; }
        for (var clock = 0; clock < clocks; clock++) Tick();
    }
    private int Milliseconds(int value) => checked((int)((long)C.SiliconClock * value / C.MillisecondsPerSecond));
    private void Complete(byte errors = 0)
    {
        _errors |= errors;
        _phase = Phase.Idle;
        _timer = 0;
        _step = _drq = false;
        _irq = true;
        _idleIndexes = 0;
        _writer.SetGate(false);
        DriveSignalsChanged?.Invoke();
    }
}
