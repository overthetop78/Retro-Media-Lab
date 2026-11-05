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
    public byte Control { get; private set; }
    public bool Motor => (Control & C.MotorMask) != 0;
    public bool Rs423Selected => (Control & C.Rs423Mask) != 0;
    public bool Rs423ReceiveData { get; set; } = true;
    public bool Rs423ClearToSendHigh { get; set; } = true;
    public bool Rs423TransmitData { get; private set; } = true;
    public bool Rs423RequestToSendHigh { get; private set; } = true;
    public bool ReceiveData => Rs423Selected ? Rs423ReceiveData : _cassetteData;
    public bool ClearToSendHigh => Rs423Selected && Rs423ClearToSendHigh;
    public bool DataCarrierDetectHigh => !Rs423Selected && _carrierPulse > 0;
    public bool TransmitClockEdge { get; private set; }
    public bool ReceiveClockEdge { get; private set; }
    public short CassetteOutput { get; private set; }

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
        _sourcePhase = _synthesisPhase = 0;
        _dividerCounter = 0;
        _synthesisStep = 0;
        TransmitClockEdge = ReceiveClockEdge = false;
        Rs423TransmitData = Rs423RequestToSendHigh = true;
        CassetteOutput = 0;
        ResetSeparator();
        Write(0);
    }

    public void Write(byte value)
    {
        Control = value;
        _transmitDivider = C.Dividers[value & C.RateMask];
        _receiveDivider = C.Dividers[(value >> C.ReceiveRateShift) & C.RateMask];
        // Free-running divider phase is preserved across register writes.
    }

    public byte Read()
    {
        Write(C.ReadControlValue);
        return C.ReadBusValue;
    }

    public void Tick(bool transmitData, bool requestToSendHigh)
    {
        _sourcePhase += C.MasterClock;
        var sourceEdge = _sourcePhase >= _clockDenominator;
        if (sourceEdge)
        {
            _sourcePhase -= _clockDenominator;
            _dividerCounter = (_dividerCounter + 1) & C.DividerCounterMask;
        }
        TransmitClockEdge = sourceEdge && (_dividerCounter & (_transmitDivider - 1)) == 0;
        var serialReceiveEdge = sourceEdge && (_dividerCounter & (_receiveDivider - 1)) == 0;
        var cassetteReceiveEdge = sourceEdge && (_dividerCounter & (C.CassetteClockDivider - 1)) == 0;
        TickSeparator();
        ReceiveClockEdge = Rs423Selected ? serialReceiveEdge : cassetteReceiveEdge;
        Rs423TransmitData = !Rs423Selected || transmitData;
        Rs423RequestToSendHigh = !Rs423Selected || requestToSendHigh;
        TickSynthesis(transmitData, requestToSendHigh || Rs423Selected);
    }
}
