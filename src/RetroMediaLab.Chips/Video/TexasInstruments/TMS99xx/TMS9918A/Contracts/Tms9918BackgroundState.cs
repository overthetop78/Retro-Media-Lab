namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Verrous du nom, motif et couleur en préparation, et octets du
/// motif en sortie. Aucune copie de table ou de mémoire vidéo.</summary>
public readonly record struct Tms9918BackgroundState(byte Name, bool NameKnown,
    byte Pattern, bool PatternKnown, byte Colour, bool ColourKnown,
    byte ActivePattern, byte ActiveColour, bool ActivePatternKnown, bool ActiveColourKnown,
    int PixelPosition);
