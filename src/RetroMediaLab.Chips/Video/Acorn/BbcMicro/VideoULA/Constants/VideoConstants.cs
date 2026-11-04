using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants;

/// <summary>Dimensions du tampon du rendu BBC courant ; elles ne définissent pas toutes les machines Acorn.</summary>
public static class VideoConstants
{
    public const int CpuMasterDots = 8;
    public const int ColourBits = BbcVideoUlaConstants.ColourBits;
    public const int ColourMask = BbcVideoUlaConstants.ColourMask;
    public const int CpuPhaseMask = 1;
    public const int TeletextClockNumerator = 3;
    public const int TeletextClockDenominator = 4;
    public const int TeletextViewportMasterDots = (TeletextViewportHalfDots * TeletextPixelNumerator + TeletextPixelDenominator - 1) / TeletextPixelDenominator - CpuMasterDots;
    public const int MemoryRasterBits = 3;
    public const int MemoryRasterMask = 7;
    public const int BitmapRasterBlankBit = 8;
    public const int MemoryTeletextSelect = 0x2000;
    public const int MemoryWrapSelect = 0x1000;
    public const int MemoryHighShift = 8;
    public const int MemoryHighMask = 15;
    public const int MemoryHighAddressShift = 11;
    public const int MemoryLowMask = 255;
    public const int MemoryTeletextBase = 0x3c00;
    public const int MemoryTopBankBit = 8;
    public const int DefaultScreenWrap = 0x5800;
    public const int PackedPixelBytes = 4;
    public const int Width = 640;
    public const int Height = 256;
    public const int NtscHeight = 200;
    public const int BitmapCellWidth = 8;
    public const int WideBitmapCellWidth = 16;
    public const int TeletextF1PhaseHalfDots = 6;
    public const int TeletextInputLatchHalfDots = 12;
    public const int TeletextWindowDelayHalfDots = Saa5050Constants.GraphicsDelayHalfClocks;
    public const int TeletextViewportHalfDots = TeletextF1PhaseHalfDots + TeletextInputLatchHalfDots + TeletextWindowDelayHalfDots;
    public const int TeletextPixelNumerator = 4;
    public const int TeletextPixelDenominator = 3;
    public const int TeletextRamMask = 0x3ff;
    public const int TeletextSpace = 32;
    private static readonly uint[] ColourPixels = [0xff000000, 0xffff0000, 0xff00ff00, 0xffffff00, 0xff0000ff, 0xffff00ff, 0xff00ffff, 0xffffffff];
    private static readonly uint[] MonochromePixels = [0xff000000, 0xff4c4c4c, 0xff959595, 0xffe1e1e1, 0xff1d1d1d, 0xff696969, 0xffb2b2b2, 0xffffffff];
    public static ReadOnlySpan<uint> PackedColours => ColourPixels;
    public static ReadOnlySpan<uint> PackedMonochrome => MonochromePixels;

}
