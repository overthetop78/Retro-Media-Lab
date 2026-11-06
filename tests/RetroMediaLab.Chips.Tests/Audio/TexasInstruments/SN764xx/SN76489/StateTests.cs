using System.Text.Json;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;
using SoundChip = RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services.Sn76489;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TonesNoiseAndPendingBusTransferContinueIdentically(bool groupedClocks)
    {
        for (var noise = 0; noise < 8; noise++)
        foreach (var instant in new[] { 0, 1, 7, 15, 16, 31, 63, 127 })
        {
            var trace = new List<int>();
            var chip = new SoundChip { OutputChanged = trace.Add };
            chip.Write(0x85);
            chip.Write(0x12);
            chip.Write(0xa3);
            chip.Write(0x02);
            chip.Write(0xc7);
            chip.Write(0x01);
            chip.Write((byte)(0xe0 | noise));
            chip.SetBus(0x92, true, true);
            for (var clock = 0; clock < instant; clock++) Advance(chip, clock, groupedClocks);
            var snapshot = JsonSerializer.Deserialize<Sn76489State>(Serialize(chip))!;
            var restoredTrace = new List<int>();
            var restored = new SoundChip { OutputChanged = restoredTrace.Add };
            restoredTrace.Clear();
            restored.RestoreState(snapshot);
            Assert.Empty(restoredTrace);
            trace.Clear();
            for (var clock = instant; clock < instant + 800; clock++)
            {
                Advance(chip, clock, groupedClocks);
                Advance(restored, clock, groupedClocks);
                Assert.Equal(chip.Ready, restored.Ready);
                Assert.Equal(chip.ClocksUntilNextEvent, restored.ClocksUntilNextEvent);
                Assert.Equal(chip.Output, restored.Output);
            }
            Assert.Equal(trace, restoredTrace);
            Assert.Equal(Serialize(chip), Serialize(restored));
        }
    }

    [Fact]
    public void StateArraysAreIndependentAndMalformedStateDoesNotMutateTheChip()
    {
        var chip = new SoundChip();
        var state = chip.CaptureState();
        chip.RestoreState(state);
        var expected = Serialize(chip);
        state.Periods[0] = 123;
        state.Counts[0] = 456;
        state.Amplitudes[0] = 789;
        state.Levels[0] = !state.Levels[0];
        Assert.Equal(expected, Serialize(chip));
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { Counts = [] }));
        Assert.Equal(expected, Serialize(chip));
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { FormatVersion = -1 }));
        Assert.Equal(expected, Serialize(chip));
    }

    private static byte[] Serialize(SoundChip chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());

    private static void Advance(SoundChip chip, int clock, bool grouped)
    {
        if (clock == 37) chip.SetBus(0, false, false);
        if (clock == 81) chip.Write(0x9a);
        if (clock == 201) chip.Write(0xe7);
        if (clock == 301) chip.SetBus(0xb3, true, true);
        if (clock == 380) chip.SetBus(0, false, false);
        if (grouped) chip.AdvanceClocks(3);
        else chip.Tick();
    }
}
