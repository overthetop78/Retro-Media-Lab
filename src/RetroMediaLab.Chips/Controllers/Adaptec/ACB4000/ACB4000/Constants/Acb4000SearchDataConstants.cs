namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>SEARCH DATA EQUAL : OEM §6.3.2.6 et firmware 17AF–18BC.</summary>
public static class Acb4000SearchDataConstants
{
    public const byte InvertMask = 0x10;
    public const int HeaderLength = 20;
    public const int RecordSizeOffset = 0;
    public const int FirstRecordOffset = 4;
    public const int RecordCountOffset = 8;
    public const int ArgumentLengthOffset = 12;
    public const int DisplacementOffset = 14;
    public const int PatternLengthOffset = 18;
    public const int ArgumentOverhead = 6;
    public const int DataReadAttempts = 3;
}
