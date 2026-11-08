using RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemKeyboardRepeatTests
{
    [Fact]
    public void AutoRepeatUsesInitialDelayThenPreservesEveryStrobeEdge()
    {
        var circuit = new SystemKeyboardRepeat(1_000_000);
        var edges = new List<(long Time, bool High)>();
        circuit.StrobeChanged = high => edges.Add((circuit.ClockCount, high));
        circuit.SetInputs(true, false);
        circuit.SetInputs(true, false);
        circuit.AdvanceTo(499_999);
        Assert.True(circuit.Strobe);
        circuit.AdvanceTo(650_000);
        Assert.Equal(new[] { (0L, false), (500_000L, true), (550_000L, false),
            (600_000L, true), (650_000L, false) }, edges);
        circuit.SetInputs(false, false);
        Assert.Null(circuit.NextClockEvent);
        Assert.False(circuit.Strobe);
    }

    [Fact]
    public void RepeatWorksWithoutAnyKeyDownAndStopsWhenReleased()
    {
        var circuit = new SystemKeyboardRepeat(1_000_000);
        circuit.SetInputs(true, false);
        circuit.AdvanceTo(10_000);
        circuit.SetInputs(false, false);
        circuit.SetInputs(false, true);
        Assert.Equal(60_000L, circuit.NextClockEvent);
        circuit.AdvanceTo(60_000);
        Assert.True(circuit.Strobe);
        circuit.SetInputs(false, true);
        Assert.Equal(110_000L, circuit.NextClockEvent);
        circuit.SetInputs(false, false);
        Assert.Null(circuit.NextClockEvent);
        circuit.AdvanceTo(1_000_000);
        Assert.False(circuit.Strobe);
    }

    [Fact]
    public void RepeatHasNoArtificialTickDelayAndSurvivesCharacterRelease()
    {
        var circuit = new SystemKeyboardRepeat(1_000_000);
        circuit.SetInputs(true, false);
        circuit.AdvanceTo(20_000);
        circuit.SetInputs(true, true);
        Assert.False(circuit.Strobe);
        Assert.Equal(70_000L, circuit.NextClockEvent);
        circuit.SetInputs(false, true);
        circuit.AdvanceTo(70_000);
        Assert.True(circuit.Strobe);
        circuit.Reset();
        Assert.False(circuit.Strobe);
        Assert.Null(circuit.NextClockEvent);
        Assert.Equal(70_000L, circuit.ClockCount);
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    public void GroupedAdvanceMatchesEveryIndividualReferencePeriod(int frequency)
    {
        var grouped = new SystemKeyboardRepeat(frequency);
        var unit = new SystemKeyboardRepeat(frequency);
        var groupedEdges = new List<(long, bool)>();
        var unitEdges = new List<(long, bool)>();
        grouped.StrobeChanged = level => groupedEdges.Add((grouped.ClockCount, level));
        unit.StrobeChanged = level => unitEdges.Add((unit.ClockCount, level));
        grouped.SetInputs(true, false);
        unit.SetInputs(true, false);
        grouped.AdvanceTo(frequency);
        for (var t = 1; t <= frequency; t++) unit.AdvanceTo(t);
        Assert.Equal(unitEdges, groupedEdges);
        Assert.Equal(unit.NextClockEvent, grouped.NextClockEvent);
        Assert.Equal(unit.Strobe, grouped.Strobe);
        Assert.Throws<ArgumentOutOfRangeException>(() => grouped.AdvanceTo(frequency - 1));
    }
}
