using RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;
using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;
using RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Tube.ULA;
public sealed class TubeTests
{
    [Fact]
    public void TubeTwoByteTransferKeepsItsRequestUntilTheFifoIsEmpty()
    {
        var tube = new TubeUla();
        tube.HostRead(5); // Octet initial du canal parasite vers hôte.
        tube.HostWrite(0, 0x98); // M et V : NMI et transfert de deux octets.
        tube.ParasiteWrite(5, 0x11);
        tube.ParasiteWrite(5, 0x22);
        Assert.False(tube.ParasiteNmi);
        Assert.Equal(0x11, tube.HostRead(5));
        Assert.False(tube.ParasiteNmi);
        Assert.Equal(0x22, tube.HostRead(5));
        Assert.True(tube.ParasiteNmi);
        tube.ParasiteWrite(5, 0x33);
        tube.ParasiteWrite(5, 0x44);
        tube.HostWrite(5, 0x55);
        Assert.False(tube.ParasiteNmi);
        tube.HostWrite(5, 0x66);
        Assert.True(tube.ParasiteNmi);
        Assert.Equal(0x55, tube.ParasiteRead(5));
        Assert.True(tube.ParasiteNmi);
        Assert.Equal(0x66, tube.ParasiteRead(5));
        Assert.False(tube.ParasiteNmi);
    }
}
