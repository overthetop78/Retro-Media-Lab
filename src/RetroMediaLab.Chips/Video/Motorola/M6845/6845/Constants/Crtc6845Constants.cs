namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Constants;

internal static class Crtc6845Constants
{
    internal const int HorizontalTotal = 0;
    internal const int HorizontalDisplayed = 1;
    internal const int HorizontalSyncPosition = 2;
    internal const int VerticalTotal = 4;
    internal const int VerticalAdjust = 5;
    internal const int VerticalDisplayed = 6;
    internal const int VerticalSyncPosition = 7;
    internal const int MaximumRaster = 9;
    internal const int CursorStart = 10;
    internal const int CursorEnd = 11;
    internal const int StartAddressLow = 13;
    internal const int CursorAddressLow = 15;
    internal const int InterlaceSyncMask = 1;
    internal const int InterlaceVideoMask = 3;
    internal const int DisplaySkewShift = 4;
    internal const int CursorSkewShift = 6;
    internal const int SkewMask = 3;
    internal const int SuppressedSkew = 3;
    internal const int OutputHistoryMask = 7;
    internal const int EvenRasterMask = 0x1e;
    internal const int SyncCounterMask = 0x0f;
    internal const int SyncPeriod = 16;
    internal const int InactiveSyncCounter = -1;
    internal const int CursorModeShift = 5;
    internal const int CursorModeMask = 3;
    internal const int CursorOffMode = 1;
    internal const int CursorBlink16Mode = 2;
    internal const int CursorBlink32Mode = 3;
    internal const int CursorBlink16HalfPeriod = 8;
    internal const int CursorBlink32HalfPeriod = 16;
    internal const int LastRasterLatchColumn = 1;
    internal const int TestCounterMask = 0x7f;
    internal const int TestCounterShift = 7;
    internal const int RegisterCount = 18;
    internal const int WritableRegisterCount = 16;
    internal const int RegisterAddressMask = 0x1f;
    internal const int DataMask = 0xff;
    internal const int RowMask = 0x7f;
    internal const int RasterMask = 0x1f;
    internal const int AddressHighMask = 0x3f;
    internal const int MemoryAddressMask = 0x3fff;
    internal const int MotorolaSyncWidthMask = 0x0f;
    internal const int MotorolaModeMask = 0x03;
    internal const int HitachiModeMask = 0xf3;
    internal const int SyncWidth = 3;
    internal const int Mode = 8;
    internal const int StartAddressHigh = 12;
    internal const int CursorAddressHigh = 14;
    internal const int LightPenAddressHigh = 16;
    internal const int LightPenAddressLow = 17;
    internal const int AddressByteShift = 8;
    internal const byte UnreadableRegisterValue = 0;

    internal static ReadOnlySpan<byte> RegisterMasks =>
    [
        DataMask, DataMask, DataMask, DataMask,
        RowMask, RasterMask, RowMask, RowMask,
        HitachiModeMask, RasterMask, RowMask, RasterMask,
        AddressHighMask, DataMask, AddressHighMask, DataMask,
        AddressHighMask, DataMask
    ];
}
