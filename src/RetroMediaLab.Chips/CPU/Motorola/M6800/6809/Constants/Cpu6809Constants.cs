namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants;

public static class Cpu6809Constants
{
    public const byte Carry = 0x01;
    public const byte Overflow = 0x02;
    public const byte Zero = 0x04;
    public const byte Negative = 0x08;
    public const byte IrqMask = 0x10;
    public const byte HalfCarry = 0x20;
    public const byte FirqMask = 0x40;
    public const byte EntireState = 0x80;
    public const ushort SwiThreeVector = 0xfff2;
    public const ushort SwiTwoVector = 0xfff4;
    public const ushort FirqVector = 0xfff6;
    public const ushort IrqVector = 0xfff8;
    public const ushort SwiVector = 0xfffa;
    public const ushort NmiVector = 0xfffc;
    public const ushort ResetVector = 0xfffe;
    public const int ByteBits = 8;
    public const int ByteSign = 0x80;
    public const int WordSign = 0x8000;
    public const int NibbleMask = 0x0f;
    public const int WordHighByte = 0xff00;
    public const int IndexedForm = 0x80;
    public const int IndexedIndirect = 0x10;
    public const int IndexedSmallMask = 0x1f;
    public const int IndexedSmallSign = 0x10;
    public const int RegisterMask = 3;
    public const int RegisterShift = 5;
    public const int StackInstructionBaseCycles = 5;
    public const int StackAddressCycles = 2;
    public const byte FreeRunOpcode = 0xcd;
    public const int ByteRegisterMask = 0x08;
    public const int FirstPrefix = 0x10;
    public const int SecondPrefix = 0x11;
    public const int AddressSpaceSize = 0x10000;
    public const int ResetInternalCycles = 4;
    public const int ResetReleaseInternalCycles = 3;
    public const int ResetVectorAndRecoveryCycles = 3;
    public const int InputClockDivider = 4;
    public const int InterruptClockStages = 2;
    public const int DmaCounterClearCycles = 2;
    public const int DmaRefreshMask = 0x0f;
    public const byte NzvMask = Negative | Zero | Overflow;
    public const string OpcodeFormat = "X2";
}
