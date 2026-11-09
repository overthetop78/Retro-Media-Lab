using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemCpuClockControlTests
{
    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    [InlineData(3_000_000)]
    public void EveryPhysicalClockSelectionPassesBothLevelsWithoutDuplicates(long frequency)
    {
        var circuit = new SystemCpuClockControl(frequency);
        var edges = new List<bool>();
        circuit.CpuClockChanged = edges.Add;
        for (var period = 0; period < 10; period++)
        {
            circuit.SetSelectedClock(true); circuit.SetSelectedClock(true);
            circuit.SetSelectedClock(false); circuit.SetSelectedClock(false);
        }
        Assert.Equal(frequency, circuit.ClockFrequency);
        Assert.Equal(10, circuit.InputClockCount);
        Assert.Equal(10, circuit.CpuClockCount);
        Assert.Equal(Enumerable.Range(0, 20).Select(i => (i & 1) == 0), edges);
        Assert.True(circuit.Ready); Assert.True(circuit.BusGrantHigh); Assert.True(circuit.BusBuffersEnabled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemoryReadyIsCapturedAtRisingSourceEdgeAndReleasesWithoutAnExtraCpuEdge(bool initiallyHigh)
    {
        var circuit = new SystemCpuClockControl(2_000_000);
        circuit.SetSelectedClock(initiallyHigh);
        var initialCpuLevel = circuit.CpuClockHigh;
        circuit.SetMemoryReady(false);
        Assert.Equal(initialCpuLevel, circuit.CpuClockHigh);
        circuit.SetSelectedClock(false);
        Assert.False(circuit.CpuClockHigh);
        circuit.SetSelectedClock(true);
        Assert.True(circuit.CpuClockHigh);
        var stoppedClockCount = circuit.CpuClockCount;
        circuit.SetSelectedClock(false);
        for (var i = 0; i < 5; i++) SourcePeriod(circuit);
        Assert.True(circuit.CpuClockHigh);
        Assert.Equal(stoppedClockCount, circuit.CpuClockCount);
        Assert.False(circuit.SelectedClockHigh);
        circuit.SetMemoryReady(true);
        Assert.True(circuit.CpuClockHigh);
        circuit.SetSelectedClock(true);
        Assert.Equal(stoppedClockCount, circuit.CpuClockCount);
        circuit.SetSelectedClock(false);
        Assert.False(circuit.CpuClockHigh);
        SourcePeriod(circuit);
        Assert.Equal(stoppedClockCount + 1, circuit.CpuClockCount);
    }

    [Fact]
    public void DmaRequestWaitsForReadAndReadyChangesBeforeTheCpuClockReceiver()
    {
        var circuit = new SystemCpuClockControl(2_000_000);
        var notifications = new List<string>();
        circuit.ReadyChanged = high =>
        {
            Assert.Equal(high, circuit.BusGrantHigh);
            Assert.Equal(high, circuit.BusBuffersEnabled);
            notifications.Add($"ready:{high}");
        };
        circuit.CpuClockChanged = high => notifications.Add($"clock:{high}");
        circuit.SetCpuRead(false);
        circuit.SetBusRequest(false);
        SourcePeriod(circuit);
        Assert.True(circuit.Ready);
        Assert.DoesNotContain("ready:False", notifications);
        circuit.SetCpuRead(true);
        Assert.True(circuit.Ready);
        notifications.Clear();
        circuit.SetSelectedClock(true);
        Assert.Equal(new[] { "ready:False", "clock:True" }, notifications);
        Assert.False(circuit.Ready);
        circuit.SetBusRequest(true);
        Assert.False(circuit.Ready);
        circuit.SetSelectedClock(false);
        notifications.Clear();
        circuit.SetSelectedClock(true);
        Assert.Equal(new[] { "ready:True", "clock:True" }, notifications);
        Assert.True(circuit.Ready);
    }

    [Fact]
    public void StretchedPhZeroDoesNotInventReadyCapturesDuringBusRequestChanges()
    {
        var circuit = new SystemCpuClockControl(1_000_000);
        circuit.SetMemoryReady(false);
        SourcePeriod(circuit);
        circuit.SetBusRequest(false);
        for (var i = 0; i < 5; i++) SourcePeriod(circuit);
        Assert.True(circuit.Ready);
        Assert.Equal(1, circuit.CpuClockCount);
        circuit.SetMemoryReady(true);
        SourcePeriod(circuit);
        Assert.True(circuit.Ready);
        Assert.False(circuit.CpuClockHigh);
        SourcePeriod(circuit);
        Assert.False(circuit.Ready);
        Assert.Equal(2, circuit.CpuClockCount);
        circuit.SetBusRequest(true);
        SourcePeriod(circuit);
        Assert.True(circuit.Ready);
    }

    [Fact]
    public void GeneratorAndIndependentOneMhzClockContinueDuringWaitAndDma()
    {
        var source = new SystemClockGenerator();
        var circuit = new SystemCpuClockControl(2_000_000);
        source.Clock2MHzChanged = circuit.SetSelectedClock;
        circuit.SetSelectedClock(source.Clock2MHz);
        long independentEdges = 0;
        source.Clock1MHzChanged = _ => independentEdges++;
        source.AdvanceClocks(240);
        Assert.Equal(20, circuit.CpuClockCount);
        circuit.SetMemoryReady(false);
        source.AdvanceClocks(240);
        var held = circuit.CpuClockCount;
        source.AdvanceClocks(240);
        Assert.Equal(held, circuit.CpuClockCount);
        Assert.Equal(60, circuit.InputClockCount);
        Assert.Equal(60, independentEdges);
        circuit.SetMemoryReady(true);
        source.AdvanceClocks(240);
        Assert.True(circuit.CpuClockCount > held);
        circuit.SetBusRequest(false);
        source.AdvanceClocks(240);
        Assert.False(circuit.Ready);
        Assert.Equal(100, circuit.InputClockCount);
        Assert.Equal(100, independentEdges);
        Assert.Equal(1_200, source.InputClockCount);
    }

    [Fact]
    public void OnlyTheThreePhysicalLinksAreAccepted()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemCpuClockControl(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SystemCpuClockControl(4_000_000));
    }

    private static void SourcePeriod(SystemCpuClockControl circuit)
    {
        circuit.SetSelectedClock(true);
        circuit.SetSelectedClock(false);
    }
}
