using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74S163;

public sealed class Sn74S163Tests
{
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
