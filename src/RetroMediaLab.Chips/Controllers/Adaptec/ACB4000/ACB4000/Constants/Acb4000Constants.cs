namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Format CDB et réponses du contrôleur ACB-4000 ; manuel OEM, sections 6 et annexe B.</summary>
public static class Acb4000Constants
{
    public const int DriveCount = 2;
    public const int AddressableUnitCount = 8;
    public const int BufferSize = 1024;
    public const int DefaultBlockSize = 256;
    public const int ShortCdbLength = 6;
    public const int LongCdbLength = 10;
    public const int MaximumShortBlockCount = 256;
    public const int MaximumLongBlockCount = 65536;
    public const int LunShift = 5;
    public const byte ShortAddressMask = 0x1f;
    public const uint MaximumShortAddress = 0x1fffff;
    public const int MinimumSenseLength = 4;
    public const byte SenseAddressValid = 0x80;
    public const byte CheckStatus = 2;
    public const byte EqualStatus = 4;
    public const byte BusyStatus = 8;
    public const byte CommandCompleteMessage = 0;
    public const byte NoSense = 0;
    public const byte WriteFault = 3;
    public const byte NotReady = 4;
    public const byte UncorrectableData = 0x11;
    public const byte VerifyDataCheck = 0x19;
    public const byte DataCheckReported = 0x18;
    public const byte BlownFormat = 0x1c;
    public const byte InvalidCommand = 0x20;
    public const byte IllegalAddress = 0x21;
    public const byte BadArgument = 0x24;
    public const byte InvalidUnit = 0x25;
    public const int DataReadAttempts = 6;
    public const int CapacityResponseLength = 8;
    public const int CapacityBlockSizeOffset = 4;
    public const int OpcodeOffset = 0;
    public const int UnitOffset = 1;
    public const int ShortAddressMiddleOffset = 2;
    public const int ShortAddressLowOffset = 3;
    public const int ShortCountOffset = 4;
    public const int LongAddressOffset = 2;
    public const int LongReservedOffset = 6;
    public const int LongCountOffset = 7;
    public const int CapacityPmiOffset = 8;
    public const int CommandGroupShift = 5;
    public const int LongCommandGroup = 1;
    public const int AddressHighShift = 16;
    public const int AddressMiddleShift = 8;
    public const int SenseCodeOffset = 0;
    public const int SenseUnitAddressOffset = 1;
    public const int SenseAddressMiddleOffset = 2;
    public const int SenseAddressLowOffset = 3;
}
