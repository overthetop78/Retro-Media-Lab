using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.CD4000.CD4013B;

public sealed class Cd4013BTests
{
    private static Cd4013B Create(bool? first = false, bool? second = false) =>
        new(2_000_000, 1_000_000, Cd4013BTiming.Typical5Volts, first, second);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EveryFunctionTableCombinationAppliesToBothIndependentChannels(int channel)
    {
        foreach (var set in new[] { false, true })
        foreach (var reset in new[] { false, true })
        foreach (var data in new[] { false, true })
        foreach (var initial in new[] { false, true })
        {
            var chip = Create(initial, initial);
            chip.SetAsynchronousInputs(channel, set, reset);
            chip.AdvanceTo(200);
            Assert.Equal(set || reset ? set : initial, chip.GetQ(channel));
            Assert.Equal(set || reset ? reset : !initial, chip.GetNotQ(channel));
            chip.SetData(channel, data);
            chip.AdvanceTo(250);
            chip.SetClock(channel, true);
            chip.AdvanceTo(400);
            Assert.Equal(set || reset ? set : data, chip.GetQ(channel));
            Assert.Equal(set || reset ? reset : !data, chip.GetNotQ(channel));
            chip.SetData(channel, !data);
            chip.SetClock(channel, true);
            chip.AdvanceTo(600);
            Assert.Equal(set || reset ? set : data, chip.GetQ(channel));
            chip.SetClock(channel, false);
            Assert.Equal(1, chip.GetInputClockCount(channel));
            Assert.Equal(0, chip.GetInputClockCount(1 - channel));
            Assert.Equal(initial, chip.GetQ(1 - channel));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsynchronousRiseAndFallRemainDistinctPhysicalTransitions(bool reset)
    {
        var chip = Create(reset);
        var trace = new List<(long, bool?, bool?)>();
        chip.FirstOutputsChanged = (q, notQ) => trace.Add((chip.TimeNanoseconds, q, notQ));
        if (reset) chip.SetReset(0, true); else chip.SetSet(0, true);
        chip.AdvanceTo(149);
        Assert.Equal(reset, chip.GetQ(0));
        chip.AdvanceTo(150);
        Assert.True(chip.GetQ(0)); Assert.True(chip.GetNotQ(0));
        chip.AdvanceTo(200);
        Assert.Equal(!reset, chip.GetQ(0));
        Assert.Equal(reset, chip.GetNotQ(0));
        Assert.Equal(new (long, bool?, bool?)[] { (150, true, true), (200, !reset, reset) }, trace);
        Assert.Equal(0, chip.GetInputClockCount(0));
    }

    [Fact]
    public void ReleaseDuringAnOrdinaryOutputSkewDoesNotInventAnUnknownLogicalState()
    {
        var chip = Create();
        chip.SetSet(0, true);
        chip.AdvanceTo(175);
        chip.SetSet(0, false);
        chip.AdvanceTo(200);
        Assert.True(chip.GetQ(0)); Assert.False(chip.GetNotQ(0));
        Assert.Equal(0, chip.GetInputClockCount(0));
    }

    [Fact]
    public void SimultaneousControlsHaveNoPriorityAndTheirSimultaneousReleaseHasNoInventedResolution()
    {
        var chip = Create();
        chip.SetAsynchronousInputs(0, true, true);
        chip.AdvanceTo(200);
        Assert.True(chip.GetQ(0)); Assert.True(chip.GetNotQ(0));
        chip.SetAsynchronousInputs(0, false, false);
        Assert.Null(chip.GetQ(0)); Assert.Null(chip.GetNotQ(0));
        chip.SetData(0, true);
        chip.AdvanceTo(250);
        chip.SetClock(0, true);
        chip.AdvanceTo(400);
        Assert.True(chip.GetQ(0)); Assert.False(chip.GetNotQ(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessiveReleaseResolvesAccordingToTheControlStillAsserted(bool releaseReset)
    {
        var chip = Create();
        chip.SetAsynchronousInputs(0, true, true);
        chip.AdvanceTo(200);
        if (releaseReset) chip.SetReset(0, false); else chip.SetSet(0, false);
        chip.AdvanceTo(400);
        Assert.Equal(releaseReset, chip.GetQ(0));
        Assert.Equal(!releaseReset, chip.GetNotQ(0));
        chip.SetAsynchronousInputs(0, false, false);
        chip.AdvanceTo(800);
        Assert.Equal(releaseReset, chip.GetQ(0));
    }

    [Fact]
    public void CapturedDataDoesNotFollowLaterDChangesAndARepeatedHighDoesNotClockAgain()
    {
        var chip = Create();
        chip.SetData(0, true);
        chip.AdvanceTo(50);
        chip.SetClock(0, true);
        chip.AdvanceTo(100);
        chip.SetData(0, false);
        chip.SetClock(0, true);
        chip.AdvanceTo(200);
        Assert.True(chip.GetQ(0));
        Assert.Equal(1, chip.GetInputClockCount(0));
        chip.SetClock(0, false);
        chip.AdvanceTo(500);
        chip.SetClock(0, true);
        chip.AdvanceTo(650);
        Assert.False(chip.GetQ(0));
        Assert.Equal(2, chip.GetInputClockCount(0));
    }

    [Fact]
    public void UnknownDPropagatesOnlyAtTheCapturedOutputDeadline()
    {
        var chip = Create(true);
        chip.SetData(0, null);
        chip.SetClock(0, true);
        chip.AdvanceTo(149);
        Assert.True(chip.GetQ(0));
        chip.AdvanceTo(150);
        Assert.Null(chip.GetQ(0)); Assert.Null(chip.GetNotQ(0));
        var uninitialized = new Cd4013B(1, 1, Cd4013BTiming.Typical5Volts);
        Assert.Null(uninitialized.GetQ(0)); Assert.Null(uninitialized.GetQ(1));
    }

    [Fact]
    public void SharedClockCapturesBothDataBeforeDeadlineAndOutputFeedback()
    {
        var chip = Create(false, true);
        chip.SetData(0, true); chip.SetData(1, false);
        chip.NextEventNanosecondsChanged = _ => chip.SetData(1, true);
        chip.FirstOutputsChanged = (_, _) =>
        {
            Assert.False(chip.GetQ(1));
            chip.SetData(1, true);
        };
        chip.SetClocks(true, true);
        chip.AdvanceTo(150);
        Assert.True(chip.GetQ(0)); Assert.False(chip.GetQ(1));
        Assert.Equal(1, chip.GetInputClockCount(0)); Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Equal(2_000_000, chip.GetClockFrequency(0));
        Assert.Equal(1_000_000, chip.GetClockFrequency(1));
    }

    [Fact]
    public void AsynchronousOverrideCancelsAnOppositeClockTargetButDoesNotPostponeAnIdenticalTarget()
    {
        var cancelled = Create();
        cancelled.SetData(0, true);
        cancelled.SetClock(0, true);
        cancelled.AdvanceTo(50);
        cancelled.SetReset(0, true);
        cancelled.AdvanceTo(300);
        Assert.False(cancelled.GetQ(0)); Assert.True(cancelled.GetNotQ(0));
        var reinforced = Create();
        reinforced.SetData(0, true);
        reinforced.SetClock(0, true);
        reinforced.AdvanceTo(50);
        reinforced.SetSet(0, true);
        reinforced.AdvanceTo(150);
        Assert.True(reinforced.GetQ(0)); Assert.False(reinforced.GetNotQ(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedPeriodsMatchEveryTimedEdgeWithCascadeAndFeedback(bool toggle)
    {
        var grouped = Create();
        var detailed = Create();
        var actual = Observe(grouped);
        var expected = Observe(detailed);
        grouped.AdvanceClocks(0, 1001);
        for (var i = 0; i < 1001; i++)
        {
            var start = detailed.TimeNanoseconds;
            detailed.SetClock(0, true);
            detailed.AdvanceTo(start + 250);
            detailed.SetClock(0, false);
            detailed.AdvanceTo(start + 500);
        }
        Assert.Equal(expected, actual);
        for (var channel = 0; channel < 2; channel++)
        {
            Assert.Equal(detailed.GetInputClockCount(channel), grouped.GetInputClockCount(channel));
            Assert.Equal(detailed.GetQ(channel), grouped.GetQ(channel));
            Assert.Equal(detailed.GetNotQ(channel), grouped.GetNotQ(channel));
        }
        Assert.Equal(detailed.TimeNanoseconds, grouped.TimeNanoseconds);

        List<(long, int, bool?, bool?)> Observe(Cd4013B chip)
        {
            var trace = new List<(long, int, bool?, bool?)>();
            chip.SetData(0, toggle); chip.SetData(1, toggle);
            chip.FirstOutputsChanged = (q, notQ) =>
            {
                trace.Add((chip.TimeNanoseconds, 0, q, notQ));
                chip.SetData(0, notQ);
                chip.SetClock(1, q!.Value);
            };
            chip.SecondOutputsChanged = (q, notQ) =>
            {
                trace.Add((chip.TimeNanoseconds, 1, q, notQ));
                chip.SetData(1, notQ);
                if (trace.Count == 20) chip.SecondOutputsChanged = null;
            };
            return trace;
        }
    }

    [Fact]
    public void StableCapturesDoNotSkipAnotherChannelsPendingOutput()
    {
        var chip = Create();
        chip.SetData(0, false); chip.SetData(1, true);
        chip.SetClock(1, true);
        chip.AdvanceClocks(0, 1000);
        Assert.True(chip.GetQ(1));
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Equal(1000, chip.GetInputClockCount(0));
    }

    [Theory]
    [InlineData(5, 150, 200)]
    [InlineData(10, 65, 85)]
    [InlineData(15, 45, 60)]
    public void PublishedProfilesKeepTheirClockAndAsynchronousDelays(int supply, long rise, long fall)
    {
        var timing = supply switch
        {
            5 => Cd4013BTiming.Typical5Volts,
            10 => Cd4013BTiming.Typical10Volts,
            _ => Cd4013BTiming.Typical15Volts
        };
        var chip = new Cd4013B(1_000_000, 1_000_000, timing, false, false);
        chip.SetSet(0, true);
        Assert.Equal(rise, chip.NextEventNanoseconds);
        chip.AdvanceTo(rise);
        Assert.True(chip.GetQ(0)); Assert.True(chip.GetNotQ(0));
        chip.AdvanceTo(fall);
        Assert.False(chip.GetNotQ(0));
        Assert.Equal(supply, timing.ReferenceSupplyVolts);
        var custom = new Cd4013B(1_000_000, 1_000_000, new(5, 10, 20, 30), false, false);
        custom.SetSet(0, true);
        custom.AdvanceTo(20);
        Assert.True(custom.GetQ(0)); Assert.True(custom.GetNotQ(0));
        custom.AdvanceTo(30);
        Assert.False(custom.GetNotQ(0));
    }

    [Fact]
    public void OutputFeedbackCanReplaceTheRemainingDeadlineAtTheSameInstant()
    {
        var chip = Create();
        chip.FirstOutputsChanged = (q, notQ) =>
        {
            if (q == true && notQ == true)
            {
                chip.SetAsynchronousInputs(0, false, true);
                Assert.Equal(350, chip.NextEventNanoseconds);
            }
        };
        chip.SetSet(0, true);
        chip.AdvanceTo(400);
        Assert.False(chip.GetQ(0)); Assert.True(chip.GetNotQ(0));
    }

    [Fact]
    public void InvalidArgumentsAndTimeOverflowAreRejectedAndMaxValueRemainsAValidDeadline()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4013B(0, 1, Cd4013BTiming.Typical5Volts));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4013B(3_500_001, 1, Cd4013BTiming.Typical5Volts));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4013BTiming(3, 150, 150, 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4013BTiming(5, 301, 150, 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cd4013BTiming(5, 150, 0, 200));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetClock(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(0, -1));
        Assert.Throws<OverflowException>(() => chip.AdvanceClocks(0, long.MaxValue));
        Assert.Equal(0, chip.TimeNanoseconds);
        chip.SetData(0, true);
        chip.AdvanceTo(long.MaxValue - 150);
        chip.SetClock(0, true);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.True(chip.GetQ(0)); Assert.Null(chip.NextEventNanoseconds);
        var control = Create();
        control.AdvanceTo(long.MaxValue - 10);
        Assert.Throws<OverflowException>(() => control.SetSet(0, true));
        Assert.False(control.GetSet(0));
        var fractional = new Cd4013B(3_000_000, 1, Cd4013BTiming.Typical5Volts);
        Assert.Throws<InvalidOperationException>(() => fractional.AdvanceClocks(0, 1));
    }

    [Fact]
    public void UnknownReleaseNotificationCannotAdvanceTheComponentsTimeRecursively()
    {
        var chip = Create();
        chip.SetAsynchronousInputs(0, true, true);
        chip.AdvanceTo(200);
        chip.FirstOutputsChanged = (q, _) =>
        {
            if (q.HasValue || chip.TimeNanoseconds != 200) return;
            Assert.Throws<InvalidOperationException>(() => chip.AdvanceTo(300));
            chip.AdvanceTo(200);
            chip.SetReset(0, true);
        };
        chip.SetAsynchronousInputs(0, false, false);
        chip.AdvanceTo(400);
        Assert.False(chip.GetQ(0)); Assert.True(chip.GetNotQ(0));
    }

    [Theory]
    [InlineData(5, 3_500_000)]
    [InlineData(10, 8_000_000)]
    [InlineData(15, 12_000_000)]
    public void TypicalProfilesPreserveToggleFeedbackAtTheirRecommendedMaximumFrequency(int supply, long frequency)
    {
        var timing = supply switch
        {
            5 => Cd4013BTiming.Typical5Volts,
            10 => Cd4013BTiming.Typical10Volts,
            _ => Cd4013BTiming.Typical15Volts
        };
        var chip = new Cd4013B(frequency, 1, timing, false, false);
        chip.SetData(0, true);
        chip.FirstOutputsChanged = (_, notQ) => chip.SetData(0, notQ);
        for (var i = 0; i < 1001; i++)
        {
            chip.AdvanceTo(100 + i * 1_000_000_000L / frequency);
            chip.SetClock(0, true);
            chip.AdvanceTo(100 + (2 * i + 1) * 1_000_000_000L / (2 * frequency));
            chip.SetClock(0, false);
        }
        chip.AdvanceTo(chip.TimeNanoseconds + timing.ClockNanoseconds);
        Assert.True(chip.GetQ(0)); Assert.False(chip.GetNotQ(0));
        Assert.Equal(1001, chip.GetInputClockCount(0));
        Assert.Equal(0, chip.GetInputClockCount(1));
    }
}
