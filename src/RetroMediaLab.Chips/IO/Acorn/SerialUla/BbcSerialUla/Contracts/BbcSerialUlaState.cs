namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Contracts;

/// <summary>État numérique complet du SERPROC, y compris les horloges différées.</summary>
public sealed record BbcSerialUlaState
{
    public required int FormatVersion { get; init; }
    public required long ClockDenominator { get; init; }
    public required byte Control { get; init; }
    public required bool? CassetteInput { get; init; }
    public required bool SeparatorActive { get; init; }
    public required bool PreviousLevel { get; init; }
    public required bool HasLevel { get; init; }
    public required bool CassetteData { get; init; }
    public required int EdgeTicks { get; init; }
    public required int CarrierEdges { get; init; }
    public required int CarrierPulse { get; init; }
    public required long SynthesisPhase { get; init; }
    public required int SynthesisStep { get; init; }
    public required int PendingClockTicks { get; init; }
    public required int ClockCountdown { get; init; }
    public required long SourcePhase { get; init; }
    public required int DividerCounter { get; init; }
    public required int TransmitDivider { get; init; }
    public required int ReceiveDivider { get; init; }
    public required bool Motor { get; init; }
    public required bool Rs423Selected { get; init; }
    public required bool TransmitClockEdge { get; init; }
    public required bool ReceiveClockEdge { get; init; }
    public required bool Rs423TransmitData { get; init; }
    public required bool Rs423RequestToSendHigh { get; init; }
    public required short CassetteOutput { get; init; }
    public required bool Rs423ReceiveData { get; init; }
    public required bool Rs423ClearToSendHigh { get; init; }
    public required bool TransmitInput { get; init; }
    public required bool RequestToSendInput { get; init; }
    public required bool OutputsKnown { get; init; }
    public required bool PublishedCts { get; init; }
    public required bool PublishedDcd { get; init; }
    public required bool PublishedReceiveData { get; init; }
    public required bool PublishedSelection { get; init; }
}
