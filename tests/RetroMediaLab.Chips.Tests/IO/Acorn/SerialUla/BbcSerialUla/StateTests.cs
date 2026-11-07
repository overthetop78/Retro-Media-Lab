using System.Text.Json;
using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Contracts;
using SerialChip = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services.BbcSerialUla;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.SerialUla.BbcSerialUla;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoredSerialAndTapeSignalsContinueAtTheSameClock(bool externalSerial)
    {
        for (var rate = 0; rate < 8; rate++)
        foreach (var instant in new[] { 0, 1, 7, 16, 127, 511, 1333 })
        {
            var trace = new List<string>();
            var chip = Create(trace);
            chip.Write((byte)(0x80 | (externalSerial ? 0x40 : 0) | rate | rate << 3));
            for (var clock = 0; clock < instant; clock++) Advance(chip, clock);
            var snapshot = JsonSerializer.Deserialize<BbcSerialUlaState>(JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState()))!;
            var restoredTrace = new List<string>();
            var restored = Create(restoredTrace);
            restored.RestoreState(snapshot);
            Assert.Empty(restoredTrace);
            trace.Clear();
            for (var clock = instant; clock < instant + 4000; clock++)
            {
                Advance(chip, clock);
                Advance(restored, clock);
                Assert.Equal(chip.CaptureState(), restored.CaptureState());
                Assert.Equal(chip.TapeOutput, restored.TapeOutput);
                Assert.Equal(chip.SourceClockPhase, restored.SourceClockPhase);
            }
            Assert.Equal(trace, restoredTrace);
        }
    }

    [Fact]
    public void IncompatibleClockIsRejectedBeforeMutation()
    {
        var chip = Create([]);
        chip.Tick();
        var state = chip.CaptureState();
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { ClockDenominator = 1 }));
        Assert.Equal(state, chip.CaptureState());
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { FormatVersion = -1 }));
        Assert.Equal(state, chip.CaptureState());
    }

    private static SerialChip Create(List<string> trace) => new(2_000_000)
    {
        SerialPinsChanged = (cts, dcd, data, selected) => trace.Add($"P:{cts}:{dcd}:{data}:{selected}"),
        SerialClocksRaised = (tx, rx, data) => trace.Add($"C:{tx}:{rx}:{data}")
    };

    private static void Advance(SerialChip chip, int clock)
    {
        if (clock % 47 == 0) chip.SetTransmitPins((clock / 47 & 1) == 0, false);
        if (clock % 417 == 0) chip.SetTapeInput((clock / 417 & 1) == 0);
        if (clock % 257 == 0) chip.Rs423ReceiveData = (clock / 257 & 1) == 0;
        if (clock % 809 == 0) chip.Rs423ClearToSendHigh = (clock / 809 & 1) == 0;
        if (clock == 2900) chip.SetTapeInput(null);
        chip.Tick();
    }
}
