using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class Vdu80CursorCircuitTests
{
    [Fact]
    public void OneCharacterPulseDischargesFromItsActualChargeInsteadOfAFixedDelay()
    {
        var circuit = new Vdu80CursorCircuit(1000, 2700, 330, 0.5, 0);
        circuit.SetInput(true);
        circuit.AdvanceTo(500);
        Assert.True(circuit.OutputHigh);
        circuit.SetInput(false);
        Assert.Equal(635, circuit.NextEventNanoseconds);
        circuit.AdvanceTo(634);
        Assert.True(circuit.OutputHigh);
        circuit.AdvanceTo(635);
        Assert.False(circuit.OutputHigh);
    }

    [Fact]
    public void BoardComponentsGiveTheCalculatedTimeConstantWithoutAnOscillator()
    {
        var circuit = new Vdu80CursorCircuit(1000, 2700, 330, 0.5, 0);
        Assert.Equal(27.0 / 37, circuit.DcGain, 12);
        Assert.Equal(240.810810810811, circuit.TimeConstantNanoseconds, 10);
        circuit.SetInput(true);
        Assert.Equal(167, circuit.NextEventNanoseconds);
        circuit.AdvanceTo(166);
        Assert.False(circuit.OutputHigh);
        circuit.AdvanceTo(167);
        Assert.True(circuit.OutputHigh);
        Assert.Null(circuit.NextEventNanoseconds);
    }

    [Fact]
    public void RisingAndFallingCrossingsNotifyAtTheirLocalDeadlines()
    {
        var circuit = Fixture();
        var levels = new List<(long, bool)>();
        circuit.OutputChanged = high => levels.Add((circuit.TimeNanoseconds, high));
        circuit.SetInput(true);
        circuit.AdvanceTo(10_000);
        circuit.SetInput(false);
        Assert.Equal(10_694, circuit.NextEventNanoseconds);
        circuit.AdvanceTo(20_000);
        Assert.Equal(new[] { (694L, true), (10_694L, false) }, levels);
        Assert.Equal(20_000, circuit.TimeNanoseconds);
    }

    [Fact]
    public void AShortPulseKeepsItsChargeButNeverCrossesTheThreshold()
    {
        var circuit = Fixture();
        var levels = new List<bool>();
        circuit.OutputChanged = levels.Add;
        circuit.SetInput(true);
        circuit.AdvanceTo(200);
        var charged = circuit.NormalizedVoltage;
        Assert.Equal(1 - Math.Exp(-0.2), charged, 12);
        circuit.SetInput(false);
        Assert.Equal(charged, circuit.NormalizedVoltage, 12);
        Assert.Null(circuit.NextEventNanoseconds);
        circuit.AdvanceTo(1000);
        Assert.Equal(charged * Math.Exp(-0.8), circuit.NormalizedVoltage, 12);
        circuit.SetInput(true);
        Assert.InRange(circuit.NextEventNanoseconds!.Value, 1600, 1610);
        Assert.Empty(levels);
        circuit.AdvanceTo(1700);
        Assert.Equal(new[] { true }, levels);
    }

    [Fact]
    public void SmallAndGroupedTimeAdvancesGiveTheSameVoltageAndOutputTrace()
    {
        var grouped = Fixture();
        var unit = Fixture();
        var first = new List<(long, bool)>();
        var second = new List<(long, bool)>();
        grouped.OutputChanged = high => first.Add((grouped.TimeNanoseconds, high));
        unit.OutputChanged = high => second.Add((unit.TimeNanoseconds, high));
        var time = 0;
        foreach (var duration in new[] { 100, 200, 1200, 500, 700, 2000 })
        {
            var high = !grouped.InputHigh;
            grouped.SetInput(high);
            unit.SetInput(high);
            var target = time + duration;
            grouped.AdvanceTo(target);
            while (time < target) unit.AdvanceTo(++time);
            Assert.Equal(grouped.NormalizedVoltage, unit.NormalizedVoltage, 12);
            Assert.Equal(first, second);
            Assert.Equal(grouped.NextEventNanoseconds, unit.NextEventNanoseconds);
        }
    }

    [Fact]
    public void ACallbackCanReverseTheInputWithoutLosingTheFollowingCrossing()
    {
        var circuit = Fixture();
        var levels = new List<(long, bool)>();
        circuit.OutputChanged = high =>
        {
            levels.Add((circuit.TimeNanoseconds, high));
            if (high) circuit.SetInput(false);
        };
        circuit.SetInput(true);
        circuit.SetInput(true);
        circuit.AdvanceTo(1000);
        Assert.Equal(new[] { (694L, true), (695L, false) }, levels);
        Assert.Throws<ArgumentOutOfRangeException>(() => circuit.AdvanceTo(999));
    }

    [Fact]
    public void NonphysicalParametersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vdu80CursorCircuit(0, 2700, 330, 0.5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vdu80CursorCircuit(1000, -1, 330, 0.5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vdu80CursorCircuit(1000, 2700, double.NaN, 0.5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vdu80CursorCircuit(1000, 2700, 330, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vdu80CursorCircuit(1000, 2700, 330, 0.5, -1));
    }

    private static Vdu80CursorCircuit Fixture() => new(2000, 2000, 1000, 0.5, 0);
}
