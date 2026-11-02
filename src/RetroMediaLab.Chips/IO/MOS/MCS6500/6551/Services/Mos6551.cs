using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

/// <summary>MOS6551 : registres et broches électriques, sans dépendance au transport hôte.
/// Le quartz produit l'horloge 16× ; RxC peut recevoir une horloge extérieure indépendante.</summary>
public sealed partial class Mos6551
:RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Interfaces.IMos6551{
    private readonly int _ticksPerSecond;
    private readonly int _crystalClock;
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
    private bool _externalClock;
    private bool _txd = true;
    private long _clockPhase;

    public Mos6551(int ticksPerSecond, int crystalClock = C.CrystalClockHz)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(crystalClock);
        _ticksPerSecond = ticksPerSecond;
        _crystalClock = crystalClock;
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? TransmitChanged { get; set; }
    public bool Irq => _interrupts != 0;
    public bool Txd => _txd;
    public bool Dtr => !Enabled;
    public bool Rts => (_command & (C.TransmitControlMask | C.Echo)) == 0;
    private bool Enabled => (_command & C.Enable) != 0;
    private bool HasParity => (_command & C.ParityEnable) != 0;
    private int DataBits => C.MaximumDataBits - ((_control >> C.WordLengthShift) & C.WordLengthMask);
    private int StopTicks => (_control & C.ExtraStop) == 0 || DataBits == C.MaximumDataBits && HasParity
        ? C.Oversampling : DataBits == C.MinimumDataBits && !HasParity ? C.Oversampling + C.HalfBit : C.Oversampling * 2;

    public byte Read(int register)
    {
        switch ((Mos6551Register)(register & C.RegisterMask))
        {
            case Mos6551Register.Data:
                _status &= unchecked((byte)~C.ReceiveStatusMask);
                return _receiveData;
            case Mos6551Register.Status:
                var status = (byte)(_status | (_dcd ? C.CarrierAbsent : 0) | (_dsr ? C.DataSetAbsent : 0)
                    | (Irq ? C.Interrupt : 0));
                if (_cts) status &= unchecked((byte)~C.TransmitEmpty);
                SetInterrupts(0);
                return status;
            case Mos6551Register.Command: return _command;
            default: return _control;
        }
    }

    public void Write(int register, byte value)
    {
        switch ((Mos6551Register)(register & C.RegisterMask))
        {
            case Mos6551Register.Data:
                _transmitData = value;
                _status &= unchecked((byte)~C.TransmitEmpty);
                break;
            case Mos6551Register.Status:
                // Le reset logiciel conserve le contrôle, la parité et les données.
                _status &= unchecked((byte)~C.Overrun);
                SetInterrupts((byte)(_interrupts & ~(C.CarrierAbsent | C.DataSetAbsent)));
                WriteCommand((byte)(_command & C.ProgramResetPreserve));
                break;
            case Mos6551Register.Command: WriteCommand(value); break;
            case Mos6551Register.Control:
                _control = value;
                _clockPhase = 0;
                break;
        }
    }

    private void WriteCommand(byte value)
    {
        _command = value;
        var pending = _interrupts;
        if (!Enabled) pending = 0;
        if ((_command & C.DisableReceiveInterrupt) != 0) pending &= unchecked((byte)~C.ReceiveFull);
        if ((_command & C.TransmitControlMask) != C.TransmitInterrupt) pending &= unchecked((byte)~C.TransmitEmpty);
        SetInterrupts(pending);
        if (!Enabled)
        {
            _receiveStep = C.IdleReceive;
            SetTransmit(true);
        }
    }

    private void SetInterrupts(byte pending)
    {
        var before = Irq;
        _interrupts = pending;
        if (before != Irq) InterruptChanged?.Invoke(Irq);
    }

    private void RaiseInterrupt(byte source)
    {
        if (Enabled) SetInterrupts((byte)(_interrupts | source));
    }

    public void SetRxd(bool high) => _rxd = high;
    public void SetCts(bool high)
    {
        _cts = high;
        if (high) SetTransmit(true);
    }
    public void SetDsr(bool high)
    {
        if (_dsr != high) { _dsr = high; RaiseInterrupt(C.DataSetAbsent); }
    }
    public void SetDcd(bool high)
    {
        if (_dcd != high) { _dcd = high; RaiseInterrupt(C.CarrierAbsent); }
    }

    public void Reset()
    {
        _command = _control = _receiveData = _transmitData = 0;
        _status = C.TransmitEmpty;
        _clockPhase = 0;
        _transmitTicks = _transmitRemaining = _receiveTicks = _receiveValue = 0;
        _receiveStep = C.IdleReceive;
        _receiveParity = _receiveParityError = false;
        _externalClock = false;
        SetInterrupts(0);
        SetTransmit(true);
    }
}
