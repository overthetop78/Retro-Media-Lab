namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Comptes des états du séquenceur 400108-00 ; un compteur normal N émet N+1 octets.</summary>
public static class Acb4000FormatChannelConstants
{
    public const byte GapByte = 0x4e;
    public const byte PreambleByte = 0;
    public const int FirstGapLength = 9;
    public const int InterleavedGapLength = 9;
    public const int SequentialGapLength = 19;
    public const int IdPreambleLength = 12;
    public const int DataPreambleLength = 15;
    public const int PostDataLength = 2;
}
