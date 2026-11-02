using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.INS815x.Chip8154;
public sealed class RamIoTests
{
    [Fact]
    public void RamIoBitAddressesSelectBothPortsAndPreserveInputPins()
    {
        var port = new RamIo8154
        {
            ReadPortA = () => 0xa5,
            ReadPortB = () => 0x5a
        };
        port.WriteIo(0x22, 0x0f);
        port.WriteIo(0x23, 0xf0);
        port.WriteIo(0x10, 0);
        port.WriteIo(0x1f, 0);
        Assert.Equal(0xa1, port.ReadIo(0x20));
        Assert.Equal(0x8a, port.ReadIo(0x21));
        Assert.Equal(0x80, port.ReadIo(0));
        Assert.Equal(0x80, port.ReadIo(0x0f));
        port.WriteIo(0, 0xff);
        port.WriteIo(0x0f, 0xff);
        Assert.Equal(0xa0, port.ReadIo(0x20));
        Assert.Equal(0x0a, port.ReadIo(0x21));
        port.WriteRam(0xff, 0x45);
        Assert.Equal(0x45, port.ReadRam(0x7f));
    }
}
