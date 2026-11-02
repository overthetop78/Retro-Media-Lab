using System.Collections.Concurrent;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class EmulationFrameDeadlineTests
{
    [Fact]
    public void SmallWakeupOvershootsDoNotAccumulateIntoTheFramePeriod()
    {
        var clock = new Clock();
        var deadlines = new List<long>();
        Run(clock, deadlines, target => Math.Max(clock.Now, target) + 1);
        Assert.Equal(new long[] { 20, 40, 60 }, deadlines);
    }

    [Fact]
    public void LongSuspensionAbandonsDebtInsteadOfRunningAnUnboundedCatchup()
    {
        var clock = new Clock();
        var deadlines = new List<long>();
        Run(clock, deadlines, target => deadlines.Count == 1 ? 200 : target + 1);
        Assert.Equal(new long[] { 20, 220, 240 }, deadlines);
    }

    private static void Run(Clock clock, List<long> deadlines, Func<long, long> wakeup) =>
        EmulationFrameTimer.RunLoop(0, new object(), new ConcurrentQueue<EmulationMachineCommand>(),
            () => false, () => clock.Now += 5, () => 50, (previous, _) => previous + 20,
            (target, _) => { deadlines.Add(target); clock.Now = wakeup(target); },
            CancellationToken.None, 1, () => deadlines.Count < 3, clock);

    private sealed class Clock : TimeProvider
    {
        internal long Now;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Now;
    }
}
