using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using static RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489.Sn76489ToneTests;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489BusTests
{
    [Fact]
    public void ReadyCountsThirtyTwoInputClocksForEveryByteIndependentlyOfAudioPhase()
    {
        var chip = new Sn76489();
        Assert.True(chip.Ready);
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            Clock(chip, value & 15);
            chip.Write((byte)value);
            Assert.False(chip.Ready);
            for (var tick = 0; tick < 31; tick++)
            {
                chip.Tick();
                Assert.False(chip.Ready);
            }
            chip.Tick();
            Assert.True(chip.Ready);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void InactiveChipSelectOrWriteEnableDoesNotLoadTheRegisters(bool selected, bool enabled)
    {
        var chip = new Sn76489();
        var reference = new Sn76489();
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            chip.SetBus((byte)value, selected, enabled);
            for (var clock = 0; clock < 47; clock++)
            {
                chip.Tick();
                reference.Tick();
                Assert.Equal(reference.Output, chip.Output);
            }
        }
    }

    [Fact]
    public void HeldEnableResamplesDataWithoutAnotherWriteEnableEdge()
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, 0, 1023);
        Clock(chip, 16);
        for (var attenuation = 0; attenuation < 16; attenuation++)
        {
            chip.SetBus((byte)(0x90 | attenuation), true, true);
            Clock(chip, 16);
            var expected = attenuation == 15 ? 0
                : (int)Math.Round(short.MaxValue / 4.0 * Math.Pow(10, -attenuation / 10.0));
            Assert.Equal(expected, chip.Output);
        }
    }

    [Fact]
    public void ChipSelectAssertsReadyEvenWithoutAWriteAndOnlyInputClocksReleaseIt()
    {
        var chip = new Sn76489();
        chip.SetBus(0x9f, true, false);
        Assert.False(chip.Ready);
        Clock(chip, 31);
        Assert.False(chip.Ready);
        chip.SetBus(0x9f, true, false);
        Assert.False(chip.Ready); // no new CE edge: no restart.
        chip.Tick();
        Assert.True(chip.Ready);
        chip.SetBus(0x9f, false, false);
        chip.SetBus(0x9f, true, false);
        Assert.False(chip.Ready);
        chip.Reset();
        Assert.True(chip.Ready);
    }

    [Fact]
    public void HeldNoiseCommandKeepsTheShiftRegisterResetUntilDataChanges()
    {
        var chip = new Sn76489();
        Mute(chip);
        chip.Write(0xf0);
        chip.SetBus(0xe4, true, true);
        for (var clock = 0; clock < 40000; clock++)
        {
            chip.Tick();
            Assert.Equal(0, chip.Output);
        }
        // Keep WE active but put attenuation on the bus instead: the noise generator can now run.
        chip.SetBus(0xf0, true, true);
        var sawNonzero = false;
        for (var clock = 0; clock < 40000; clock++)
        {
            chip.Tick();
            sawNonzero |= chip.Output != 0;
        }
        Assert.True(sawNonzero);
    }

    [Fact]
    public void AWriteWhileBusyRestartsTheTransferWithoutQueueingOrStallingAudio()
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, 0, 1);
        chip.Write(0x90);
        Clock(chip, 16);
        Assert.True(chip.Output > 0);
        Assert.False(chip.Ready);
        chip.Write(15);
        Assert.Equal(0, chip.Output);
        Clock(chip, 31);
        Assert.False(chip.Ready);
        chip.Tick();
        Assert.True(chip.Ready);
        Assert.Equal(0, chip.Output);
    }

    [Fact]
    public void ResetRestoresEveryGeneratorAndReadinessDuringATransfer()
    {
        var chip = new Sn76489();
        foreach (var value in new byte[] { 0x8e, 0x3f, 0x94, 0xa1, 0x12, 0xb7, 0xcd, 0x21, 0xd8, 0xe7, 0xf1 })
        {
            chip.Write(value);
            Clock(chip, 153);
        }
        chip.Write(0xff);
        chip.SetBus(0xe4, true, true);
        Assert.False(chip.Ready);
        chip.Reset();
        var fresh = new Sn76489();
        Assert.True(chip.Ready);
        Assert.Equal(0, chip.Output);
        // Volumes return to zero dB, not silence. Full-state reset must reproduce power-on waveform.
        for (var clock = 0; clock < 40000; clock++)
        {
            Assert.Equal(fresh.Output, chip.Output);
            chip.Tick();
            fresh.Tick();
        }
    }

    [Fact]
    public void AStableThirtyTwoClockBusWriteWorksFromEveryDividerPhase()
    {
        for (var phase = 0; phase < 16; phase++)
        {
            var chip = new Sn76489();
            Mute(chip);
            Tone(chip, 0, 1023);
            Clock(chip, 16 + phase);
            chip.SetBus(0x90, true, true);
            Clock(chip, 31);
            Assert.False(chip.Ready);
            Clock(chip, 1);
            Assert.True(chip.Ready);
            Assert.True(chip.Output > 0);
            chip.SetBus(0x9f, false, true);
            Clock(chip, 32);
            Assert.True(chip.Output > 0);
            chip.SetBus(0x9f, true, true);
            Clock(chip, 32);
            Assert.Equal(0, chip.Output);
        }
    }
}
