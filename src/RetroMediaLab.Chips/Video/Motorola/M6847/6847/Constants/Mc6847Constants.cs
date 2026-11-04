using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants;

internal static class Mc6847Constants
{
    internal const int HalfClocksPerClock = 2;
    internal const int ClocksPerLine = 228;
    internal const int HalfClocksPerLine = ClocksPerLine * HalfClocksPerClock;
    internal const int LinesPerField = 262;
    internal const int HalfClocksPerField = HalfClocksPerLine * LinesPerField;
    internal const int HorizontalSyncEnd = 33;
    internal const int ActiveHalfClockStart = 143;
    internal const int ActiveDots = 256;
    internal const int ActiveHalfClockEnd = ActiveHalfClockStart + ActiveDots;
    internal const int ActiveLineStart = 38;
    internal const int ActiveLines = 192;
    internal const int ActiveLineEnd = ActiveLineStart + ActiveLines;
    internal const int FieldSyncStart = ActiveLineEnd;
    internal const int RowPresetStart = 48;
    internal const int RowPresetLength = 7;
    internal const int RowPresetEnd = RowPresetStart + RowPresetLength;

    internal const int AddressPreparationCells = 2;
    internal const int ShortLineAccesses = 42;
    internal const int LongLineAccesses = 22;
    internal const int BorderHalfClockStart = 84;
    internal const int VerticalBlankLines = 13;
    internal const int RetraceLineStart = 256;
    internal const int VerticalSyncLineStart = 259;
    internal const int EqualisingLines = 3;
    internal const int EqualisingPulseHalfClocks = 17;
    internal const int BurstHalfClockStart = 40;
    internal const int BurstHalfClockLength = 21;
    internal const int BurstHalfClockEnd = BurstHalfClockStart + BurstHalfClockLength;
    internal const int LuminanceSyncMillivolts = 1000;
    internal const int LuminanceBlankMillivolts = 770;
    internal const int LuminanceBlackMillivolts = 720;
    internal const int LuminanceLowMillivolts = 650;
    internal const int LuminanceMediumMillivolts = 540;
    internal const int LuminanceHighMillivolts = 420;
    internal const int ChromaLowMillivolts = 1000;
    internal const int ChromaBiasMillivolts = 1500;
    internal const int ChromaHighMillivolts = 2000;
    internal const int ChromaBurstMillivolts = 1250;
    internal const Mc6847Mode LatchedModeMask = Mc6847Mode.Inverse | Mc6847Mode.External | Mc6847Mode.Semigraphics | Mc6847Mode.ColourSet;
    internal const Mc6847Mode LiveModeMask = Mc6847Mode.Graphics | Mc6847Mode.GraphicsMode0 | Mc6847Mode.GraphicsMode1 | Mc6847Mode.GraphicsMode2;

    internal const int AddressMask = 8191;
    internal const int DataBits = 8;
    internal const int DataHighBit = DataBits - 1;
    internal const int CharacterMask = 63;
    internal const int CharacterHeight = 12;
    internal const int CharacterDots = 8;
    internal const int PackedColourBits = 4;
    internal const int PackedColourMask = 15;
    internal const int GraphicsModeShift = 4;
    internal const int GraphicsModeMask = 7;
    internal const int ResolutionModeBit = 1;
    internal const int ColourPixelBits = 2;
    internal const int ColourBankSize = 4;
    internal const int ShortCellDots = 8;
    internal const int LongCellDots = 16;
    internal const int ShortBytesPerLine = 32;
    internal const int LongBytesPerLine = 16;
    internal const int SemigraphicsBlockWidth = 4;
    internal const int SemigraphicsFourRows = 6;
    internal const int SemigraphicsSixRows = 4;
    internal const int SemigraphicsFourColourShift = 4;
    internal const int SemigraphicsFourColourMask = 7;
    internal const int SemigraphicsSixColourShift = 6;
    internal const int BlocksPerRow = 2;

    internal static ReadOnlySpan<byte> GraphicsRowRepeats => [3, 3, 3, 2, 2, 1, 1, 1];
}
