namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Enums;

public enum Saa5050Control : byte
{
    AlphaRed = 0x01,
    AlphaGreen = 0x02,
    AlphaYellow = 0x03,
    AlphaBlue = 0x04,
    AlphaMagenta = 0x05,
    AlphaCyan = 0x06,
    AlphaWhite = 0x07,
    Flash = 0x08,
    Steady = 0x09,
    EndBox = 0x0a,
    StartBox = 0x0b,
    NormalHeight = 0x0c,
    DoubleHeight = 0x0d,
    GraphicsRed = 0x11,
    GraphicsGreen = 0x12,
    GraphicsYellow = 0x13,
    GraphicsBlue = 0x14,
    GraphicsMagenta = 0x15,
    GraphicsCyan = 0x16,
    GraphicsWhite = 0x17,
    Conceal = 0x18,
    ContiguousGraphics = 0x19,
    SeparatedGraphics = 0x1a,
    BlackBackground = 0x1c,
    NewBackground = 0x1d,
    HoldGraphics = 0x1e,
    ReleaseGraphics = 0x1f
}
