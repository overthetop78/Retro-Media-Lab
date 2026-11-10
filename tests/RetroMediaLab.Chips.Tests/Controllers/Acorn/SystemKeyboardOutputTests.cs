using RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemKeyboardOutputTests
{
    [Theory]
    [InlineData(1_000_000, 8)]
    [InlineData(2_000_000, 16)]
    public void OverlappingValidatedKeysKeepTheRealAkdLowInterval(int frequency, int gap)
    {
        var chip = new SystemKeyboardOutput(frequency);
        var edges = new List<(long Time, bool High, byte Data)>();
        chip.StrobeChanged = high => edges.Add((chip.ClockCount, high, chip.Data));
        chip.SetInputs((byte)'b', true, false, true);
        chip.AdvanceTo(20_000);
        chip.SetInputs((byte)'a', true, false, true);
        Assert.Equal((byte)'a', chip.Data);
        Assert.False(chip.Strobe);
        Assert.Equal(20_000 + gap, chip.NextClockEvent);
        chip.AdvanceTo(20_000 + gap - 1);
        Assert.False(chip.Strobe);
        chip.AdvanceTo(20_000 + gap);
        Assert.True(chip.Strobe);
        Assert.Equal(new[] { (0L, false, (byte)'b'), (20_000L, true, (byte)'b'),
            (20_000L + gap, false, (byte)'a') }, edges);
    }

    [Fact]
    public void ReleasingBeforeTheEncoderReassertionCancelsIt()
    {
        var chip = new SystemKeyboardOutput(2_000_000);
        var edges = new List<bool>(); chip.StrobeChanged = edges.Add;
        chip.SetInputs((byte)'a', true, false, true);
        chip.AdvanceTo(100);
        chip.SetInputs((byte)'b', true, false, true);
        chip.AdvanceTo(105);
        chip.SetInputs(null, false, false, false);
        chip.AdvanceTo(1000);
        Assert.Equal(new[] { false, true }, edges);
        Assert.False(chip.Strobe); Assert.Null(chip.NextClockEvent);
        Assert.Equal((byte)'b', chip.Data);
    }

    [Fact]
    public void ADataChangeOnTheSameHeldKeyDoesNotInventANewDepression()
    {
        var chip = new SystemKeyboardOutput(1_000_000);
        var edges = new List<bool>(); chip.StrobeChanged = edges.Add;
        chip.SetInputs((byte)',', true, false, true);
        var deadline = chip.NextClockEvent;
        chip.SetInputs((byte)'?', true, false, false);
        Assert.Equal((byte)'?', chip.Data);
        Assert.True(chip.Strobe); Assert.Equal(deadline, chip.NextClockEvent);
        Assert.Equal(new[] { false }, edges);
        chip.SetInputs(null, false, false, false);
        chip.SetInputs((byte)'?', true, false, true);
        Assert.Equal(new[] { false, true, false }, edges);
    }

    [Fact]
    public void RepeatRetainsTheReleasedCodeAndResetKeepsTime()
    {
        var chip = new SystemKeyboardOutput(1_000_000);
        chip.SetInputs((byte)'a', true, false, true);
        chip.AdvanceTo(10_000);
        chip.SetInputs(null, false, false, false);
        chip.SetInputs(null, false, true, false);
        chip.AdvanceTo(60_000);
        Assert.True(chip.Strobe); Assert.Equal((byte)'a', chip.Data);
        chip.Reset();
        Assert.Equal(60_000, chip.ClockCount);
        Assert.Equal(0x7f, chip.Data); Assert.False(chip.Strobe); Assert.Null(chip.NextClockEvent);
    }

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    public void GroupedAndUnitAdvancesKeepEachObservableTransition(int frequency)
    {
        var a = new SystemKeyboardOutput(frequency); var b = new SystemKeyboardOutput(frequency);
        var actual = new List<(long, bool)>(); var expected = new List<(long, bool)>();
        a.StrobeChanged = high => actual.Add((a.ClockCount, high));
        b.StrobeChanged = high => expected.Add((b.ClockCount, high));
        a.SetInputs((byte)'x', true, false, true); b.SetInputs((byte)'x', true, false, true);
        Advance(1000);
        a.SetInputs((byte)'y', true, false, true); b.SetInputs((byte)'y', true, false, true);
        Advance(frequency);
        Assert.Equal(expected, actual);
        Assert.Equal(b.NextClockEvent, a.NextClockEvent);
        Assert.Equal(b.Strobe, a.Strobe); Assert.Equal(b.Data, a.Data);
        void Advance(long target)
        {
            a.AdvanceTo(target);
            for (var t = b.ClockCount + 1; t <= target; t++) b.AdvanceTo(t);
        }
    }
}
