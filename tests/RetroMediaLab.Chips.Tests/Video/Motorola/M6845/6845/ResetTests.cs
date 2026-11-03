using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class ResetTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void ResetPinRetainsRegistersAndForcesCountersAndOutputsLow(Crtc6845Model model)
    {
        var crtc = Create(model);
        NextFrame(crtc); Clocks(crtc, 10);
        var registers = Enumerable.Range(0, 18).Select(i => crtc[i]).ToArray();
        crtc.SetReset(true);
        for (var tick = 0; tick < 64; tick++)
        {
            Assert.False(crtc.Tick());
            Assert.Equal(0, crtc.Column); Assert.Equal(0, crtc.Row);
            Assert.Equal(0, crtc.MemoryAddress); Assert.Equal(0, crtc.RasterAddress);
            Assert.False(crtc.HSync); Assert.False(crtc.VSync); Assert.False(crtc.DisplayEnabled); Assert.False(crtc.Cursor);
            Assert.Equal(registers, Enumerable.Range(0, 18).Select(i => crtc[i]).ToArray());
        }
        crtc.Select(14); crtc.Write(0x2a); Assert.Equal(0x2a, crtc.Read());
        crtc.SetReset(false); crtc.Tick();
        Assert.Equal(1, crtc.Column); Assert.Equal(0x2a, crtc.Read());
    }

    [Fact]
    public void MotorolaSuppressesDisplayUntilFirstCompleteFrameAfterReset()
    {
        var crtc = Create(Crtc6845Model.Mc6845);
        crtc.SetReset(true); crtc.Tick(); crtc.SetReset(false);
        for (var tick = 0; tick < 2000; tick++)
        {
            var frame = crtc.Tick();
            if (frame) { Assert.True(crtc.DisplayEnabled); return; }
            Assert.False(crtc.DisplayEnabled);
        }
        Assert.Fail("No frame after releasing RESET");
    }

    [Fact]
    public void ResetAndLightPenHighSelectDocumentedTestStructureWithoutClearingRegisters()
    {
        var crtc = Create(Crtc6845Model.Mc6845);
        crtc.SetReset(true); crtc.Tick(); crtc.SetLightPen(true);
        for (var tick = 1; tick <= 256; tick++)
        {
            crtc.Tick(); Assert.True(crtc.TestMode);
            Assert.Equal(tick & 127, crtc.MemoryAddress & 127);
            Assert.Equal(tick & 127, crtc.MemoryAddress >> 7);
            Assert.Equal(15, crtc[0]); Assert.False(crtc.DisplayEnabled);
        }
        crtc.SetLightPen(false); crtc.Tick();
        Assert.False(crtc.TestMode); Assert.Equal(0, crtc.MemoryAddress);
    }
}
