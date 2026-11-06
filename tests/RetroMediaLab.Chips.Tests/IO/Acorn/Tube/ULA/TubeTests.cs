using RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;
using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;
using RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Tube.ULA;
public sealed class TubeTests
{
    [Fact]
    public void TubeNotifiesConnectedInterruptInputsOnlyWhenTheOutputLevelChanges()
    {
        var tube = new TubeUla();
        var host = new List<bool>();
        var parasite = new List<bool>();
        var nmi = new List<bool>();
        tube.HostIrqChanged = host.Add;
        tube.ParasiteIrqChanged = parasite.Add;
        tube.ParasiteNmiChanged = nmi.Add;
        tube.HostWrite(0, 0x83); // Enable host IRQ and parasite R1 IRQ.
        tube.HostWrite(1, 0x5a);
        tube.HostWrite(1, 0x66); // Full receive latch, no additional transition.
        for (var read = 0; read < 10; read++) tube.ParasiteRead(0);
        Assert.Equal(new[] { true }, parasite);
        tube.ParasiteRead(1);
        Assert.Equal(new[] { true, false }, parasite);
        tube.ParasiteWrite(7, 0x12);
        tube.ParasiteWrite(7, 0x34);
        Assert.Equal(new[] { true }, host);
        tube.HostRead(7);
        Assert.Equal(new[] { true, false }, host);
        tube.HostRead(5);
        tube.HostWrite(0, 0x88); // Enable parasite NMI; initial empty R3 requests data.
        Assert.Equal(new[] { true }, nmi);
        tube.ParasiteWrite(5, 0x77);
        Assert.Equal(new[] { true, false }, nmi);
        tube.Reset();
        Assert.False(tube.HostIrq);
        Assert.False(tube.ParasiteIrq);
        Assert.False(tube.ParasiteNmi);
    }

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
