using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Interfaces;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

/// <summary>6551 : registres et broches, sans transport hôte. Les accès Read/Write sont déjà validés par CS/phi2 par la carte.</summary>
public sealed partial class Mos6551 : IMos6551
{
    private readonly int _ticksPerSecond;
    private readonly int _crystalClock;
    private readonly byte _resetCommand;
    private byte _command;
    private byte _control;
    private byte _status;
    private byte _receiveData;
    private byte _transmitData;
    private byte _interrupts;
    private bool _cts = true;
    private bool _dsr = true;
    private bool _dcd = true;
    private bool _rxd = true;
    private bool _txd = true;
    private bool _resetHeld;
    private bool _enabled;
    private bool _receiveInterrupt;
    private bool _transmitInterrupt;
    private bool _echo;
    private bool _break;
    private int _dataBits;
    private int _parity;
    private int _stopTicks;

    /// <param name="crystalClock">Quartz simulé ; zéro permet uniquement les fronts XTAL1 explicites.</param>
    public Mos6551(int ticksPerSecond, int crystalClock = C.CrystalClockHz, Mos6551Variant variant = Mos6551Variant.Mos6551)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegative(crystalClock);
        if (!Enum.IsDefined(variant)) throw new ArgumentOutOfRangeException(nameof(variant));
        _ticksPerSecond = ticksPerSecond;
        _crystalClock = crystalClock;
        _resetCommand = variant == Mos6551Variant.Mos6551 ? C.MosResetCommand : (byte)0;
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? TransmitChanged { get; set; }
    public Action<bool>? ReceiveClockChanged { get; set; }
    public bool Irq => _interrupts != 0;
    public bool Txd => _txd;
    /// <summary>Niveau physique de DTR, actif bas.</summary>
    public bool Dtr => !_enabled;
    /// <summary>Niveau physique de RTS, actif bas.</summary>
    public bool Rts { get; private set; }

    public byte Read(int register)
    {
        switch ((Mos6551Register)(register & C.RegisterMask))
        {
            case Mos6551Register.Data:
                _status &= unchecked((byte)~C.ReceiveStatusMask);
                return _receiveData;
            case Mos6551Register.Status:
                var value = (byte)(_status | (Irq ? C.Interrupt : 0));
                if (_cts) value &= unchecked((byte)~C.TransmitEmpty);
                SetInterrupts(0);
                RefreshModem(); // Un second changement masqué devient une nouvelle cause après acquittement.
                return value;
            case Mos6551Register.Command: return _command;
            default: return _control;
        }
    }

    public void Write(int register, byte value)
    {
        if (_resetHeld) return;
        switch ((Mos6551Register)(register & C.RegisterMask))
        {
            case Mos6551Register.Data:
                _transmitData = value;
                _status &= unchecked((byte)~C.TransmitEmpty);
                break;
            case Mos6551Register.Status:
                _status &= unchecked((byte)~C.Overrun);
                SetInterrupts((byte)(_interrupts & ~(C.CarrierAbsent | C.DataSetAbsent)));
                WriteCommand((byte)((_command & C.ProgramResetPreserve) | _resetCommand));
                break;
            case Mos6551Register.Command: WriteCommand(value); break;
            case Mos6551Register.Control:
                _control = value;
                DecodeControl();
                break;
        }
    }

    private void DecodeControl()
    {
        _dataBits = C.MaximumDataBits - ((_control >> C.WordLengthShift) & C.WordLengthMask);
        _stopTicks = (_control & C.ExtraStop) == 0 || _dataBits == C.MaximumDataBits && _parity != 0
            ? C.Oversampling : _dataBits == C.MinimumDataBits && _parity == 0
                ? C.OneAndHalfStopTicks : C.Oversampling * C.ClockEdgesPerPeriod;
        _divider = C.BaudDivisors[_control & C.BaudMask];
        _clockPeriod = (long)_ticksPerSecond * _divider;
        _internalReceiveClock = (_control & C.InternalReceiveClock) != 0;
        if (!_internalReceiveClock) SetReceiveClockOutput(true);
    }

    private void WriteCommand(byte value)
    {
        var wasEcho = _echo;
        var wasBreak = _break;
        _command = value;
        _enabled = (_command & C.Enable) != 0;
        _parity = (_command & C.ParityEnable) == 0 ? 0 : _command & C.ParityMask;
        _echo = (_command & C.Echo) != 0;
        if (_echo && !wasEcho)
        {
            _echoHistory = C.EchoInitialHistory;
            _echoDelayedRxd = true;
        }
        _break = (_command & C.TransmitControlMask) == C.TransmitBreak;
        _breakPending |= _enabled && _break && !wasBreak;
        _receiveInterrupt = _enabled && (_command & C.DisableReceiveInterrupt) == 0;
        _transmitInterrupt = _enabled && (_command & C.TransmitControlMask) == C.TransmitInterrupt;
        Rts = (_command & (C.TransmitControlMask | C.Echo)) == 0;
        DecodeControl();
        if (!_enabled)
        {
            SetTransmit(true);
            _transmitOutput = Mos6551TransmitOutput.Mark;
            _breakPending = false;
            SetInterrupts((byte)(_interrupts & ~(C.CarrierAbsent | C.DataSetAbsent)));
        }
        if (wasEcho && !_echo) SetTransmit(true);
        RefreshModem();
    }

    private void SetInterrupts(byte pending)
    {
        var before = Irq;
        _interrupts = pending;
        if (before != Irq) InterruptChanged?.Invoke(Irq);
    }

    private void RaiseInterrupt(byte source) => SetInterrupts((byte)(_interrupts | source));

    private void RefreshModem()
    {
        Observe(C.CarrierAbsent, _dcd);
        Observe(C.DataSetAbsent, _dsr);
        void Observe(byte mask, bool high)
        {
            if (_enabled && (_interrupts & mask) != 0) return;
            var previous = (_status & mask) != 0;
            if (high) _status |= mask;
            else _status &= unchecked((byte)~mask);
            if (_enabled && previous != high) RaiseInterrupt(mask);
        }
    }

    public void SetRxd(bool high) => _rxd = high;
    public void SetCts(bool high)
    {
        _cts = high;
        if (!high) return;
        if (_transmitOutput == Mos6551TransmitOutput.Data) _transmitOutput = Mos6551TransmitOutput.Mark;
        SetTransmit(true);
        if (_echo) _echoAwaitStart = true;
    }
    public void SetDsr(bool high) { _dsr = high; RefreshModem(); }
    public void SetDcd(bool high)
    {
        _dcd = high;
        if (high) { _receiveStep = C.IdleReceive; _receiveTicks = 0; }
        RefreshModem();
    }

    public void Reset()
    {
        _resetHeld = false;
        _command = _resetCommand;
        _control = _receiveData = _transmitData = 0;
        _status = (byte)(C.TransmitEmpty | (_dcd ? C.CarrierAbsent : 0) | (_dsr ? C.DataSetAbsent : 0));
        _clockPhase = 0;
        _externalTransmitEdges = 0;
        _externalTransmitClock = _externalReceiveClock = _internalClockHigh = false;
        _transmitState = Mos6551TransmitState.Start;
        _transmitOutput = Mos6551TransmitOutput.Mark;
        _transmitTicks = _transmitStep = 0;
        _receiveStep = C.IdleReceive;
        _receiveTicks = _receiveValue = 0;
        _receiveWaitMark = _echoAwaitStart = _breakPending = false;
        _pendingTransmitInterrupt = false;
        _echoDelayedRxd = true;
        _echoHistory = C.EchoInitialHistory;
        SetInterrupts(0);
        WriteCommand(_resetCommand);
        SetTransmit(true);
    }

    /// <summary>Niveau physique RES, actif bas ; un reset maintenu interdit les transferts.</summary>
    public void SetReset(bool high)
    {
        if (!high && !_resetHeld) Reset();
        _resetHeld = !high;
    }

    private void SetTransmit(bool high)
    {
        if (_txd == high) return;
        _txd = high;
        TransmitChanged?.Invoke(high);
    }
}
