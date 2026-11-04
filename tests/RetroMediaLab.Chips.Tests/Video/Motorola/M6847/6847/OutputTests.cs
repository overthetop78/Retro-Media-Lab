using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class OutputTests
{
    private static void Until(Mc6847 chip, int line, int half)
    {
        var limit = 456 * 262 + 1;
        while ((chip.Scanline != line || chip.HalfClock != half) && --limit > 0) chip.SetClock(!chip.Clock);
        Assert.True(limit > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllEightColourOutputsMatchMotorolasNominalVoltageLevels(bool upperSet)
    {
        int[] y = [540, 420, 650, 650, 420, 540, 540, 540];
        int[] a = [1000, 1500, 1500, 2000, 1500, 1000, 2000, 2000];
        int[] b = [1000, 1000, 2000, 1500, 1500, 1500, 2000, 1000];
        var chip = new Mc6847(new byte[280]) { Mode = Mc6847Mode.Graphics | (upperSet ? Mc6847Mode.ColourSet : 0) };
        chip.DataRequested = () => chip.Data = 0x1b;
        for (var pair = 0; pair < 4; pair++)
        {
            Until(chip, 38, 143 + pair * 4);
            var index = pair + (upperSet ? 4 : 0);
            Assert.Equal((Mc6847Colour)(index + 1), chip.Colour);
            Assert.Equal(y[index], chip.LuminanceMillivolts);
            Assert.Equal(a[index], chip.ChromaAMillivolts);
            Assert.Equal(b[index], chip.ChromaBMillivolts);
            Assert.Equal(1500, chip.ChromaBiasMillivolts);
        }
    }

    [Fact]
    public void SyncBlankingBurstAndBorderRemainSeparateFromFieldSync()
    {
        var chip = new Mc6847(new byte[280]);
        Until(chip, 13, 0);
        Assert.Equal(1000, chip.LuminanceMillivolts);
        Assert.False(chip.HorizontalSync);
        Assert.True(chip.FieldSync);
        Until(chip, 13, 33);
        Assert.Equal(770, chip.LuminanceMillivolts);
        Assert.Equal(1500, chip.ChromaBMillivolts);
        Until(chip, 13, 40);
        Assert.Equal(1250, chip.ChromaBMillivolts);
        Until(chip, 13, 61);
        Assert.Equal(1500, chip.ChromaBMillivolts);
        Until(chip, 13, 84);
        Assert.Equal(720, chip.LuminanceMillivolts);
        Until(chip, 259, 34);
        Assert.True(chip.HorizontalSync);
        Assert.False(chip.FieldSync);
        Assert.Equal(1000, chip.LuminanceMillivolts);
        Until(chip, 259, 423);
        Assert.Equal(770, chip.LuminanceMillivolts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void BurstSuppressionRequiresAllThreeAgCssGm0Inputs(int mode)
    {
        foreach (var graphics in new[] { false, true })
        foreach (var css in new[] { false, true })
        {
            var pins = (Mc6847Mode)(mode * 16) | (graphics ? Mc6847Mode.Graphics : 0) | (css ? Mc6847Mode.ColourSet : 0);
            var chip = new Mc6847(new byte[280]) { Mode = pins };
            Until(chip, 0, 40);
            Assert.Equal(1500, chip.ChromaBMillivolts);
            Until(chip, 13, 40);
            Assert.Equal(graphics && css && mode % 2 == 1 ? 1500 : 1250, chip.ChromaBMillivolts);
        }
    }
    [Fact]
    public void ReapplyingIdenticalPinsPreservesEveryOutputAcrossAField()
    {
        var reference = new Mc6847(new byte[280]);
        var repeated = new Mc6847(new byte[280]);
        reference.DataRequested = () => reference.Data = (byte)reference.Address!.Value;
        repeated.DataRequested = () => repeated.Data = (byte)repeated.Address!.Value;
        for (var edge = 0; edge < 456 * 262; edge++)
        {
            var mode = edge < 456 * 100 ? Mc6847Mode.None
                : Mc6847Mode.Graphics | Mc6847Mode.ColourSet | Mc6847Mode.GraphicsMode0;
            reference.Mode = mode;
            repeated.Mode = mode;
            reference.SetClock(!reference.Clock);
            repeated.SetClock(!repeated.Clock);
            repeated.Mode = mode;
            Assert.Equal(reference.Colour, repeated.Colour);
            Assert.Equal(reference.LuminanceMillivolts, repeated.LuminanceMillivolts);
            Assert.Equal(reference.ChromaAMillivolts, repeated.ChromaAMillivolts);
            Assert.Equal(reference.ChromaBMillivolts, repeated.ChromaBMillivolts);
            Assert.Equal(reference.Address, repeated.Address);
            Assert.Equal(reference.HorizontalSync, repeated.HorizontalSync);
            Assert.Equal(reference.FieldSync, repeated.FieldSync);
            Assert.Equal(reference.RowPreset, repeated.RowPreset);
        }
    }
}
