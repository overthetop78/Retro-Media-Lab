using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class RemoteTests
{
    [Fact]
    public void AllTelevisionCodesSelectTvAndClearSuperimposeAndReveal()
    {
        var chip = new TeletextSaa5050([]);
        for (byte code = 0; code < 64; code++)
        {
            chip.ReceiveRemoteCommand(0x5e);
            chip.ReceiveRemoteCommand(0x47);
            chip.ReceiveRemoteCommand(code);
            Assert.False(chip.TextMode);
            Assert.False(chip.Superimpose);
            Assert.False(chip.Reveal);
        }
    }

    [Fact]
    public void AllViewdataCodesSelectTextAndClearSuperimpose()
    {
        var chip = new TeletextSaa5050([]);
        for (var code = 96; code < 128; code++)
        {
            chip.ReceiveRemoteCommand(0x5e);
            chip.ReceiveRemoteCommand((byte)code);
            Assert.True(chip.TextMode);
            Assert.False(chip.Superimpose);
            Assert.Equal(code is 0x66 or 0x67, chip.Reveal);
        }
    }

    [Fact]
    public void TeletextCommandsPreserveSuperimposeUnlessExplicitlyReset()
    {
        var chip = new TeletextSaa5050([]);
        for (var code = 64; code < 96; code++)
        {
            chip.ReceiveRemoteCommand(0x5e);
            chip.ReceiveRemoteCommand((byte)code);
            Assert.True(chip.TextMode);
            Assert.Equal(code != 0x5f, chip.Superimpose);
            Assert.Equal(code is 0x46 or 0x47, chip.Reveal);
        }
    }

    [Theory]
    [InlineData(0x46)]
    [InlineData(0x66)]
    public void TimedRevealExpiresAtOneHundredAndSixtyMillisecondsAndCanBeRefreshed(byte code)
    {
        var chip = new TeletextSaa5050([]);
        chip.ReceiveRemoteCommand(code);
        for (var i = 0; i < 159999; i++) chip.ClockRemoteTimer();
        Assert.True(chip.Reveal);
        chip.ReceiveRemoteCommand(code);
        for (var i = 0; i < 159999; i++) chip.ClockRemoteTimer();
        Assert.True(chip.Reveal);
        chip.ClockRemoteTimer();
        Assert.False(chip.Reveal);
    }

    [Theory]
    [InlineData(0x47)]
    [InlineData(0x67)]
    public void RevealSetPersistsUntilAnotherCommand(byte code)
    {
        var chip = new TeletextSaa5050([]);
        chip.ReceiveRemoteCommand(code);
        for (var i = 0; i < 320000; i++) chip.ClockRemoteTimer();
        Assert.True(chip.Reveal);
        chip.ReceiveRemoteCommand(0x5f);
        Assert.False(chip.Reveal);
    }

    [Fact]
    public void RevealExposesConcealedGlyphWithoutChangingItsColourOrAttribute()
    {
        var rom = new byte[960];
        rom[('A' - 32) * 10] = 31;
        var chip = new TeletextSaa5050(rom);
        chip.BeginLine(0);
        chip.Character(2, 0, 0);
        chip.Character(24, 0, 0);
        Assert.Equal((0, 2, 0), chip.Character('A', 0, 0));
        chip.ReceiveRemoteCommand(0x47);
        Assert.Equal((0x3ff, 2, 0), chip.Character('A', 0, 0));
        chip.ReceiveRemoteCommand(0x5f);
        Assert.Equal((0, 2, 0), chip.Character('A', 0, 0));
    }

    [Fact]
    public void ResetReturnsToTelevisionWithoutRevealOrSuperimpose()
    {
        var chip = new TeletextSaa5050([]);
        chip.ReceiveRemoteCommand(0x5e);
        chip.ReceiveRemoteCommand(0x47);
        chip.Reset();
        Assert.False(chip.TextMode);
        Assert.False(chip.Superimpose);
        Assert.False(chip.Reveal);
    }
}
