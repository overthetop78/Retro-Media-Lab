using System.Text.Json;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Contracts;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InFlightCharactersWindowsAndRemoteInputsContinueIdentically(bool packed)
    {
        foreach (var instant in new[] { 0, 1, 7, 15, 31, 32, 33, 47, 63, 127, 311 })
        {
            var chip = new TeletextSaa5050([]);
            for (var clock = 0; clock < instant; clock++) Advance(chip, clock, packed);
            var state = JsonSerializer.Deserialize<TeletextSaa5050State>(Serialize(chip))!;
            var restored = new TeletextSaa5050([]);
            restored.RestoreState(state);
            for (var clock = instant; clock < instant + 800; clock++)
            {
                Assert.Equal(Advance(chip, clock, packed), Advance(restored, clock, packed));
                Assert.Equal(chip.Rgb, restored.Rgb);
                Assert.Equal(chip.Monochrome, restored.Monochrome);
                Assert.Equal(chip.Blanking, restored.Blanking);
                Assert.Equal(chip.CharacterRow, restored.CharacterRow);
                Assert.Equal(chip.CharacterScanline, restored.CharacterScanline);
                Assert.Equal(chip.FlashField, restored.FlashField);
            }
            Assert.Equal(Serialize(chip), Serialize(restored));
        }
    }

    [Fact]
    public void WrongCharacterRomAndMalformedPipelineAreRejectedWithoutMutation()
    {
        var chip = new TeletextSaa5050([]);
        Advance(chip, 0, false);
        var state = chip.CaptureState();
        var expected = Serialize(chip);
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { CharacterRom = [1] }));
        Assert.Equal(expected, Serialize(chip));
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { Pipeline = [] }));
        Assert.Equal(expected, Serialize(chip));
        chip.RestoreState(state);
        state.Pipeline[0] = new(123, 127, 7, 4, 1);
        state.WindowPipeline[0] = new(456, true);
        Assert.Equal(expected, Serialize(chip));
    }

    private static byte[] Serialize(TeletextSaa5050 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());

    private static ulong Advance(TeletextSaa5050 chip, int clock, bool packed)
    {
        if (clock % 100 == 0) chip.SetLoadOutputShiftEnable(true);
        if (clock % 100 == 80) chip.SetLoadOutputShiftEnable(false);
        if (clock % 12 == 0)
        {
            chip.Data = (byte)(clock % 48 == 0 ? 17 : clock % 48 == 12 ? 13 : 127);
            chip.SetCharacterClock(true);
            chip.SetCharacterClock(false);
        }
        if (clock % 9 == 0)
        {
            chip.RemoteData = (clock / 9 & 1) == 0;
            chip.SetRemoteClock(true);
            chip.SetRemoteClock(false);
        }
        if (clock == 200) chip.PictureOn = true;
        if (clock == 300) chip.CharacterRoundingSelect = false;
        if (clock == 400) chip.SetDataEntryWindow(true);
        if (clock == 403) chip.SetDataEntryWindow(false);
        if (packed) return chip.ClockHalfDots(3);
        chip.SetDotClock((clock & 1) == 0);
        return (ulong)chip.Rgb;
    }
}
