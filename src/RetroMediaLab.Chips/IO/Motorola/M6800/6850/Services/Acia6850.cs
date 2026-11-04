using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Enums;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
using C = RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Constants.Acia6850Constants;

namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

/// <summary>MC6850 : registres de bus, signaux modem et deux chemins serie a horloges independantes.</summary>
/// <remarks>La modulation, les supports et les fichiers appartiennent a la carte. Read/Write correspondent
/// a un acces de bus deja valide par CS/E. Les broches serie se cadencent explicitement, ou par Tick pour TxC.</remarks>
public sealed partial class Acia6850 : IAcia6850
{
    private readonly int _ticksPerSecond;
    private readonly int _serialClock;
    private byte _control;
    private byte _data;
    private byte _status;
    private bool _masterReset;
    private bool _firstMasterReset;
    private bool _dcdInput;
    private bool _dcdObserved;
    private bool _dcdPending;
    private bool _dcdStatusRead;
    private bool _ctsBlocked;
    private bool _overrunPending;
    private bool _receiveIrqEnabled;
    private bool _transmitIrqEnabled;
    private bool _break;
    private bool _irq;
    private int _divider;
    private int _dataBits;
    private bool _hasParity;
    private bool _oddParity;
    private int _stopBits;
    private int _baudRateOverride;
    private int _baudRate;
    private long _inputClockRate;
    private long _clockPhase;

    public Acia6850(int ticksPerSecond, int serialClock)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegative(serialClock);
        _ticksPerSecond = ticksPerSecond;
        _serialClock = serialClock;
        Reset();
    }

    /// <summary>Niveau physique de TXD : true au repos (mark), false pour start/space/break.</summary>
    public bool TransmitBit => _masterReset || (!_break && _transmitLine);
    /// <summary>Niveau physique de RTS : false est la demande active basse.</summary>
    public bool RtsHigh { get; private set; }
    public bool Irq => _irq;
    public int BaudRate => _baudRate;
    public int ClockDivider => _masterReset ? 0 : _divider;
    public Acia6850WordFormat WordFormat { get; private set; }
    public Acia6850TransmitControl TransmitControl { get; private set; }

    /// <summary>Adaptation de l'horloge par la carte ; zero suit le diviseur programme.</summary>
    public int BaudRateOverride
    {
        get => _baudRateOverride;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _baudRateOverride = value;
            UpdateClockRate();
        }
    }

    /// <summary>Initialisation du modele, et non une broche RESET inexistante sur le boitier.</summary>
    /// <remarks>Les entrees CTS/DCD restent des conditions externes. Le logiciel doit programmer le controle.</remarks>
    public void Reset()
    {
        _data = 0;
        _clockPhase = 0;
        _firstMasterReset = true;
        _control = C.MasterResetSelection;
        DecodeControl();
        ClearTransfers();
        RtsHigh = true;
    }

    public byte Read(bool data)
    {
        if (!data)
        {
            _dcdStatusRead = _dcdPending;
            var status = _status;
            if (_ctsBlocked) status |= C.ClearToSendBlocked;
            if (_dcdObserved || _dcdPending) status |= C.CarrierAbsent;
            if (_masterReset || _ctsBlocked) status &= unchecked((byte)~C.TransmitDataEmpty);
            if (_masterReset || _dcdObserved) status &= unchecked((byte)~C.ReceiveDataFull);
            if (_irq) status |= C.InterruptRequest;
            return status;
        }

        // The unread byte survives an overrun; only this read exposes the pending loss.
        if (_overrunPending)
        {
            _status |= C.Overrun | C.ReceiveDataFull;
            _overrunPending = false;
        }
        else
            _status &= unchecked((byte)~(C.Overrun | C.ReceiveDataFull));
        if (_dcdStatusRead)
        {
            _dcdPending = false;
            _dcdStatusRead = false;
        }
        UpdateIrq();
        return _data;
    }

    public void Write(bool data, byte value)
    {
        if (data)
        {
            if (_masterReset) return;
            _transmitData = value;
            _transmitPending = true;
            _status &= unchecked((byte)~C.TransmitDataEmpty);
        }
        else
        {
            _control = value;
            DecodeControl();
            if (_masterReset)
            {
                ClearTransfers();
                if (_firstMasterReset) RtsHigh = true;
                _firstMasterReset = false;
            }
        }
        UpdateIrq();
    }

    public void SetCts(bool blocked)
    {
        _ctsBlocked = blocked;
        UpdateIrq();
    }

    /// <summary>DCD est echantillonne par RxC ; changer cette entree ne cree pas une horloge.</summary>
    public void SetDcd(bool high) => _dcdInput = high;

    private void DecodeControl()
    {
        _divider = (_control & C.DividerMask) switch
        {
            0 => C.DirectClockDivider,
            1 => C.ClockDivider16,
            2 => C.ClockDivider64,
            _ => C.ResetClockDivider
        };
        _masterReset = _divider == C.ResetClockDivider;
        WordFormat = (Acia6850WordFormat)((_control >> C.WordFormatShift) & C.WordFormatMask);
        _dataBits = (int)WordFormat < C.FirstEightBitFormat ? C.SevenDataBits : C.EightDataBits;
        _hasParity = WordFormat is not (Acia6850WordFormat.EightNoParityOneStop or Acia6850WordFormat.EightNoParityTwoStops);
        _oddParity = ((int)WordFormat & C.LeastSignificantBit) != 0;
        _stopBits = WordFormat is Acia6850WordFormat.SevenEvenTwoStops or Acia6850WordFormat.SevenOddTwoStops
            or Acia6850WordFormat.EightNoParityTwoStops ? C.TwoStopBits : C.OneStopBit;
        TransmitControl = (Acia6850TransmitControl)((_control >> C.TransmitControlShift) & C.TransmitControlMask);
        _break = TransmitControl == Acia6850TransmitControl.SendBreak;
        RtsHigh = TransmitControl == Acia6850TransmitControl.ReleaseRequestToSend;
        _transmitIrqEnabled = TransmitControl == Acia6850TransmitControl.RequestToSendWithInterrupt;
        _receiveIrqEnabled = (_control & C.ReceiveInterruptEnable) != 0;
        UpdateClockRate();
    }

    private void UpdateClockRate()
    {
        _baudRate = _masterReset ? 0 : _baudRateOverride > C.FollowProgrammedBaudRate
            ? _baudRateOverride : _serialClock / _divider;
        _inputClockRate = _baudRateOverride > C.FollowProgrammedBaudRate && !_masterReset
            ? (long)_baudRateOverride * _divider : _serialClock;
    }

    private void ClearTransfers()
    {
        _status = C.TransmitDataEmpty;
        _dcdObserved = _dcdInput;
        _dcdPending = false;
        _dcdStatusRead = false;
        _overrunPending = false;
        CancelReceive();
        _receiveClockCounter = 0;
        _receiveWaitMark = false;
        _transmitState = Acia6850TransmitState.Idle;
        _transmitPending = false;
        _transmitClockCounter = 0;
        _transmitLine = true;
        _irq = false;
    }

    private void UpdateIrq() => _irq = !_masterReset
        && ((_receiveIrqEnabled && ((_status & C.ReceiveDataFull) != 0 || _overrunPending || _dcdPending))
            || (_transmitIrqEnabled && (_status & C.TransmitDataEmpty) != 0 && !_ctsBlocked));
}
