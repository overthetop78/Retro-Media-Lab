using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemClockGeneratorTests
{
    [Fact]
    public void AttachingEarlierOutputsDuringALaterPublicationDoesNotReplayTheSameEdge()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        circuit.Clock6MHzChanged = high =>
        {
            edges.Add((circuit.InputClockCount, 6, high));
            circuit.Clock12MHzChanged = level => edges.Add((circuit.InputClockCount, 12, level));
            circuit.Clock8MHzChanged = level => edges.Add((circuit.InputClockCount, 8, level));
            circuit.Clock6MHzChanged = null;
        };
        circuit.AdvanceClocks(3);
        Assert.Equal(new[] { (2L, 6, true), (3L, 12, true), (3L, 8, false) }, edges);
    }

    [Fact]
    public void ReceiversCanChangeDuringTheSameResolvedEdgeWithoutLosingLaterOutputs()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, bool)>();
        circuit.Clock12MHzChanged = _ =>
        {
            if (circuit.InputClockCount == 2)
                circuit.Clock8MHzChanged = high => edges.Add((circuit.InputClockCount, high));
            if (circuit.InputClockCount == 3)
            {
                circuit.Clock8MHzChanged = null;
                circuit.Clock12MHzChanged = null;
            }
        };
        circuit.AdvanceClocks(4);
        Assert.Equal(new[] { (2L, true) }, edges);
        circuit.Clock8MHzChanged = high => edges.Add((circuit.InputClockCount, high));
        Assert.Single(edges); // Wiring alone does not fabricate an edge.
        circuit.AdvanceClocks(2);
        Assert.Equal(new[] { (2L, true), (5L, true), (6L, false) }, edges);
        Assert.Equal(ExpectedLevels(6), Levels(circuit));
    }

    [Fact]
    public void FallingEdgeReachesEveryCounterWithoutChangingResolvedOutputs()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        Connect(circuit, (frequency, high) => edges.Add((circuit.InputClockCount, frequency, high)));
        for (var period = 1; period <= 96; period++)
        {
            circuit.SetOscillator(true);
            var outputs = Levels(circuit);
            var notifications = edges.Count;
            var counts = new[] { circuit.Ic2.Count, circuit.Ic3.Count, circuit.Ic4.Count };
            circuit.SetOscillator(false);
            Assert.Equal(outputs, Levels(circuit));
            Assert.Equal(notifications, edges.Count);
            var index = 0;
            foreach (var counter in new[] { circuit.Ic2, circuit.Ic3, circuit.Ic4 })
            {
                Assert.False(counter.ClockHigh);
                Assert.Equal(period, counter.InputClockCount);
                Assert.Equal(counts[index++], counter.Count);
            }
        }
    }

    [Fact]
    public void EveryMasterEdgeMatchesFigureNineIncludingTheEightMhzDutyCycle()
    {
        var circuit = new SystemClockGenerator();
        Assert.Equal(24_000_000L, circuit.OscillatorFrequency);
        for (var time = 1; time <= 96; time++)
        {
            circuit.Clock();
            Assert.Equal((time + 2) % 3, circuit.Ic2.Count);
            Assert.Equal(time / 3 % 16, circuit.Ic3.Count);
            Assert.Equal(time % 16, circuit.Ic4.Count);
            Assert.Equal(time % 48 == 47, circuit.Ic3.Carry);
            Assert.Equal(ExpectedLevels(time), Levels(circuit));
            foreach (var counter in new[] { circuit.Ic2, circuit.Ic3, circuit.Ic4 })
            {
                Assert.Equal(time, counter.InputClockCount);
                Assert.Equal(24_000_000L, counter.ClockFrequency);
                Assert.False(counter.ClockHigh);
            }
        }
    }

    [Fact]
    public void CarrySynchronizesAnInitiallyOutOfPhaseIcFourAtTheRealSharedEdge()
    {
        var circuit = new SystemClockGenerator(2, 0, 7);
        circuit.AdvanceClocks(47);
        Assert.True(circuit.Ic3.Carry);
        Assert.Equal(6, circuit.Ic4.Count);
        circuit.Clock();
        Assert.Equal(0, circuit.Ic4.Count);
        Assert.Equal(0, circuit.Ic3.Count);
        Assert.Equal(ExpectedLevels(48), Levels(circuit));
    }

    [Fact]
    public void OutputReceiversSeeAllLevelsResolvedForTheSamePhysicalEdge()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        Connect(circuit, (frequency, high) =>
        {
            Assert.True(circuit.OscillatorHigh);
            Assert.Equal(ExpectedLevels(circuit.InputClockCount), Levels(circuit));
            edges.Add((circuit.InputClockCount, frequency, high));
        });
        circuit.AdvanceClocks(48);
        (int Frequency, int FallingEdges)[] counts = [(12, 24), (8, 16), (6, 12), (4, 8), (3, 6), (2, 4), (1, 2)];
        foreach (var (frequency, expected) in counts)
            Assert.Equal(expected, edges.Count(edge => edge.Item2 == frequency && !edge.Item3));
        Assert.Equal(new long[] { 12, 24, 36, 48 }, edges.Where(edge => edge.Item2 == 2 && !edge.Item3).Select(edge => edge.Item1));
        Assert.Equal(new long[] { 24, 48 }, edges.Where(edge => edge.Item2 == 1 && !edge.Item3).Select(edge => edge.Item1));
    }

    [Fact]
    public void ARepeatedClockLevelDoesNotInventAnExtraEdge()
    {
        var circuit = new SystemClockGenerator();
        var changes = 0;
        circuit.Clock12MHzChanged = _ => changes++;
        circuit.SetOscillator(true); circuit.SetOscillator(true);
        Assert.Equal(1, circuit.InputClockCount);
        Assert.Equal(1, changes);
        Assert.Throws<InvalidOperationException>(() => circuit.Clock());
        circuit.SetOscillator(false); circuit.SetOscillator(false);
        Assert.Equal(1, changes);
        circuit.Clock();
        Assert.Equal(2, circuit.InputClockCount);
        Assert.Equal(2, changes);
        Assert.Throws<ArgumentOutOfRangeException>(() => circuit.AdvanceClocks(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(48)]
    [InlineData(1001)]
    public void GroupedSourcePeriodsPreserveAllConnectedOutputEdges(int periods)
    {
        var grouped = new SystemClockGenerator();
        var unit = new SystemClockGenerator();
        var levels = new SystemClockGenerator();
        var first = new List<(long, int, bool)>();
        var second = new List<(long, int, bool)>();
        var third = new List<(long, int, bool)>();
        Connect(grouped, (frequency, high) => first.Add((grouped.InputClockCount, frequency, high)));
        Connect(unit, (frequency, high) => second.Add((unit.InputClockCount, frequency, high)));
        Connect(levels, (frequency, high) => third.Add((levels.InputClockCount, frequency, high)));
        grouped.AdvanceClocks(periods);
        for (var i = 0; i < periods; i++)
        {
            unit.Clock();
            levels.SetOscillator(true);
            levels.SetOscillator(false);
        }
        Assert.Equal(second, first);
        Assert.Equal(third, first);
        Assert.Equal(Levels(unit), Levels(grouped));
        Assert.Equal(Levels(levels), Levels(grouped));
        Assert.Equal(levels.InputClockCount, grouped.InputClockCount);
        Assert.Equal(unit.Ic3.Count, grouped.Ic3.Count);
        Assert.Equal(unit.Ic4.Count, grouped.Ic4.Count);
    }

    private static int ExpectedLevels(long sourceTime) =>
        (sourceTime % 2 != 0 ? 1 : 0) | (sourceTime % 3 == 2 ? 2 : 0)
        | (sourceTime % 4 >= 2 ? 4 : 0) | (sourceTime / 3 % 2 != 0 ? 8 : 0)
        | (sourceTime % 8 >= 4 ? 16 : 0) | (sourceTime / 6 % 2 != 0 ? 32 : 0)
        | (sourceTime / 12 % 2 != 0 ? 64 : 0);

    private static int Levels(SystemClockGenerator circuit) =>
        (circuit.Clock12MHz ? 1 : 0) | (circuit.Clock8MHz ? 2 : 0) | (circuit.Clock6MHz ? 4 : 0)
        | (circuit.Clock4MHz ? 8 : 0) | (circuit.Clock3MHz ? 16 : 0)
        | (circuit.Clock2MHz ? 32 : 0) | (circuit.Clock1MHz ? 64 : 0);

    private static void Connect(SystemClockGenerator circuit, Action<int, bool> edge)
    {
        circuit.Clock12MHzChanged = high => edge(12, high); circuit.Clock8MHzChanged = high => edge(8, high);
        circuit.Clock6MHzChanged = high => edge(6, high); circuit.Clock4MHzChanged = high => edge(4, high);
        circuit.Clock3MHzChanged = high => edge(3, high); circuit.Clock2MHzChanged = high => edge(2, high);
        circuit.Clock1MHzChanged = high => edge(1, high);
    }
}
