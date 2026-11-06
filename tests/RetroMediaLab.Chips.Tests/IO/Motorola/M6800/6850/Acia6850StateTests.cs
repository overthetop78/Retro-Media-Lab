using System.Text.Json;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Contracts;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

public sealed class Acia6850StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplexTransfersAndModemAcknowledgementsContinueWithTheSameSignals(bool internalTransmitClock)
    {
        for (var format = 0; format < 8; format++)
        for (var divider = 0; divider < 3; divider++)
        foreach (var instant in new[] { 0, 1, 7, 15, 16, 31, 63, 127, 159 })
        {
            var trace = new List<string>();
            var chip = Create(trace);
            chip.Write(false, (byte)(0x80 | format << 2 | divider));
            chip.Write(true, 0xa5);
            for (var clock = 0; clock < instant; clock++) Advance(chip, clock, internalTransmitClock);
            var snapshot = JsonSerializer.Deserialize<Acia6850State>(JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState()))!;
            var continuedTrace = new List<string>();
            var continued = Create(continuedTrace);
            continued.RestoreState(snapshot);
            Assert.Empty(continuedTrace);
            trace.Clear();
            for (var clock = instant; clock < instant + 900; clock++)
            {
                Advance(chip, clock, internalTransmitClock);
                Advance(continued, clock, internalTransmitClock);
                Assert.Equal(chip.CaptureState(), continued.CaptureState());
            }
            Assert.Equal(trace, continuedTrace);
            Assert.Equal(chip.Read(false), continued.Read(false));
            Assert.Equal(chip.Read(true), continued.Read(true));
            Assert.Equal(chip.CaptureState(), continued.CaptureState());
        }
    }

    [Fact]
    public void DifferentClockConfigurationIsRejectedWithoutChangingTheChip()
    {
        var chip = Create([]);
        var snapshot = chip.CaptureState();
        Assert.Throws<ArgumentException>(() => chip.RestoreState(snapshot with { SerialClock = 1 }));
        Assert.Equal(snapshot, chip.CaptureState());
    }

    private static Acia6850 Create(List<string> trace) => new(2_000_000, 153_600)
    {
        IrqChanged = value => trace.Add("IRQ:" + value),
        TransmitPinsChanged = (data, request) => trace.Add("TX:" + data + ":" + request),
        ClockDividerChanged = value => trace.Add("DIV:" + value)
    };

    private static void Advance(Acia6850 chip, int clock, bool internalTransmitClock)
    {
        if (clock == 128) chip.Write(true, 0x3c);
        if (clock == 256) chip.SetCts(true);
        if (clock == 320) chip.SetCts(false);
        if (clock == 384) chip.SetDcd(true);
        if (clock == 400) chip.Read(false);
        if (clock == 416) chip.Read(true);
        if (clock == 448) chip.SetDcd(false);
        if (internalTransmitClock) chip.Tick(); else chip.ClockTransmit();
        chip.ClockReceive(chip.TransmitBit);
    }
}
