using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74S163;

public sealed class Sn74S163Tests
{
    [Fact]
    public void PeriodicInputIntervalsMatchIndividualSharedEdges()
    {
        for (var seed = 0; seed < 256; seed++)
        {
            byte[] pins = [(byte)seed, (byte)(seed * 73 + 17), (byte)(~seed ^ 0x5a)];
            var inputs = new Sn74S163PeriodicInputs(pins, 6);
            for (byte initial = 0; initial < 16; initial++)
            for (var phase = 0; phase < 3; phase++)
            for (var periods = 1; periods <= 6; periods++)
            {
                var grouped = new Sn74S163(24_000_000, initial);
                var unit = new Sn74S163(24_000_000, initial);
                grouped.CaptureClockPeriods(periods, inputs, -phase);
                for (var edge = 0; edge < periods; edge++)
                {
                    Apply(unit, pins[(phase + edge) % 3]);
                    unit.CaptureClock(true);
                    Apply(unit, pins[(phase + edge + 1) % 3]);
                    unit.ResolveClock();
                    unit.ReceiveFallingClock();
                }
                grouped.ResolveClock();
                grouped.ReceiveFallingClock();
                Assert.Equal(unit.Count, grouped.Count);
                Assert.Equal(unit.Carry, grouped.Carry);
                Assert.Equal(unit.InputClockCount, grouped.InputClockCount);
                Assert.False(grouped.ClockHigh);
                unit.Clock();
                grouped.Clock();
                Assert.Equal(unit.Count, grouped.Count);
                Assert.Equal(unit.Carry, grouped.Carry);
            }
        }

        static void Apply(Sn74S163 counter, byte pins)
        {
            counter.SetClear((pins & 1) != 0);
            counter.SetLoad((pins & 2) != 0);
            counter.SetEnable((pins & 4) != 0, (pins & 8) != 0);
            counter.SetData((byte)(pins >> 4));
        }
    }

    [Fact]
    public void EveryControlCombinationMatchesTheTiPriorityTableForEveryInitialCount()
    {
        for (var flags = 0; flags < 16; flags++)
        for (byte data = 0; data < 16; data++)
        for (byte count = 0; count < 16; count++)
        {
            var clear = (flags & 1) != 0;
            var load = (flags & 2) != 0;
            var p = (flags & 4) != 0;
            var t = (flags & 8) != 0;
            var chip = new Sn74S163(24_000_000, count);
            chip.SetClear(clear);
            chip.SetLoad(load);
            chip.SetData((byte)(0x10 | data)); // Ignore bits outside the four physical data pins.
            chip.SetEnable(p, t);
            Assert.Equal(count, chip.Count); // Control inputs are synchronous.
            chip.Clock();
            var expected = !clear ? 0 : !load ? data : p && t ? (count + 1) & 15 : count;
            Assert.Equal((byte)expected, chip.Count);
            Assert.Equal(t && expected == 15, chip.Carry);
            Assert.Equal(1, chip.InputClockCount);
            Assert.False(chip.ClockHigh);
        }
    }

    [Fact]
    public void CompletePeriodsMatchExplicitLevelsWithFeedbackOnTheSameEdge()
    {
        var periods = new Sn74S163(24_000_000, 13);
        var levels = new Sn74S163(24_000_000, 13);
        var periodEdges = new List<(long, byte, bool, bool)>();
        var levelEdges = new List<(long, byte, bool, bool)>();
        void Connect(Sn74S163 chip, List<(long, byte, bool, bool)> edges)
        {
            chip.SetEnable(true, true);
            chip.OutputsChanged = (count, carry) =>
            {
                edges.Add((chip.InputClockCount, count, carry, chip.ClockHigh));
                chip.SetClear(count != 7);
                chip.SetData((byte)((count + 3) & 15));
                chip.SetLoad(count != 11);
            };
        }
        Connect(periods, periodEdges);
        Connect(levels, levelEdges);
        for (var clock = 0; clock < 133; clock++)
        {
            periods.Clock();
            levels.SetClock(true);
            levels.SetClock(true);
            levels.SetClock(false);
            levels.SetClock(false);
            Assert.Equal(levels.Count, periods.Count);
            Assert.Equal(levels.Carry, periods.Carry);
            Assert.Equal(levels.InputClockCount, periods.InputClockCount);
            Assert.False(periods.ClockHigh);
        }
        Assert.Equal(levelEdges, periodEdges);
        Assert.All(periodEdges, edge => Assert.True(edge.Item4));
    }

    [Fact]
    public void CompletePeriodsRejectHighClockAndGeneralInputRejectsUnresolvedEdge()
    {
        var chip = new Sn74S163(24_000_000);
        chip.SetClock(true);
        Assert.Throws<InvalidOperationException>(() => chip.Clock());
        Assert.Throws<InvalidOperationException>(() => chip.AdvanceClocks(1));
        chip.SetClock(false);
        chip.CaptureClock(true);
        Assert.Throws<InvalidOperationException>(() => chip.SetClock(false));
        chip.ResolveClock();
        chip.SetClock(false);
        Assert.False(chip.ClockHigh);
        Assert.Equal(2, chip.InputClockCount);
    }

    [Fact]
    public void SharedEdgePublishesOnlyTheResolvedCountAndCarry()
    {
        var chip = new Sn74S163(24_000_000, 14);
        chip.SetEnable(true, true);
        var outputs = new List<(byte, bool)>();
        chip.OutputsChanged = (count, carry) => outputs.Add((count, carry));
        chip.CaptureClock(true);
        chip.SetEnable(false, false);
        Assert.Empty(outputs);
        chip.ResolveClock();
        Assert.Equal(new[] { ((byte)15, false) }, outputs);
        Assert.Equal(1, chip.InputClockCount);
        chip.SetClock(false);
        chip.SetEnable(true, true);
        Assert.Equal(((byte)15, true), outputs[^1]);
    }

    [Fact]
    public void AllSixteenPresetsCountAndWrapOnRisingEdgesOnly()
    {
        for (byte value = 0; value < 16; value++)
        {
            var chip = new Sn74S163(24_000_000);
            chip.SetData(value);
            chip.SetLoad(false);
            Assert.Equal(0, chip.Count);
            chip.Clock();
            Assert.Equal(value, chip.Count);
            chip.SetLoad(true);
            chip.SetEnable(true, true);
            chip.SetClock(true);
            Assert.Equal((value + 1) & 15, chip.Count);
            chip.SetClock(true);
            chip.SetClock(false);
            Assert.Equal((value + 1) & 15, chip.Count);
            Assert.Equal(2, chip.InputClockCount);
        }
    }

    [Fact]
    public void SynchronousClearHasPriorityOverPresetAndBothEnables()
    {
        var chip = new Sn74S163(24_000_000, 15);
        chip.SetData(12);
        chip.SetEnable(false, false);
        chip.SetLoad(false);
        chip.SetClear(false);
        Assert.Equal(15, chip.Count);
        chip.Clock();
        Assert.Equal(0, chip.Count);
        chip.SetClear(true);
        chip.Clock();
        Assert.Equal(12, chip.Count);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BothEnablesGateCountButOnlyEntGatesCarry(bool p, bool t)
    {
        var chip = new Sn74S163(24_000_000, 15);
        chip.SetEnable(p, t);
        Assert.Equal(t, chip.Carry);
        chip.Clock();
        Assert.Equal(p && t ? 0 : 15, chip.Count);
        Assert.Equal(t && !p, chip.Carry);
    }

    [Fact]
    public void CarryChangesWithEntWithoutAClockAndDoesNotEmitDuplicates()
    {
        var chip = new Sn74S163(24_000_000, 15);
        var pins = new List<(byte, bool)>();
        chip.OutputsChanged = (count, carry) => pins.Add((count, carry));
        chip.SetEnable(false, true);
        chip.SetEnable(true, true);
        chip.SetEnable(true, false);
        Assert.Equal(new[] { ((byte)15, true), ((byte)15, false) }, pins);
        Assert.Equal(0, chip.InputClockCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(133)]
    public void GroupingPreservesStatesAndEveryConnectedEdge(int clocks)
    {
        foreach (var mode in new[] { 0, 1, 2, 3 })
        {
            var grouped = new Sn74S163(24_000_000, 13);
            var unit = new Sn74S163(24_000_000, 13);
            var groupEdges = new List<(long, byte, bool)>();
            var unitEdges = new List<(long, byte, bool)>();
            grouped.OutputsChanged = (value, carry) => groupEdges.Add((grouped.InputClockCount, value, carry));
            unit.OutputsChanged = (value, carry) => unitEdges.Add((unit.InputClockCount, value, carry));
            foreach (var chip in new[] { grouped, unit })
            {
                chip.SetEnable(mode != 3, true);
                chip.SetClear(mode != 1);
                chip.SetData(7);
                chip.SetLoad(mode != 2);
            }
            grouped.AdvanceClocks(clocks);
            for (var clock = 0; clock < clocks; clock++) unit.Clock();
            Assert.Equal(unit.Count, grouped.Count);
            Assert.Equal(unit.Carry, grouped.Carry);
            Assert.Equal(unit.InputClockCount, grouped.InputClockCount);
            Assert.Equal(unitEdges, groupEdges);
        }
    }
}
