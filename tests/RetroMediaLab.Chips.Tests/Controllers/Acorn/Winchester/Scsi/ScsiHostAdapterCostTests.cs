using System.Diagnostics;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn.Winchester.Scsi;

public sealed class ScsiHostAdapterCostTests(ITestOutputHelper output)
{
    private const int Warmup = 10_000_000;
    private const int Iterations = 1_000_000;
    private const int Samples = 5;
    private const byte Data = 0xa5;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataStrobesAreAllocationFreeAndPreserveTheHardwareLatches(bool writing)
    {
        var host = new ScsiHostAdapter();
        host.SetTarget(ScsiBusSignals.Request | (writing ? ScsiBusSignals.None : ScsiBusSignals.InputOutput), Data);
        _ = Run(host, writing, Warmup);
        var nanoseconds = new double[Samples];
        for (var sample = 0; sample < Samples; sample++)
        {
            var beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            var before = Stopwatch.GetTimestamp();
            var checksum = Run(host, writing, Iterations);
            var elapsed = Stopwatch.GetTimestamp() - before;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            nanoseconds[sample] = elapsed * (1_000_000_000d / Stopwatch.Frequency) / Iterations;
            Assert.Equal(0, allocated);
            Assert.Equal(writing ? 0 : Iterations * Data, checksum);
            Assert.True(host.Acknowledge);
            if (writing) Assert.Equal(Data, host.OutputData);
        }
        Array.Sort(nanoseconds);
        output.WriteLine("FC40 {0}: median {1:F3} ns/access; min {2:F3}; max {3:F3}; 0 allocated bytes",
            writing ? "write" : "read", nanoseconds[Samples / 2], nanoseconds[0], nanoseconds[^1]);
    }

    private static int Run(ScsiHostAdapter host, bool writing, int count)
    {
        var checksum = 0;
        if (writing)
            for (var index = 0; index < count; index++) host.Write(0, Data);
        else
            for (var index = 0; index < count; index++) checksum = unchecked(checksum + host.Read(0));
        return checksum;
    }
}
