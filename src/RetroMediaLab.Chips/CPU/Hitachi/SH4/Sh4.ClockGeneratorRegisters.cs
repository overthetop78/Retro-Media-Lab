namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4ClockGeneratorRegisters
{
    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(0xFFC00000, 2, 0x0FFF);
        SetReadWrite(0xFFC00004, 1);
        SetReadWrite(0xFFC00008, 1);
        SetReadWrite(0xFFC0000C, 2, 0xFF);
        SetReadWrite(0xFFC00010, 1, 0x80);
        Reset();
    }
}
