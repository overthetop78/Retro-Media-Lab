namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4UserBreakRegisters
{
    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(0xFF200000, 4);
        SetReadWrite(0xFF200004, 1, 0x0F);
        SetReadWrite(0xFF200008, 2, 0x007F);
        SetReadWrite(0xFF20000C, 4);
        SetReadWrite(0xFF200010, 1, 0x0F);
        SetReadWrite(0xFF200014, 2, 0x007F);
        SetReadWrite(0xFF200018, 4);
        SetReadWrite(0xFF20001C, 4);
        SetReadWrite(0xFF200020, 2, 0xC4C9);
        Reset();
    }
}
