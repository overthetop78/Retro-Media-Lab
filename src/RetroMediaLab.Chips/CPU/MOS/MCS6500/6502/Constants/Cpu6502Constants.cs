namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants;

public static class Cpu6502Constants
{
    public const ushort StackBase = 0x0100;
    public const ushort PageMask = 0xff00;
    public const ushort ResetVector = 0xfffc;
    public const ushort NmiVector = 0xfffa;
    public const ushort IrqVector = 0xfffe;
    public const int HighByteShift = 8;
    public const byte UnstableBusMask = 0xee;
    public const ushort JamBusAddress = 0xffff;
}
