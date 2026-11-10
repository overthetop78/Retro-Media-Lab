using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class LineBoundaryTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 0)]
    [InlineData(Crtc6845Model.Mc6845, 1)]
    [InlineData(Crtc6845Model.Mc6845, 3)]
    [InlineData(Crtc6845Model.Hd6845S, 0)]
    [InlineData(Crtc6845Model.Hd6845S, 1)]
    [InlineData(Crtc6845Model.Hd6845S, 3)]
    public void LineBoundaryNeverCrossesAnUnannouncedFieldEnd(Crtc6845Model model, byte mode)
    {
        var chip = CrtcTestFixture.Create(model, adjust: 2, mode: mode);
        for (var line = 0; line < 80; line++)
        {
            var frame = chip.Frame; var scanline = chip.Scanline; var bound = chip.ClocksToLineBoundary;
            Assert.InRange(bound, 1, 256);
            for (var clock = 1; clock < bound; clock++)
            {
                Assert.False(chip.Tick()); Assert.Equal(frame, chip.Frame); Assert.Equal(scanline, chip.Scanline);
            }
            chip.Tick(); Assert.True(chip.Scanline != scanline || chip.Frame != frame);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(255)]
    public void MovingHorizontalTotalBehindTheColumnPreservesTheRealCounterWrap(byte total)
    {
        var chip = CrtcTestFixture.Create(); CrtcTestFixture.Clocks(chip, 7);
        CrtcTestFixture.Write(chip, 0, total);
        var bound = chip.ClocksToLineBoundary; var line = chip.Scanline;
        Assert.Equal(((total - 7) & 255) + 1, bound);
        for (var clock = 1; clock < bound; clock++) { chip.Tick(); Assert.Equal(line, chip.Scanline); }
        chip.Tick(); Assert.Equal(line + 1, chip.Scanline);
    }

    [Fact]
    public void ResetKeepsTheSingleClockFallback()
    {
        var chip = CrtcTestFixture.Create(); chip.SetReset(true);
        Assert.Equal(1, chip.ClocksToLineBoundary); chip.Tick(); Assert.Equal(1, chip.ClocksToLineBoundary);
    }
}
