using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class ClockTests
{
    [Fact]
    public void HeldClockLevelsDoNotAdvanceAndBothEdgesEmitHalfPeriods()
    {
        var chip = new Mc6847Timing();
        chip.SetClock(false);
        Assert.Equal(0, chip.HalfClock);
        chip.SetClock(true);
        Assert.Equal(1, chip.HalfClock);
        for (var repeat = 0; repeat < 20; repeat++) chip.SetClock(true);
        Assert.Equal(1, chip.HalfClock);
        chip.SetClock(false);
        Assert.Equal(2, chip.HalfClock);
        for (var edge = 2; edge < 456; edge++) chip.SetClock(!chip.Clock);
        Assert.Equal(0, chip.HalfClock);
        Assert.Equal(1, chip.Scanline);
        Assert.Equal(0, chip.Field);
    }

    [Fact]
    public void CompleteFieldsPreserveTheFullClockRemainder()
    {
        var chip = new Mc6847Timing();
        for (var field = 0; field < 3; field++)
        {
            for (var edge = 0; edge < 456 * 262 - 1; edge++) chip.SetClock(!chip.Clock);
            Assert.Equal(455, chip.HalfClock);
            Assert.Equal(261, chip.Scanline);
            Assert.Equal(field, chip.Field);
            chip.SetClock(!chip.Clock);
            Assert.Equal(0, chip.HalfClock);
            Assert.Equal(0, chip.Scanline);
            Assert.Equal(field + 1, chip.Field);
        }
    }

    [Fact]
    public void SoftwareInitialisationRestoresTheChosenPowerOnOrigin()
    {
        var chip = new Mc6847Timing { Graphics = true };
        for (var edge = 0; edge < 456 * 263 + 117; edge++) chip.SetClock(!chip.Clock);
        chip.Reset();
        Assert.False(chip.Clock);
        Assert.False(chip.Graphics);
        Assert.Equal(0, chip.HalfClock);
        Assert.Equal(0, chip.Scanline);
        Assert.Equal(0, chip.Field);
        Assert.Equal(0, chip.CharacterRow);
        Assert.True(chip.FieldSync);
        Assert.False(chip.HorizontalSync);
        Assert.True(chip.RowPreset);
    }
}
