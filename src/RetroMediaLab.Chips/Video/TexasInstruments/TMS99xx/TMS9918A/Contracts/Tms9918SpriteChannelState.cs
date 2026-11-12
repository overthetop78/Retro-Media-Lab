namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Verrous d'un canal physique. KnownMask : Y/X/Name/Tag/Left/Right,
/// bits 0..5. Un canal non sélectionné ne constitue pas un sprite opaque.</summary>
public readonly record struct Tms9918SpriteChannelState(byte Y, byte X, byte Name,
    byte Tag, byte Left, byte Right, byte KnownMask, int PatternRow, bool Selected);
