namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants;

/// <summary>Valeurs numériques du VIDPROC BBC d'origine, sans extension VideoNuLA.</summary>
public static class BbcVideoUlaConstants
{
    public const int MasterClockHz = 16000000;
    public const int CpuMasterClocks = 8;
    public const int ColourBits = 3;
    public const int LastCpuPixelShift = (CpuMasterClocks - 1) * ColourBits;
    public const int ClockPhaseMask = 15;
    public const int SlowCharacterPeriod = 16;
    public const int FastCharacterPeriod = 8;
    public const int RegisterSelectMask = 1;
    public const int PaletteEntries = 16;
    public const int PaletteIndexShift = 4;
    public const int PaletteValueMask = 15;
    public const int ColourMask = 7;
    public const int FlashPaletteBit = 8;
    public const int FlashControlBit = 1;
    public const int TeletextControlBit = 2;
    public const int PixelRateShift = 2;
    public const int PixelRateMask = 3;
    public const int FastCrtcBit = 16;
    public const int FirstCursorBit = 128;
    public const int SecondCursorBit = 64;
    public const int LastCursorBit = 32;
    public const int FirstCursorSegment = 1;
    public const int SecondCursorSegment = 2;
    public const int LastCursorSegments = 12;
    public const int HighColourTap = 7;
    public const int RedColourTap = 5;
    public const int GreenColourTap = 3;
    public const int BlueColourTap = 1;
    public const int HighColourShift = 3;
    public const int RedColourShift = 2;
    public const int GreenColourShift = 1;
    public const int ShiftInputBit = 1;
    public const int ShiftDataMask = 255;
    public const int ShiftDataBits = 8;
    public const int Clock8Mask = 1;
    public const int Clock4Mask = 2;
    public const int Clock2Mask = 4;
    public const int Clock1Mask = 8;
    public const int RedMask = 1;
    public const int GreenMask = 2;
    public const int BlueMask = 4;
    private static readonly byte[] ColourTapValues = CreateColourTaps();
    internal static byte GetColourTap(int value) => ColourTapValues[value];

    private static byte[] CreateColourTaps()
    {
        var values = new byte[ShiftDataMask + 1];
        for (var data = 0; data < values.Length; data++)
            values[data] = (byte)((((data >> HighColourTap) & 1) << HighColourShift)
                | (((data >> RedColourTap) & 1) << RedColourShift)
                | (((data >> GreenColourTap) & 1) << GreenColourShift)
                | ((data >> BlueColourTap) & 1));
        return values;
    }
}
