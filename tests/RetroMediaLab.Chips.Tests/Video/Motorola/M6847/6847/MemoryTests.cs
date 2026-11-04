using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class MemoryTests
{
    [Theory]
    [InlineData(0, 32, 12)]
    [InlineData(128, 16, 3)]
    [InlineData(144, 16, 3)]
    [InlineData(160, 32, 3)]
    [InlineData(176, 16, 2)]
    [InlineData(192, 32, 2)]
    [InlineData(208, 16, 1)]
    [InlineData(224, 32, 1)]
    [InlineData(240, 32, 1)]
    public void EachModeRepeatsRowsAndWrapsOnlyItsHorizontalAddressBits(byte mode, int bytes, int repeat)
    {
        var chip = new Mc6847AddressCounter { Mode = (Mc6847Mode)mode };
        chip.BeginField();
        for (var line = 0; line < 192; line++)
        {
            chip.BeginLine();
            var rowBase = line / repeat * bytes;
            var accesses = bytes == 32 ? 42 : 22;
            for (var cell = 0; cell < accesses; cell++)
            {
                Assert.Equal((ushort)(rowBase + cell % bytes), chip.Value);
                Assert.Equal(chip.Value, chip.DrivenAddress);
                chip.AdvanceCell();
            }
            chip.EndActiveLine();
        }
    }

    [Fact]
    public void MemorySelectReleasesOnlyTheDriversWhileCountersKeepRunning()
    {
        var chip = new Mc6847AddressCounter { Mode = (Mc6847Mode)240 };
        for (var cell = 0; cell < 17; cell++) chip.AdvanceCell();
        chip.MemorySelect = false;
        Assert.Null(chip.DrivenAddress);
        chip.EndActiveLine();
        chip.AdvanceCell();
        Assert.Equal((ushort)50, chip.Value);
        Assert.Null(chip.DrivenAddress);
        chip.MemorySelect = true;
        Assert.Equal((ushort)50, chip.DrivenAddress);
    }

    [Fact]
    public void ThirteenBitAddressCarryAndFieldRestartPreserveTheSelectedMode()
    {
        var chip = new Mc6847AddressCounter { Mode = (Mc6847Mode)240 };
        for (var line = 0; line < 256; line++) chip.EndActiveLine();
        Assert.Equal((ushort)0, chip.Value);
        chip.EndActiveLine();
        Assert.Equal((ushort)32, chip.Value);
        chip.MemorySelect = false;
        chip.BeginField();
        Assert.Equal((ushort)0, chip.Value);
        Assert.Null(chip.DrivenAddress);
        Assert.Equal((Mc6847Mode)240, chip.Mode);
        chip.Reset();
        Assert.Equal(Mc6847Mode.None, chip.Mode);
        Assert.Equal((ushort)0, chip.DrivenAddress);
    }

    [Fact]
    public void ReturningToAlphaRestartsTheRowDividerWithoutMovingTheVerticalCounter()
    {
        var chip = new Mc6847AddressCounter { Mode = Mc6847Mode.Graphics };
        chip.EndActiveLine();
        chip.EndActiveLine();
        chip.Mode = Mc6847Mode.None;
        for (var line = 0; line < 11; line++) chip.EndActiveLine();
        Assert.Equal((ushort)0, chip.Value);
        chip.EndActiveLine();
        Assert.Equal((ushort)32, chip.Value);
    }

    private static void Until(Mc6847 chip, int line, int half)
    {
        var remaining = 456 * 262 + 1;
        while ((chip.Scanline != line || chip.HalfClock != half) && --remaining > 0) chip.SetClock(!chip.Clock);
        Assert.True(remaining > 0);
    }

    [Fact]
    public void FullCircuitLoadsEachByteBeforeItsPixelsAndDoesNotReadRamAgainPerDot()
    {
        var ram = new byte[8192];
        ram[0] = 128;
        var reads = new List<ushort>();
        var chip = new Mc6847(new byte[280]) { Mode = (Mc6847Mode)240 };
        chip.DataRequested = () => { var address = chip.Address!.Value; reads.Add(address); chip.Data = ram[address]; };
        Until(chip, 38, 143);
        Assert.Equal(Mc6847Colour.Green, chip.Colour);
        Assert.Equal(2, reads.Count);
        ram[0] = 255;
        for (var dot = 1; dot < 8; dot++)
        {
            chip.SetClock(!chip.Clock);
            Assert.Equal(Mc6847Colour.Black, chip.Colour);
            Assert.Equal(2, reads.Count);
        }
        Until(chip, 38, 455);
        Assert.Equal(41, reads.Count);
        Assert.Equal(Enumerable.Range(0, 41).Select(index => (ushort)(index % 32)), reads);
        Assert.Equal((ushort)9, chip.Address);
    }

    [Fact]
    public void CssChangesAfterLoadingACellKeepBothAlreadySampledCellsUnchanged()
    {
        var chip = new Mc6847(new byte[280]) { Mode = Mc6847Mode.External };
        chip.DataRequested = () => chip.Data = 255;
        Until(chip, 38, 143);
        Assert.Equal(Mc6847Colour.Green, chip.Colour);
        chip.Mode |= Mc6847Mode.ColourSet;
        for (var dot = 0; dot < 16; dot++)
        {
            Assert.Equal(Mc6847Colour.Green, chip.Colour);
            chip.SetClock(!chip.Clock);
        }
        Assert.Equal(Mc6847Colour.BrightOrange, chip.Colour);
    }

    [Fact]
    public void CompatibleGraphicsPairChangesInterpretationWithoutRestartingTheCell()
    {
        var chip = new Mc6847(new byte[280]) { Mode = Mc6847Mode.Graphics };
        chip.DataRequested = () => chip.Data = 128;
        Until(chip, 38, 145);
        Assert.Equal(Mc6847Colour.Blue, chip.Colour);
        chip.Mode |= Mc6847Mode.GraphicsMode0;
        Assert.Equal(Mc6847Colour.Black, chip.Colour);
        Assert.Equal(145, chip.HalfClock);
        Assert.Equal(38, chip.Scanline);
    }

    [Fact]
    public void ReleasedAddressDriversDoNotDisableDataSamplingOrSync()
    {
        var calls = 0;
        var chip = new Mc6847(new byte[280]) { Mode = Mc6847Mode.External, Data = 255, MemorySelect = false };
        chip.DataRequested = () => calls++;
        Until(chip, 38, 143);
        Assert.Null(chip.Address);
        Assert.Equal(0, calls);
        Assert.Equal(Mc6847Colour.Green, chip.Colour);
        Assert.True(chip.HorizontalSync);
        Assert.True(chip.FieldSync);
        chip.MemorySelect = true;
        Assert.NotNull(chip.Address);
    }

    [Fact]
    public void ResetInsideTheBusCallbackDoesNotRecreateTheDiscardedPipeline()
    {
        var chip = new Mc6847(new byte[280]) { Mode = Mc6847Mode.Graphics };
        var callbacks = 0;
        Action read = () => { callbacks++; chip.Reset(); };
        chip.DataRequested = read;
        Until(chip, 38, 126);
        chip.SetClock(!chip.Clock);
        Assert.Equal(1, callbacks);
        Assert.Equal(0, chip.Scanline);
        Assert.Equal(0, chip.HalfClock);
        Assert.False(chip.Active);
        Assert.Equal(Mc6847Mode.None, chip.Mode);
        Assert.Equal((ushort)0, chip.Address);
        Assert.Same(read, chip.DataRequested);
    }
}
