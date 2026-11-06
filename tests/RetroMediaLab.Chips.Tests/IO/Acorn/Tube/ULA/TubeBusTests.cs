using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Tube.ULA;

public sealed class TubeBusTests
{
    [Fact]
    public void HostClockCapturesOneTransferAtTheFallingEdgeEvenWithSimultaneousDeselection()
    {
        var tube = new TubeUla();
        tube.SetHostBus(true, true, false, 1, 0x11);
        Assert.Equal(0, tube.ParasiteRead(0) & 0x80);
        tube.SetHostBus(true, true, false, 1, 0x22);
        tube.SetHostBus(false, false, false, 7, 0x99);
        Assert.Equal(0x22, tube.ParasiteRead(1));
        tube.SetHostBus(false, false, false, 7, 0x99);
        Assert.Equal(0, tube.ParasiteRead(6) & 0x80);
    }

    [Fact]
    public void HostReadPresentsDataDuringPhi2AndConsumesOnlyAtItsEnd()
    {
        var tube = new TubeUla();
        tube.ParasiteWrite(1, 0x33);
        tube.ParasiteWrite(1, 0x44);
        tube.SetHostBus(true, true, true, 1, 0);
        Assert.Equal(0x33, tube.HostData);
        Assert.Equal(0x33, tube.HostData);
        tube.SetHostBus(false, false, true, 0, 0);
        Assert.Equal(0x33, tube.HostData);
        Assert.Equal(0x44, tube.HostRead(1));
    }

    [Fact]
    public void ParasiteStrobesDoNotRequireAHostClockAndReleaseCapturesExactlyOnce()
    {
        var tube = new TubeUla();
        tube.SetParasiteBus(true, true, false, false, 3, 0x55);
        Assert.Equal(0, tube.HostRead(2) & 0x80);
        tube.SetParasiteBus(false, true, true, false, 0, 0);
        Assert.Equal(0x55, tube.HostRead(3));
        tube.HostWrite(3, 0x66);
        tube.SetParasiteBus(true, false, true, false, 3, 0);
        Assert.Equal(0x66, tube.ParasiteData);
        Assert.Equal(0x80, tube.ParasiteRead(2) & 0x80);
        tube.SetParasiteBus(false, true, true, false, 0, 0);
        Assert.Equal(0, tube.ParasiteRead(2) & 0x80);
    }

    [Fact]
    public void DmaAcknowledgementSelectsRegisterThreeAndReversesMemoryStrobes()
    {
        var tube = new TubeUla();
        tube.HostRead(5);
        tube.HostWrite(5, 0x77);
        tube.SetParasiteBus(false, true, false, true, 0, 0);
        Assert.Equal(0x77, tube.ParasiteData);
        tube.SetParasiteBus(false, true, true, false, 0, 0);
        Assert.Equal(0x40, tube.HostRead(4) & 0x40);
        tube.SetParasiteBus(false, false, true, true, 7, 0x88);
        tube.SetParasiteBus(false, true, true, false, 0, 0);
        Assert.Equal(0x88, tube.HostRead(5));
        Assert.Equal(0, tube.HostRead(6) & 0x80);
    }

    [Fact]
    public void UnselectedStrobesHaveNoEffectsAndResetCancelsPendingTransfers()
    {
        var tube = new TubeUla();
        tube.SetParasiteBus(false, true, false, false, 7, 0x99);
        tube.SetParasiteBus(false, true, true, false, 7, 0x99);
        Assert.Equal(0, tube.HostRead(6) & 0x80);
        tube.SetHostBus(true, true, false, 1, 0x11);
        tube.SetParasiteBus(true, true, false, false, 7, 0x22);
        tube.SetHostReset(true);
        tube.SetHostReset(false);
        tube.SetHostBus(false, false, false, 0, 0);
        tube.SetParasiteBus(false, true, true, false, 0, 0);
        Assert.Equal(0, tube.ParasiteRead(0) & 0x80);
        Assert.Equal(0, tube.HostRead(6) & 0x80);
    }
}
