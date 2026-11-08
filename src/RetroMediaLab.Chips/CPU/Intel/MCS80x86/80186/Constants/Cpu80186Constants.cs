namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants;

public static class Cpu80186Constants
{
    public const int HalfClocksPerCycle = 2;
    public const int ResetReleaseHalfClocks = 13;
    public const int ResetOutputReleaseHalfClocks = 4;
    public const int Ax = 0;
    public const int Cx = 1;
    public const int Dx = 2;
    public const int Bx = 3;
    public const int Sp = 4;
    public const int Bp = 5;
    public const int Si = 6;
    public const int Di = 7;
    public const int Es = 0;
    public const int Cs = 1;
    public const int Ss = 2;
    public const int Ds = 3;
    public const ushort Carry = 0x0001;
    public const ushort Reserved = 0x0002;
    public const ushort ReservedHigh = 0xf000;
    public const ushort Parity = 0x0004;
    public const ushort Auxiliary = 0x0010;
    public const ushort Zero = 0x0040;
    public const ushort Sign = 0x0080;
    public const ushort Trap = 0x0100;
    public const ushort InterruptEnable = 0x0200;
    public const ushort Direction = 0x0400;
    public const ushort Overflow = 0x0800;
    public const int AddressMask = 0xfffff;
    public const byte DivideVector = 0;
    public const byte SingleStepVector = 1;
    public const byte NmiVector = 2;
    public const byte BoundsVector = 5;
    public const byte InvalidOpcodeVector = 6;
    public const int ShiftCountMask = 31;
    public const int RegisterMask = 7;
    public const int StackWordBytes = 2;
    public const int ModRmOperationShift = 3;
    public const int InterruptCycles = 45;
    public const int HighAccumulatorByte = 4;
    public const int DecimalDigitMask = 0x0f;
    public const int DecimalDigitMaximum = 9;
    public const int DecimalAdjustment = 6;
    public const int DecimalHighDigitAdjustment = 0x60;
    public const int PackedDecimalMaximum = 0x99;
    public const int PrefixWorkLimit = 0x10000;
}
