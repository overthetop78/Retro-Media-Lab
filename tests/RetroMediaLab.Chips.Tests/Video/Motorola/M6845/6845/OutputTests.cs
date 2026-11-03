using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class OutputTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void HorizontalPulseContinuesAcrossLineBoundary(Crtc6845Model model)
    {
        var crtc = Create(model, total: 7, displayed: 4);
        Write(crtc, 2, 6); Write(crtc, 3, 0x24);
        NextFrame(crtc);
        for (var sample = 0; sample < 24; sample++)
        {
            Assert.Equal(crtc.Column is 6 or 7 or 0 or 1, crtc.HSync);
            crtc.Tick();
        }
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 0)]
    [InlineData(Crtc6845Model.Hd6845S, 16)]
    public void HorizontalZeroWidthDistinguishesMotorolaFromHitachi(Crtc6845Model model, int expected)
    {
        var crtc = Create(model, total: 31);
        Write(crtc, 2, 12); Write(crtc, 3, 0x20);
        NextFrame(crtc);
        var active = 0;
        for (var tick = 0; tick < 32; tick++) { if (crtc.HSync) active++; crtc.Tick(); }
        Assert.Equal(expected, active);
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 16)]
    [InlineData(Crtc6845Model.Hd6845S, 3)]
    public void VerticalWidthUsesVariantSpecificRule(Crtc6845Model model, int lines)
    {
        var crtc = Create(model, total: 15, rows: 31, rasters: 0);
        Write(crtc, 3, 0x33); Write(crtc, 7, 4);
        NextFrame(crtc);
        for (var tick = 0; tick < 64; tick++) crtc.Tick();
        Assert.True(crtc.VSync);
        Clocks(crtc, lines * 16 - 1); Assert.True(crtc.VSync);
        crtc.Tick(); Assert.False(crtc.VSync);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DisplayDelayRetainsMatchingAddressAndSuppression(byte skew)
    {
        var crtc = Create(total: 15, displayed: 4);
        Write(crtc, 8, (byte)(skew << 4));
        NextFrame(crtc);
        for (var column = 0; column < 16; column++)
        {
            var displayed = skew != 3 && column >= skew && column < skew + 4;
            Assert.Equal(displayed, crtc.DisplayEnabled);
            if (displayed) Assert.Equal(0x1234 + column - skew, crtc.Address);
            Assert.Equal(0x1234 + column, crtc.MemoryAddress);
            crtc.Tick();
        }
    }

    [Fact]
    public void CursorDelayIsIndependentFromDisplayDelayAndGatedByDisplay()
    {
        var crtc = Create(total: 15, displayed: 6);
        Write(crtc, 8, 0x90); Write(crtc, 10, 0); Write(crtc, 11, 1);
        Write(crtc, 14, 0x12); Write(crtc, 15, 0x36);
        NextFrame(crtc);
        for (var column = 0; column < 16; column++)
        {
            Assert.Equal(column == 4, crtc.Cursor);
            crtc.Tick();
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0x20, 0)]
    [InlineData(0x40, 8)]
    [InlineData(0x60, 16)]
    public void CursorBlinkUsesSixteenAndThirtyTwoFieldPeriods(byte mode, int halfPeriod)
    {
        var crtc = Create(total: 15, displayed: 8, rows: 3, rasters: 0);
        Write(crtc, 10, mode); Write(crtc, 11, 0);
        Write(crtc, 14, 0x12); Write(crtc, 15, 0x35);
        for (var field = 1; field <= 64; field++)
        {
            NextFrame(crtc); crtc.Tick();
            var expected = mode != 0x20 && (halfPeriod == 0 || (field & halfPeriod) == 0);
            Assert.Equal(expected, crtc.Cursor);
        }
    }

    [Fact]
    public void InterlaceSyncShiftsVerticalPulseByHalfLine()
    {
        var crtc = Create(total: 15, rows: 31, rasters: 0, mode: 1);
        Write(crtc, 3, 0x23); Write(crtc, 7, 4);
        // Le premier R6 termine le champ visible et prépare la synchro impaire qui le suit.
        Clocks(crtc, 64);
        Assert.False(crtc.VSync);
        Clocks(crtc, 8); Assert.True(crtc.VSync);
        Clocks(crtc, 31); Assert.True(crtc.VSync);
        crtc.Tick(); Assert.False(crtc.VSync);
    }
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void AllProgrammedHorizontalAndVerticalPulseWidthsFollowNativeCounters(Crtc6845Model model)
    {
        for (var width = 1; width <= 15; width++)
        {
            var crtc = Create(model, total: 31, rows: 31, rasters: 0);
            Write(crtc, 3, (byte)(width << 4 | width)); Write(crtc, 7, 4);
            NextFrame(crtc);
            var horizontal = 0;
            for (var tick = 0; tick < 32; tick++) { if (crtc.HSync) horizontal++; crtc.Tick(); }
            Assert.Equal(width, horizontal);
            Clocks(crtc, 3 * 32); Assert.True(crtc.VSync);
            var lines = model == Crtc6845Model.Mc6845 ? 16 : width;
            Clocks(crtc, lines * 32 - 1); Assert.True(crtc.VSync);
            crtc.Tick(); Assert.False(crtc.VSync);
        }
        var zero = Create(model, total: 31, rows: 31, rasters: 0);
        Write(zero, 3, 3); Write(zero, 7, 4);
        NextFrame(zero); Clocks(zero, 4 * 32 + 16 * 32 - 1); Assert.True(zero.VSync);
        zero.Tick(); Assert.False(zero.VSync);
    }

    [Fact]
    public void ChangingWidthAfterCounterPassedItWaitsForCounterWrap()
    {
        var crtc = Create(total: 31, rows: 31, rasters: 0);
        Write(crtc, 3, 0x88); Write(crtc, 2, 12); Write(crtc, 7, 4);
        NextFrame(crtc); Clocks(crtc, 15);
        Assert.True(crtc.HSync);
        Write(crtc, 3, 0x82);
        Clocks(crtc, 14); Assert.True(crtc.HSync);
        crtc.Tick(); Assert.False(crtc.HSync);
        Clocks(crtc, 4 * 32 + 3 * 32 - 30);
        Assert.True(crtc.VSync);
        Write(crtc, 3, 0x22);
        Clocks(crtc, 15 * 32 - 1); Assert.True(crtc.VSync);
        crtc.Tick(); Assert.False(crtc.VSync);
    }

    [Fact]
    public void VerticalPulseCanCrossFrameBoundaryWithoutBeingReset()
    {
        var crtc = Create(total: 15, rows: 7, rasters: 0);
        Write(crtc, 3, 0x43); Write(crtc, 7, 6);
        Clocks(crtc, 6 * 16); Assert.True(crtc.VSync);
        Clocks(crtc, 4 * 16 - 1); Assert.Equal(1, crtc.Frame); Assert.True(crtc.VSync);
        crtc.Tick(); Assert.False(crtc.VSync);
    }

    [Fact]
    public void CursorShapeAndSkewSuppressionNeverLeakIntoBlank()
    {
        var crtc = Create(total: 15, rows: 3, rasters: 7);
        Write(crtc, 10, 2); Write(crtc, 11, 4);
        Write(crtc, 14, 0x12); Write(crtc, 15, 0x36);
        NextFrame(crtc);
        for (var raster = 0; raster < 8; raster++)
        for (var column = 0; column < 16; column++)
        {
            Assert.Equal(column == 2 && raster is >= 2 and <= 4, crtc.Cursor);
            crtc.Tick();
        }
        Write(crtc, 8, 0xc0);
        for (var tick = 0; tick < 128; tick++) { Assert.False(crtc.Cursor); crtc.Tick(); }
    }
}
