namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

/// <summary>Destination matérielle d'une sélection VRAM.</summary>
internal enum Tms9918VramSlotKind : byte
{
    Cpu, Refresh, BackgroundName, BackgroundColour, BackgroundPattern,
    SpriteVisibility, SpriteY, SpriteX, SpriteName, SpriteColour, SpritePatternLeft, SpritePatternRight,
}
