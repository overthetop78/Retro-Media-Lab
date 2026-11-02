namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants;

internal static class ChipSelect80186Constants
{
    internal const int UpperRegister = 0xa0;
    internal const int LowerRegister = 0xa2;
    internal const int PeripheralRegister = 0xa4;
    internal const int MiddleRegister = 0xa6;
    internal const int MiddleSizeRegister = 0xa8;
    internal const ushort ResetUpper = 0xfffb;
    internal const ushort UpperFixedBits = 0xc038;
    internal const ushort LowerWritableBits = 0x3fff;
    internal const ushort BoundaryFixedBits = 0x0038;
    internal const ushort MiddleFixedBits = 0x01f8;
    internal const ushort MiddleSizeFixedBits = 0x8038;
    internal const int BoundaryMask = 0xffc0;
    internal const int BoundaryTail = 0x003f;
    internal const int BoundaryShift = 4;
    internal const int AddressLowBits = 0x000f;
    internal const int MiddleBoundaryMask = 0xfe00;
    internal const int MiddleSizeMask = 0x7f00;
    internal const int MiddleSizeShift = 5;
    internal const int MiddleCount = 4;
    internal const int PeripheralCount = 7;
    internal const int PeripheralBasicCount = 5;
    internal const int PeripheralGroupCount = 4;
    internal const int PeripheralBlockBytes = 128;
    internal const int ExtendedPeripherals = 0x0080;
    internal const int PeripheralsInMemory = 0x0040;
    internal const int WaitMask = 3;
    internal const int UpperWritten = 1;
    internal const int LowerWritten = 2;
    internal const int PeripheralWritten = 4;
    internal const int MiddleWritten = 8;
    internal const int MiddleSizeWritten = 16;
    internal const int Address1 = 2;
    internal const int Address2 = 4;
}
