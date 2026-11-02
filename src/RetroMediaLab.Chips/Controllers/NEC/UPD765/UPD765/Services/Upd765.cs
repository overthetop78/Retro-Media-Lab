using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>NEC command/result bus and clocked sector transfers; images and drive pins are borrowed.</summary>
public sealed partial class Upd765
{
    private readonly Func<int, IDiskImage?> _disk;
    private readonly Func<int, byte> _statusPins;
    private readonly Func<int, int> _physicalCylinder;
    private readonly Upd765DriveControl _drives;
    private readonly Upd765Model _model;
    private readonly int _clock;
    private readonly long _registerTicks;
    private readonly byte[] _parameters = new byte[C.CommandBytes];
    private readonly byte[] _results = new byte[C.ResultBytes];
    private readonly long[] _rotation = new long[C.DriveCount];
    private readonly long[] _unload = new long[C.DriveCount];
    private readonly bool[] _loaded = new bool[C.DriveCount];
    private readonly long[] _readBytes = new long[C.DriveCount];
    private readonly long[] _writtenBytes = new long[C.DriveCount];
    private Upd765Phase _phase;
    private Upd765Command _command;
    private Upd765DataRate _rate = Upd765DataRate.Kbps500;
    private int _parameterCount;
    private int _expected;
    private int _resultCount;
    private int _resultOffset;
    private int _unit;
    private int _head;
    private byte _cylinder;
    private byte _logicalHead;
    private byte _record;
    private byte _size;
    private long _delay;
    private bool _request;
    private bool _resultInterrupt;
    private bool _irq;
    private bool _tcPin;
    private bool _terminalCount;
    private IDiskImage? _image;
    private byte[] _buffer = [];
    private int _offset;
    private int _length;
    private bool _dataError;
    private bool _controlMark;
    private bool _scanEqual;
    private bool _scanSatisfied;
    private int _trackFields;
    private byte _trackStatus1;
    private byte _trackStatus2;
    private readonly List<DiskSector> _formatSectors = [];
    private int _formatCylinder;
    private long _recordIndex;
    private long[] _trackRecords = [];
    private DiskSector[] _trackSectors = [];
    private int _recordSlot;
    private int _trackCylinder = -1;
    private int _trackHead = -1;

    public Upd765(Func<int, IDiskImage?> disk, Func<int, byte> statusPins,
        Action<int, int> step, Func<int, int> physicalCylinder,
        int boardClockHz = C.ReferenceClockHz, int controllerClockHz = C.ReferenceClockHz,
        Upd765Model model = Upd765Model.Upd765A)
    {
        ArgumentNullException.ThrowIfNull(disk);
        ArgumentNullException.ThrowIfNull(statusPins);
        ArgumentNullException.ThrowIfNull(physicalCylinder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boardClockHz);
        if (!Enum.IsDefined(model)) throw new ArgumentOutOfRangeException(nameof(model));
        _disk = disk;
        _statusPins = statusPins;
        _physicalCylinder = physicalCylinder;
        _clock = boardClockHz;
        _model = model;
        _registerTicks = Math.Max(1, (long)boardClockHz * C.RegisterMicroseconds / C.MicrosecondsPerSecond);
        _drives = new(statusPins, step, boardClockHz, controllerClockHz);
        _drives.InterruptChanged = _ => UpdateInterrupt();
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? DataRequestChanged { get; set; }
    public bool Irq => _irq;
    public bool Drq => _request && !_drives.NonDma;
    public int Unit => _unit;
    public long NextEventTicks => Math.Min(_delay > 0 ? _delay : long.MaxValue, _drives.NextEventTicks);
    public bool Active => _phase is Upd765Phase.HeadLoad or Upd765Phase.SearchId
        or Upd765Phase.Reading or Upd765Phase.Writing or Upd765Phase.DataEnd
        or Upd765Phase.FormatLead or Upd765Phase.FormatGap or Upd765Phase.FormatTail;
    public bool Writing => Active && WriteCommand;
    private bool WriteCommand => _command is Upd765Command.WriteData or Upd765Command.WriteDeleted or Upd765Command.FormatTrack;
    private bool Formatting => _command == Upd765Command.FormatTrack;
    public bool Reading => Active && (_command is Upd765Command.ReadData or Upd765Command.ReadDeleted
        or Upd765Command.ReadId or Upd765Command.ReadTrack || Scanning);
    private bool Scanning => _command is Upd765Command.ScanEqual or Upd765Command.ScanLowEqual
        or Upd765Command.ScanHighEqual;
    public long ReadBytes(int unit) => _readBytes[CheckedUnit(unit)];
    public long WrittenBytes(int unit) => _writtenBytes[CheckedUnit(unit)];
    public long ByteTicks => Math.Max(1, (long)_clock * C.BitsPerByte
        * ((_parameters[0] & C.Mfm) == 0 ? 2 : 1) / (int)_rate);
    private long RevolutionTicks => Math.Max(1, _clock / C.RevolutionsPerSecond);

    public Upd765DataRate DataRate
    {
        get => _rate;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _rate = value;
        }
    }

    public void Reset()
    {
        _image?.Mechanics.SetActivity(false, false);
        _image = null;
        SetRequest(false);
        _phase = Upd765Phase.Idle;
        _delay = 0;
        _parameterCount = _expected = _resultCount = _resultOffset = 0;
        _terminalCount = _tcPin = _resultInterrupt = false;
        _buffer = [];
        Array.Clear(_loaded);
        Array.Clear(_unload);
        _trackRecords = [];
        _trackSectors = [];
        _formatSectors.Clear();
        _trackCylinder = _trackHead = -1;
        _drives.Reset();
        UpdateInterrupt();
    }

    public byte ReadMainStatus()
    {
        var status = _drives.BusyMask;
        if (_phase != Upd765Phase.Idle) status |= C.ControllerBusy;
        if (_phase == Upd765Phase.Idle || _phase is Upd765Phase.Command or Upd765Phase.Result && _delay == 0)
            status |= C.RequestForMaster;
        if (_phase == Upd765Phase.Result) status |= C.DataToHost;
        if (Active && _drives.NonDma) status |= C.NonDmaExecution;
        if (_phase == Upd765Phase.Reading)
        {
            status |= C.DataToHost;
            if (_request && _drives.NonDma) status |= C.RequestForMaster;
        }
        if (_phase == Upd765Phase.Writing && _request && _drives.NonDma) status |= C.RequestForMaster;
        return status;
    }

    public byte ReadFifo()
    {
        if (_phase == Upd765Phase.Result && _delay == 0)
        {
            _resultInterrupt = false;
            var result = _results[_resultOffset++];
            if (_resultOffset == _resultCount) { _phase = Upd765Phase.Idle; _delay = 0; }
            else _delay = _registerTicks;
            UpdateInterrupt();
            return result;
        }
        return _drives.NonDma ? ConsumeRead() : C.OpenBus;
    }

    public void WriteFifo(byte value)
    {
        if (_phase == Upd765Phase.Writing && _drives.NonDma) { ConsumeWrite(value); return; }
        if (_phase is not (Upd765Phase.Idle or Upd765Phase.Command) || _delay != 0) return;
        if (_phase == Upd765Phase.Idle)
        {
            _parameterCount = 0;
            _command = (Upd765Command)(value & C.CommandMask);
            _expected = CommandLength(_command);
            _resultInterrupt = false;
        }
        _parameters[_parameterCount++] = value;
        _phase = Upd765Phase.Command;
        _delay = _registerTicks;
        UpdateInterrupt();
    }

    public byte ReadDma() => !_drives.NonDma ? ConsumeRead() : C.OpenBus;
    public void WriteDma(byte value) { if (!_drives.NonDma) ConsumeWrite(value); }

    public void SetTerminalCount(bool high)
    {
        if (high && !_tcPin && Active && !Formatting)
        {
            _terminalCount = true;
            if (_phase is Upd765Phase.Reading or Upd765Phase.Writing && !_request && _offset > 0)
                EndHostTransfer();
        }
        _tcPin = high;
    }

    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (Active && (!Ready() || !ReferenceEquals(_image, _disk(_unit))))
        {
            FinishData(status: C.Abnormal | C.NotReady);
        }
        var remaining = (long)ticks;
        while (remaining > 0)
        {
            var elapsed = _delay > 0 ? Math.Min(remaining, _delay) : remaining;
            _drives.Advance((int)elapsed);
            for (var unit = 0; unit < C.DriveCount; unit++)
            {
                if ((_statusPins(unit) & C.ReadyPin) != 0)
                    _rotation[unit] = (_rotation[unit] + elapsed) % RevolutionTicks;
                if (_unload[unit] <= 0) continue;
                _unload[unit] = Math.Max(0, _unload[unit] - elapsed);
                if (_unload[unit] == 0) _loaded[unit] = false;
            }
            remaining -= elapsed;
            if (_delay <= 0) continue;
            _delay -= elapsed;
            if (_delay == 0) ExecuteEvent();
        }
    }

    private void ExecuteEvent()
    {
        switch (_phase)
        {
            case Upd765Phase.Command:
                if (_parameterCount == _expected) ExecuteCommand();
                break;
            case Upd765Phase.HeadLoad:
                _loaded[_unit] = true;
                SearchSector();
                break;
            case Upd765Phase.SearchId: SectorFound(); break;
            case Upd765Phase.Reading:
            case Upd765Phase.Writing:
                if (_request) FinishData(C.Overrun);
                else { _delay = ByteTicks; SetRequest(true); }
                break;
            case Upd765Phase.DataEnd: FinishSector(); break;
            case Upd765Phase.FormatLead:
            case Upd765Phase.FormatGap: BeginFormatIdentifier(); break;
            case Upd765Phase.FormatTail: FinishData(); break;
        }
    }

    private void Result(ReadOnlySpan<byte> bytes, bool interrupt = false)
    {
        bytes.CopyTo(_results);
        _resultCount = bytes.Length;
        _resultOffset = 0;
        _phase = Upd765Phase.Result;
        _delay = 0;
        _resultInterrupt = interrupt;
        SetRequest(false);
        UpdateInterrupt();
    }

    private void SetRequest(bool request)
    {
        var previous = Drq;
        _request = request;
        if (previous != Drq) DataRequestChanged?.Invoke(Drq);
        UpdateInterrupt();
    }

    private void UpdateInterrupt()
    {
        var irq = _resultInterrupt || _drives.Irq || (_request && _drives.NonDma);
        if (_irq == irq) return;
        _irq = irq;
        InterruptChanged?.Invoke(irq);
    }

    private bool Ready() => (_statusPins(_unit) & C.ReadyPin) != 0;

    private static int CheckedUnit(int unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unit);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unit, C.DriveCount);
        return unit;
    }
}
