using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Interfaces;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

/// <summary>ADLC Motorola : registres, FIFO et broches, sans socket ni connaissance des machines.</summary>
public sealed partial class Mc6854 : IMc6854
{
    private byte _control1;
    private byte _control2;
    private byte _control3;
    private byte _control4;
    private byte _receiveStatus;
    private byte _readReceiveStatus;
    private byte _readTransmitStatus;
    private byte _acknowledgedReceive;
    private byte _acknowledgedTransmit;
    private readonly int _cyclesPerByte;
    private long _clockFraction;
    private bool _stableIdle;
    private bool _clockPresent;
    private bool _clearToSend;
    private bool _carrierPresent;
    private bool _ctsRaised;
    private bool _dcdRaised;
    private bool _resetAsserted;
    private bool _lineIdle = true;
    private bool _rtsAsserted;
    private int _rtsReleaseBits;

    public Action<ReadOnlyMemory<byte>>? FrameTransmitted { get; set; }
    public bool Irq => (StatusOne() & C.Interrupt) != 0;
    public bool Receiving => (_rxInFrame && _rxTotalBits != 0) || _packetBits is not null || _receiveCount != 0;
    public bool Transmitting => _transmitting || _transmitCount != 0;
    public bool Rts => _rtsAsserted;
    public bool DataTerminalReady => (_control3 & C.Loop) != 0 ? _loopOnline : (_control3 & C.LoopOnline) != 0;
    public bool ReceiveServiceRequest => !_receiveDmaSuppressed && (StatusTwo() & C.ReceiveAvailable) != 0 &&
        ((_control2 & C.Prioritize) == 0 || (RawReceiveStatus() & ~C.ReceiveAvailable) == 0);
    public bool TransmitServiceRequest => !_transmitDmaSuppressed && (_control1 & C.TransmitReset) == 0 && _clearToSend &&
        (_control2 & C.FrameCompleteMode) == 0 && TransmitRoom &&
        ((_control2 & C.Prioritize) == 0 || (!_underrun && !_ctsRaised));

    public Mc6854(int cyclesPerByte = C.DefaultCyclesPerByte)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cyclesPerByte);
        _cyclesPerByte = cyclesPerByte;
        Reset();
    }

    /// <summary>Impulsion RESET matériel ; les entrées physiques et rappels restent ceux du propriétaire.</summary>
    public void Reset()
    {
        SetReset(true);
        SetReset(false);
    }

    public void SetReset(bool asserted)
    {
        _stableIdle = false;
        _statusDirty = true;
        _resetAsserted = asserted;
        if (!asserted)
            return;
        // Seuls les bits nommés par Motorola sont affectés par RESET.
        _control1 |= C.ReceiveReset | C.TransmitReset;
        _control2 &= unchecked((byte)~C.Rts);
        _control3 &= unchecked((byte)~(C.Loop | C.LoopOnline));
        _control4 &= unchecked((byte)~C.TransmitAbort);
        _readReceiveStatus = _readTransmitStatus = 0;
        _acknowledgedReceive = _acknowledgedTransmit = 0;
        _receiveStatus = 0;
        _ctsRaised = _dcdRaised = false;
        _receiveDmaSuppressed = _transmitDmaSuppressed = false;
        _rtsAsserted = false;
        _rtsReleaseBits = 0;
        ResetReceive();
        ResetTransmit();
        ResetLoop();
        _busData = _busEnable && _busSelected && _busRead ? Read(_busRegister) : null;
    }

    /// <summary>Adaptateur d’horloge et de ligne pour les transports par trames ; pas une détection interne de câble.</summary>
    public void SetLine(bool clockPresent, bool clearToSend = true, bool idle = true)
    {
        SetClockPresent(clockPresent, idle);
        SetCarrierPresent(clockPresent);
        SetClearToSend(clockPresent && clearToSend);
    }

    /// <summary>Horloge de l’adaptateur par trames, sans modifier les entrées CTS/DCD.</summary>
    public void SetClockPresent(bool present, bool idle = true)
    {
        if (_clockPresent == present && _lineIdle == idle)
            return;
        _stableIdle = false;
        _clockPresent = present;
        _lineIdle = idle;
    }

    public void SetClearToSend(bool allowed)
    {
        if (_clearToSend == allowed)
            return;
        _stableIdle = false;
        _statusDirty = true;
        if (_clearToSend && !allowed && (_control1 & C.TransmitReset) == 0)
        {
            _ctsRaised = true;
            _acknowledgedTransmit &= unchecked((byte)~C.NotClearToSend);
        }
        _clearToSend = allowed;
    }

    public void SetCarrierPresent(bool present)
    {
        if (_carrierPresent == present)
            return;
        _stableIdle = false;
        _statusDirty = true;
        if (_carrierPresent && !present && (_control1 & C.ReceiveReset) == 0)
        {
            _dcdRaised = true;
            _acknowledgedReceive &= unchecked((byte)~C.DataCarrierDetect);
            DiscardCurrentReceive();
        }
        _carrierPresent = present;
    }

    public byte Read(int register)
    {
        register &= C.RegisterMask;
        if (register > 1)
            return ReadReceive();
        if (register == 0)
        {
            var status = StatusOne();
            _flagRead |= (status & C.FlagDetected) != 0;
            _readTransmitStatus |= (byte)(status & (C.NotClearToSend | C.TransmitUnderrun | C.TransmitAvailable));
            return status;
        }
        var receive = StatusTwo();
        _readReceiveStatus |= receive;
        return receive;
    }

    public void Write(int register, byte value)
    {
        _stableIdle = false;
        _statusDirty = true;
        register &= C.RegisterMask;
        switch (register)
        {
            case 0:
                _control1 = _resetAsserted ? (byte)(value | C.ReceiveReset | C.TransmitReset) : value;
                if ((_control1 & C.ReceiveReset) != 0)
                {
                    ResetReceive();
                    _receiveStatus = _readReceiveStatus = _acknowledgedReceive = 0;
                    _dcdRaised = false;
                }
                if ((_control1 & C.TransmitReset) != 0)
                {
                    ResetTransmit();
                    _readTransmitStatus = _acknowledgedTransmit = 0;
                    _ctsRaised = false;
                }
                if ((_control1 & C.Discontinue) != 0)
                    DiscontinueReceive();
                break;
            case 1 when (_control1 & C.AddressControl) != 0:
                var oldControl = _control3;
                _control3 = _resetAsserted ? (byte)(value & ~(C.Loop | C.LoopOnline)) : value;
                if (((oldControl ^ _control3) & C.Loop) != 0)
                    ResetLoop();
                if (((oldControl ^ _control3) & C.ZeroIdle) != 0 && (_control3 & C.ZeroIdle) != 0)
                    _markIdlePrefix = true;
                break;
            case 1:
                if (((_control2 ^ value) & C.FrameCompleteMode) != 0)
                    _readTransmitStatus &= unchecked((byte)~C.TransmitAvailable);
                _control2 = (byte)(value & ~(C.ClearReceive | C.ClearTransmit | C.LastData));
                if (_resetAsserted)
                    _control2 &= unchecked((byte)~C.Rts);
                if ((value & C.ClearReceive) != 0)
                    ClearReceiveStatus();
                if ((value & C.ClearTransmit) != 0)
                    ClearTransmitStatus();
                if ((value & C.LastData) != 0)
                    MarkTransmitEnd();
                if ((_control2 & C.Rts) != 0)
                {
                    _rtsAsserted = true;
                    _rtsReleaseBits = 0;
                }
                break;
            case 3 when (_control1 & C.AddressControl) != 0:
                _control4 = (byte)(value & ~C.TransmitAbort);
                if ((value & C.TransmitAbort) != 0 && !_resetAsserted)
                    RequestAbort();
                break;
            default:
                WriteTransmit(value, register == C.RegisterMask);
                break;
        }
    }

    /// <summary>Un cycle du propriétaire. Le rapport configuré conserve l’horloge des bits dans le temps émulé.</summary>
    public void Tick()
    {
        if (!_clockPresent)
            return;
        _clockFraction += C.BitsPerOctet;
        if (_clockFraction < _cyclesPerByte)
            return;
        AdvanceClockBits();
    }

    private void AdvanceClockBits()
    {
        if (_stableIdle || CanAdvanceStableIdle())
        {
            _stableIdle = true;
            do
            {
                _clockFraction -= _cyclesPerByte;
                _fillBit = (_fillBit + 1) & (C.BitsPerOctet - 1);
            }
            while (_clockFraction >= _cyclesPerByte);
            // Un cycle complet conserve les mêmes niveaux de fin TxC/RxC.
            _txClock = false;
            _rxClock = true;
            return;
        }
        while (_clockFraction >= _cyclesPerByte)
        {
            _clockFraction -= _cyclesPerByte;
            SetTransmitClock(true);
            SetTransmitClock(false);
            SetReceiveClock(false);
            SetReceiveData(NextPacketBit());
            SetReceiveClock(true);
        }
    }
}
