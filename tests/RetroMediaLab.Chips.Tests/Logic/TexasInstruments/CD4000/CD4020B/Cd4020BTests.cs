using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.CD4000.CD4020B;

public sealed class Cd4020BTests
{
    private const ushort Pins = 0x3ff9;
    private static Cd4020B Create(ushort seed = 0) => new(15_625, seed, Cd4020BTiming.Typical5Volts);

    [Fact]
    public void EveryPhaseCountsModulo16384AndQ2Q3RemainInternal()
    {
        for (ushort seed = 0; seed < 0x4000; seed++)
        {
            var chip = Create(seed);
            chip.SetClock(true);
            chip.SetClock(true);
            Assert.Equal(seed, chip.StageLevels);
            chip.AdvanceTo(32_000);
            chip.SetClock(false);
            chip.SetClock(false);
            chip.AdvanceTo(64_000);
            var expected = (ushort)((seed + 1) & 0x3fff);
            Assert.Equal(expected, chip.StageLevels);
            Assert.Equal((ushort)(expected & Pins), chip.OutputLevels);
            Assert.Equal(1, chip.InputClockCount);
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void InternalQ2AndQ3RippleWithoutInventingOutputPins()
    {
        var chip = Create(7);
        var trace = new List<(long, ushort, ushort)>();
        chip.OutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, levels, mask));
        chip.SetClock(true);
        chip.AdvanceTo(32_000);
        chip.SetClock(false);
        Assert.Equal(32_180, chip.NextEventNanoseconds);
        chip.AdvanceTo(32_379);
        Assert.Equal(4, chip.StageLevels);
        Assert.Equal(0, chip.OutputLevels);
        chip.AdvanceTo(32_480);
        Assert.Equal(8, chip.OutputLevels);
        Assert.Equal(new (long, ushort, ushort)[] { (32_180, 0, 1), (32_480, 8, 8) }, trace);
    }

    [Fact]
    public void Q13OnPinTwoUsesBitTwelveAndChangesAfter4096InputEdges()
    {
        var chip = Create();
        chip.SetObservedOutputs(0x1000);
        var trace = new List<(long, ushort, ushort)>();
        chip.OutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, levels, mask));
        chip.AdvanceClocks(4095);
        Assert.Empty(trace);
        chip.AdvanceClocks(1);
        Assert.Equal(new (long, ushort, ushort)[] { (262_113_380, 0x1000, 0x1000) }, trace);
        Assert.Equal(262_144_000, chip.TimeNanoseconds);
        Assert.Equal(4096, chip.InputClockCount);
        Assert.Equal(4096, chip.StageLevels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetClearsAllStagesWithoutAClockAndReleaseDoesNotCount(bool high)
    {
        var chip = Create(0x3fff);
        chip.SetClock(high);
        chip.SetReset(true);
        chip.AdvanceTo(139);
        Assert.Equal(0x3fff, chip.StageLevels);
        chip.AdvanceTo(140);
        Assert.Equal(0, chip.StageLevels);
        chip.SetClock(true);
        chip.AdvanceTo(500);
        chip.SetClock(false);
        chip.AdvanceTo(1000);
        Assert.Equal(0, chip.StageLevels);
        Assert.Equal(1, chip.InputClockCount);
        chip.SetReset(false);
        chip.AdvanceTo(2000);
        Assert.Equal(0, chip.StageLevels);
        chip.AdvanceClocks(1);
        Assert.Equal(1, chip.StageLevels);
    }

    [Fact]
    public void ResetDuringRippleCancelsTheFutureCarryAndOutputFeedbackIsHonoured()
    {
        var chip = Create(7);
        var trace = new List<(long, ushort)>();
        chip.OutputsChanged = (value, mask) =>
        {
            trace.Add((chip.TimeNanoseconds, value));
            if (mask == 1) chip.SetReset(true);
        };
        chip.AdvanceClocks(1);
        Assert.Equal(new (long, ushort)[] { (32_180, 0) }, trace);
        Assert.True(chip.ResetHigh);
        Assert.Equal(0, chip.StageLevels);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Theory]
    [InlineData(5, 180, 100, 140)]
    [InlineData(10, 80, 40, 60)]
    [InlineData(15, 65, 30, 50)]
    public void PublishedSupplyProfilesPreserveTheDistinctStageAndResetDelays(int supply, long first, long later, long reset)
    {
        var profile = supply switch
        {
            5 => Cd4020BTiming.Typical5Volts,
            10 => Cd4020BTiming.Typical10Volts,
            _ => Cd4020BTiming.Typical15Volts
        };
        Assert.Equal(supply, profile.ReferenceSupplyVolts);
        var chip = new Cd4020B(15_625, 0x3fff, profile);
        var times = new List<long>();
        chip.OutputsChanged = (_, _) => times.Add(chip.TimeNanoseconds);
        chip.AdvanceClocks(1);
        Assert.Equal(12, times.Count);
        Assert.Equal(32_000 + first, times[0]);
        Assert.Equal(32_000 + first + 13 * later, times[^1]);
        var clear = new Cd4020B(15_625, 15, profile);
        clear.SetReset(true);
        Assert.Equal(reset, clear.NextEventNanoseconds);
        clear.AdvanceTo(reset);
        Assert.Equal(0, clear.StageLevels);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(0x1000)]
    [InlineData(0x3ff9)]
    public void GroupingMatchesTimedEdgesWithFeedbackAndChangesOfSubscription(ushort mask)
    {
        foreach (ushort seed in new ushort[] { 0, 3, 7, 4095, 8191, 16383 })
        {
            var grouped = Create(seed);
            var explicitPins = Create(seed);
            var actual = Observe(grouped);
            var expected = Observe(explicitPins);
            grouped.AdvanceClocks(8195);
            for (var i = 0; i < 8195; i++)
            {
                var start = explicitPins.TimeNanoseconds;
                explicitPins.SetClock(true);
                explicitPins.AdvanceTo(start + 32_000);
                explicitPins.SetClock(false);
                explicitPins.AdvanceTo(start + 64_000);
            }
            Assert.Equal(expected, actual);
            Assert.Equal(explicitPins.StageLevels, grouped.StageLevels);
            Assert.Equal(explicitPins.TimeNanoseconds, grouped.TimeNanoseconds);
            Assert.Equal(8195, grouped.InputClockCount);

            List<(long, ushort, ushort)> Observe(Cd4020B chip)
            {
                var trace = new List<(long, ushort, ushort)>();
                chip.SetObservedOutputs(mask);
                chip.OutputsChanged = (value, changed) =>
                {
                    trace.Add((chip.TimeNanoseconds, value, changed));
                    if (trace.Count == 2) chip.SetObservedOutputs(Pins);
                    if (trace.Count == 4) chip.SetReset(true);
                    if (trace.Count == 5) chip.OutputsChanged = null;
                };
                return trace;
            }
        }
    }

    [Fact]
    public void ExpiredDeadlineIsReplacedBeforeTheOutputNotificationAndHeldLevelsDoNotRepublish()
    {
        var chip = Create(7);
        var deadlines = new List<long?>();
        chip.NextEventNanosecondsChanged = deadlines.Add;
        chip.OutputsChanged = (_, changed) =>
        {
            if (changed == 1) Assert.Equal(32_280, chip.NextEventNanoseconds);
        };
        chip.SetClock(true);
        chip.AdvanceTo(32_000);
        chip.SetClock(false);
        chip.SetClock(false);
        chip.AdvanceTo(32_180);
        chip.AdvanceTo(32_180);
        chip.AdvanceTo(64_000);
        Assert.Equal(new long?[] { 32_180, 32_280, null }, deadlines);
    }

    [Fact]
    public void ANewResetDeadlineDuringOutputNotificationReplacesTheOldCarry()
    {
        var chip = Create(7);
        chip.OutputsChanged = (_, changed) =>
        {
            if (changed != 1) return;
            chip.SetReset(true);
            Assert.Equal(32_320, chip.NextEventNanoseconds);
        };
        chip.AdvanceClocks(1);
        Assert.Equal(0, chip.StageLevels);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void InFlightRippleIsRetainedAcrossShortPeriodBoundaries()
    {
        var chip = new Cd4020B(1_000_000, 0x3fff, Cd4020BTiming.Typical5Volts);
        chip.AdvanceClocks(1);
        Assert.NotEqual(0, chip.StageLevels);
        Assert.NotNull(chip.NextEventNanoseconds);
        chip.AdvanceClocks(1000);
        chip.AdvanceTo(chip.TimeNanoseconds + 5000);
        Assert.Equal(1000, chip.StageLevels);
        Assert.Equal(1001, chip.InputClockCount);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void NoObserverDoesNotRemoveTheFourteenBitStateOrInventAnotherClock()
    {
        var chip = Create(13);
        chip.AdvanceClocks(10_000_003);
        Assert.Equal((13 + 10_000_003) & 0x3fff, chip.StageLevels);
        Assert.Equal(640_000_192_000, chip.TimeNanoseconds);
        Assert.Equal(10_000_003, chip.InputClockCount);
        Assert.False(chip.ClockHigh);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Theory]
    [InlineData(5, 3_500_000)]
    [InlineData(10, 8_000_000)]
    [InlineData(15, 12_000_000)]
    public void TypicalProfilesCountAtTheirRatedInputFrequencyWithTheirOwnRippleStillInFlight(int supply, long frequency)
    {
        var timing = supply switch
        {
            5 => Cd4020BTiming.Typical5Volts,
            10 => Cd4020BTiming.Typical10Volts,
            _ => Cd4020BTiming.Typical15Volts
        };
        var chip = new Cd4020B(frequency, 0x3fff, timing);
        for (var edge = 0; edge < 1001; edge++)
        {
            chip.AdvanceTo(edge * 1_000_000_000L / frequency);
            chip.SetClock(true);
            chip.AdvanceTo((2 * edge + 1) * 1_000_000_000L / (2 * frequency));
            chip.SetClock(false);
        }
        chip.AdvanceTo(chip.TimeNanoseconds + timing.FullRippleNanoseconds);
        Assert.Equal(1000, chip.StageLevels);
        Assert.Equal(1001, chip.InputClockCount);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void CustomTimingChangesPropagationWithoutChangingTheFunctionTable()
    {
        var chip = new Cd4020B(15_625, 7, new(5, 100, 50, 80));
        var trace = new List<long>();
        chip.OutputsChanged = (_, _) => trace.Add(chip.TimeNanoseconds);
        chip.AdvanceClocks(1);
        Assert.Equal(new long[] { 32_100, 32_250 }, trace);
        Assert.Equal(8, chip.StageLevels);
        chip.SetReset(true);
        chip.AdvanceTo(chip.TimeNanoseconds + 80);
        Assert.Equal(0, chip.StageLevels);
    }

    [Fact]
    public void InvalidParametersOverflowAndFractionalPeriodsDoNotCorruptState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020B(0, 0, Cd4020BTiming.Typical5Volts));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020B(3_500_001, 0, Cd4020BTiming.Typical5Volts));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(0x4000));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020BTiming(6, 180, 100, 140));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020BTiming(5, 361, 100, 140));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020BTiming(5, 180, 0, 140));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4020BTiming(5, 180, 100, 281));
        var chip = Create(15);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(0x4000));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(-1));
        Assert.Throws<OverflowException>(() => chip.AdvanceClocks(long.MaxValue));
        Assert.Equal(0, chip.InputClockCount);
        Assert.Equal(0, chip.TimeNanoseconds);
        chip.AdvanceTo(long.MaxValue - 140);
        chip.SetReset(true);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(0, chip.StageLevels);
        Assert.Null(chip.NextEventNanoseconds);
        var fractional = new Cd4020B(3_000_000, 0, Cd4020BTiming.Typical5Volts);
        Assert.Throws<InvalidOperationException>(() => fractional.AdvanceClocks(1));
        fractional.SetClock(true);
        fractional.AdvanceTo(166);
        fractional.SetClock(false);
        fractional.AdvanceTo(400);
        Assert.Equal(1, fractional.StageLevels);
    }
}
