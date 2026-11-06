namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Contracts;

/// <summary>État du VIDPROC sans connaissance des autres composants vidéo.</summary>
public sealed record BbcVideoUlaState
{
    public required int FormatVersion { get; init; }
    public required byte[] Palette { get; init; }
    public required byte[] Colours { get; init; }
    public required ulong[] PackedColours { get; init; }
    public required bool MasterClock { get; init; }
    public required int Phase { get; init; }
    public required int Shift { get; init; }
    public required int Logical { get; init; }
    public required int CursorSegments { get; init; }
    public required uint PaletteRevision { get; init; }
    public required int PixelPeriod { get; init; }
    public required byte Control { get; init; }
    public required byte Data { get; init; }
    public required bool Cursor { get; init; }
    public required bool TeletextSelected { get; init; }
    public required bool FastCrtc { get; init; }
    public required int CharacterPeriodMask { get; init; }
    public required int PackedPatternBase { get; init; }
    public required int PackedShifts { get; init; }
    public required int PackedFill { get; init; }
    public required int CursorPattern { get; init; }
    public required bool DisplayEnabled { get; init; }
    public required bool Inverted { get; init; }
    public required int TeletextRgb { get; init; }
    public required int Rgb { get; init; }
}
