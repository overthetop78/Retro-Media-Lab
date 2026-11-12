namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants;

/// <summary>Six indicateurs documentés par Zilog UM008011-0816, pages 65–70.</summary>
internal static class Z80Flags
{
    internal const byte Carry = 0x01;
    internal const byte Subtract = 0x02;
    internal const byte ParityOverflow = 0x04;
    internal const byte HalfCarry = 0x10;
    internal const byte Zero = 0x40;
    internal const byte Sign = 0x80;
    internal const byte ResultBits = 0x28;
    internal const byte Documented = Carry | Subtract | ParityOverflow | HalfCarry | Zero | Sign;
}
