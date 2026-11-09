using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS374;

public sealed class Sn74Ls374Tests
{
    [Fact]
    public void AllPatternsCaptureOnlyOnRisingClockAndPreserveEachBit()
    {
        var chip = new Sn74Ls374(2_000_000);
        var notifications = new List<(byte?, bool)>();
        chip.OutputsChanged = (value, driven) => notifications.Add((value, driven));
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var previous = chip.StoredData;
            chip.SetData((byte)value);
            Assert.Equal(previous, chip.StoredData);
            chip.SetClock(true);
            Assert.Equal((byte)value, chip.StoredData);
            Assert.Equal((byte)value, chip.OutputData);
            chip.SetData((byte)~value);
            chip.SetClock(true);
            chip.SetClock(false);
            chip.SetClock(false);
            Assert.Equal((byte)value, chip.StoredData);
        }
        Assert.Equal(256, chip.InputClockCount);
        Assert.Equal(256, notifications.Count);
        Assert.All(notifications, edge => Assert.True(edge.Item2));
        Assert.Equal(Enumerable.Range(0, 256).Select(value => (byte?)value), notifications.Select(edge => edge.Item1));
        Assert.Equal(2_000_000, chip.ClockFrequency);
        Assert.False(chip.ClockHigh);
    }

    [Fact]
    public void OutputControlIsAsynchronousAndDoesNotStopCapture()
    {
        var chip = new Sn74Ls374(2_000_000, 0x5a);
        var notifications = new List<(byte?, bool)>();
        chip.OutputsChanged = (value, driven) => notifications.Add((value, driven));
        chip.SetOutputControl(true);
        chip.SetOutputControl(true);
        Assert.Null(chip.OutputData);
        Assert.False(chip.OutputsDriven);
        Assert.Equal((byte)0x5a, chip.StoredData);
        chip.SetData(0xa5);
        chip.Clock();
        Assert.Equal((byte)0xa5, chip.StoredData);
        Assert.Single(notifications);
        chip.SetOutputControl(false);
        chip.SetOutputControl(false);
        Assert.Equal((byte)0xa5, chip.OutputData);
        Assert.Equal(new (byte?, bool)[] { (null, false), (0xa5, true) }, notifications);
        Assert.Equal(1, chip.InputClockCount);
    }

    [Fact]
    public void UnspecifiedPowerOnStateIsDistinctFromHighImpedance()
    {
        var chip = new Sn74Ls374(2_000_000);
        Assert.Null(chip.StoredData);
        Assert.Null(chip.OutputData);
        Assert.True(chip.OutputsDriven);
        chip.SetOutputControl(true);
        Assert.Null(chip.OutputData);
        Assert.False(chip.OutputsDriven);
        chip.SetOutputControl(false);
        Assert.Null(chip.OutputData);
        chip.SetData(0);
        chip.Clock();
        Assert.Equal((byte)0, chip.StoredData);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(257)]
    public void GroupedPeriodsMatchExplicitLevelsWithOutputFeedback(int periods)
    {
        var grouped = new Sn74Ls374(2_000_000);
        var levels = new Sn74Ls374(2_000_000);
        var groupEdges = new List<(long, byte?, bool)>();
        var levelEdges = new List<(long, byte?, bool)>();
        void Connect(Sn74Ls374 chip, List<(long, byte?, bool)> edges)
        {
            chip.SetData(1);
            chip.OutputsChanged = (value, driven) =>
            {
                edges.Add((chip.InputClockCount, value, driven));
                if (driven && value.HasValue) chip.SetData((byte)(value.Value + 1));
            };
        }
        Connect(grouped, groupEdges);
        Connect(levels, levelEdges);
        grouped.AdvanceClocks(periods);
        for (var i = 0; i < periods; i++) { levels.SetClock(true); levels.SetClock(false); }
        Assert.Equal(levelEdges, groupEdges);
        Assert.Equal(levels.StoredData, grouped.StoredData);
        Assert.Equal(periods, grouped.InputClockCount);
        Assert.False(grouped.ClockHigh);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StableOrHiddenPeriodsKeepTheFinalStoredValueAndEveryClock(bool hidden)
    {
        var chip = new Sn74Ls374(2_000_000, 0x33);
        var notifications = 0;
        chip.OutputsChanged = (_, _) => notifications++;
        chip.SetOutputControl(hidden);
        var initial = notifications;
        chip.SetData(0x33);
        chip.AdvanceClocks(1001);
        Assert.Equal(initial, notifications);
        chip.SetData(0x55);
        chip.AdvanceClocks(1001);
        Assert.Equal(initial + (hidden ? 0 : 1), notifications);
        Assert.Equal((byte)0x55, chip.StoredData);
        Assert.Equal(2002, chip.InputClockCount);
        chip.SetOutputControl(false);
        Assert.Equal((byte)0x55, chip.OutputData);
    }

    [Fact]
    public void InvalidPeriodsCannotStartFromHighAndZeroPeriodsCannotInitializeStorage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls374(0));
        var chip = new Sn74Ls374(2_000_000);
        chip.AdvanceClocks(0);
        Assert.Null(chip.StoredData);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(-1));
        chip.SetClock(true);
        Assert.Throws<InvalidOperationException>(() => chip.Clock());
        Assert.Throws<InvalidOperationException>(() => chip.AdvanceClocks(1));
    }
}
