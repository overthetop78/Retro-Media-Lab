namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants;

public static class AtomVideoConstants
{
    public const int Width = 256;
    public const int Height = 192;
    public const int PixelBytes = 4;
    public const int FrameBytes = Width * Height * PixelBytes;
    public const int CharacterClock = 3579545;
    public const int ClocksPerLine = 228;
    public const int ActiveClockStart = 29;
    public const int ActiveClocks = 128;
    public const int PixelsPerClock = 2;
    public const int ActiveLineStart = 25;
    public const int FrameSyncLine = ActiveLineStart + Height - 1;
    public const int LinesPerFrame = 262;
    public const int ClocksPerFrame = ClocksPerLine * LinesPerFrame;
    public const double FramesPerSecond = (double)CharacterClock / ClocksPerFrame;
    public const float AspectRatio = 4f / 3f;
    public const int GraphicsSelect = 0x10;
    public const int GraphicsModeShift = 5;
    public const int GraphicsModeMask = 7;
    public const int MonochromeModeBit = 1;
    public const int LowResolutionRowSamples = 16;
    public const int HighResolutionRowSamples = 32;
    public const int LowResolutionRows = 64;
    public const int MediumResolutionRows = 96;
    public const int ColorSetSelect = 0x08;
    public const int SemiGraphicsSelect = 0x40;
    public const int InverseSelect = 0x80;
    public const int CharacterMask = 0x3f;
    public const int Columns = 32;
    public const int CharacterWidth = 8;
    public const int CharacterHeight = 12;
    public const int CharacterRomBytes = 280;
    public const int CharacterCount = 64;
    public const int GlyphRows = 7;
    public const int GlyphColumns = 5;
    public const int GlyphTop = 3;
    public const int RawFontBytesPerRow = 8;
    public const int RawFontBottomRow = 28;
    public const int RawFontColumnStride = RawFontBytesPerRow * GlyphRows;
    public const int ForegroundGlyphBit = 5;
    public const int ByteBits = 8;
    public const int ByteHighBit = 7;
    public const int BitsPerColorPixel = 2;
    public const int ColorPixelMask = 3;
    public const int ColorSetOffset = 4;
    public const int SemiGraphicsColumns = 2;
    public const int SemiGraphicsRows = 3;
    public const int SemiGraphicsColorShift = 6;
    public const int ColorChannelShift = 8;
    public const int RedChannelShift = 16;
    public const int ColorChannelMask = 255;
    public const int Black = 0x263016;
    public const int Green = 0x30d200;
    public const int Yellow = 0xc1e500;
    public const int Blue = 0x4c3ab4;
    public const int Red = 0x9a3236;
    public const int Buff = 0xbfc8ad;
    public const int Cyan = 0x41af71;
    public const int Magenta = 0xc84ef0;
    public const int Orange = 0xd47f00;
    public const int TextDarkGreen = 0x007c00;
    public const int TextBrightGreen = Green;
    public const int TextDarkOrange = 0x6b2700;
    public const int TextBrightOrange = 0xffb700;
    public static IReadOnlyList<(int Samples, int Rows)> GraphicsGeometry { get; } = Array.AsReadOnly<(int Samples, int Rows)>(
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
