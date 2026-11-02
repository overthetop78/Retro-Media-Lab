namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants;

internal static class ElectronVideoConstants
{
    internal const int Width = 640;
    internal const int Height = 256;
    internal const int PixelBytes = 4;
    internal const int FrameBytes = Width * Height * PixelBytes;
    internal const int CyclesPerLine = 128;
    internal const int LinesPerFrame = 312;
    internal const int CyclesPerFrame = CyclesPerLine * LinesPerFrame;
    internal const int ActiveCycles = 80;
    internal const int PixelsPerCycle = Width / ActiveCycles;
    internal const int ClockInterruptLine = 100;
    internal const int ActiveRasters = 8;
    internal const int SpacedRasters = 10;
    internal const int SpacedHeight = 250;
    internal const int ModeShift = 3;
    internal const int ModeMask = 7;
    internal const int LowBandwidthMask = 4;
    internal const int HighBandwidthColumns = 80;
    internal const int LowBandwidthColumns = 40;
    internal const int DefaultStart = 0x3000;
    internal const int VideoWrapAddress = 0x8000;
    internal const int StartLowMask = 0xe0;
    internal const int StartLowShift = 1;
    internal const int StartHighMask = 0x3f;
    internal const int StartHighShift = 9;
    internal const int StartRetainedHighMask = 0x7e00;
    internal const int StartRetainedLowMask = 0x01ff;
    internal const int PaletteRegisters = 8;
    internal const int PaletteEntries = 16;
    internal const int PalettePairMask = 6;
    internal const int PalettePairShift = 1;
    internal const int ByteBits = 8;
    internal const int ByteMask = 255;
    internal const int OneBitPixel = 1;
    internal const int TwoBitPixel = 2;
    internal const int FourBitPixel = 4;
    internal const int TwoColorMask = 8;
    internal const int FourColorMask = 10;
    internal const int SixteenColorMask = 15;
    internal const int BlueShift = 16;
    internal const int GreenShift = 8;
    internal const int RedComponent = 1;
    internal const int GreenComponent = 2;
    internal const int BlueComponent = 4;
    internal const double FramesPerSecond = (double)ElectronConstants.CpuClock / CyclesPerFrame;
    internal const float AspectRatio = 4f / 3f;

    internal static IReadOnlyList<int> ModeBytes { get; } = Array.AsReadOnly<int>([0x5000, 0x5000, 0x5000, 0x4000, 0x2800, 0x2800, 0x2000, 0x2000]);
    internal static IReadOnlyList<int> ModeHeight { get; } = Array.AsReadOnly<int>([Height, Height, Height, SpacedHeight, Height, Height, SpacedHeight, SpacedHeight]);
    internal static IReadOnlyList<int> ModeRasters { get; } = Array.AsReadOnly<int>([ActiveRasters, ActiveRasters, ActiveRasters, SpacedRasters, ActiveRasters, ActiveRasters, SpacedRasters, SpacedRasters]);
    internal static IReadOnlyList<int> ModeBits { get; } = Array.AsReadOnly<int>([OneBitPixel, TwoBitPixel, FourBitPixel, OneBitPixel, OneBitPixel, TwoBitPixel, OneBitPixel, OneBitPixel]);
    internal static IReadOnlyList<int> PaletteBases { get; } = Array.AsReadOnly<int>([0, 4, 5, 1]);
}
