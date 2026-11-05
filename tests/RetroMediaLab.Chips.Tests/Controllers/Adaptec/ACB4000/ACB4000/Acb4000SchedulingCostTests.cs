using System.Diagnostics;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000SchedulingCostTests(ITestOutputHelper output)
{
    private const int OwnerClock = 2_000_000;
    private const int WarmupTicks = 50_000;
    private const int MeasuredTicks = 200_000;
    private const int Samples = 5;
    private const int JitWarmupTicks = 10_000_000;

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidOwnerClockIsRejectedBeforeTheCachedClockRatioIsCalculated(int frequency) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000(_ => null, frequency));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnerTicksKeepTheirSignalsAndAllocateNoMemory(bool selected)
    {
        var warmup = new Acb4000(_ => null, OwnerClock);
        for (var tick = 0; tick < JitWarmupTicks; tick++) warmup.Tick();
        var nanoseconds = new double[Samples];
        for (var sample = 0; sample < Samples; sample++)
        {
            var controller = new Acb4000(_ => null, OwnerClock);
            if (selected)
            {
                controller.SetInitiator(1, true, false, false);
                for (var tick = 0; tick < 20; tick++) controller.Tick();
                controller.SetInitiator(0, false, false, false);
                for (var tick = 0; tick < 20 && !controller.Bus.Request; tick++) controller.Tick();
                Assert.True(controller.Bus.Request);
            }
            for (var tick = 0; tick < WarmupTicks; tick++) controller.Tick();
            var beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            var before = Stopwatch.GetTimestamp();
            for (var tick = 0; tick < MeasuredTicks; tick++) controller.Tick();
            var duration = Stopwatch.GetTimestamp() - before;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            nanoseconds[sample] = duration * (1_000_000_000d / Stopwatch.Frequency) / MeasuredTicks;
            Assert.Equal(0, allocated);
            Assert.Equal(selected, controller.Bus.Busy);
            Assert.Equal(selected, controller.Bus.Request);
            Assert.Equal(!selected, controller.IsIdle);
            Assert.False(controller.Bus.TimedOut);
        }
        Array.Sort(nanoseconds);
        output.WriteLine("{0}: median {1:F3} ns/tick; min {2:F3}; max {3:F3}; 0 allocated bytes",
            selected ? "REQ awaiting ACK" : "Idle", nanoseconds[Samples / 2], nanoseconds[0], nanoseconds[^1]);
    }
}
