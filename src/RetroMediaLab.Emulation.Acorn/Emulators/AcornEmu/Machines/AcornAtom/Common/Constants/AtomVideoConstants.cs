namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants;

internal static class AtomVideoConstants
{
    internal const int Width = 256;
    internal const int Height = 192;
    internal const int PixelBytes = 4;
    internal const int FrameBytes = Width * Height * PixelBytes;
    internal const int CharacterClock = 3579545;
    internal const int ClocksPerLine = 228;
    internal const int ActiveClockStart = 29;
    internal const int ActiveClocks = 128;
    internal const int PixelsPerClock = 2;
    internal const int ActiveLineStart = 25;
    internal const int FrameSyncLine = ActiveLineStart + Height - 1;
    internal const int LinesPerFrame = 262;
    internal const int ClocksPerFrame = ClocksPerLine * LinesPerFrame;
    internal const double FramesPerSecond = (double)CharacterClock / ClocksPerFrame;
    internal const float AspectRatio = 4f / 3f;
    internal const int GraphicsSelect = 0x10;
    internal const int GraphicsModeShift = 5;
    internal const int GraphicsModeMask = 7;
    internal const int MonochromeModeBit = 1;
    internal const int LowResolutionRowSamples = 16;
    internal const int HighResolutionRowSamples = 32;
    internal const int LowResolutionRows = 64;
    internal const int MediumResolutionRows = 96;
    internal const int ColorSetSelect = 0x08;
    internal const int SemiGraphicsSelect = 0x40;
    internal const int InverseSelect = 0x80;
    internal const int CharacterMask = 0x3f;
    internal const int Columns = 32;
    internal const int CharacterWidth = 8;
    internal const int CharacterHeight = 12;
    internal const int CharacterRomBytes = 280;
    internal const int CharacterCount = 64;
    internal const int GlyphRows = 7;
    internal const int GlyphColumns = 5;
    internal const int GlyphTop = 3;
    internal const int RawFontBytesPerRow = 8;
    internal const int RawFontBottomRow = 28;
    internal const int RawFontColumnStride = RawFontBytesPerRow * GlyphRows;
    internal const int ForegroundGlyphBit = 5;
    internal const int ByteBits = 8;
    internal const int ByteHighBit = 7;
    internal const int BitsPerColorPixel = 2;
    internal const int ColorPixelMask = 3;
    internal const int ColorSetOffset = 4;
    internal const int SemiGraphicsColumns = 2;
    internal const int SemiGraphicsRows = 3;
    internal const int SemiGraphicsColorShift = 6;
    internal const int ColorChannelShift = 8;
    internal const int RedChannelShift = 16;
    internal const int ColorChannelMask = 255;
    internal const int Black = 0x263016;
    internal const int Green = 0x30d200;
    internal const int Yellow = 0xc1e500;
    internal const int Blue = 0x4c3ab4;
    internal const int Red = 0x9a3236;
    internal const int Buff = 0xbfc8ad;
    internal const int Cyan = 0x41af71;
    internal const int Magenta = 0xc84ef0;
    internal const int Orange = 0xd47f00;
    internal const int TextDarkGreen = 0x007c00;
    internal const int TextBrightGreen = Green;
    internal const int TextDarkOrange = 0x6b2700;
    internal const int TextBrightOrange = 0xffb700;

    internal static IReadOnlyList<(int Samples, int Rows)> GraphicsGeometry { get; } = Array.AsReadOnly<(int Samples, int Rows)>(
    [
        (LowResolutionRowSamples, LowResolutionRows),
        (LowResolutionRowSamples, LowResolutionRows),
        (HighResolutionRowSamples, LowResolutionRows),
        (LowResolutionRowSamples, MediumResolutionRows),
        (HighResolutionRowSamples, MediumResolutionRows),
        (LowResolutionRowSamples, Height),
        (HighResolutionRowSamples, Height),
        (HighResolutionRowSamples, Height)
    ]);
}
