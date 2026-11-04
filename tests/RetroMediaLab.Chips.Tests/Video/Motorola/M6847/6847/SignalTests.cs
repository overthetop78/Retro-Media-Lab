using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class SignalTests
{
    [Fact]
    public void EveryLineMatchesTheNominalSyncWindowAndActiveDotBoundaries()
    {
        var chip = new Mc6847Timing();
        var syncLow = 0;
        var fieldLow = 0;
        var active = 0;
        for (var line = 0; line < 262; line++)
        for (var half = 0; half < 456; half++)
        {
            Assert.Equal(line, chip.Scanline);
            Assert.Equal(half, chip.HalfClock);
            Assert.Equal(half >= 33, chip.HorizontalSync);
            Assert.Equal(line < 230, chip.FieldSync);
            Assert.Equal(line is >= 38 and < 230 && half is >= 143 and < 399, chip.Active);
            if (!chip.HorizontalSync) syncLow++;
            if (!chip.FieldSync) fieldLow++;
            if (chip.Active) active++;
            chip.SetClock(!chip.Clock);
        }
        Assert.Equal(262 * 33, syncLow);
        Assert.Equal(32 * 456, fieldLow);
        Assert.Equal(256 * 192, active);
    }

    [Fact]
    public void RowPresetStartsAfterTheFirstTwelveActiveLinesAndHasSevenHalfClocks()
    {
        var chip = new Mc6847Timing();
        var pulseLines = new List<int>();
        var lowCount = 0;
        for (var edge = 0; edge < 456 * 262; edge++)
        {
            if (!chip.RowPreset)
            {
                if (chip.HalfClock == 48) pulseLines.Add(chip.Scanline);
                Assert.InRange(chip.HalfClock, 48, 54);
                Assert.Equal(0, chip.CharacterRow);
                lowCount++;
            }
            if (chip.ActiveLine) Assert.Equal((chip.Scanline - 38) % 12, chip.CharacterRow);
            chip.SetClock(!chip.Clock);
        }
        Assert.Equal(Enumerable.Range(1, 16).Select(row => 38 + row * 12), pulseLines);
        Assert.Equal(16 * 7, lowCount);
    }

    [Fact]
    public void GraphicsSuppressesPresetAndReturningToAlphaPresetsOnlyItsRowCounter()
    {
        var chip = new Mc6847Timing { Graphics = true };
        for (var edge = 0; edge < 456 * 52 + 50; edge++)
        {
            Assert.True(chip.RowPreset);
            chip.SetClock(!chip.Clock);
        }
        Assert.Equal(2, chip.CharacterRow);
        Assert.Equal(52, chip.Scanline);
        Assert.Equal(50, chip.HalfClock);
        chip.Graphics = false;
        Assert.Equal(0, chip.CharacterRow);
        Assert.Equal(52, chip.Scanline);
        Assert.Equal(50, chip.HalfClock);
        Assert.True(chip.RowPreset);
        chip.Graphics = false;
        for (var edge = 0; edge < 456; edge++) chip.SetClock(!chip.Clock);
        Assert.Equal(1, chip.CharacterRow);
    }
}
