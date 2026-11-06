using System.Text.Json;
using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Contracts;
using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SerialFramesAndFifosContinueAfterReinjectingTheSavedState(bool nrzi)
    {
        foreach (var instant in new[] { 1, 17, 41, 63 })
        {
            var chip = Create(nrzi);
            chip.Write(2, 1);
            chip.Write(3, 0xf8);
            Assert.True(chip.ReceiveFrame([1, 2, 0xff, 3]));
            for (var clock = 0; clock < instant; clock++) Advance(chip);
            var state = JsonSerializer.Deserialize<Mc6854State>(Serialize(chip))!;
            var restored = Create(nrzi);
            restored.Tick();
            var trace = new List<string>();
            var restoredTrace = new List<string>();
            Observe(chip, trace);
            Observe(restored, restoredTrace);
            restored.RestoreState(state);
            Assert.Empty(restoredTrace);
            for (var clock = 0; clock < 500; clock++)
            {
                Assert.Equal(Advance(chip), Advance(restored));
                Assert.Equal(chip.TransmitData, restored.TransmitData);
                Assert.Equal(chip.Irq, restored.Irq);
                Assert.Equal(chip.Rts, restored.Rts);
            }
            Assert.Equal(trace, restoredTrace);
            Assert.Equal(Serialize(chip), Serialize(restored));
        }
    }

    [Fact]
    public void ClockMismatchIsRejectedWithoutChangingTheChip()
    {
        var chip = Create(false);
        var before = Serialize(chip);
        Assert.Throws<ArgumentException>(() => chip.RestoreState(chip.CaptureState() with { CyclesPerByte = 1 }));
        Assert.Equal(before, Serialize(chip));
    }

    private static Mc6854 Create(bool nrzi) => AdlcTestBench.Device(
        control1: C.ReceiveInterrupt, control4: nrzi ? C.Nrzi : (byte)0);

    private static void Observe(Mc6854 chip, List<string> trace)
    {
        chip.IrqChanged = level => trace.Add("IRQ:" + level);
        chip.TransmitStateChanged = (rts, transmitting) => trace.Add($"TX:{rts}:{transmitting}");
        chip.FrameTransmitted = frame => trace.Add("FRAME:" + Convert.ToHexString(frame.Span));
    }

    private static int Advance(Mc6854 chip)
    {
        chip.Tick();
        return (chip.Read(1) & C.ReceiveAvailable) != 0 ? chip.Read(2) : -1;
    }

    private static byte[] Serialize(Mc6854 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
