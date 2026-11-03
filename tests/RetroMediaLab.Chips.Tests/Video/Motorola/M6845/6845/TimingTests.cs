using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class TimingTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void CharacterRasterRowAndAdjustPeriodsFollowProgrammedCounts(Crtc6845Model model)
    {
        foreach (byte raster in new byte[] { 0, 1, 7, 30, 31 })
        foreach (byte adjust in new byte[] { 0, 1, 2, 31 })
        {
            var crtc = Create(model, rasters: raster, adjust: adjust);
            var expected = 16 * (6 * (raster + 1) + adjust);
            Assert.Equal(expected, NextFrame(crtc));
            Assert.Equal(expected, NextFrame(crtc));
            Assert.Equal(0, crtc.Column); Assert.Equal(0, crtc.Row); Assert.Equal(0, crtc.RasterAddress);
            Assert.Equal(0x1234, crtc.MemoryAddress);
        }
    }

    [Fact]
    public void MemoryAddressAdvancesDuringBlankAndRowAddressLatchesAtHorizontalDisplayed()
    {
        var crtc = Create(total: 15, displayed: 4, rows: 3, rasters: 1);
        NextFrame(crtc);
        Clocks(crtc, 6);
        Assert.Equal(0x123a, crtc.MemoryAddress); Assert.False(crtc.DisplayEnabled);
        Write(crtc, 1, 5); // C0 already passed both comparisons: the captured 4 must survive.
        Clocks(crtc, 10);
        Assert.Equal(1, crtc.RasterAddress); Assert.Equal(0x1234, crtc.MemoryAddress);
        Clocks(crtc, 16);
        // R1=5 is seen on the second raster and determines the following character row.
        Assert.Equal(1, crtc.Row); Assert.Equal(0x1239, crtc.MemoryAddress);
    }

    [Fact]
    public void StartAddressWaitsForFrameAndWrapsAtFourteenBits()
    {
        var crtc = Create(displayed: 8, rows: 3, rasters: 0);
        NextFrame(crtc);
        Write(crtc, 12, 0x3f); Write(crtc, 13, 0xfc);
        Assert.Equal(0x1234, crtc.MemoryAddress);
        NextFrame(crtc); Clocks(crtc, 5);
        Assert.Equal(1, crtc.MemoryAddress);
        Clocks(crtc, 11);
        Assert.Equal(4, crtc.MemoryAddress);
    }

    [Fact]
    public void HorizontalCounterMissedEqualityWrapsInsteadOfEndingImmediately()
    {
        var crtc = Create(total: 15);
        NextFrame(crtc); Clocks(crtc, 6); Write(crtc, 0, 3);
        Clocks(crtc, 249);
        Assert.Equal(255, crtc.Column); Assert.Equal(0, crtc.RasterAddress);
        Clocks(crtc, 1); Assert.Equal(0, crtc.Column); Assert.Equal(0, crtc.RasterAddress);
        Clocks(crtc, 4); Assert.Equal(0, crtc.Column); Assert.Equal(1, crtc.RasterAddress);
    }

    [Fact]
    public void HitachiLastRasterLatchSurvivesLaterRasterRewrite()
    {
        var crtc = Create(total: 15, rows: 1, rasters: 0);
        Clocks(crtc, 16 + 3); // Last row, after C0=1 latch and following adjust comparison.
        Write(crtc, 9, 6);
        Assert.Equal(13, NextFrame(crtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void HitachiInterlaceParityAndOddHeightAlternateAcrossRows(byte mode)
    {
        var crtc = Create(rows: 3, rasters: 3, mode: mode);
        NextFrame(crtc);
        var fieldParity = crtc.RasterAddress & 1;
        Clocks(crtc, 16);
        if (mode != 3) { Assert.Equal(1, crtc.RasterAddress); return; }
        Assert.Equal(2 + fieldParity, crtc.RasterAddress);
        for (var count = 0; count < 64 && crtc.Row == 0; count++) crtc.Tick();
        Assert.Equal(1, crtc.Row);
        Assert.Equal(fieldParity ^ 1, crtc.RasterAddress);
    }

    [Fact]
    public void HitachiEvenInterlaceHeightKeepsParityAcrossRows()
    {
        var crtc = Create(rows: 3, rasters: 4, mode: 3);
        NextFrame(crtc);
        var first = crtc.RasterAddress;
        for (var count = 0; count < 64 && crtc.Row == 0; count++) crtc.Tick();
        Assert.Equal(first, crtc.RasterAddress);
    }
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void EveryHorizontalCounterPeriodUsesR0PlusOne(Crtc6845Model model)
    {
        for (var total = 0; total <= byte.MaxValue; total++)
        {
            var crtc = Create(model, total: (byte)total, rows: 3, rasters: 31);
            for (var tick = 1; tick <= total; tick++)
            {
                crtc.Tick(); Assert.Equal(tick, crtc.Column); Assert.Equal(0, crtc.RasterAddress);
            }
            crtc.Tick(); Assert.Equal(0, crtc.Column); Assert.Equal(1, crtc.RasterAddress);
        }
    }

    [Fact]
    public void VerticalCounterMissedEqualityWrapsAtSevenBits()
    {
        var crtc = Create(rows: 10, rasters: 0);
        Clocks(crtc, 5 * 16); Assert.Equal(5, crtc.Row);
        Write(crtc, 4, 3);
        Clocks(crtc, 122 * 16); Assert.Equal(127, crtc.Row); Assert.Equal(0, crtc.Frame);
        Clocks(crtc, 16); Assert.Equal(0, crtc.Row); Assert.Equal(0, crtc.Frame);
        Assert.Equal(4 * 16, NextFrame(crtc));
    }

    [Fact]
    public void HitachiOddInterlaceHeightBalancesThreeAndTwoRastersBetweenRows()
    {
        var crtc = Create(rows: 3, rasters: 3, mode: 3);
        // Field 0, line 0: RA=0,2,4; line 1: RA=1,3. Table 9, Nr+2=5.
        Assert.Equal(0, crtc.RasterAddress);
        Clocks(crtc, 16); Assert.Equal(2, crtc.RasterAddress);
        Clocks(crtc, 16); Assert.Equal(4, crtc.RasterAddress); Assert.Equal(0, crtc.Row);
        Clocks(crtc, 16); Assert.Equal(1, crtc.RasterAddress); Assert.Equal(1, crtc.Row);
        Clocks(crtc, 16); Assert.Equal(3, crtc.RasterAddress);
        Clocks(crtc, 16); Assert.Equal(0, crtc.RasterAddress); Assert.Equal(2, crtc.Row);
    }

    [Fact]
    public void InterlaceExtraRasterAlternatesWithVisibleFieldCounter()
    {
        var crtc = Create(rows: 5, rasters: 1, mode: 1);
        Assert.Equal(13 * 16, NextFrame(crtc));
        Assert.Equal(12 * 16, NextFrame(crtc));
        Assert.Equal(13 * 16, NextFrame(crtc));
        Assert.Equal(12 * 16, NextFrame(crtc));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MotorolaRasterModesKeepFieldParityAcrossVisibleRows(byte mode)
    {
        var crtc = Create(Crtc6845Model.Mc6845, rows: 5, rasters: 3, mode: mode);
        Write(crtc, 6, 4);
        var video = mode == 3;
        var rowClocks = (video ? 2 : 4) * 16;
        Clocks(crtc, 16);
        Assert.Equal(video ? 2 : 1, crtc.RasterAddress);
        Clocks(crtc, rowClocks - 16);
        Assert.Equal(1, crtc.Row); Assert.Equal(0, crtc.RasterAddress);
        NextFrame(crtc);
        Assert.Equal(video ? 1 : 0, crtc.RasterAddress);
        Clocks(crtc, 16);
        Assert.Equal(video ? 3 : 1, crtc.RasterAddress);
        Clocks(crtc, rowClocks - 16);
        Assert.Equal(1, crtc.Row); Assert.Equal(video ? 1 : 0, crtc.RasterAddress);
    }
}
