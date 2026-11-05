using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000PositioningTests
{
    [Theory]
    [InlineData(0, 6000)]
    [InlineData(1, 56)]
    [InlineData(2, 24)]
    public void SeekIssuesEveryStepAtTheConfiguredRateAndCompletesAtTheLastEdge(byte code, int interval)
    {
        var channel = new Acb4000Positioning(2_000_000);
        channel.BeginSeek(-4, code);
        var edges = new List<int>();
        for (var tick = 0; channel.Busy && tick < interval * 4; tick++)
        {
            channel.Tick(true, true, false);
            if (channel.StepIssued) edges.Add(tick);
        }
        Assert.Equal(new[] { 0, interval, interval * 2, interval * 3 }, edges);
        Assert.True(channel.Complete);
        Assert.False(channel.DirectionIn);
        Assert.Equal(0, channel.Error);
        Assert.Equal(4, channel.IssuedSteps);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(7, 7)]
    public void CalibrationLeavesAnInitiallyActiveZeroBeforeSearchingBack(int initialPosition, int expectedSteps)
    {
        var channel = new Acb4000Positioning(2_000_000);
        var position = initialPosition;
        var waiting = 0;
        channel.BeginRezero(2);
        for (var tick = 0; channel.Busy && tick < 100_000; tick++)
        {
            channel.Tick(true, waiting == 0, position == 0);
            if (waiting > 0) waiting--;
            if (!channel.StepIssued) continue;
            position += channel.DirectionIn ? 1 : -1;
            waiting = 3;
        }
        Assert.True(channel.Complete);
        Assert.Equal(0, channel.Error);
        Assert.Equal(0, position);
        Assert.Equal(expectedSteps, channel.IssuedSteps);
    }

    [Fact]
    public void StuckActiveZeroIsRejectedAfterTheExcursion()
    {
        var channel = new Acb4000Positioning(2_000_000);
        channel.BeginRezero(2);
        for (var tick = 0; channel.Busy && tick < 1000; tick++) channel.Tick(true, true, true);
        Assert.True(channel.Complete);
        Assert.Equal(6, channel.Error);
        Assert.Equal(16, channel.IssuedSteps);
    }

    [Fact]
    public void MissingZeroIsBoundedAndDoesNotBecomeASuccessfulCalibration()
    {
        var channel = new Acb4000Positioning(2_000_000);
        channel.BeginRezero(2);
        for (var tick = 0; channel.Busy && tick < 100_000; tick++) channel.Tick(true, true, false);
        Assert.True(channel.Complete);
        Assert.Equal(6, channel.Error);
        Assert.Equal(2048, channel.IssuedSteps);
    }

    [Fact]
    public void SeekCompleteWaitAndItsExternalExpiryDoNotInventSteps()
    {
        var channel = new Acb4000Positioning(2_000_000);
        channel.BeginSeek(1, 2);
        for (var tick = 0; tick < 100; tick++) channel.Tick(true, false, false);
        Assert.Equal(0, channel.IssuedSteps);
        Assert.True(channel.Busy);
        channel.ExpireSeekComplete();
        Assert.Equal(2, channel.Error);
        Assert.True(channel.Complete);
        Assert.False(channel.StepIssued);
    }

    [Fact]
    public void ReadyLossAndResetStopTheSequenceWithoutMovingTheBorrowedDrive()
    {
        var channel = new Acb4000Positioning(2_000_000);
        channel.BeginSeek(30, 1);
        channel.Tick(true, true, false);
        Assert.Equal(1, channel.IssuedSteps);
        channel.Tick(false, false, false);
        Assert.Equal(4, channel.Error);
        Assert.False(channel.StepIssued);
        channel.BeginRezero(2);
        channel.Reset();
        channel.Tick(true, true, true);
        Assert.Equal(Acb4000PositioningPhase.Idle, channel.Phase);
        Assert.False(channel.StepIssued);
        Assert.Equal(0, channel.IssuedSteps);
    }

    [Fact]
    public void FractionalOwnerClockDoesNotAccumulatePerStepRounding()
    {
        var channel = new Acb4000Positioning(1_234_567);
        channel.BeginSeek(100, 2);
        var tick = 0;
        while (channel.Busy && tick < 2000)
        {
            channel.Tick(true, true, false);
            tick++;
        }
        Assert.True(channel.Complete);
        Assert.Equal(1 + (int)Math.Ceiling(99 * 12_000d * 1_234_567 / 1_000_000_000), tick);
    }
}
