using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class PipelineTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void GraphicsPipelineDelaysRgbAndYAndSerializesTwelveHalfDots(int colour)
    {
        var fixture = new SignalFixture();
        fixture.Character((byte)(16 + colour));
        fixture.Sample(127);
        fixture.HalfDots(31);
        Assert.False(fixture.Chip.Monochrome);
        fixture.HalfDots(1);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(colour, fixture.Chip.Rgb);
            Assert.True(fixture.Chip.Monochrome);
            Assert.True(fixture.Chip.Blanking);
            fixture.HalfDots(1);
        }
        Assert.False(fixture.Chip.Monochrome);
    }

    [Fact]
    public void F1SamplesDataAtFallingEdgeAndSerializesAlphaInCorrectOrder()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 1;
        var fixture = new SignalFixture(rom);
        fixture.Chip.Data = (byte)'B';
        fixture.Chip.SetCharacterClock(true);
        fixture.Chip.Data = (byte)'A';
        fixture.Chip.SetCharacterClock(false);
        fixture.Chip.Data = (byte)'B';
        fixture.HalfDots(31);
        Assert.False(fixture.Chip.Monochrome);
        fixture.HalfDots(1);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(i >= 10, fixture.Chip.Monochrome);
            fixture.HalfDots(1);
        }
    }

    [Fact]
    public void LoseFallingDoesNotDiscardTheLastCharacterStillInFlight()
    {
        var fixture = new SignalFixture();
        fixture.Character(17);
        fixture.Character(127);
        fixture.Chip.SetLoadOutputShiftEnable(false);
        fixture.HalfDots(20);
        Assert.True(fixture.Chip.Monochrome);
        Assert.Equal(1, fixture.Chip.Rgb);
    }

    [Fact]
    public void CrsChoosesNormalHeightRoundingWithoutChangingTheLineCounter()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 2;
        rom[('A' - 32) * 10 + 1] = 1;
        var even = new SignalFixture(rom);
        var odd = new SignalFixture(rom);
        odd.Chip.CharacterRoundingSelect = false;
        even.Sample((byte)'A'); odd.Sample((byte)'A');
        even.HalfDots(32); odd.HalfDots(32);
        var evenMask = 0;
        var oddMask = 0;
        for (var i = 0; i < 12; i++)
        {
            evenMask = (evenMask << 1) | (even.Chip.Monochrome ? 1 : 0);
            oddMask = (oddMask << 1) | (odd.Chip.Monochrome ? 1 : 0);
            even.HalfDots(1); odd.HalfDots(1);
        }
        Assert.Equal(12, evenMask);
        Assert.Equal(14, oddMask);
        Assert.Equal(0, odd.Chip.CharacterScanline);
    }

    [Fact]
    public void NoDotClockMeansNoAdvanceOfPendingPixels()
    {
        var fixture = new SignalFixture();
        fixture.Sample(17);
        for (var i = 0; i < 100; i++) fixture.Chip.SetDotClock(false);
        Assert.False(fixture.Chip.Monochrome);
        fixture.HalfDots(34);
        Assert.False(fixture.Chip.Monochrome);
    }
    [Fact]
    public void InputJustBeforeLoseRisesIsRetainedUntilTheDelayedWindowOpens()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 31;
        var fixture = new SignalFixture(rom, enable: false);
        fixture.Sample((byte)'A');
        fixture.Chip.SetLoadOutputShiftEnable(true);
        fixture.HalfDots(36);
        Assert.True(fixture.Chip.Monochrome);
    }

    [Fact]
    public void DisplayEnableAndPictureOnGateAnExistingPixelWithoutAnotherClock()
    {
        var fixture = new SignalFixture();
        fixture.Character(17);
        fixture.Sample(127);
        fixture.HalfDots(32);
        Assert.Equal(1, fixture.Chip.Rgb);
        fixture.Chip.DisplayEnabled = false;
        Assert.Equal(0, fixture.Chip.Rgb);
        Assert.False(fixture.Chip.Monochrome);
        fixture.Chip.PictureOn = true;
        Assert.False(fixture.Chip.Blanking);
        fixture.Chip.DisplayEnabled = true;
        Assert.Equal(0, fixture.Chip.Rgb);
        fixture.Chip.PictureOn = false;
        Assert.Equal(1, fixture.Chip.Rgb);
    }

    [Fact]
    public void LoseClosingEdgeHasItsOwnDelayAndGatesAnExistingPixel()
    {
        var fixture = new SignalFixture();
        fixture.Character(17);
        fixture.Character(127);
        fixture.HalfDots(20);
        Assert.True(fixture.Chip.Monochrome);
        fixture.Chip.SetLoadOutputShiftEnable(false);
        fixture.HalfDots(1);
        Assert.True(fixture.Chip.Monochrome);
        fixture.HalfDots(31);
        Assert.False(fixture.Chip.Monochrome);
        Assert.Equal(0, fixture.Chip.Rgb);
    }
    [Fact]
    public void BackgroundRemainsEnabledAfterTheLastDotWhileLoseWindowStaysOpen()
    {
        var fixture = new SignalFixture();
        fixture.Character(17);
        fixture.Character(29);
        fixture.HalfDots(64);
        Assert.False(fixture.Chip.Monochrome);
        Assert.Equal(1, fixture.Chip.Rgb);
        Assert.True(fixture.Chip.Blanking);
    }
    [Fact]
    public void OneCharacterLoseWindowContainsAllTenAlphaHalfDots()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 31;
        var fixture = new SignalFixture(rom);
        fixture.Character((byte)'A');
        fixture.Chip.SetLoadOutputShiftEnable(false);
        fixture.HalfDots(20);
        var mask = 0;
        for (var i = 0; i < 12; i++)
        {
            mask = (mask << 1) | (fixture.Chip.Monochrome ? 1 : 0);
            fixture.HalfDots(1);
        }
        Assert.Equal(0x3ff, mask);
    }
}
