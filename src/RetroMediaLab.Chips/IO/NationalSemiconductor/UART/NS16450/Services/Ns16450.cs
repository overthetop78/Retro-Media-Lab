using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Enums;
using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Interfaces;
using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

/// <summary>NS16450 registers and electrical pins, without a FIFO or host serial transport.</summary>
/// <remarks>National Semiconductor datasheet, sections 7–8. Modem inputs and outputs use
/// physical pin levels; the status register exposes the complemented input signals.</remarks>
public sealed partial class Ns16450 : INs16450
{
    private readonly int _ticksPerSecond;
    private readonly int _sourceClock;
    private readonly int _clockDivider;
    private readonly bool _externalReceiveClock;
    private ushort _divisor;
    private byte _interruptEnable;
    private byte _lineControl;
    private byte _modemControl;
    private byte _lineStatus;
    private byte _scratch;
    private byte _received;
    private byte _holding;
    private bool _holdingFull;
    private bool _transmitInterrupt;
    private bool _irq;
    private long _clockPhase;
    private long _clockPeriod;
    private bool _reset;

    public Ns16450(int ticksPerSecond, int sourceClockHz = C.DefaultClockHz,
        int clockDivider = 1, bool externalReceiveClock = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockDivider);
        if ((long)ticksPerSecond * clockDivider > long.MaxValue / ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(clockDivider));
        _ticksPerSecond = ticksPerSecond;
        _sourceClock = sourceClockHz;
        _clockDivider = clockDivider;
        _externalReceiveClock = externalReceiveClock;
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? TransmitChanged { get; set; }
    public Action<bool>? DtrChanged { get; set; }
    public Action<bool>? RtsChanged { get; set; }
    public Action<bool>? Out1Changed { get; set; }
    public Action<bool>? Out2Changed { get; set; }
    public bool Irq => _irq;
    public bool Txd => _txd;
    public bool Dtr => _dtr;
    public bool Rts => _rts;
    public bool Out1 => _out1;
    public bool Out2 => _out2;
    private bool Loopback => (_modemControl & C.Loopback) != 0;
    private bool DivisorAccess => (_lineControl & C.DivisorAccess) != 0;

    public void SetReset(bool active)
    {
        if (_reset == active) return;
        _reset = active;
        if (active) Reset();
    }

    public void Reset()
    {
        // DLL/DLM and SCR have no specified master-reset value in the reset-function table.
        _interruptEnable = _lineControl = _modemControl = 0;
        _lineStatus = C.InitialLineStatus;
        _holdingFull = _transmitInterrupt = false;
        ResetSerial();
        _busWriteActive = _busReadActive = false;
        _modemStatus = ExternalModemStatus();
        _modemDelta = 0;
        RefreshOutputs();
        UpdateInterrupt();
    }

    public byte Read(int register)
    {
        switch ((Ns16450Register)(register & C.RegisterMask))
        {
            case Ns16450Register.DataDivisorLow:
                if (DivisorAccess) return (byte)_divisor;
                var received = _received;
                _lineStatus &= unchecked((byte)~C.DataReady);
                ClearReceiveInterrupt(C.DataReady);
                UpdateInterrupt();
                return received;
            case Ns16450Register.InterruptEnableDivisorHigh:
                return DivisorAccess ? (byte)(_divisor >> C.ByteBits) : _interruptEnable;
            case Ns16450Register.InterruptId:
                var interrupt = PendingInterrupt();
                if (interrupt == Ns16450Interrupt.TransmitEmpty)
                {
                    _transmitInterrupt = false;
                    UpdateInterrupt();
                }
                return (byte)interrupt;
            case Ns16450Register.LineControl: return _lineControl;
            case Ns16450Register.ModemControl: return _modemControl;
            case Ns16450Register.LineStatus:
                var status = _lineStatus;
                _lineStatus &= unchecked((byte)~C.ReceiveErrors);
                ClearReceiveInterrupt(C.ReceiveErrors);
                UpdateInterrupt();
                return status;
            case Ns16450Register.ModemStatus:
                var modem = (byte)(_modemStatus | _modemDelta);
                _modemDelta = 0;
                UpdateInterrupt();
                return modem;
            default: return _scratch;
        }
    }

    public void Write(int register, byte value)
    {
        if (_reset) return;
        switch ((Ns16450Register)(register & C.RegisterMask))
        {
            case Ns16450Register.DataDivisorLow:
                if (DivisorAccess)
                {
                    var previousBaudOut = BaudOutHigh;
                    _divisor = (ushort)((_divisor & ~C.ByteMask) | value);
                    ReloadClockPeriod(previousBaudOut);
                }
                else
                {
                    WriteHolding(value);
                }
                break;
            case Ns16450Register.InterruptEnableDivisorHigh:
                if (DivisorAccess)
                {
                    var previousBaudOut = BaudOutHigh;
                    _divisor = (ushort)((_divisor & C.ByteMask) | value << C.ByteBits);
                    ReloadClockPeriod(previousBaudOut);
                }
                else
                {
                    if ((value & C.EnableTransmitEmpty) != 0 && (_interruptEnable & C.EnableTransmitEmpty) == 0
                        && (_lineStatus & C.HoldingEmpty) != 0) _transmitInterrupt = true;
                    _interruptEnable = (byte)(value & C.InterruptEnableMask);
                }
                break;
            case Ns16450Register.LineControl:
                _lineControl = value;
                RefreshOutputs();
                break;
            case Ns16450Register.ModemControl:
                _modemControl = (byte)(value & C.ModemControlMask);
                RefreshOutputs();
                RefreshModem();
                break;
            case Ns16450Register.Scratch:
                _scratch = value;
                break;
            // IIR, LSR and MSR are not writable software registers. In particular there is no FCR.
        }
        UpdateInterrupt();
    }

    private Ns16450Interrupt PendingInterrupt()
    {
        if ((_interruptEnable & C.EnableLineStatus) != 0 && (_receiveInterruptStatus & C.ReceiveErrors) != 0)
            return Ns16450Interrupt.LineStatus;
        if ((_interruptEnable & C.EnableReceivedData) != 0 && (_receiveInterruptStatus & C.DataReady) != 0)
            return Ns16450Interrupt.ReceivedData;
        if ((_interruptEnable & C.EnableTransmitEmpty) != 0 && _transmitInterrupt)
            return Ns16450Interrupt.TransmitEmpty;
        if ((_interruptEnable & C.EnableModemStatus) != 0 && _modemDelta != 0)
            return Ns16450Interrupt.ModemStatus;
        return Ns16450Interrupt.None;
    }

    private void UpdateInterrupt()
    {
        var irq = PendingInterrupt() != Ns16450Interrupt.None;
        if (_irq == irq) return;
        _irq = irq;
        InterruptChanged?.Invoke(irq);
    }
}
