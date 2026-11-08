using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

internal static class SystemVideoConstants
{
    internal const int Width = 480;
    internal const int Height = 250;
    internal const int PixelBytes = 4;
    internal const int FrameBytes = Width * Height * PixelBytes;
    internal const int TeletextRamBytes = 1024;
    internal const int MonochromeRamBytes = 2048;
    internal const int TeletextRamStart = 0x0400;
    internal const int MonochromeRamStart = 0x1000;
    internal const int RelocatedMonochromeRamStart = 0xf000;
    internal const int TeletextCrtcStart = 0x0800;
    internal const int TeletextCrtcMirror = 0xfe;
    internal const int MonochromeCrtcStart = 0x1840;
    internal const int RelocatedMonochromeCrtcStart = 0xe840;
    internal const int MonochromeCrtcMirror = 0x3e;
    internal const int TeletextCellPixels = 12;
    internal const int TeletextHalfDotsPerCharacter = 12;
    internal const int TeletextViewportHalfDots = Saa5050Constants.GraphicsDelayHalfClocks;
    private static readonly uint[] TeletextColourPixels = [0, 0x00ff0000, 0x0000ff00, 0x00ffff00, 0x000000ff, 0x00ff00ff, 0x0000ffff, 0x00ffffff];
    internal static ReadOnlySpan<uint> TeletextColours => TeletextColourPixels;
    internal const int MonochromeCellPixels = 6;
    internal const int MonochromeGlyphPadding = 2;
    internal const int MonochromeGlyphBytes = 16;
    internal const int CharacterRomBytes = 2048;
    internal const int MonochromeRasterMask = 15;
    internal const int TeletextRasterMultiplier = 2;
    internal const int InterlaceFieldStep = 2;
    internal const int CrtcRasterHeight = 9;
    internal const int RedMask = 1;
    internal const int GreenMask = 2;
    internal const int BlueMask = 4;
    internal const int PixelLevel = 255;
    internal const int White = 7;
    internal const byte VerticalBlankStatus = 0x20;
}
