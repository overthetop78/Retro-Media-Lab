using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>NEC 765 command processor and serial channels. No medium, rotation or physical head position is owned here.</summary>
public sealed partial class Upd765 : IUpd765
{
    private readonly Func<int, byte> _statusPins;
    private readonly Upd765DriveControl _drives;
    private readonly Upd765Model _model;
    private readonly int _clock;
    private readonly long _registerTicks;
    private readonly Upd765ReadChannel _reader = new();
    private readonly Upd765WriteChannel _writer = new();
    private readonly byte[] _parameters = new byte[C.CommandBytes];
    private readonly byte[] _results = new byte[C.ResultBytes];
    private readonly byte[] _id = new byte[C.IdFieldBytes];
    private readonly byte[] _formatId = new byte[C.IdentifierBytes];
    private readonly long[] _unload = new long[C.DriveCount];
    private readonly bool[] _loaded = new bool[C.DriveCount];
    private readonly long[] _readBytes = new long[C.DriveCount];
    private readonly long[] _writtenBytes = new long[C.DriveCount];
    private Upd765Phase _phase;
    private Upd765SerialPhase _serialPhase;
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
    private bool _mfmOutput;
    private bool _request;
    private bool _resultInterrupt;
    private bool _irq;
    private bool _tcPin;
    private bool _terminalCount;
    private bool _stickyDrq;
    private bool _dmaSuppressed;
    private bool _dmaAcknowledged;
    private byte _dataLatch;
    private byte _hostData;
    private bool _hostDataReady;
    private int _offset;
    private int _hostOffset;
    private int _length;
    private int _hostLength;
    private bool _dataError;
    private bool _controlMark;
    private bool _scanEqual;
    private bool _scanSatisfied;
    private int _trackFields;
    private byte _trackStatus1;
    private byte _trackStatus2;
    private uint _generation;

    public Upd765(Func<int, byte> statusPins, Action<int, int> step,
        int clockFrequency = C.ReferenceClockHz, Upd765Model model = Upd765Model.Upd765A)
    {
        ArgumentNullException.ThrowIfNull(statusPins);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        if (!Enum.IsDefined(model)) throw new ArgumentOutOfRangeException(nameof(model));
        _statusPins = statusPins;
        _clock = clockFrequency;
        _model = model;
        _registerTicks = C.RegisterClocks;
        _drives = new(statusPins, step, clockFrequency, clockFrequency,
            model == Upd765Model.Upd72065B ? C.CmosRecalibrationSteps : C.RecalibrationSteps,
            model == Upd765Model.Upd72065B ? C.CmosUnitResponseClocks : C.UnitResponseClocks);
        _drives.DriveStatusCompleted = status => Result([status]);
        _drives.OutputsChanged = () => DriveSignalsChanged?.Invoke();
        _drives.InterruptChanged = _ => UpdateInterrupt();
        _reader.ByteReceived += ReceiveSerialByte;
        _writer.PreviewNextWord = PreviewNextWriteWord;
        _writer.PrecompensationChanged += (time, value) => PrecompensationChanged?.Invoke(time, value);
        _writer.WordRequested += RequestWriteWord;
        _writer.WordCompleted += CompleteWriteWord;
        _writer.WriteDataChanged += (time, active) => WriteDataChanged?.Invoke(time, active);
        _writer.WriteEnableChanged += (time, active) => WriteEnableChanged?.Invoke(time, active);
        Reset();
    }

    public Action? DriveSignalsChanged { get; set; }
    public bool SeekMode => _drives.SeekMode;
    public bool Step => _drives.Step;
    public bool Direction => _drives.Direction;
    public bool FaultResetStep => SeekMode ? Step : FaultReset;
    public bool LowCurrentDirection => SeekMode ? Direction : LowCurrent;
    public Action<bool>? FaultResetChanged { get; set; }
    public bool FaultReset => _faultReset;
    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? DataRequestChanged { get; set; }
    public Action<ulong, bool>? WriteDataChanged { get; set; }
    public Action<ulong, bool>? WriteEnableChanged { get; set; }
    public bool Irq => _irq;
    public bool Drq => !_resetAsserted && !_drives.NonDma && (_stickyDrq || _request && !_dmaSuppressed);
    public int Unit => _drives.SignalsBusy ? _drives.SelectedUnit : _unit;
    public int Head => _drives.SignalsBusy ? _drives.SelectedHead : _head;
    public bool Mfm => _mfmOutput;
    public bool Sync => !_resetAsserted && Active && !Formatting && HeadLoaded && !_writer.Gate
        && !(_serialPhase == Upd765SerialPhase.SearchId && _indexSkipClocks > 0)
        && _syncGapCells == 0 && _serialPhase is Upd765SerialPhase.SearchId or Upd765SerialPhase.Id
            or Upd765SerialPhase.SearchData or Upd765SerialPhase.Data or Upd765SerialPhase.Crc;
    public bool LowCurrent => Active && _cylinder >= C.LowCurrentCylinder;
    public bool HeadLoaded => _loaded[_unit];
    public bool WriteEnable => _writer.WriteEnable;
    public bool WriteData => _writer.WriteData;
    public bool Active => _phase is Upd765Phase.DrivePrepare or Upd765Phase.HeadLoad or Upd765Phase.SearchId or Upd765Phase.Reading
        or Upd765Phase.Writing or Upd765Phase.DataEnd or Upd765Phase.FormatLead or Upd765Phase.FormatGap or Upd765Phase.FormatTail;
    public bool Writing => Active && WriteCommand;
    public bool Reading => Active && !WriteCommand;
    private bool WriteCommand => _command is Upd765Command.WriteData or Upd765Command.WriteDeleted or Upd765Command.FormatTrack;
    private bool Formatting => _command == Upd765Command.FormatTrack;
    private bool Scanning => _command is Upd765Command.ScanEqual or Upd765Command.ScanLowEqual or Upd765Command.ScanHighEqual;
    public long ReadBytes(int unit) => _readBytes[CheckedUnit(unit)];
    public long WrittenBytes(int unit) => _writtenBytes[CheckedUnit(unit)];
    public long ByteTicks => Math.Max(1, (long)_clock * C.BitsPerByte * (Mfm ? 1 : 2) / (int)_rate);
    public long NextEventTicks
    {
        get
        {
            if (PowerClockStopped) return _powerState == Upd765PowerState.Standby ? long.MaxValue : _powerDelay;
            var next = Math.Min(_delay > 0 ? _delay : long.MaxValue, _drives.NextEventTicks);
            for (var unit = 0; unit < C.DriveCount; unit++)
                if (_unload[unit] > 0) next = Math.Min(next, _unload[unit]);
            next = Math.Min(next, _writer.NextEventClocks);
            if (_indexSkipClocks > 0) next = Math.Min(next, _indexSkipClocks);
            return _powerState == Upd765PowerState.EnteringStandby ? Math.Min(next, _powerDelay) : next;
        }
    }

    /// <summary>Board data-rate description; the actual serial clocks remain WCLK and WINDOW inputs.</summary>
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
        _generation++;
        ResetBus();
        ResetPower();
        _phase = Upd765Phase.Idle;
        _serialPhase = Upd765SerialPhase.Idle;
        _writePhase = Upd765WritePhase.Idle;
        _delay = 0;
        _unit = _head = 0;
        _mfmOutput = _drivePreparationWaiting = false;
        _parameterCount = _expected = _resultCount = _resultOffset = 0;
        _terminalCount = _tcPin = _resultInterrupt = _request = _stickyDrq = _dmaSuppressed = _dmaAcknowledged = false;
        _hostDataReady = false;
        _offset = _hostOffset = _length = _hostLength = 0;
        _writeGapCells = _syncGapCells = 0;
        _indexSkipClocks = 0;
        _reader.Reset();
        _writer.Reset();
        SetFaultReset(false);
        Array.Clear(_loaded);
        Array.Clear(_unload);
        _drives.Reset();
        DataRequestChanged?.Invoke(false);
        UpdateInterrupt();
    }

    public byte ReadMainStatus()
    {
        if (_resetAsserted) return 0;
        var status = _drives.BusyMask;
        if (_phase != Upd765Phase.Idle) status |= C.ControllerBusy;
        if (_phase == Upd765Phase.Idle || _phase is Upd765Phase.Command or Upd765Phase.Result && _delay == 0)
            status |= C.RequestForMaster;
        if (_phase == Upd765Phase.Result) status |= C.DataToHost;
        if (Active && _drives.NonDma) status |= C.NonDmaExecution;
        if (_phase == Upd765Phase.Reading) status |= C.DataToHost;
        if (_phase is Upd765Phase.Reading or Upd765Phase.Writing && _request && _drives.NonDma)
            status |= C.RequestForMaster;
        return status;
    }

    public byte ReadFifo()
    {
        if (_resetAsserted || PowerClockStopped) return C.OpenBus;
        if (_phase == Upd765Phase.Result && _delay == 0)
        {
            _resultInterrupt = false;
            var result = _results[_resultOffset++];
            if (_resultOffset == _resultCount)
            {
                _phase = Upd765Phase.Idle;
                _drives.PollingEnabled = true;
            }
            else _delay = _registerTicks;
            UpdateInterrupt();
            return result;
        }
        return _drives.NonDma ? ConsumeRead() : C.OpenBus;
    }

    public void WriteFifo(byte value)
    {
        if (_resetAsserted || PowerClockStopped) return;
        if (_phase == Upd765Phase.Writing && _drives.NonDma) { ConsumeWrite(value); return; }
        if (_phase is not (Upd765Phase.Idle or Upd765Phase.Command) || _delay != 0) return;
        if (_phase == Upd765Phase.Idle)
        {
            var generation = _generation;
            _drives.PollingEnabled = false;
            if (_generation != generation || _resetAsserted) return;
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

    public byte ReadDma()
    {
        if (_resetAsserted || PowerClockStopped || _drives.NonDma) return C.OpenBus;
        AcknowledgeDma();
        return _phase == Upd765Phase.Result ? ReadFifo() : ConsumeRead();
    }

    public void WriteDma(byte value)
    {
        if (_resetAsserted || PowerClockStopped || _drives.NonDma) return;
        AcknowledgeDma();
        ConsumeWrite(value);
    }

    public void SetTerminalCount(bool high)
    {
        if (_resetAsserted || PowerClockStopped) return;
        if (high && !_tcPin && Active && !Formatting && _command != Upd765Command.ReadId) _terminalCount = true;
        _tcPin = high;
    }

    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (_resetAsserted) return;
        var remaining = (long)ticks;
        while (remaining > 0)
        {
            if (PowerClockStopped)
            {
                if (_powerState == Upd765PowerState.Standby) return;
                var wake = Math.Min(remaining, _powerDelay);
                _powerDelay -= wake;
                remaining -= wake;
                if (_powerDelay == 0) _powerState = Upd765PowerState.Active;
                continue;
            }
            var powerState = _powerState;
            var powerDelay = _powerDelay;
            var generation = _generation;
            if (Active && !_drivePreparationWaiting)
            {
                var pins = _statusPins(_unit);
                if (_generation != generation || _resetAsserted) return;
                if ((pins & C.ReadyPin) == 0) FinishData(status: C.Abnormal | C.NotReady);
                else if (Writing && (pins & C.WriteProtectedPin) != 0) FinishData(C.WriteProtected);
                else if (_phase != Upd765Phase.DrivePrepare && (pins & C.FaultPin) != 0)
                    FinishData(status: C.Abnormal | C.EquipmentCheck);
                if (_generation != generation || _resetAsserted) return;
            }
            var delay = _delay;
            var phase = _phase;
            var indexSkip = _indexSkipClocks;
            var elapsed = Math.Min(remaining, NextEventTicks);
            _writer.Advance((int)elapsed);
            if (_generation != generation || _resetAsserted) return;
            _drives.Advance((int)elapsed);
            if (_generation != generation || _resetAsserted) return;
            for (var unit = 0; unit < C.DriveCount; unit++)
            {
                if (_unload[unit] <= 0 || Active && unit == _unit) continue;
                _unload[unit] = Math.Max(0, _unload[unit] - elapsed);
                if (_unload[unit] == 0) _loaded[unit] = false;
            }
            remaining -= elapsed;
            if (indexSkip > 0 && _indexSkipClocks == indexSkip)
            {
                _indexSkipClocks = Math.Max(0, indexSkip - elapsed);
                if (_indexSkipClocks == 0 && _serialPhase == Upd765SerialPhase.SearchId) _reader.Reset();
            }
            if (powerState == Upd765PowerState.EnteringStandby && _powerState == powerState && _powerDelay == powerDelay)
            {
                _powerDelay -= elapsed;
                if (_powerDelay == 0) _powerState = Upd765PowerState.Standby;
            }
            if (_delay <= 0 || _delay != delay || _phase != phase) continue;
            _delay -= elapsed;
            if (_delay == 0)
            {
                if (_phase == Upd765Phase.Command && _parameterCount == _expected) ExecuteCommand();
                else if (_phase == Upd765Phase.DrivePrepare) CompleteDrivePreparation();
                else if (_phase == Upd765Phase.HeadLoad)
                {
                    _loaded[_unit] = true;
                    SearchSector();
                }
            }
        }
    }

    private void Result(ReadOnlySpan<byte> bytes, bool interrupt = false)
    {
        bytes.CopyTo(_results);
        _resultCount = bytes.Length;
        _resultOffset = 0;
        _phase = Upd765Phase.Result;
        _serialPhase = Upd765SerialPhase.Idle;
        _delay = 0;
        _resultInterrupt = interrupt;
        SetRequest(false);
        UpdateInterrupt();
    }

    private void SetRequest(bool request)
    {
        var previous = Drq;
        _request = request;
        if (request) _dmaSuppressed = _dmaAcknowledged;
        if (previous != Drq) DataRequestChanged?.Invoke(Drq);
        UpdateInterrupt();
    }

    private void AcknowledgeDma()
    {
        var previous = Drq;
        _dmaSuppressed = true;
        _stickyDrq = false;
        if (previous != Drq) DataRequestChanged?.Invoke(Drq);
    }

    private void UpdateInterrupt()
    {
        var irq = !_resetAsserted && (_resultInterrupt || _drives.Irq || _request && _drives.NonDma);
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
