using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS00;

public sealed class Sn74Ls00Tests
{
    private static Sn74Ls00 Create() => new(Sn74Ls00Timing.Typical);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EachGateImplementsEveryKnownAndUnknownInputCombinationIndependently(int gate)
    {
        foreach (var a in new bool?[] { null, false, true })
        foreach (var b in new bool?[] { null, false, true })
        {
            var chip = Create();
            chip.SetInputs(gate, a, b);
            chip.AdvanceTo(15);
            bool? expected = a == false || b == false ? true : a == true && b == true ? false : null;
            Assert.Equal(expected, chip.GetOutput(gate));
            for (var other = 0; other < 4; other++)
                if (other != gate) Assert.Null(chip.GetOutput(other));
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void RisingAndFallingPropagationUseTheirOwnDatesAndSameInputsDoNotRearm()
    {
        var chip = Create();
        var trace = new List<(long, bool?)>();
        chip.OutputChanged = (_, value) => trace.Add((chip.TimeNanoseconds, value));
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(5);
        chip.SetInputB(0, false); // Same NAND result, not a new output response.
        Assert.Equal(9, chip.NextEventNanoseconds);
        chip.AdvanceTo(8);
        Assert.Null(chip.GetOutput(0));
        chip.AdvanceTo(9);
        Assert.True(chip.GetOutput(0));
        chip.AdvanceTo(20);
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(29);
        Assert.True(chip.GetOutput(0));
        chip.AdvanceTo(30);
        Assert.False(chip.GetOutput(0));
        Assert.Equal(new (long, bool?)[] { (9, true), (30, false) }, trace);
    }

    [Fact]
    public void SimultaneousInputWritesPublishOnlyTheResolvedResultOfTheFront()
    {
        var chip = Create();
        var trace = new List<(long, bool?)>();
        chip.OutputChanged = (_, value) => trace.Add((chip.TimeNanoseconds, value));
        chip.SetInputs(0, false, false);
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(20);
        Assert.Equal(new (long, bool?)[] { (10, false) }, trace);
    }

    [Fact]
    public void ReturningToTheEstablishedOutputCancelsTheSupersededResponse()
    {
        var chip = Create();
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(20);
        chip.SetInputA(0, true);
        chip.AdvanceTo(25);
        chip.SetInputA(0, false);
        Assert.Null(chip.NextEventNanoseconds);
        chip.AdvanceTo(50);
        Assert.True(chip.GetOutput(0));
    }

    [Fact]
    public void AnUnknownInputDoesNotBecomeAKnownFloatingHigh()
    {
        var chip = Create();
        chip.SetInputs(0, false, null);
        chip.AdvanceTo(20);
        Assert.True(chip.GetOutput(0));
        chip.SetInputA(0, true);
        chip.AdvanceTo(29);
        Assert.True(chip.GetOutput(0));
        chip.AdvanceTo(30);
        Assert.Null(chip.GetOutput(0));
    }

    [Fact]
    public void AllGatesAtOneDateResolveBeforePublicationAndObserverChangesTakeEffect()
    {
        var chip = Create();
        var seen = new List<int>();
        chip.OutputChanged = (gate, _) =>
        {
            for (var other = 0; other < 4; other++) Assert.True(chip.GetOutput(other));
            seen.Add(gate);
            if (gate == 0) chip.SetObservedOutputs(8);
        };
        for (var gate = 0; gate < 4; gate++) chip.SetInputs(gate, false, true);
        chip.AdvanceTo(9);
        Assert.Equal(new[] { 0, 3 }, seen);
    }

    [Fact]
    public void CascadeReceivesRealOutputChangesAtTheirPropagationDates()
    {
        var chip = Create();
        chip.SetInputB(1, true);
        chip.OutputChanged = (gate, level) =>
        {
            if (gate == 0) chip.SetInputA(1, level);
        };
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(10);
        Assert.False(chip.GetOutput(0));
        Assert.Equal(19, chip.NextEventNanoseconds);
        chip.AdvanceTo(19);
        Assert.True(chip.GetOutput(1));
    }

    [Fact]
    public void CrossCoupledNandsHoldSetAndResetWithoutASoftwareClock()
    {
        var chip = Create();
        chip.OutputChanged = (gate, level) =>
        {
            if (gate == 0) chip.SetInputB(1, level);
            else if (gate == 1) chip.SetInputB(0, level);
        };
        chip.SetInputA(0, false);
        chip.SetInputA(1, true);
        chip.AdvanceTo(30);
        Assert.True(chip.GetOutput(0)); Assert.False(chip.GetOutput(1));
        chip.SetInputA(0, true);
        chip.AdvanceTo(100);
        Assert.True(chip.GetOutput(0)); Assert.False(chip.GetOutput(1));
        chip.SetInputA(1, false);
        chip.AdvanceTo(130);
        Assert.False(chip.GetOutput(0)); Assert.True(chip.GetOutput(1));
        chip.SetInputA(1, true);
        chip.AdvanceTo(200);
        Assert.False(chip.GetOutput(0)); Assert.True(chip.GetOutput(1));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void DeadlineNotificationsTrackArmCancelAndCompletionWithoutRepeatedLevels()
    {
        var chip = Create();
        var trace = new List<long?>();
        chip.NextEventNanosecondsChanged = trace.Add;
        chip.SetInputs(0, false, true);
        chip.SetInputs(0, false, false);
        chip.AdvanceTo(9);
        chip.AdvanceTo(9);
        Assert.Equal(new long?[] { 9, null }, trace);
        chip.OutputChanged = (_, _) => Assert.Throws<InvalidOperationException>(() => chip.AdvanceTo(100));
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(30);
        Assert.False(chip.GetOutput(0));
    }

    [Fact]
    public void CustomProfilePreservesTheTableWithItsOwnDelays()
    {
        var chip = new Sn74Ls00(new(3, 4));
        chip.SetInputs(0, false, true);
        Assert.Equal(3, chip.NextEventNanoseconds);
        chip.AdvanceTo(3);
        chip.SetInputA(0, true);
        Assert.Equal(7, chip.NextEventNanoseconds);
        chip.AdvanceTo(7);
        Assert.False(chip.GetOutput(0));
    }

    [Fact]
    public void InvalidParametersAndOverflowPreserveTheEstablishedOutputAndMaxValueIsARealDate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls00Timing(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls00Timing(9, 16));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputA(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.GetOutput(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(-1));
        chip.AdvanceTo(long.MaxValue - 9);
        chip.SetInputs(0, false, true);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.True(chip.GetOutput(0));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Throws<OverflowException>(() => chip.SetInputs(0, true, true));
        Assert.True(chip.GetOutput(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void KnownInputOverloadsPreserveEveryOutputDateAndStateOfTheNullablePath()
    {
        var known = Create();
        var nullable = Create();
        var actual = new List<(long, int, bool?)>();
        var expected = new List<(long, int, bool?)>();
        known.OutputChanged = (gate, level) => actual.Add((known.TimeNanoseconds, gate, level));
        nullable.OutputChanged = (gate, level) => expected.Add((nullable.TimeNanoseconds, gate, level));
        for (var step = 0; step < 1000; step++)
        {
            for (var gate = 0; gate < 4; gate++)
            {
                var a = ((step >> gate) & 1) != 0;
                var b = ((step >> (3 - gate)) & 1) != 0;
                known.SetInputs(gate, a, b);
                nullable.SetInputs(gate, (bool?)a, b);
            }
            known.AdvanceTo(step * 20L + 20);
            nullable.AdvanceTo(step * 20L + 20);
        }
        Assert.Equal(expected, actual);
        for (var gate = 0; gate < 4; gate++) Assert.Equal(nullable.GetOutput(gate), known.GetOutput(gate));
    }

    [Fact]
    public void PackedPinBanksMatchIndependentScalarInputsIncludingUnknownPins()
    {
        foreach (byte aKnown in new byte[] { 0, 3, 5, 10, 15 })
        foreach (byte bKnown in new byte[] { 0, 3, 5, 10, 15 })
        for (byte aLevels = 0; aLevels < 16; aLevels++)
        for (byte bLevels = 0; bLevels < 16; bLevels++)
        {
            var packed = Create();
            var scalar = Create();
            var actual = new List<(long, int, bool?)>();
            var expected = new List<(long, int, bool?)>();
            packed.OutputChanged = (gate, level) => actual.Add((packed.TimeNanoseconds, gate, level));
            scalar.OutputChanged = (gate, level) => expected.Add((scalar.TimeNanoseconds, gate, level));
            packed.SetInputLevels(aLevels, aKnown, bLevels, bKnown);
            for (var gate = 0; gate < 4; gate++)
            {
                bool? a = (aKnown & (1 << gate)) == 0 ? null : (aLevels & (1 << gate)) != 0;
                bool? b = (bKnown & (1 << gate)) == 0 ? null : (bLevels & (1 << gate)) != 0;
                scalar.SetInputs(gate, a, b);
            }
            packed.AdvanceTo(15);
            scalar.AdvanceTo(15);
            Assert.Equal(expected, actual);
            for (var gate = 0; gate < 4; gate++) Assert.Equal(scalar.GetOutput(gate), packed.GetOutput(gate));
        }
    }

    [Fact]
    public void OverflowInACommonInputFrontDoesNotPartiallyArmAnotherGate()
    {
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputLevels(16, 15, 0, 15));
        chip.AdvanceTo(long.MaxValue - 9);
        Assert.Throws<OverflowException>(() => chip.SetInputLevels(2, 3, 3, 3));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Null(chip.GetOutput(0)); Assert.Null(chip.GetOutput(1));
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(long.MaxValue);
        Assert.True(chip.GetOutput(0));
    }
}
