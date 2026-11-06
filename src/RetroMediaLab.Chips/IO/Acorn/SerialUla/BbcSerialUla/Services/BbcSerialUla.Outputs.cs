using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

public sealed partial class BbcSerialUla
{
    private bool _transmitInput = true;
    private bool _requestToSendInput = true;
    private bool _outputsKnown;
    private bool _publishedCts;
    private bool _publishedDcd;
    private bool _publishedReceiveData;
    private bool _publishedSelection;
    public Action<bool, bool, bool, bool>? SerialPinsChanged { get; set; }
    public Action<bool, bool, bool>? SerialClocksRaised { get; set; }
    public long SourceClockPhase => _pendingClockTicks == 0 ? _sourcePhase
        : (_sourcePhase + (long)_pendingClockTicks * C.MasterClock) % _clockDenominator;

    /// <summary>Entrées retenues ; leur changement ne crée pas de front d'horloge.</summary>
    public void SetTransmitPins(bool data, bool requestToSendHigh)
    {
        if (_transmitInput == data && _requestToSendInput == requestToSendHigh) return;
        FlushPendingClock();
        _clockCountdown = 0;
        _transmitInput = data;
        _requestToSendInput = requestToSendHigh;
    }

    private void PublishSerialPins()
    {
        var cts = ClearToSendHigh;
        var dcd = DataCarrierDetectHigh;
        var data = ReceiveData;
        if (!_outputsKnown || _publishedCts != cts || _publishedDcd != dcd
            || _publishedReceiveData != data || _publishedSelection != _rs423Selected)
        {
            _publishedCts = cts;
            _publishedDcd = dcd;
            _publishedReceiveData = data;
            _publishedSelection = _rs423Selected;
            _outputsKnown = true;
            SerialPinsChanged?.Invoke(cts, dcd, data, _rs423Selected);
        }
    }
}
