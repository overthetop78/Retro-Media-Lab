namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4BusStateController
{
    private const uint Bcr1 = 0xFF800000;
    private const uint Bcr2 = 0xFF800004;
    private const uint Wcr1 = 0xFF800008;
    private const uint Wcr2 = 0xFF80000C;
    private const uint Wcr3 = 0xFF800010;
    private const uint Mcr = 0xFF800014;
    private const uint Pcr = 0xFF800018;
    private const uint Rtcsr = 0xFF80001C;
    private const uint Rtcnt = 0xFF800020;
    private const uint Rtcor = 0xFF800024;
    private const uint Rfcr = 0xFF800028;
    private const uint Pctra = 0xFF80002C;
    private const uint Pdtra = 0xFF800030;
    private const uint Pctrb = 0xFF800040;
    private const uint Pdtrb = 0xFF800044;
    private const uint Gpioic = 0xFF800048;

    internal uint PortAData { set => SetValue(Pdtra, value); }

    private void WritePortAArcade(uint address, ushort data)
    {
        SetValue(Pdtra, data);
        _platform.WriteBoardId(data);
    }

    private void WritePortA(uint address, ushort data) => SetValue(Pdtra, data);

    private ushort ReadPortAArcade(uint address) => _platform.ReadBoardId();

    private ushort ReadPortA(uint address)
    {
        uint control = GetValue(Pctra);
        uint data = GetValue(Pdtra);
        ushort result = 0;

        if ((control & 0xF) == 0x8)
            result = 3;
        else if ((control & 0xF) == 0xB)
            result = 3;
        else
            result = 0;

        if ((control & 0xF) == 0xB && (data & 0xF) == 2)
            result = 0;
        else if ((control & 0xF) == 0xC && (data & 0xF) == 2)
            result = 3;

        result |= (ushort)(_platform.Cable << 8);
        return result;
    }

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(Bcr1, 4, 0x033EFFFD);
        SetReadWrite(Bcr2, 2, 0x3FFD);
        SetReadWrite(Wcr1, 4, 0x77777777);
        SetReadWrite(Wcr2, 4, 0xFFFEEFFF);
        SetReadWrite(Wcr3, 4, 0x07777777);
        SetReadWrite(Mcr, 4, 0xF8BBFFFF);
        SetReadWrite(Pcr, 2);
        SetReadWrite(Rtcsr, 2, 0x00FF);
        SetReadWrite(Rtcnt, 2, 0x00FF);
        SetReadWrite(Rtcor, 2, 0x00FF);
        SetReadOnly(Rfcr, 2);
        SetReadWrite(Pctra, 4);
        SetReadWrite(Pdtra, 2);
        SetReadWrite(Pctrb, 4, 0x000000FF);
        SetReadWrite(Pdtrb, 2, 0x000F);
        SetReadWrite(Gpioic, 2);
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
        SetValue(Bcr2, 0x3FFC);
        SetValue(Wcr1, 0x77777777);
        SetValue(Wcr2, 0xFFFEEFFF);
        SetValue(Wcr3, 0x07777777);
        SetValue(Rfcr, 17);

        if (_platform.IsNaomi || _platform.IsSystemSp)
            SetHandlers16(Pdtra, ReadPortAArcade, WritePortAArcade);
        else
            SetHandlers16(Pdtra, ReadPortA, WritePortA);
    }
}
