namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4RealTimeClockRegisters
{
    private const uint Control2Address = 0xFFC8003C;

    internal override void Initialize()
    {
        base.Initialize();
        SetReadOnly(0xFFC80000, 1);
        SetReadWrite(0xFFC80004, 1, 0x7F);
        SetReadWrite(0xFFC80008, 1, 0x7F);
        SetReadWrite(0xFFC8000C, 1, 0x3F);
        SetReadWrite(0xFFC80010, 1, 0x07);
        SetReadWrite(0xFFC80014, 1, 0x3F);
        SetReadWrite(0xFFC80018, 1, 0x1F);
        SetReadWrite(0xFFC8001C, 2);
        SetReadWrite(0xFFC80020, 1);
        SetReadWrite(0xFFC80024, 1);
        SetReadWrite(0xFFC80028, 1, 0xBF);
        SetReadWrite(0xFFC8002C, 1, 0x87);
        SetReadWrite(0xFFC80030, 1, 0xBF);
        SetReadWrite(0xFFC80034, 1, 0x9F);
        SetReadWrite(0xFFC80038, 1, 0x99);
        SetReadWrite(Control2Address, 1);
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
        SetValue(Control2Address, 9);
    }
}
