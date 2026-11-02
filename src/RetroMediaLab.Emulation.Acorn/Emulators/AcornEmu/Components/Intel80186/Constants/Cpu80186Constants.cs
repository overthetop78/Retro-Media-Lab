namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants;

internal static class Cpu80186Constants
{
    internal const int Ax = 0;
    internal const int Cx = 1;
    internal const int Dx = 2;
    internal const int Bx = 3;
    internal const int Sp = 4;
    internal const int Bp = 5;
    internal const int Si = 6;
    internal const int Di = 7;
    internal const int Es = 0;
    internal const int Cs = 1;
    internal const int Ss = 2;
    internal const int Ds = 3;
    internal const ushort Carry = 0x0001;
    internal const ushort Reserved = 0x0002;
    internal const ushort ReservedHigh = 0xf000;
    internal const ushort Parity = 0x0004;
    internal const ushort Auxiliary = 0x0010;
    internal const ushort Zero = 0x0040;
    internal const ushort Sign = 0x0080;
    internal const ushort Trap = 0x0100;
    internal const ushort InterruptEnable = 0x0200;
    internal const ushort Direction = 0x0400;
    internal const ushort Overflow = 0x0800;
    internal const int AddressMask = 0xfffff;
    internal const byte DivideVector = 0;
    internal const byte SingleStepVector = 1;
    internal const byte NmiVector = 2;
    internal const byte BoundsVector = 5;
    internal const byte InvalidOpcodeVector = 6;
    internal const int ShiftCountMask = 31;
    internal const int RegisterMask = 7;
    internal const int StackWordBytes = 2;
    internal const int ModRmOperationShift = 3;
    internal const int InterruptCycles = 45;
    internal const int HighAccumulatorByte = 4;
    internal const int DecimalDigitMask = 0x0f;
    internal const int DecimalDigitMaximum = 9;
    internal const int DecimalAdjustment = 6;
    internal const int DecimalHighDigitAdjustment = 0x60;
    internal const int PackedDecimalMaximum = 0x99;
    internal const int PrefixWorkLimit = 0x10000;
}
