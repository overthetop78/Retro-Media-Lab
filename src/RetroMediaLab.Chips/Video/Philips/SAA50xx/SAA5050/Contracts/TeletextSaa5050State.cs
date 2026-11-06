namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Contracts;

public readonly record struct TeletextCharacterState(ulong Due, byte Data, int Raster, int Frame, int Row);
public readonly record struct TeletextWindowState(ulong Due, bool Level);
public readonly record struct TeletextOutputState(int Rgb, bool Monochrome, bool Blanking);

/// <summary>État télétexte et pipelines en vol ; la ROM sert à valider la configuration.</summary>
public sealed record TeletextSaa5050State
{
    public required int FormatVersion { get; init; }
    public required byte[] CharacterRom { get; init; }
    public required TeletextCharacterState[] Pipeline { get; init; }
    public required TeletextWindowState[] WindowPipeline { get; init; }
    public required TeletextOutputState SerialOn { get; init; }
    public required TeletextOutputState SerialOff { get; init; }
    public required int Foreground { get; init; }
    public required int Background { get; init; }
    public required int Held { get; init; }
    public required int Row { get; init; }
    public required bool Graphics { get; init; }
    public required bool Flash { get; init; }
    public required bool Conceal { get; init; }
    public required bool Separated { get; init; }
    public required bool Hold { get; init; }
    public required bool Double { get; init; }
    public required bool Bottom { get; init; }
    public required bool RowDouble { get; init; }
    public required bool HeldSeparated { get; init; }
    public required bool Boxed { get; init; }
    public required int PreviousCode { get; init; }
    public required bool PictureOn { get; init; }
    public required bool DisplayEnabled { get; init; }
    public required bool SuperimposeInputHigh { get; init; }
    public required int PipelineHead { get; init; }
    public required int PipelineCount { get; init; }
    public required int WindowHead { get; init; }
    public required int WindowCount { get; init; }
    public required ulong HalfDotTime { get; init; }
    public required bool OutputWindow { get; init; }
    public required int SerialMask { get; init; }
    public required int SerialReversedMask { get; init; }
    public required int SerialForeground { get; init; }
    public required int SerialBackground { get; init; }
    public required int SerialPixel { get; init; }
    public required int SerialRow { get; init; }
    public required bool SerialBox { get; init; }
    public required int Rgb { get; init; }
    public required bool Monochrome { get; init; }
    public required bool Blanking { get; init; }
    public required bool TextMode { get; init; }
    public required bool Superimpose { get; init; }
    public required bool Reveal { get; init; }
    public required int RevealClocks { get; init; }
    public required byte Data { get; init; }
    public required bool F1 { get; init; }
    public required bool Tr6 { get; init; }
    public required bool Lose { get; init; }
    public required bool Dew { get; init; }
    public required bool RemoteClock { get; init; }
    public required bool GeneralLineResetHigh { get; init; }
    public required bool RemoteHalf { get; init; }
    public required int RemoteBit { get; init; }
    public required byte RemoteCommand { get; init; }
    public required int Scanline { get; init; }
    public required int CharacterRow { get; init; }
    public required int FlashField { get; init; }
    public required bool CharacterRoundingSelect { get; init; }
    public required bool BigCharacterSelectHigh { get; init; }
    public required bool RemoteData { get; init; }
}