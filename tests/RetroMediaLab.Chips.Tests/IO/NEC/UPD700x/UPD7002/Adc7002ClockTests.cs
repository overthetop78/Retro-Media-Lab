using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

namespace RetroMediaLab.Chips.Tests.IO.NEC.UPD700x.UPD7002;

public sealed class Adc7002ClockTests
{
    [Theory]
    [InlineData(2_000_000, 1_000_000, 0, 8_000)]
    [InlineData(2_000_000, 1_000_000, 8, 20_000)]
    [InlineData(2_000_003, 1_000_000, 0, 8_001)]
    [InlineData(2_000_003, 1_000_000, 8, 20_001)]
    [InlineData(1_000_000, 3_000_000, 8, 3_334)]
    [InlineData(1_000, 3_000_000, 8, 4)]
    [InlineData(2_000_000, 1_500_000, 0, 5_334)]
    public void EocAppearsOnTheFirstInputBoundaryAtOrAfterTheNominalDuration(
        int inputClock, int conversionClock, byte command, long expected)
    {
        Adc7002? chip = null;
        var fallingEdges = new List<long>();
        chip = new Adc7002(high => { if (!high) fallingEdges.Add(chip!.InputClockCount); },
            inputClock, conversionClock);
        chip.Write(0, command);
        chip.AdvanceTo(expected - 1);
        Assert.True(chip.Converting);
        Assert.Empty(fallingEdges);
        chip.AdvanceTo(expected);
        Assert.False(chip.Converting);
        Assert.Equal(new[] { expected }, fallingEdges);
        Assert.Null(chip.NextClockEvent);
        chip.AdvanceTo(expected * 2);
        Assert.Single(fallingEdges);
    }

    [Fact]
    public void LongModeRoundsItsTotalRatherThanAddingRoundedPhaseDurations()
    {
        var chip = new Adc7002(_ => { }, clockHz: 2_000_003, conversionClockHz: 1_000_000);
        chip.Write(0, 8);
        Assert.Equal(16_385L, chip.NextClockEvent);
        chip.AdvanceTo(16_385);
        Assert.Equal(20_001L, chip.NextClockEvent);
        chip.AdvanceTo(20_001);
        Assert.False(chip.Converting);
    }

    [Fact]
    public void BusStrobeCancelsTheOldDeadlineAndPublishesTheNewOneOnRelease()
    {
        var chip = new Adc7002(_ => { });
        var deadlines = new List<long?>();
        chip.NextClockEventChanged = deadlines.Add;
        chip.SetBus(false, true, false, 0, 8);
        Assert.Empty(deadlines);
        chip.SetBus(true, true, true, 0, 8);
        Assert.Equal(new long?[] { 16_384 }, deadlines);
        chip.AdvanceTo(1_000);
        chip.SetBus(false, true, false, 0, 0);
        Assert.Null(chip.NextClockEvent);
        Assert.Null(deadlines[^1]);
        chip.SetBus(true, true, true, 0, 0);
        Assert.Equal(9_000L, chip.NextClockEvent);
        Assert.Equal(9_000L, deadlines[^1]);
    }
}
