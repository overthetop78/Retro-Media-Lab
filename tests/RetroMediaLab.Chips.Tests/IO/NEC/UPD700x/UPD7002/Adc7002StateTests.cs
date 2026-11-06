using System.Text.Json;
using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Contracts;
using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;
using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.Tests.IO.NEC.UPD700x.UPD7002;

public sealed class Adc7002StateTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 8)]
    [InlineData(1_000_000, 0)]
    [InlineData(1_000_000, 8)]
    public void ConversionAndDeadlineContinueAtTheSameInstantAfterSerializedRestoration(int oscillator, byte command)
    {
        foreach (var instant in new[] { 0, 1, 3999, 8191, 8192, 16383, 16384, 19999 })
        {
            var edges = new List<bool>();
            var deadlines = new List<long?>();
            var chip = new Adc7002(edges.Add, conversionClockHz: oscillator) { NextClockEventChanged = deadlines.Add };
            chip.SetChannel(0, 0xabcd);
            chip.Write(C.StatusRegister, command);
            Advance(chip, instant / 2, oscillator);
            chip.SetChannel(0, 0x4567);
            Advance(chip, instant - instant / 2, oscillator);
            var snapshot = JsonSerializer.Deserialize<Adc7002State>(JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState()))!;
            var continuedEdges = new List<bool>();
            var continuedDeadlines = new List<long?>();
            var continued = new Adc7002(continuedEdges.Add, conversionClockHz: oscillator) { NextClockEventChanged = continuedDeadlines.Add };
            continued.RestoreState(snapshot);
            Assert.Empty(continuedEdges);
            Assert.Empty(continuedDeadlines);
            edges.Clear();
            deadlines.Clear();
            for (var step = 0; step < 320; step++)
            {
                if (step == 3) { chip.SetChannel(0, 0xf123); continued.SetChannel(0, 0xf123); }
                Advance(chip, 64, oscillator);
                Advance(continued, 64, oscillator);
                Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState()), JsonSerializer.SerializeToUtf8Bytes(continued.CaptureState()));
            }
            Assert.Equal(edges, continuedEdges);
            Assert.Equal(deadlines, continuedDeadlines);
            Assert.Equal(chip.Read(C.HighRegister), continued.Read(C.HighRegister));
        }
    }

    [Fact]
    public void PendingWriteAndTestModeArePreservedWithoutAliasingChannels()
    {
        var chip = new Adc7002(_ => { });
        chip.SetChannel(0, 0x9876);
        chip.SetBus(false, true, false, C.StatusRegister, 8);
        var snapshot = chip.CaptureState();
        var continued = new Adc7002(_ => { });
        continued.RestoreState(snapshot);
        snapshot.Channels[0] = 0;
        chip.SetBus(true, true, true, C.StatusRegister, 8);
        continued.SetBus(true, true, true, C.StatusRegister, 8);
        chip.AdvanceTo(20_000);
        continued.AdvanceTo(20_000);
        Assert.Equal(chip.Read(C.HighRegister), continued.Read(C.HighRegister));
        chip.Write(C.TestRegister, 0);
        continued.RestoreState(chip.CaptureState());
        Assert.True(continued.TestMode);
        var correct = continued.CaptureState();
        Assert.Throws<ArgumentException>(() => continued.RestoreState(correct with { InputClockFrequency = 1 }));
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(correct), JsonSerializer.SerializeToUtf8Bytes(continued.CaptureState()));
    }

    private static void Advance(Adc7002 chip, int clocks, int oscillator)
    {
        chip.Advance(clocks);
        if (oscillator == 0)
            for (var clock = 0; clock < clocks; clock++) chip.Clock();
    }
}
