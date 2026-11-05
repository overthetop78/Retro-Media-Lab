using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000SearchDeadlineTests
{
    [Theory]
    [InlineData(0, 2_000_000, 3_000_000, 287230)]
    [InlineData(1, 2_000_000, 3_000_000, 414)]
    [InlineData(2, 2_000_000, 3_000_000, 6654)]
    [InlineData(0, 2_000_003, 3_000_000, 287231)]
    [InlineData(1, 2_000_003, 3_000_000, 415)]
    [InlineData(2, 2_000_003, 3_000_000, 6655)]
    [InlineData(0, 1_000_000, 1_000_000, 430845)]
    [InlineData(1, 1_000_000, 1_000_000, 621)]
    [InlineData(2, 1_000_000, 1_000_000, 9981)]
    public void PollingLoopExpiresAtTheExactCeilingWithoutExpiringTwice(int kind, int owner, int sequencer, int ticks)
    {
        var timer = new Acb4000SearchDeadline(owner, sequencer);
        if (kind == 0) timer.BeginIndexWait();
        else if (kind == 1) timer.BeginFirstFieldWait();
        else timer.BeginFollowingFieldWait();
        for (var tick = 1; tick < ticks; tick++) Assert.Equal(Acb4000SearchWait.None, timer.Tick());
        Assert.Equal(kind == 0 ? Acb4000SearchWait.Index : Acb4000SearchWait.Field, timer.Tick());
        Assert.Equal(Acb4000SearchWait.None, timer.TakeExpired());
        Assert.Equal(Acb4000SearchWait.None, timer.Tick());
    }

    [Fact]
    public void ContinuingWindowsRetainsFractionalOvershootIncludingSeveralExpirationsPerTick()
    {
        var timer = new Acb4000SearchDeadline(1, 3_000_000);
        timer.BeginFollowingFieldWait();
        var count = 0;
        for (var seconds = 1; seconds <= 10; seconds++)
        {
            var result = timer.Tick();
            while (result != Acb4000SearchWait.None)
            {
                count++;
                Assert.Equal(Acb4000SearchWait.Field, result);
                timer.ContinueFieldWait();
                result = timer.TakeExpired();
            }
            Assert.Equal((long)seconds * 3_000_000 / 9981, count);
        }
    }

    [Fact]
    public void ResetAndNewFieldDiscardThePreviousLoopButOnlyAnExpiredFieldCanContinue()
    {
        var timer = new Acb4000SearchDeadline(1, 1_000_000);
        Assert.Throws<InvalidOperationException>(timer.ContinueFieldWait);
        timer.BeginIndexWait();
        Assert.Equal(Acb4000SearchWait.Index, timer.Tick());
        Assert.Throws<InvalidOperationException>(timer.ContinueFieldWait);
        timer.BeginFirstFieldWait();
        Assert.Equal(Acb4000SearchWait.Field, timer.Tick());
        timer.Reset();
        Assert.Equal(Acb4000SearchWait.None, timer.Tick());
        Assert.Throws<InvalidOperationException>(timer.ContinueFieldWait);
        timer.BeginFollowingFieldWait();
        Assert.Throws<InvalidOperationException>(timer.ContinueFieldWait);
    }

    [Fact]
    public void FrequenciesAreRequiredAndTicksDoNotAllocate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000SearchDeadline(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000SearchDeadline(1, 0));
        var timer = new Acb4000SearchDeadline(2_000_000, 3_000_000);
        timer.BeginIndexWait();
        for (var index = 0; index < 1000; index++) timer.Tick();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++) timer.Tick();
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocated);
    }
}
