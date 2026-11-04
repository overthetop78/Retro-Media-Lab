using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class OutputControlTests
{
    // PO, DE, superposition, boîte : transcription des sept lignes de table 3.
    public static TheoryData<bool, bool, bool, bool, bool, bool> Table => new()
    {
        { true, false, false, false, false, false },
        { true, false, true, true, false, false },
        { false, true, false, false, true, true },
        { false, true, true, true, true, true },
        { false, false, false, false, false, true },
        { false, false, true, true, false, true },
        { true, true, false, false, false, false },
        { true, true, true, false, true, false },
        { true, true, true, true, true, true },
        { true, true, false, true, true, true }
    };

    [Theory]
    [MemberData(nameof(Table))]
    public void VideoOutputsFollowAllRowsOfTableThree(bool po, bool de, bool superimpose,
        bool box, bool enabled, bool blanking)
    {
        var chip = new TeletextSaa5050([]);
        chip.BeginLine(0);
        chip.ReceiveRemoteCommand(superimpose ? (byte)0x5e : (byte)0x5f);
        chip.PictureOn = po;
        chip.DisplayEnabled = de;
        if (box) { chip.Character(11, 0, 0); chip.Character(11, 0, 0); }
        for (var foreground = 0; foreground < 8; foreground++)
            for (var background = 0; background < 8; background++)
            {
                Assert.Equal((enabled ? foreground : 0, enabled, blanking),
                    chip.ResolveVideo(true, foreground, background, true));
                Assert.Equal((enabled && blanking ? background : 0, false, blanking),
                    chip.ResolveVideo(false, foreground, background, true));
            }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutsideDisplayWindowOnlyPictureOnSelectsBlanking(bool po)
    {
        var chip = new TeletextSaa5050([]) { PictureOn = po };
        chip.BeginLine(0);
        chip.ReceiveRemoteCommand(0x5e);
        chip.Character(11, 0, 0); chip.Character(11, 0, 0);
        Assert.Equal((0, false, !po), chip.ResolveVideo(true, 7, 7, false));
    }

    [Fact]
    public void TelevisionOnlyAllowsBoxedHeaderAndSiLowOverridesThatRestriction()
    {
        var chip = new TeletextSaa5050([]) { PictureOn = true };
        foreach (var row in new[] { 0, 1, 24 })
        {
            chip.BeginLine(row);
            chip.Character(11, 0, 0); chip.Character(11, 0, 0);
            Assert.Equal(row == 0 ? (7, true, true) : (0, false, false),
                chip.ResolveVideo(true, 7, 3, true));
        }
        chip.SuperimposeInputHigh = false;
        Assert.True(chip.TextMode);
        Assert.Equal((7, true, true), chip.ResolveVideo(true, 7, 3, true));
        chip.SuperimposeInputHigh = true;
        Assert.False(chip.TextMode);
    }

    [Fact]
    public void BoxingRequiresConsecutiveCodesAndResetsAtEachDisplayLine()
    {
        var chip = new TeletextSaa5050([]);
        chip.BeginLine(0);
        chip.Character(11, 0, 0);
        Assert.False(chip.Boxed);
        chip.Character(32, 0, 0);
        chip.Character(11, 0, 0);
        Assert.False(chip.Boxed);
        chip.Character(11, 0, 0);
        Assert.True(chip.Boxed);
        chip.Character(10, 0, 0);
        Assert.True(chip.Boxed);
        chip.Character(10, 0, 0);
        Assert.False(chip.Boxed);
        chip.Character(11, 0, 0); chip.Character(11, 0, 0);
        chip.BeginLine(0);
        Assert.False(chip.Boxed);
    }

    [Fact]
    public void SuperimposeOutputIsOpenDrainAndRequiresDisplayEnable()
    {
        var chip = new TeletextSaa5050([]);
        Assert.False(chip.SuperimposeDriveLow);
        chip.ReceiveRemoteCommand(0x5e);
        Assert.True(chip.SuperimposeDriveLow);
        chip.DisplayEnabled = false;
        Assert.False(chip.SuperimposeDriveLow);
        chip.DisplayEnabled = true;
        Assert.True(chip.SuperimposeDriveLow);
        chip.ReceiveRemoteCommand(0x5f);
        Assert.False(chip.SuperimposeDriveLow);
    }
}
