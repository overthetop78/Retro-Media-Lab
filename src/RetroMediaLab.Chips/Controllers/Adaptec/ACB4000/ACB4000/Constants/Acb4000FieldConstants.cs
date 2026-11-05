namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Champs physiques émis par le séquenceur du firmware 400108-00.</summary>
public static class Acb4000FieldConstants
{
    public const byte SyncByte = 0xa1;
    public const byte IdMark = 0xfe;
    public const byte DataMark = 0xf8;
    public const int SyncOffset = 0;
    public const int MarkOffset = 1;
    public const int AddressOffset = 2;
    public const int AddressByteCount = 3;
    public const int FlagsOffset = 5;
    public const int IdContentLength = 6;
    public const int IdRecordLength = IdContentLength + Acb4000ErrorCheckConstants.CheckByteCount;
    public const int DataOffset = 2;
    public const int DataOverhead = DataOffset + Acb4000ErrorCheckConstants.CheckByteCount;
    public const uint DefectiveAddressMask = 0x800000;
    public const uint LogicalAddressMask = 0x7fffff;
    public const byte LastSectorMask = 0x80;
    public const byte DefectCountMask = 0x7f;
}
