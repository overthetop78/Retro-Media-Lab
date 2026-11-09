using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS74A;

public sealed class Sn74Ls74Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EveryFunctionTableCombinationAppliesToBothIndependentChannels(int channel)
    {
        foreach (var preset in new[] { false, true })
        foreach (var clear in new[] { false, true })
        foreach (var data in new[] { false, true })
        foreach (var initial in new[] { false, true })
        {
            var chip = new Sn74Ls74(2_000_000, 1_000_000, initial, initial);
            chip.SetData(channel, data);
            chip.SetAsynchronousInputs(channel, preset, clear);
            Assert.Equal(preset && clear ? initial : !preset, chip.GetQ(channel));
            Assert.Equal(preset && clear ? !initial : !clear, chip.GetNotQ(channel));
            chip.SetClock(channel, true);
            Assert.Equal(preset && clear ? data : !preset, chip.GetQ(channel));
            Assert.Equal(preset && clear ? !data : !clear, chip.GetNotQ(channel));
            chip.SetData(channel, !data); chip.SetClock(channel, true);
            Assert.Equal(preset && clear ? data : !preset, chip.GetQ(channel));
            chip.SetClock(channel, false);
            Assert.Equal(1, chip.GetInputClockCount(channel));
            Assert.Equal(0, chip.GetInputClockCount(1 - channel));
            Assert.Equal(initial, chip.GetQ(1 - channel));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AsynchronousControlsActWithoutAClockAndReleaseKeepsTheValidState(int channel)
    {
        var chip = new Sn74Ls74(2_000_000, 2_000_000, false, false);
        chip.SetPreset(channel, false);
        Assert.True(chip.GetQ(channel)); Assert.False(chip.GetNotQ(channel));
        chip.SetClear(channel, false);
        Assert.True(chip.GetQ(channel)); Assert.True(chip.GetNotQ(channel));
        chip.SetPreset(channel, true);
        Assert.False(chip.GetQ(channel)); Assert.True(chip.GetNotQ(channel));
        chip.SetData(channel, true); chip.SetClear(channel, true);
        Assert.False(chip.GetQ(channel));
        Assert.Equal(0, chip.GetInputClockCount(channel));
        chip.Clock(channel);
        Assert.True(chip.GetQ(channel)); Assert.False(chip.GetNotQ(channel));
    }

    [Fact]
    public void SimultaneousForbiddenReleaseRemainsIndeterminateUntilAValidClock()
    {
        var chip = new Sn74Ls74(1_000_000, 1_000_000);
        Assert.Null(chip.GetQ(0)); Assert.Null(chip.GetNotQ(0));
        var changes = new List<(bool?, bool?)>();
        chip.FirstOutputsChanged = (q, notQ) => changes.Add((q, notQ));
        chip.SetAsynchronousInputs(0, false, false);
        chip.SetAsynchronousInputs(0, true, true);
        Assert.Null(chip.GetQ(0)); Assert.Null(chip.GetNotQ(0));
        chip.Clock(0);
        Assert.Equal(new (bool?, bool?)[] { (true, true), (null, null), (false, true) }, changes);
    }

    [Fact]
    public void SharedClockCapturesBothOldDataInputsBeforeOutputFeedback()
    {
        var chip = new Sn74Ls74(2_000_000, 2_000_000, false, false);
        chip.SetData(0, true); chip.SetData(1, false);
        chip.FirstOutputsChanged = (q, _) => chip.SetData(1, q!.Value);
        chip.SetClocks(true, true);
        Assert.True(chip.GetQ(0)); Assert.False(chip.GetQ(1));
        chip.SetClocks(false, false);
        chip.SetClocks(true, true);
        Assert.True(chip.GetQ(1));
        Assert.Equal(2, chip.GetInputClockCount(0)); Assert.Equal(2, chip.GetInputClockCount(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(133)]
    public void GroupedClocksMatchUnitClocksEvenWithOutputFeedback(int clocks)
    {
        foreach (var channel in new[] { 0, 1 })
        foreach (var mode in new[] { 0, 1, 2, 3 })
        {
            var grouped = new Sn74Ls74(2_000_000, 1_000_000, false, false);
            var unit = new Sn74Ls74(2_000_000, 1_000_000, false, false);
            var groupedEdges = new List<(long, bool?, bool?)>();
            var unitEdges = new List<(long, bool?, bool?)>();
            Configure(grouped, groupedEdges); Configure(unit, unitEdges);
            grouped.AdvanceClocks(channel, clocks);
            for (var i = 0; i < clocks; i++) unit.Clock(channel);
            Assert.Equal(unitEdges, groupedEdges);
            Assert.Equal(unit.GetQ(channel), grouped.GetQ(channel));
            Assert.Equal(unit.GetNotQ(channel), grouped.GetNotQ(channel));
            Assert.Equal(unit.GetInputClockCount(channel), grouped.GetInputClockCount(channel));
            Assert.False(grouped.GetClock(channel));

            void Configure(Sn74Ls74 chip, List<(long, bool?, bool?)> edges)
            {
                chip.SetData(channel, true);
                chip.SetAsynchronousInputs(channel, mode is not 1 and not 3, mode is not 2 and not 3);
                void Output(bool? q, bool? notQ)
                {
                    edges.Add((chip.GetInputClockCount(channel), q, notQ));
                    if (mode == 0) chip.SetData(channel, !q!.Value);
                }
                if (channel == 0) chip.FirstOutputsChanged = Output; else chip.SecondOutputsChanged = Output;
            }
        }
    }

    [Fact]
    public void NominalSourcesAreIndependentAndInvalidChannelCannotChangeTheChip()
    {
        var chip = new Sn74Ls74(2_000_000, 1_000_000);
        Assert.Equal(2_000_000, chip.GetClockFrequency(0)); Assert.Equal(1_000_000, chip.GetClockFrequency(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetData(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetClock(2, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(0, -1));
        chip.SetClock(0, true);
        Assert.Throws<InvalidOperationException>(() => chip.Clock(0));
        Assert.Throws<InvalidOperationException>(() => chip.AdvanceClocks(0, 1));
        Assert.Equal(0, chip.GetInputClockCount(1));
    }
}
