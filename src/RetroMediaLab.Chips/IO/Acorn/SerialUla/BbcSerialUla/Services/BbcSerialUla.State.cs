using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Contracts;
using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

public sealed partial class BbcSerialUla
{
    /// <summary>Copie sans vider les intervalles différés ni produire de signal.</summary>
    public BbcSerialUlaState CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        ClockDenominator = _clockDenominator,
        Control = Control,
        CassetteInput = _cassetteInput,
        SeparatorActive = _separatorActive,
        PreviousLevel = _previousLevel,
        HasLevel = _hasLevel,
        CassetteData = _cassetteData,
        EdgeTicks = _edgeTicks,
        CarrierEdges = _carrierEdges,
        CarrierPulse = _carrierPulse,
        SynthesisPhase = _synthesisPhase,
        SynthesisStep = _synthesisStep,
        PendingClockTicks = _pendingClockTicks,
        ClockCountdown = _clockCountdown,
        SourcePhase = _sourcePhase,
        DividerCounter = _dividerCounter,
        TransmitDivider = _transmitDivider,
        ReceiveDivider = _receiveDivider,
        Motor = _motor,
        Rs423Selected = _rs423Selected,
        TransmitClockEdge = _transmitClockEdge,
        ReceiveClockEdge = _receiveClockEdge,
        Rs423TransmitData = _rs423TransmitData,
        Rs423RequestToSendHigh = _rs423RequestToSendHigh,
        CassetteOutput = _cassetteOutput,
        Rs423ReceiveData = _rs423ReceiveData,
        Rs423ClearToSendHigh = _rs423ClearToSendHigh,
        TransmitInput = _transmitInput,
        RequestToSendInput = _requestToSendInput,
        OutputsKnown = _outputsKnown,
        PublishedCts = _publishedCts,
        PublishedDcd = _publishedDcd,
        PublishedReceiveData = _publishedReceiveData,
        PublishedSelection = _publishedSelection,
    };

    /// <summary>Restaure sans front supplémentaire ; conserve les récepteurs raccordés.</summary>
    public void RestoreState(BbcSerialUlaState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.ClockDenominator != _clockDenominator)
            throw new ArgumentException(nameof(state));
        Control = state.Control;
        _cassetteInput = state.CassetteInput;
        _separatorActive = state.SeparatorActive;
        _previousLevel = state.PreviousLevel;
        _hasLevel = state.HasLevel;
        _cassetteData = state.CassetteData;
        _edgeTicks = state.EdgeTicks;
        _carrierEdges = state.CarrierEdges;
        _carrierPulse = state.CarrierPulse;
        _synthesisPhase = state.SynthesisPhase;
        _synthesisStep = state.SynthesisStep;
        _pendingClockTicks = state.PendingClockTicks;
        _clockCountdown = state.ClockCountdown;
        _sourcePhase = state.SourcePhase;
        _dividerCounter = state.DividerCounter;
        _transmitDivider = state.TransmitDivider;
        _receiveDivider = state.ReceiveDivider;
        _motor = state.Motor;
        _rs423Selected = state.Rs423Selected;
        _transmitClockEdge = state.TransmitClockEdge;
        _receiveClockEdge = state.ReceiveClockEdge;
        _rs423TransmitData = state.Rs423TransmitData;
        _rs423RequestToSendHigh = state.Rs423RequestToSendHigh;
        _cassetteOutput = state.CassetteOutput;
        _rs423ReceiveData = state.Rs423ReceiveData;
        _rs423ClearToSendHigh = state.Rs423ClearToSendHigh;
        _transmitInput = state.TransmitInput;
        _requestToSendInput = state.RequestToSendInput;
        _outputsKnown = state.OutputsKnown;
        _publishedCts = state.PublishedCts;
        _publishedDcd = state.PublishedDcd;
        _publishedReceiveData = state.PublishedReceiveData;
        _publishedSelection = state.PublishedSelection;
    }
}