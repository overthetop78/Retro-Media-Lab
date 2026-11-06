using System.Text.Json;
using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Contracts;
using VideoChip = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services.BbcVideoUla;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PixelsPaletteAndClockPhasesContinueIdentically(bool packed)
    {
        foreach (byte control in new byte[] { 0, 2, 8, 12, 16, 20, 24, 28, 0xff })
        foreach (var instant in new[] { 0, 1, 3, 7, 8, 15, 31 })
        {
            var chip = new VideoChip();
            chip.Write(0, control);
            for (var index = 0; index < 16; index++) chip.Write(1, (byte)(index << 4 | index));
            for (var clock = 0; clock < instant; clock++) Advance(chip, clock, packed);
            var state = JsonSerializer.Deserialize<BbcVideoUlaState>(Serialize(chip))!;
            var restored = new VideoChip();
            restored.RestoreState(state);
            for (var clock = instant; clock < instant + 160; clock++)
            {
                Assert.Equal(Advance(chip, clock, packed), Advance(restored, clock, packed));
                Assert.Equal(chip.Rgb, restored.Rgb);
                Assert.Equal(chip.CrtcClock, restored.CrtcClock);
                Assert.Equal(chip.Clock8, restored.Clock8);
                Assert.Equal(chip.Clock4, restored.Clock4);
                Assert.Equal(chip.Clock2, restored.Clock2);
                Assert.Equal(chip.Clock1, restored.Clock1);
                Assert.Equal(chip.CursorActive, restored.CursorActive);
            }
            Assert.Equal(Serialize(chip), Serialize(restored));
        }
    }

    [Fact]
    public void PaletteCopiesDoNotAliasTheRestoredChip()
    {
        var chip = new VideoChip();
        var state = chip.CaptureState();
        chip.RestoreState(state);
        var expected = Serialize(chip);
        state.Palette[0] ^= 1;
        state.Colours[0] ^= 1;
        state.PackedColours[0] ^= 1;
        Assert.Equal(expected, Serialize(chip));
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { Colours = [] }));
        Assert.Equal(expected, Serialize(chip));
    }

    private static byte[] Serialize(VideoChip chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());

    private static uint Advance(VideoChip chip, int clock, bool packed)
    {
        chip.SetCharacterInputs((byte)(clock * 37), clock % 5 == 0, clock % 11 != 0);
        chip.Inverted = clock % 17 == 0;
        if (clock == 63) chip.Write(1, 0x35);
        if (clock == 90) chip.Write(0, (byte)(chip.Control ^ 3));
        if (packed) return chip.ClockMasterPeriods((uint)(clock * 871));
        chip.TeletextRgb = clock & 7;
        chip.SetMasterClock(false);
        chip.SetMasterClock(true);
        return (uint)chip.Rgb;
    }
}
