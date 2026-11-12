namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;

/// <summary>Enveloppes NTSC du TMS9918A, TI MP010A §3.6/table 3-3.
/// Les coordonnées partent du premier pixel graphique de la zone active.</summary>
public static class Tms9918Timing
{
    public const int DotsPerLine = 342;
    public const int LinesPerFrame = 262;
    public const int ActiveWidth = 256;
    public const int ActiveLines = 192;
    public const int TextLeft = 6;
    public const int TextWidth = 240;
    public const int HorizontalBlankStart = 271;
    public const int HorizontalSyncStart = 279;
    public const int HorizontalSyncEnd = 305;
    public const int ColorBurstStart = 307;
    public const int ColorBurstEnd = 321;
    public const int HorizontalBlankEnd = 329;
    public const int VerticalBlankStart = 216;
    public const int VerticalSyncStart = 219;
    public const int VerticalSyncEnd = 222;
    public const int VerticalBlankEnd = 235;
}
