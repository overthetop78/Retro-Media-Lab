namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Constants;

internal static class Cpu6809Constants
{
    internal const byte Carry = 0x01;
    internal const byte Overflow = 0x02;
    internal const byte Zero = 0x04;
    internal const byte Negative = 0x08;
    internal const byte IrqMask = 0x10;
    internal const byte HalfCarry = 0x20;
    internal const byte FirqMask = 0x40;
    internal const byte EntireState = 0x80;
    internal const ushort SwiThreeVector = 0xfff2;
    internal const ushort SwiTwoVector = 0xfff4;
    internal const ushort FirqVector = 0xfff6;
    internal const ushort IrqVector = 0xfff8;
    internal const ushort SwiVector = 0xfffa;
    internal const ushort NmiVector = 0xfffc;
    internal const ushort ResetVector = 0xfffe;
    internal const int ByteBits = 8;
    internal const int ByteSign = 0x80;
    internal const int WordSign = 0x8000;
    internal const int NibbleMask = 0x0f;
    internal const int WordHighByte = 0xff00;
    internal const int IndexedForm = 0x80;
    internal const int IndexedIndirect = 0x10;
    internal const int IndexedSmallMask = 0x1f;
    internal const int IndexedSmallSign = 0x10;
    internal const int RegisterMask = 3;
    internal const int RegisterShift = 5;
    internal const string OpcodeFormat = "X2";
}
