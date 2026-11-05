namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Cellules horloge/données du canal MFM de la carte.</summary>
public static class Acb4000MfmConstants
{
    public const int BitsPerByte = 8;
    public const int CellsPerByte = BitsPerByte * 2;
    public const int CellsPerBit = 2;
    public const int FirstClockCell = CellsPerByte - 1;
    public const int FirstDataCell = CellsPerByte - 2;
    public const ushort MissingClockA1 = 0x4489;
    public const byte FirstDataBit = 0x80;
    internal const int DataCellsMask = 0x5555;
    internal const int ClockCellsMask = 0xaaaa;
    internal const int PreviousDataClockMask = 0x8000;
    internal const int PackedPairsMask = 0x3333;
    internal const int PackedNibblesMask = 0x0f0f;
    internal const int ByteMask = 0xff;
    internal const int NibbleBits = BitsPerByte / CellsPerBit;
}
