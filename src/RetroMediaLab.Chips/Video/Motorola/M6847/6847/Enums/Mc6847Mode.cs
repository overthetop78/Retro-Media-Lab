namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;

/// <summary>Niveaux logiques indépendants des huit entrées de sélection du MC6847.</summary>
[Flags]
public enum Mc6847Mode : byte
{
    None = 0,
    Inverse = 1,
    External = 2,
    Semigraphics = 4,
    ColourSet = 8,
    GraphicsMode0 = 16,
    GraphicsMode1 = 32,
    GraphicsMode2 = 64,
    Graphics = 128
}
