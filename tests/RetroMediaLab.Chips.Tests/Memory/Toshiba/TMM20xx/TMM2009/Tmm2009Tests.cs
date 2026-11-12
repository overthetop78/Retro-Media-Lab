using System.Text.Json;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Toshiba.TMM20xx.TMM2009;

public sealed class Tmm2009Tests
{
    [Fact]
    public void AllAddressesAndByteValuesRoundTripForBothExplicitSelectPolarities()
    {
        foreach (bool ce2High in new[] { false, true })
        {
            var chip = new Tmm2009(ce2High);
            long time = 0;
            for (int address = 0; address < 1024; address++)
            for (int data = 0; data < 256; data++)
            {
                var pins = new Tmm2009Pins((ushort)address, true, false, ce2High, false, false, (byte)data, 255);
                chip.ReceivePins(time++, pins);
                Assert.False(chip.Output.Driven);
                chip.ReceivePins(time++, pins with { WriteHigh = true });
                Assert.Equal(new((byte)data, 255, true), chip.Output);
                chip.ReceivePins(time++, pins with { WriteHigh = true, Ce1High = true });
                Assert.False(chip.Output.Driven);
            }
            Assert.False(chip.ElectricalTimingQualified);
            var state = chip.CaptureState();
            Assert.All(state.Cells, cell => Assert.Equal(255, cell));
            Assert.All(state.Known, known => Assert.Equal(255, known));
        }
    }

    [Fact]
    public void SelectsOutputEnablePartialDataAndEndOfWriteRemainIndependent()
    {
        var chip = new Tmm2009(true);
        var pins = new Tmm2009Pins(37, true, false, true, false, true, 0, 0);
        chip.ReceivePins(0, pins);
        Assert.Equal(new(0, 0, true), chip.Output);
        chip.ReceivePins(1, pins with { WriteHigh = false, Data = 0xa5, DataKnown = 0xf0 });
        chip.ReceivePins(2, pins with { WriteHigh = false, Data = 0x5a, DataKnown = 0xf0 });
        chip.ReceivePins(3, pins with { WriteHigh = false, Ce1High = true }); // CE ends the write
        chip.ReceivePins(4, pins);
        Assert.Equal(new(0x50, 0xf0, true), chip.Output);
        chip.ReceivePins(5, pins with { OutputEnableHigh = true });
        Assert.False(chip.Output.Driven);
        chip.ReceivePins(6, pins with { Ce2High = false });
        Assert.False(chip.Output.Driven);
        chip.ReceivePins(7, pins with { AddressKnown = false });
        Assert.Equal(new(0, 0, true), chip.Output);
    }

    [Fact]
    public void StateDuringWriteResumesWithoutNotificationsAndRejectsBadSnapshots()
    {
        var chip = new Tmm2009(false);
        var pins = new Tmm2009Pins(511, true, false, false, false, false, 0x69, 255);
        chip.ReceivePins(100, pins);
        var state = JsonSerializer.Deserialize<Tmm2009State>(Snapshot(chip))!;
        var restored = new Tmm2009(false);
        int edges = 0;
        restored.OutputChanged = _ => edges++;
        try
        {
            restored.RestoreState(state);
            Assert.Equal(0, edges);
            foreach (var invalid in new[]
            {
                state with { Version = 2 }, state with { Ce2ActiveHigh = true },
                state with { Time = -1 }, state with { Cells = [] }, state with { Writing = false }
            })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(chip), Snapshot(restored));
            }
            chip.ReceivePins(200, pins with { WriteHigh = true });
            restored.ReceivePins(200, pins with { WriteHigh = true });
            Assert.Equal(1, edges);
            Assert.Equal(new(0x69, 255, true), restored.Output);
            Assert.Equal(Snapshot(chip), Snapshot(restored));
            state.Cells[511] = 0;
            Assert.Equal(0x69, restored.CaptureState().Cells[511]);
        }
        finally { restored.OutputChanged = null; }
    }
    private static byte[] Snapshot(Tmm2009 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
