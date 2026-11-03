using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class BusTests
{
    [Fact]
    public void WriteSamplesTrailingDataAndHeldEnableDoesNotCommitEarly()
    {
        var crtc = new Crtc6845();
        crtc.SetBus(true, false, false, true, 1);
        crtc.SetBus(true, false, false, false, 14);
        crtc.SetBus(true, true, false, true, 0x12);
        crtc.SetBus(true, true, false, true, 0x23);
        Assert.Equal(0, crtc[14]);
        crtc.SetBus(true, true, false, false, 0x34);
        Assert.Equal(0x34, crtc[14]);
        crtc.SetBus(true, true, false, false, 0);
        Assert.Equal(0x34, crtc[14]);
        Assert.False(crtc.DataBusDriven);
        Assert.Null(crtc.DataBusValue);
        Assert.Equal(0, crtc.Column);
        Assert.Equal(0, crtc.Frame);
    }

    [Fact]
    public void OnlySelectedEnabledReadDrivesBusAndReadDoesNotChangeLatch()
    {
        var crtc = new Crtc6845();
        crtc.Select(14); crtc.Write(0x2a);
        foreach (var selected in new[] { false, true })
        foreach (var read in new[] { false, true })
        foreach (var enable in new[] { false, true })
        {
            crtc.SetBus(false, true, true, false, 0);
            crtc.SetBus(selected, true, read, enable, 0);
            Assert.Equal(selected && read && enable, crtc.DataBusDriven);
            Assert.Equal(selected && read && enable ? (byte?)0x2a : null, crtc.DataBusValue);
        }
        Assert.Equal(0x2a, crtc.Read());
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void ControlChangeDuringEnableCancelsWrite(bool selected, bool registerSelect, bool read)
    {
        var crtc = new Crtc6845();
        crtc.Select(14); crtc.Write(0x2a);
        crtc.SetBus(true, true, false, true, 0);
        crtc.SetBus(selected, registerSelect, read, true, 0);
        crtc.SetBus(true, true, false, false, 0);
        Assert.Equal(0x2a, crtc[14]);
    }

    [Fact]
    public void InitializationCancelsPendingBusWriteAndReleasesDataBus()
    {
        var crtc = new Crtc6845();
        crtc.Select(14);
        crtc.SetBus(true, true, false, true, 0x3f);
        crtc.Reset();
        crtc.SetBus(true, true, false, false, 0x3f);
        Assert.Equal(0, crtc[14]);
        Assert.Null(crtc.DataBusValue);
    }
}
