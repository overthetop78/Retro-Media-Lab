namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants;

public static class ChipSelect80186Constants
{
    public const int UpperRegister = 0xa0;
    public const int LowerRegister = 0xa2;
    public const int PeripheralRegister = 0xa4;
    public const int MiddleRegister = 0xa6;
    public const int MiddleSizeRegister = 0xa8;
    public const ushort ResetUpper = 0xfffb;
    public const ushort UpperFixedBits = 0xc038;
    public const ushort LowerWritableBits = 0x3fff;
    public const ushort BoundaryFixedBits = 0x0038;
    public const ushort MiddleFixedBits = 0x01f8;
    public const ushort MiddleSizeFixedBits = 0x8038;
    public const int BoundaryMask = 0xffc0;
    public const int BoundaryTail = 0x003f;
    public const int BoundaryShift = 4;
    public const int AddressLowBits = 0x000f;
    public const int MiddleBoundaryMask = 0xfe00;
    public const int MiddleSizeMask = 0x7f00;
    public const int MiddleSizeShift = 5;
    public const int MiddleCount = 4;
    public const int PeripheralCount = 7;
    public const int PeripheralBasicCount = 5;
    public const int PeripheralGroupCount = 4;
    public const int PeripheralBlockBytes = 128;
    public const int ExtendedPeripherals = 0x0080;
    public const int PeripheralsInMemory = 0x0040;
    public const int WaitMask = 3;
    public const int UpperWritten = 1;
    public const int LowerWritten = 2;
    public const int PeripheralWritten = 4;
    public const int MiddleWritten = 8;
    public const int MiddleSizeWritten = 16;
    public const int Address1 = 2;
    public const int Address2 = 4;
}
