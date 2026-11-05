namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Géométries FORMAT du manuel OEM ACB-4000, tableau A-1.</summary>
public static class Acb4000FormatConstants
{
    public const byte DefaultInterleave = 2;
    public const byte SequentialInterleave = 1;
    public const byte RequiredInterleaveHighByte = 0;
    public const int SmallBlockSequentialSectors = 32;
    public const int SmallBlockInterleavedSectors = 33;
    public const int MediumBlockSequentialSectors = 17;
    public const int MediumBlockInterleavedSectors = 18;
    public const int LargeBlockSectors = 9;
    public const int CommandLength = 6;
    public const byte Opcode = 0x04;
    public const int OptionsOffset = 1;
    public const int FillOffset = 2;
    public const int InterleaveHighOffset = 3;
    public const int InterleaveLowOffset = 4;
    public const int ControlOffset = 5;
    public const int UnitShift = 5;
    public const byte OptionsMask = 0x1f;
    public const byte DefectDataMask = 0x10;
    public const byte CompleteDefectsMask = 0x08;
    public const byte ExtendedOptionsMask = 0x04;
    public const byte UniqueFillMask = 0x02;
    public const byte DefectEncodingMask = 0x01;
    public const byte DefaultFill = 0x6c;
    public const int DefectHeaderLength = 4;
    public const int DefectLengthOffset = 2;
    public const int DefectEntryLength = 8;
    public const int DefectBufferLength = 1024;
    public const int DefectHeadOffset = 3;
    public const int DefectBytePositionOffset = 4;
    public const int CylinderHeadShift = 8;
    // Firmware 400108-00: 022F/0232 and 0DD4–0E51.
    public const int InterleavedRecordOverhead = 0x36;
    public const int SequentialRecordOverhead = 0x40;
    public const uint TrackBytePositionLimit = 0x28b1;
    public const int TrackSlotByteBudget = 0x2842;
    public const byte UnassignedSlot = 0x40;
    public const byte DefectiveSlot = 0x80;
    public const byte LastLogicalSectorMask = 0x40;
    public const byte LogicalSectorMask = 0x3f;
}
