using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Interfaces;
using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

/// <summary>Générateurs et multiplexeurs du SERPROC ; ne possède ni ACIA ni ressource hôte.</summary>
public sealed partial class BbcSerialUla : IBbcSerialUla
{
    private readonly long _clockDenominator;
    private readonly int _silenceTicks;
    private readonly int _minimumHalfWaveTicks;
    private readonly int _maximumHalfWaveTicks;
    private readonly int _halfWaveThresholdTicks;
    private readonly int _carrierPulseTicks;
    private long _sourcePhase;
    private int _dividerCounter;
    private int _transmitDivider;
    private int _receiveDivider;
    private bool _motor;
    private bool _rs423Selected;
    private bool _transmitClockEdge;
    private bool _receiveClockEdge;
    private bool _rs423TransmitData = true;
    private bool _rs423RequestToSendHigh = true;
    private short _tapeOutput;
    private bool _rs423ReceiveData = true;
    private bool _rs423ClearToSendHigh = true;
    public byte Control { get; private set; }
    public bool Motor => _motor;
    public bool Rs423Selected => _rs423Selected;
    public bool Rs423ReceiveData
    {
        get => _rs423ReceiveData;
        set
        {
            if (_rs423ReceiveData == value) return;
            _rs423ReceiveData = value;
            if (_rs423Selected) PublishSerialPins();
        }
    }
    public bool Rs423ClearToSendHigh
    {
        get => _rs423ClearToSendHigh;
        set
        {
            if (_rs423ClearToSendHigh == value) return;
            _rs423ClearToSendHigh = value;
            if (_rs423Selected) PublishSerialPins();
        }
    }
    public bool Rs423TransmitData => _rs423TransmitData;
    public bool Rs423RequestToSendHigh => _rs423RequestToSendHigh;
    public bool ReceiveData => _rs423Selected ? Rs423ReceiveData : _tapeData;
    public bool ClearToSendHigh => _rs423Selected && Rs423ClearToSendHigh;
    public bool DataCarrierDetectHigh => !_rs423Selected && _carrierPulse > 0;
    public bool TransmitClockEdge => _transmitClockEdge;
    public bool ReceiveClockEdge => _receiveClockEdge;
    public short TapeOutput => _tapeOutput;

    public BbcSerialUla(int ticksPerSecond)
    {
        if (ticksPerSecond < C.MasterClock / C.ExternalClockDivider)
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
        _clockDenominator = (long)ticksPerSecond * C.ExternalClockDivider;
        _silenceTicks = ticksPerSecond / C.SilenceRate;
        _minimumHalfWaveTicks = ticksPerSecond / C.MaximumHalfWaveRate;
        _maximumHalfWaveTicks = ticksPerSecond / C.MinimumHalfWaveRate;
        _halfWaveThresholdTicks = (int)((long)ticksPerSecond * C.HalfWaveThresholdNumerator / C.HalfWaveThresholdDenominator);
        _carrierPulseTicks = (ticksPerSecond + C.CarrierPulseRate - 1) / C.CarrierPulseRate;
        PowerOn();
    }

    /// <summary>Initialisation de carte ; ne représente pas une broche RESET non documentée du circuit.</summary>
    public void PowerOn()
    {
        _pendingClockTicks = _clockCountdown = 0;
        _outputsKnown = false;
        _sourcePhase = _synthesisPhase = 0;
        _dividerCounter = 0;
        _synthesisStep = 0;
        _transmitClockEdge = _receiveClockEdge = false;
        _rs423TransmitData = _rs423RequestToSendHigh = true;
        _tapeOutput = 0;
        ResetSeparator();
        Write(0);
    }

    public void Write(byte value)
    {
        FlushPendingClock();
        _clockCountdown = 0;
        var previousSelection = _rs423Selected;
        Control = value;
        _motor = (value & C.MotorMask) != 0;
        _rs423Selected = (value & C.Rs423Mask) != 0;
        _transmitDivider = C.Dividers[value & C.RateMask];
        _receiveDivider = C.Dividers[(value >> C.ReceiveRateShift) & C.RateMask];
        // Free-running divider phase is preserved across register writes.
        if (!_outputsKnown || previousSelection != _rs423Selected) PublishSerialPins();
    }

    public byte Read()
    {
        Write(C.ReadControlValue);
        return C.ReadBusValue;
    }

    private void ClockInputs(bool transmitData, bool requestToSendHigh)
    {
        _sourcePhase += C.MasterClock;
        var sourceEdge = _sourcePhase >= _clockDenominator;
        if (sourceEdge)
        {
            _sourcePhase -= _clockDenominator;
            _dividerCounter = (_dividerCounter + 1) & C.DividerCounterMask;
        }
        _transmitClockEdge = sourceEdge && (_dividerCounter & (_transmitDivider - 1)) == 0;
        var serialReceiveEdge = sourceEdge && (_dividerCounter & (_receiveDivider - 1)) == 0;
        var tapeReceiveEdge = sourceEdge && (_dividerCounter & (C.TapeClockDivider - 1)) == 0;
        if (_separatorActive) TickSeparator();
        _receiveClockEdge = _rs423Selected ? serialReceiveEdge : tapeReceiveEdge;
        _rs423TransmitData = !_rs423Selected || transmitData;
        _rs423RequestToSendHigh = !_rs423Selected || requestToSendHigh;
        TickSynthesis(transmitData, requestToSendHigh || _rs423Selected);
        if (_transmitClockEdge || _receiveClockEdge)
            SerialClocksRaised?.Invoke(_transmitClockEdge, _receiveClockEdge, ReceiveData);
    }
}
