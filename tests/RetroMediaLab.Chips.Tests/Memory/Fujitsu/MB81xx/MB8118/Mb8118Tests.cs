using System.Text.Json;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;

namespace RetroMediaLab.Chips.Tests.Memory.Fujitsu.MB81xx.MB8118;

public sealed class Mb8118Tests
{
    [Fact]
    public void EntireAddressSpaceSupportsEarlyAndLateWritesWithoutAliasing()
    {
        foreach (var timing in new[] { Mb8118Timing.Speed10, Mb8118Timing.Speed12 })
        {
            var bus = new Bus(timing);
            for (int address = 0; address < 16384; address++)
            {
                bool value = ((address ^ (address >> 7)) & 1) != 0;
                bus.Write(address, value, (address & 2) != 0);
                Assert.Equal(value ? LogicPinState.High : LogicPinState.Low, bus.Read(address));
            }
            Assert.True(bus.Chip.IsTimingQualified);
            var state = bus.Chip.CaptureState();
            Assert.All(state.Cells, value => Assert.InRange(value, (byte)1, (byte)2));
            for (int address = 0; address < 16384; address++)
                Assert.Equal(((address ^ (address >> 7)) & 1) != 0 ? 2 : 1, state.Cells[address]);
            Assert.Equal(8, state.StartupCycles);
        }
    }

    [Fact]
    public void PageModeReadModifyWriteAndHiddenRefreshKeepTheOutputLatch()
    {
        var bus = new Bus(Mb8118Timing.Speed12);
        bus.Write(5 | (3 << 7), true, false);
        bus.Write(5 | (4 << 7), false, false);
        long start = bus.Time;
        bus.Set(start, 5, true, true, true);
        bus.Set(start, 5, false, true, true);
        bus.Set(start + 50, 3, false, true, true);
        bus.Set(start + 70, 3, false, false, true);
        bus.Chip.AdvanceTo(start + 134);
        Assert.Equal(LogicPinState.HighImpedance, bus.Chip.Output);
        bus.Chip.AdvanceTo(start + 135);
        Assert.Equal(LogicPinState.High, bus.Chip.Output);
        bus.Set(start + 150, 3, false, false, false, false); // late WE captures DIN, old read latch stays high
        Assert.Equal(LogicPinState.High, bus.Chip.Output);
        bus.Set(start + 220, 3, false, true, true, false);
        bus.Chip.AdvanceTo(start + 270);
        Assert.Equal(LogicPinState.HighImpedance, bus.Chip.Output);
        bus.Set(start + 280, 4, false, true, true);
        bus.Set(start + 290, 4, false, false, true);
        bus.Chip.AdvanceTo(start + 355);
        Assert.Equal(LogicPinState.Low, bus.Chip.Output);
        bus.Set(start + 400, 4, true, false, true); // RAS high leaves output latched
        Assert.Equal(LogicPinState.Low, bus.Chip.Output);
        bus.Set(start + 540, 6, true, false, true);
        bus.Set(start + 550, 6, false, false, true); // hidden refresh, CAS remains low
        bus.Set(start + 700, 6, true, false, true);
        Assert.Equal(LogicPinState.Low, bus.Chip.Output);
        bus.Set(start + 720, 6, true, true, true);
        bus.Chip.AdvanceTo(start + 770);
        Assert.Equal(LogicPinState.HighImpedance, bus.Chip.Output);
        bus.Time = start + 1000;
        Assert.Equal(LogicPinState.Low, bus.Read(5 | (3 << 7)));
        Assert.True(bus.Chip.IsTimingQualified);
    }

    [Fact]
    public void RefreshPreservesDataButExpiredCellsAndInvalidHoldsAreUnknown()
    {
        var bus = new Bus(Mb8118Timing.Speed12);
        int address = 17 | (71 << 7);
        Assert.Equal(LogicPinState.Unknown, bus.Read(address));
        bus.Write(address, true, false);
        for (int repetition = 0; repetition < 4; repetition++)
        {
            bus.Time += 1_000_000;
            bus.Refresh(17);
        }
        Assert.Equal(LogicPinState.High, bus.Read(address));
        bus.Time += 2_000_001;
        bus.Refresh(17);
        Assert.Equal(LogicPinState.Unknown, bus.Read(address));
        bus.Write(address, false, false);
        long start = bus.Time;
        bus.Set(start, 17, true, true, false, true);
        bus.Set(start, 17, false, true, false, true);
        bus.Set(start + 50, 71, false, true, false, true);
        bus.Set(start + 70, 71, false, false, false, true);
        bus.Set(start + 71, 71, false, false, false, false); // DIN hold violated
        Assert.False(bus.Chip.IsTimingQualified);
        Assert.Equal(0, bus.Chip.CaptureState().Cells[address]);
        var uninitialized = new Mb8118(Mb8118Timing.Speed12);
        uninitialized.ReceivePins(0, new(0, true, false, true, true, null));
        Assert.False(uninitialized.IsTimingQualified);
    }

    [Fact]
    public void JsonStateContinuesAnAccessAndRejectsIncompatibleSnapshotsAtomically()
    {
        var bus = new Bus(Mb8118Timing.Speed12);
        bus.Write(0x1234, true, false);
        long start = bus.Time;
        bus.Set(start, 0x34, true, true, true);
        bus.Set(start, 0x34, false, true, true);
        bus.Set(start + 50, 0x24, false, true, true);
        bus.Set(start + 70, 0x24, false, false, true);
        var state = JsonSerializer.Deserialize<Mb8118State>(Snapshot(bus.Chip))!;
        var restored = new Mb8118(Mb8118Timing.Speed12);
        int edges = 0;
        restored.OutputChanged = _ => edges++;
        try
        {
            restored.RestoreState(state);
            Assert.Equal(0, edges);
            foreach (var invalid in new[]
            {
                state with { Version = 2 }, state with { Time = -1 }, state with { Row = 128 },
                state with { Cells = [] }, state with { OutputDue = start },
                state with { Refreshed = [] }, state with { RowActive = false }
            })
            {
                Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                Assert.Equal(Snapshot(bus.Chip), Snapshot(restored));
            }
            Assert.Throws<ArgumentException>(() => new Mb8118(Mb8118Timing.Speed10).RestoreState(state));
            bus.Chip.AdvanceTo(start + 135); restored.AdvanceTo(start + 135);
            Assert.Equal(1, edges);
            Assert.Equal(LogicPinState.High, restored.Output);
            Assert.Equal(Snapshot(bus.Chip), Snapshot(restored));
            state.Cells[0x1234] = 0;
            Assert.Equal(2, restored.CaptureState().Cells[0x1234]);
        }
        finally { restored.OutputChanged = null; }
    }

    private static byte[] Snapshot(Mb8118 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());

    private sealed class Bus
    {
        public Mb8118 Chip { get; }
        public long Time { get; set; } = 200_000;
        public Bus(Mb8118Timing timing)
        {
            Chip = new(timing);
            for (int row = 0; row < 8; row++) Refresh(row);
        }
        public void Set(long time, byte address, bool ras, bool cas, bool we, bool? data = null)
            => Chip.ReceivePins(time, new(address, true, ras, cas, we, data));
        public void Refresh(int row)
        {
            Set(Time, (byte)row, true, true, true);
            Set(Time, (byte)row, false, true, true);
            Set(Time + 150, (byte)row, true, true, true);
            Time += 400;
        }
        public void Write(int address, bool data, bool late)
        {
            byte row = (byte)(address & 127), col = (byte)(address >> 7);
            Set(Time, row, true, true, true, data);
            Set(Time, row, false, true, true, data);
            Set(Time + 50, col, false, true, !late ? false : true, data);
            Set(Time + 70, col, false, false, !late ? false : true, data);
            if (late) Set(Time + 150, col, false, false, false, data);
            Set(Time + 250, col, true, false, false, data);
            Set(Time + 270, col, true, true, true, data);
            Chip.AdvanceTo(Time + 320);
            Time += 500;
        }
        public LogicPinState Read(int address)
        {
            byte row = (byte)(address & 127), col = (byte)(address >> 7);
            Set(Time, row, true, true, true);
            Set(Time, row, false, true, true);
            Set(Time + 50, col, false, true, true);
            Set(Time + 70, col, false, false, true);
            Chip.AdvanceTo(Time + 135);
            var output = Chip.Output;
            Set(Time + 250, col, true, false, true);
            Set(Time + 270, col, true, true, true);
            Chip.AdvanceTo(Time + 320);
            Time += 500;
            return output;
        }
    }
}
