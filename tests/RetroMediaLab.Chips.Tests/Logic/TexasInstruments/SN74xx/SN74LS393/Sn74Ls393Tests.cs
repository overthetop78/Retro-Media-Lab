using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS393;

public sealed class Sn74Ls393Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void DeferredQaClockCountMatchesExplicitInputsWithoutMaterializingTheCounter(byte observed)
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var chip = new Sn74Ls393(4_000_000, 1_000_000, seed, 0, Sn74Ls393Timing.CalculatedTypical);
            var scalar = new Sn74Ls393(4_000_000, 1_000_000, seed, 0, Sn74Ls393Timing.CalculatedTypical);
            chip.SetObservedOutputs(0, observed); chip.FirstOutputsChanged = (_, _) => { };
            chip.SetClock(0, true); scalar.SetClock(0, true);
            chip.ConnectPeriodicClock(0, 125, 250, 125);
            for (long time = 1; time <= 10_000; time++)
            {
                scalar.AdvanceTo(time); scalar.SetClock(0, time % 250 < 125);
                while (chip.NextOutputEventNanoseconds is long next && next <= time) chip.AdvanceTo(next);
                var beforeTime = chip.TimeNanoseconds; var beforeWord = chip.GetOutputs(0);
                var beforeClocks = chip.GetInputClockCount(0); var beforeQa = chip.GetQaRisingClockCount(0);
                Assert.True(chip.TryGetQaRisingClockCountAt(0, time, out var projected));
                Assert.Equal(scalar.GetQaRisingClockCount(0), projected);
                Assert.Equal(beforeTime, chip.TimeNanoseconds); Assert.Equal(beforeWord, chip.GetOutputs(0));
                Assert.Equal(beforeClocks, chip.GetInputClockCount(0)); Assert.Equal(beforeQa, chip.GetQaRisingClockCount(0));
            }
            chip.AdvanceTo(10_000);
            Assert.Equal(scalar.GetOutputs(0), chip.GetOutputs(0));
            Assert.Equal(scalar.GetQaRisingClockCount(0), chip.GetQaRisingClockCount(0));
        }
    }

    [Fact]
    public void QaClockProjectionStopsAtARequestedResponseAndRejectsUnknownOrClearedClocks()
    {
        var chip = new Sn74Ls393(4_000_000, 1_000_000, 0, 0, Sn74Ls393Timing.CalculatedTypical);
        chip.SetClock(0, true); chip.ConnectPeriodicClock(0, 125, 250, 125);
        chip.FirstQaClockReached = _ => { }; chip.RequestQaClock(0, 2);
        var deadline = chip.NextOutputEventNanoseconds!.Value;
        Assert.True(chip.TryGetQaRisingClockCountAt(0, deadline - 1, out var count)); Assert.Equal(1, count);
        Assert.False(chip.TryGetQaRisingClockCountAt(0, deadline, out _));
        chip.AdvanceTo(deadline);
        Assert.Equal(2, chip.GetQaRisingClockCount(0));
        chip.SetClear(0, true);
        Assert.False(chip.TryGetQaRisingClockCountAt(0, deadline + 1, out _));
        chip.AdvanceTo(deadline + 100); chip.SetClear(0, false); chip.DisconnectPeriodicClock(0);
        Assert.False(chip.TryGetQaRisingClockCountAt(0, deadline + 101, out _));
    }

    [Theory]
    [InlineData(40, 20)]
    [InlineData(250, 238)]
    public void PeriodicInputMatchesOverlappingRipplesAndResponsesAtTheNextInputRise(long period, long highDuration)
    {
        var timing = period == 40 ? new Sn74Ls393Timing(20, 20, 13, 13, 14, 39) : Sn74Ls393Timing.CalculatedTypical;
        var fast = new Sn74Ls393(25_000_000, 25_000_000, 15, 0, timing);
        var unit = new Sn74Ls393(25_000_000, 25_000_000, 15, 0, timing);
        var actual = new List<(long, byte, bool)>(); var expected = new List<(long, byte, bool)>();
        fast.FirstOutputsChanged = (word, _) => actual.Add((fast.TimeNanoseconds, word, fast.GetClock(0)));
        unit.FirstOutputsChanged = (word, _) => expected.Add((unit.TimeNanoseconds, word, unit.GetClock(0)));
        fast.ConnectPeriodicClock(0, period, period, highDuration);
        long next = period - highDuration; var high = false;
        for (long time = 7; time < 10000; time += 7)
        {
            while (next <= time)
            {
                unit.AdvanceTo(next); high = !high; unit.SetClock(0, high); next += high ? highDuration : period - highDuration;
            }
            unit.AdvanceTo(time); fast.AdvanceTo(time);
            Assert.Equal(unit.GetOutputs(0), fast.GetOutputs(0)); Assert.Equal(unit.GetInputClockCount(0), fast.GetInputClockCount(0));
        }
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PeriodicInputsMatchEveryInitialWordMaskChannelAndIntermediatePhase()
    {
        for (var channel = 0; channel < 2; channel++)
        for (byte initial = 0; initial < 16; initial++)
        for (byte mask = 0; mask < 16; mask++)
        {
            var fast = new Sn74Ls393(4_000_000, 4_000_000, initial, initial, Sn74Ls393Timing.CalculatedTypical);
            var unit = new Sn74Ls393(4_000_000, 4_000_000, initial, initial, Sn74Ls393Timing.CalculatedTypical);
            var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
            fast.SetObservedOutputs(channel, mask); unit.SetObservedOutputs(channel, mask);
            long? expectedDate = null;
            var silent = false;
            Action<byte, byte> a = (levels, changed) =>
            {
                Assert.False(silent);
                Assert.Equal(expectedDate, fast.TimeNanoseconds);
                actual.Add((fast.TimeNanoseconds, levels, changed));
            };
            Action<byte, byte> b = (levels, changed) => expected.Add((unit.TimeNanoseconds, levels, changed));
            if (channel == 0) { fast.FirstOutputsChanged = a; unit.FirstOutputsChanged = b; }
            else { fast.SecondOutputsChanged = a; unit.SecondOutputsChanged = b; }
            fast.ConnectPeriodicClock(channel, 250, 250, 125);
            long nextInput = 125; var level = false;
            foreach (var time in Enumerable.Range(0, 96).Select(i => i * 127L + (i % 3)))
            {
                while (nextInput <= time)
                {
                    unit.AdvanceTo(nextInput); level = !level; unit.SetClock(channel, level); nextInput += 125;
                }
                unit.AdvanceTo(time);
                while (fast.NextOutputEventNanoseconds is long due && due <= time)
                {
                    expectedDate = due; fast.AdvanceTo(due); expectedDate = null;
                }
                silent = true; fast.AdvanceTo(time); silent = false;
                Assert.Equal(unit.GetOutputs(channel), fast.GetOutputs(channel));
                Assert.Equal(unit.GetClock(channel), fast.GetClock(channel));
                Assert.Equal(unit.GetInputClockCount(channel), fast.GetInputClockCount(channel));
                Assert.Equal(unit.GetQaRisingClockCount(channel), fast.GetQaRisingClockCount(channel));
                Assert.Equal(unit.TimeNanoseconds, fast.TimeNanoseconds);
            }
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void PeriodicDeadlinesRetainRequestedClocksClearsAndAsymmetricPulses()
    {
        var fast = Create(); var unit = Create();
        fast.SetObservedOutputs(0, 8); unit.SetObservedOutputs(0, 8);
        var actual = new List<(long, byte)>(); var expected = new List<(long, byte)>();
        fast.FirstOutputsChanged = (word, _) => actual.Add((fast.TimeNanoseconds, word));
        unit.FirstOutputsChanged = (word, _) => expected.Add((unit.TimeNanoseconds, word));
        var qaActual = new List<(long, long)>(); var qaExpected = new List<(long, long)>();
        fast.FirstQaClockReached = count => { qaActual.Add((fast.TimeNanoseconds, count)); fast.RequestQaClock(0, count + 7); };
        unit.FirstQaClockReached = count => { qaExpected.Add((unit.TimeNanoseconds, count)); unit.RequestQaClock(0, count + 7); };
        fast.RequestQaClock(0, 3); unit.RequestQaClock(0, 3);
        fast.ConnectPeriodicClock(0, 250, 250, 100);
        long next = 150; var high = false;
        foreach (var time in new long[] { 1000, 1200, 2000, 2140, 2500, 9000, 12345, 20000 })
        {
            while (next <= time)
            {
                unit.AdvanceTo(next); high = !high; unit.SetClock(0, high); next += high ? 100 : 150;
            }
            unit.AdvanceTo(time);
            while (fast.NextOutputEventNanoseconds is long due && due <= time) fast.AdvanceTo(due);
            fast.AdvanceTo(time);
            Assert.Equal(unit.GetOutputs(0), fast.GetOutputs(0));
            Assert.Equal(unit.GetQaRisingClockCount(0), fast.GetQaRisingClockCount(0));
            var clear = time is 1000 or 2000;
            fast.SetClear(0, clear); unit.SetClear(0, clear);
        }
        Assert.Equal(expected, actual); Assert.Equal(qaExpected, qaActual);
    }

    [Fact]
    public void PeriodicDisconnectPreservesTheInputLevelAndExplicitEdgesReplaceTheTrain()
    {
        var chip = Create();
        chip.ConnectPeriodicClock(0, 250, 250, 125);
        chip.AdvanceTo(125); Assert.True(chip.GetClock(0));
        chip.DisconnectPeriodicClock(0); Assert.True(chip.GetClock(0));
        chip.AdvanceTo(1000); Assert.Equal(0, chip.GetInputClockCount(0));
        chip.SetClock(0, false); chip.AdvanceTo(1100);
        Assert.Equal(1, chip.GetInputClockCount(0));
        chip.ConnectPeriodicClock(0, 1250, 250, 125);
        chip.AdvanceTo(1150); chip.SetClock(0, false);
        chip.AdvanceTo(2000);
        Assert.Equal(2, chip.GetInputClockCount(0));
        Assert.False(chip.GetClock(0));
    }

    [Fact]
    public void BothPeriodicChannelsResolveSimultaneousResponsesBeforeNotifications()
    {
        var fast = Create(7, 15); var unit = Create(7, 15);
        var actual = new List<(long, int, byte, byte)>(); var expected = new List<(long, int, byte, byte)>();
        fast.FirstOutputsChanged = (word, _) => actual.Add((fast.TimeNanoseconds, 0, word, fast.GetOutputs(1)));
        fast.SecondOutputsChanged = (word, _) => actual.Add((fast.TimeNanoseconds, 1, word, fast.GetOutputs(0)));
        unit.FirstOutputsChanged = (word, _) => expected.Add((unit.TimeNanoseconds, 0, word, unit.GetOutputs(1)));
        unit.SecondOutputsChanged = (word, _) => expected.Add((unit.TimeNanoseconds, 1, word, unit.GetOutputs(0)));
        fast.ConnectPeriodicClock(0, 250, 250, 125); fast.ConnectPeriodicClock(1, 250, 250, 125);
        for (long time = 125; time <= 8000; time += 125)
        {
            unit.AdvanceTo(time);
            var high = time % 250 != 0;
            unit.SetClock(0, high); unit.SetClock(1, high);
            fast.AdvanceTo(time);
            Assert.Equal(unit.GetOutputs(0), fast.GetOutputs(0)); Assert.Equal(unit.GetOutputs(1), fast.GetOutputs(1));
            if (time is 1500 or 2000)
            {
                var clear = time == 1500;
                for (var channel = 0; channel < 2; channel++) { unit.SetClear(channel, clear); fast.SetClear(channel, clear); }
            }
        }
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AFeedbackInputCanReplaceThePeriodicSourceDuringAResponse()
    {
        var chip = Create();
        chip.ConnectPeriodicClock(0, 250, 250, 125);
        chip.FirstOutputsChanged = (_, _) => chip.SetClock(0, true);
        chip.AdvanceTo(2000);
        Assert.Equal(1, chip.GetInputClockCount(0));
        Assert.Equal(1, chip.GetQaRisingClockCount(0));
        Assert.True(chip.GetClock(0));
        chip.FirstOutputsChanged = null;
        chip.SetClock(0, false); chip.AdvanceTo(2100);
        Assert.Equal(2, chip.GetOutputs(0));
    }

    [Fact]
    public void PeriodicInputKeepsTheLastRepresentablePulseAndItsRemainingHighLevel()
    {
        var chip = Create();
        chip.AdvanceTo(long.MaxValue - 350);
        chip.ConnectPeriodicClock(0, long.MaxValue - 100, 250, 200);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(1, chip.GetInputClockCount(0)); Assert.Equal(1, chip.GetQaRisingClockCount(0));
        Assert.Equal(1, chip.GetOutputs(0)); Assert.True(chip.GetClock(0));
        Assert.Null(chip.NextEventNanoseconds); Assert.Null(chip.NextOutputEventNanoseconds);
    }

    [Fact]
    public void ConnectedCarryDeadlineCanEndAtLongMaxWithoutIntermediateOverflow()
    {
        var chip = Create(7);
        chip.SetObservedOutputs(0, 8);
        chip.FirstOutputsChanged = (_, _) => { };
        chip.AdvanceTo(long.MaxValue - 40);
        chip.SetClock(0, true); chip.SetClock(0, false);
        chip.AdvanceTo(long.MaxValue - 27);
        Assert.Equal(long.MaxValue, chip.NextOutputEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(8, chip.GetOutputs(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void QaClockListenerWakesOnlyOnTheRequestedRisingEdgeAndCanRenewItsRequest(int channel)
    {
        var chip = Create();
        chip.SetObservedOutputs(0, 0); chip.SetObservedOutputs(1, 0);
        var clocks = new List<(long, long)>();
        Action<long> reached = count =>
        {
            clocks.Add((chip.TimeNanoseconds, count));
            if (count == 3) chip.RequestQaClock(channel, 5);
        };
        if (channel == 0) chip.FirstQaClockReached = reached; else chip.SecondQaClockReached = reached;
        chip.RequestQaClock(channel, 3);
        for (var period = 0; period < 12; period++)
        {
            chip.SetClock(channel, true);
            chip.AdvanceTo(period * 250 + 125);
            chip.SetClock(channel, false);
            while (chip.NextOutputEventNanoseconds is long next && next <= (period + 1) * 250) chip.AdvanceTo(next);
            chip.AdvanceTo((period + 1) * 250);
        }
        var qaDelay = chip.Timing.QaRiseNanoseconds;
        Assert.Equal(new[] { (1125L + qaDelay, 3L), (2125L + qaDelay, 5L) }, clocks);
        Assert.Equal(6, chip.GetQaRisingClockCount(channel));
        Assert.Equal(0, chip.GetQaRisingClockCount(1 - channel));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.RequestQaClock(channel, 6));
    }

    [Fact]
    public void OutputDeadlinesMatchInternalResponsesWithClearAndBothCounterClocks()
    {
        for (byte mask = 0; mask < 16; mask++)
        {
            var fast = Create(); var reference = Create();
            var actual = new List<(long, int, byte, byte)>(); var expected = new List<(long, int, byte, byte)>();
            void Wire(Sn74Ls393 chip, List<(long, int, byte, byte)> trace)
            {
                chip.SetObservedOutputs(0, mask); chip.SetObservedOutputs(1, (byte)(15 ^ mask));
                chip.FirstOutputsChanged = (levels, changed) => trace.Add((chip.TimeNanoseconds, 0, levels, changed));
                chip.SecondOutputsChanged = (levels, changed) => trace.Add((chip.TimeNanoseconds, 1, levels, changed));
            }
            Wire(fast, actual); Wire(reference, expected);
            var random = new Random(mask);
            long time = 0;
            for (var step = 0; step < 512; step++)
            {
                time += random.Next(50, 251);
                while (fast.NextOutputEventNanoseconds is long next && next <= time) fast.AdvanceTo(next);
                while (reference.NextEventNanoseconds is long next && next <= time) reference.AdvanceTo(next);
                fast.AdvanceTo(time); reference.AdvanceTo(time);
                var channel = random.Next(2); var high = random.Next(2) != 0;
                fast.SetClock(channel, high); reference.SetClock(channel, high);
                if (step % 7 == 0) { fast.SetClear(channel, high); reference.SetClear(channel, high); }
                Assert.Equal(reference.GetOutputs(0), fast.GetOutputs(0));
                Assert.Equal(reference.GetOutputs(1), fast.GetOutputs(1));
                Assert.Equal(reference.GetQaRisingClockCount(0), fast.GetQaRisingClockCount(0));
                Assert.Equal(reference.GetQaRisingClockCount(1), fast.GetQaRisingClockCount(1));
            }
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void GroupedClocksKeepBothQaRisingCountsAcrossClear()
    {
        var grouped = Create(); var scalar = Create();
        for (var channel = 0; channel < 2; channel++)
        {
            for (var block = 0; block < 9; block++)
            {
                var clear = block % 3 == 1;
                grouped.SetClear(channel, clear); scalar.SetClear(channel, clear);
                grouped.AdvanceClocks(channel, 17);
                for (var i = 0; i < 17; i++) scalar.AdvanceClocks(channel, 1);
                Assert.Equal(scalar.GetQaRisingClockCount(channel), grouped.GetQaRisingClockCount(channel));
                Assert.Equal(scalar.GetOutputs(channel), grouped.GetOutputs(channel));
            }
        }
    }

    [Fact]
    public void RemovingObserversAfterClockRiseFeedbackKeepsTheNextWholePeriodLowEndpoint()
    {
        static Sn74Ls393 Wired()
        {
            var chip = Create();
            chip.SetObservedOutputs(0, 1);
            chip.FirstOutputsChanged = (_, _) =>
            {
                chip.SetClock(0, true);
                chip.SetObservedOutputs(0, 0);
            };
            return chip;
        }
        var grouped = Wired();
        var explicitEdges = Wired();
        grouped.AdvanceClocks(0, 2);
        for (long start = 0; start < 500; start += 250)
        {
            explicitEdges.SetClock(0, true);
            explicitEdges.AdvanceTo(start + 125);
            explicitEdges.SetClock(0, false);
            explicitEdges.AdvanceTo(start + 250);
        }
        Assert.False(explicitEdges.GetClock(0));
        Assert.Equal(explicitEdges.GetClock(0), grouped.GetClock(0));
        Assert.Equal(2, grouped.GetInputClockCount(0));
        Assert.Equal(2, grouped.GetOutputs(0));
        Assert.Equal(500, grouped.TimeNanoseconds);
        Assert.Null(grouped.NextEventNanoseconds);
    }

    [Fact]
    public void ADeadlineReceiverMovingPastThePeriodKeepsTheBackwardTimeErrorAndResultingState()
    {
        var chip = Create();
        var moved = false;
        chip.NextEventNanosecondsChanged = next =>
        {
            if (next is null || moved) return;
            moved = true;
            chip.AdvanceTo(500);
        };
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(0, 1));
        Assert.Equal("nanoseconds", error.ParamName);
        Assert.Equal(500, chip.TimeNanoseconds);
        Assert.Equal(1, chip.GetInputClockCount(0));
        Assert.Equal(1, chip.GetOutputs(0));
        Assert.False(chip.GetClock(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void PeriodicAdvancementKeepsSourceClockRiseFeedbackBetweenItsFallingEdges()
    {
        var chip = Create();
        var trace = new List<(long, byte, bool)>();
        chip.SetObservedOutputs(0, 1);
        chip.FirstOutputsChanged = (levels, _) =>
        {
            chip.SetClock(0, true);
            trace.Add((chip.TimeNanoseconds, levels, chip.GetClock(0)));
        };
        chip.AdvanceClocks(0, 2);
        Assert.Equal(new (long, byte, bool)[] { (137, 1, true), (388, 0, true) }, trace);
        Assert.Equal(500, chip.TimeNanoseconds);
        Assert.Equal(2, chip.GetInputClockCount(0));
        Assert.Equal(2, chip.GetOutputs(0));
        Assert.True(chip.GetClock(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ClearingTheEarliestChannelKeepsTheOtherEqualOrLaterDeadline(int secondOffset)
    {
        var chip = Create();
        var deadlines = new List<(long, long?)>();
        var outputs = new List<(long, int, byte)>();
        chip.NextEventNanosecondsChanged = next => deadlines.Add((chip.TimeNanoseconds, next));
        chip.FirstOutputsChanged = (levels, _) => outputs.Add((chip.TimeNanoseconds, 0, levels));
        chip.SecondOutputsChanged = (levels, _) => outputs.Add((chip.TimeNanoseconds, 1, levels));
        chip.SetClock(0, true);
        chip.SetClock(1, true);
        chip.AdvanceTo(100);
        chip.SetClock(0, false);
        chip.AdvanceTo(100 + secondOffset);
        chip.SetClock(1, false);
        chip.AdvanceTo(101 + secondOffset);
        chip.SetClear(0, true);
        Assert.Equal(112 + secondOffset, chip.NextEventNanoseconds);
        chip.AdvanceTo(112 + secondOffset);
        Assert.Equal(new (long, int, byte)[] { (112 + secondOffset, 1, 1) }, outputs);
        var expected = new List<(long, long?)> { (100, 112) };
        if (secondOffset != 0) expected.Add((102, 113));
        expected.Add((112 + secondOffset, null));
        Assert.Equal(expected, deadlines);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(1, chip.GetInputClockCount(0));
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(0, 15)]
    [InlineData(1, 7)]
    [InlineData(1, 15)]
    public void HiddenUpperRippleKeepsTheProfileAndDeadlineOnEitherChannel(int channel, byte seed)
    {
        for (var mode = 0; mode < 3; mode++)
        {
            var timing = new Sn74Ls393Timing(4, 7, 5, 11, 13, 9);
            var chip = new Sn74Ls393(4_000_000, 4_000_000,
                channel == 0 ? seed : (byte)0, channel == 1 ? seed : (byte)0, timing);
            var trace = new List<(long, byte, byte, long?)>();
            void Changed(byte levels, byte mask) => trace.Add((chip.TimeNanoseconds, levels, mask, chip.NextEventNanoseconds));
            chip.SetObservedOutputs(channel, 9);
            if (channel == 0) chip.FirstOutputsChanged = Changed;
            else chip.SecondOutputsChanged = Changed;
            if (mode == 2) chip.AdvanceClocks(channel, 1);
            else
            {
                chip.SetClock(channel, true);
                chip.AdvanceTo(125);
                chip.SetClock(channel, false);
                if (mode == 1)
                {
                    chip.AdvanceTo(160);
                    Assert.Equal(seed & 8, chip.GetOutputs(channel));
                    Assert.Equal(161, chip.NextEventNanoseconds);
                    Assert.Single(trace);
                }
                chip.AdvanceTo(200);
            }
            Assert.Equal(new (long, byte, byte, long?)[]
            {
                (132, (byte)(seed ^ 1), 1, 137), (161, (byte)((seed + 1) & 15), 8, null)
            }, trace);
            Assert.Equal(mode == 2 ? 250 : 200, chip.TimeNanoseconds);
            Assert.Equal(0, chip.GetOutputs(1 - channel));
            Assert.Equal(1, chip.GetInputClockCount(channel));
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void SimultaneousOtherChannelClearIsPublishedBeforeTheUnobservedUpperRippleFinishes()
    {
        var timing = new Sn74Ls393Timing(4, 7, 5, 11, 13, 9);
        var chip = new Sn74Ls393(4_000_000, 4_000_000, 1, 7, timing);
        var trace = new List<(long, int, byte, byte)>();
        chip.SetObservedOutputs(0, 1);
        chip.SetObservedOutputs(1, 9);
        chip.FirstOutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, 0, levels, mask));
        chip.SecondOutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, 1, levels, mask));
        chip.SetClock(1, true);
        chip.AdvanceTo(125);
        chip.SetClock(1, false);
        chip.AdvanceTo(128);
        chip.SetClear(0, true);
        chip.AdvanceTo(200);
        Assert.Equal(new (long, int, byte, byte)[] { (132, 1, 6, 1), (137, 0, 0, 1), (161, 1, 8, 8) }, trace);
        Assert.Equal(200, chip.TimeNanoseconds);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void UnobservedRippleStopsAtCallerBoundAndAcceptsAnAsynchronousClearThere()
    {
        var chip = Create(7);
        var trace = new List<(long, byte, byte)>();
        chip.SetObservedOutputs(0, 8);
        chip.FirstOutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, levels, mask));
        chip.SetClock(0, true);
        chip.AdvanceTo(125);
        chip.SetClock(0, false);
        chip.AdvanceTo(153);
        Assert.Equal(153, chip.TimeNanoseconds);
        Assert.Equal(4, chip.GetOutputs(0));
        Assert.Equal(156, chip.NextEventNanoseconds);
        Assert.Empty(trace);
        chip.SetClear(0, true);
        chip.AdvanceTo(176);
        Assert.Equal(4, chip.GetOutputs(0));
        chip.AdvanceTo(177);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Empty(trace);
    }

    private static Sn74Ls393 Create(byte first = 0, byte second = 0) =>
        new(4_000_000, 2_000_000, first, second, Sn74Ls393Timing.CalculatedTypical);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AllSixteenPhasesCountModuloSixteenOnlyOnFallingEdges(int channel)
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var chip = Create(seed, seed);
            for (var count = 1; count <= 32; count++)
            {
                var start = chip.TimeNanoseconds;
                chip.SetClock(channel, true);
                chip.SetClock(channel, true);
                Assert.Equal((seed + count - 1) & 15, chip.GetOutputs(channel));
                chip.AdvanceTo(start + 125);
                chip.SetClock(channel, false);
                chip.SetClock(channel, false);
                Assert.Equal(count, chip.GetInputClockCount(channel));
                chip.AdvanceTo(start + 250);
                Assert.Equal((seed + count) & 15, chip.GetOutputs(channel));
                Assert.Equal(seed, chip.GetOutputs(1 - channel));
                Assert.Equal(0, chip.GetInputClockCount(1 - channel));
            }
        }
    }

    [Fact]
    public void SevenToEightPublishesEachRealRippleTransitionAtItsOwnDate()
    {
        var chip = Create(7);
        var trace = new List<(long, byte, byte)>();
        chip.FirstOutputsChanged = (levels, mask) => trace.Add((chip.TimeNanoseconds, levels, mask));
        chip.SetClock(0, true);
        chip.AdvanceTo(125);
        chip.SetClock(0, false);
        Assert.Equal(138, chip.NextEventNanoseconds);
        chip.AdvanceTo(164);
        Assert.Equal(0, chip.GetOutputs(0));
        chip.AdvanceTo(165);
        Assert.Equal(8, chip.GetOutputs(0));
        Assert.Equal(new (long, byte, byte)[]
        {
            (138, 6, 1), (147, 4, 2), (156, 0, 4), (165, 8, 8)
        }, trace);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearIsAsynchronousCancelsCountPropagationAndDoesNotClockOnRelease(bool clockHigh)
    {
        var chip = Create(15, 10);
        chip.SetClock(0, clockHigh);
        chip.SetClear(0, true);
        chip.AdvanceTo(23);
        Assert.Equal(15, chip.GetOutputs(0));
        chip.AdvanceTo(24);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(10, chip.GetOutputs(1));
        chip.SetClock(0, true);
        chip.AdvanceTo(50);
        chip.SetClock(0, false);
        chip.AdvanceTo(100);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(1, chip.GetInputClockCount(0));
        chip.SetClear(0, false);
        chip.AdvanceTo(200);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(1, chip.GetInputClockCount(0));
        chip.AdvanceClocks(0, 1);
        Assert.Equal(1, chip.GetOutputs(0));
    }

    [Fact]
    public void ClearDuringRipplePreventsTheRemainingStagesFromCounting()
    {
        var chip = Create(7);
        var trace = new List<(long, byte)>();
        chip.FirstOutputsChanged = (value, _) => trace.Add((chip.TimeNanoseconds, value));
        chip.SetClock(0, true);
        chip.AdvanceTo(125);
        chip.SetClock(0, false);
        chip.AdvanceTo(140);
        chip.SetClear(0, true);
        chip.AdvanceTo(500);
        Assert.Equal(new (long, byte)[] { (138, 6), (164, 0) }, trace);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void ClearReleasedAfterValidPulseStillCompletesItsOutputPropagation()
    {
        var chip = Create(15);
        chip.SetClear(0, true);
        chip.AdvanceTo(20);
        chip.SetClear(0, false);
        chip.AdvanceTo(24);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(0, chip.GetInputClockCount(0));
    }

    [Fact]
    public void OutputFeedbackCanClearTheCounterDuringRipple()
    {
        var chip = Create(7);
        chip.FirstOutputsChanged = (value, mask) =>
        {
            if (mask == 1 && value == 6) chip.SetClear(0, true);
        };
        chip.AdvanceClocks(0, 1);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.True(chip.GetClear(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void QdCascadesToTheSecondClockOnItsFallingEdgeWithBothPropagationDelays()
    {
        var chip = Create(15);
        chip.SetClock(1, true); // Initial physical QD level of the first channel.
        chip.SetObservedOutputs(0, 8);
        chip.FirstOutputsChanged = (value, _) => chip.SetClock(1, (value & 8) != 0);
        var second = new List<(long, byte)>();
        chip.SecondOutputsChanged = (value, _) => second.Add((chip.TimeNanoseconds, value));
        chip.AdvanceClocks(0, 1);
        Assert.Equal(new (long, byte)[] { (177, 1) }, second);
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Equal(1, chip.GetOutputs(1));
        Assert.Equal(4_000_000, chip.GetClockFrequency(0));
        Assert.Equal(2_000_000, chip.GetClockFrequency(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(15)]
    public void GroupedPeriodsMatchExplicitEdgesIncludingMaskedNotificationsAndFeedback(byte mask)
    {
        var grouped = Create(5);
        var explicitEdges = Create(5);
        var firstTrace = Configure(grouped);
        var secondTrace = Configure(explicitEdges);
        grouped.AdvanceClocks(0, 133);
        for (var i = 0; i < 133; i++)
        {
            var start = explicitEdges.TimeNanoseconds;
            explicitEdges.SetClock(0, true);
            explicitEdges.AdvanceTo(start + 125);
            explicitEdges.SetClock(0, false);
            explicitEdges.AdvanceTo(start + 250);
        }
        Assert.Equal(secondTrace, firstTrace);
        Assert.Equal(explicitEdges.GetOutputs(0), grouped.GetOutputs(0));
        Assert.Equal(explicitEdges.GetInputClockCount(0), grouped.GetInputClockCount(0));
        Assert.Equal(explicitEdges.TimeNanoseconds, grouped.TimeNanoseconds);

        List<(long, byte, byte)> Configure(Sn74Ls393 chip)
        {
            chip.SetObservedOutputs(0, mask);
            var trace = new List<(long, byte, byte)>();
            chip.FirstOutputsChanged = (value, changed) =>
            {
                trace.Add((chip.TimeNanoseconds, value, changed));
                if (trace.Count == 4) chip.SetClear(0, true);
            };
            return trace;
        }
    }

    [Fact]
    public void UnobservedStableIntervalsAdvanceArithmeticallyWithoutLosingCountsOrOtherChannelTime()
    {
        var chip = Create(3, 9);
        chip.AdvanceClocks(0, 10_000_003);
        Assert.Equal(6, chip.GetOutputs(0));
        Assert.Equal(10_000_003, chip.GetInputClockCount(0));
        Assert.Equal(2_500_000_750, chip.TimeNanoseconds);
        Assert.Equal(9, chip.GetOutputs(1));
        Assert.Equal(0, chip.GetInputClockCount(1));
        Assert.False(chip.GetClock(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void EveryPhaseAndObservationMaskMatchesEveryExplicitRippleIncludingChangesOfSubscription()
    {
        for (byte seed = 0; seed < 16; seed++)
        for (byte mask = 0; mask < 16; mask++)
        {
            var grouped = Create(seed);
            var explicitEdges = Create(seed);
            var actual = Observe(grouped);
            var expected = Observe(explicitEdges);
            grouped.AdvanceClocks(0, 67);
            for (var i = 0; i < 67; i++)
            {
                var start = explicitEdges.TimeNanoseconds;
                explicitEdges.SetClock(0, true);
                explicitEdges.AdvanceTo(start + 125);
                explicitEdges.SetClock(0, false);
                explicitEdges.AdvanceTo(start + 250);
            }
            Assert.Equal(expected, actual);
            Assert.Equal(explicitEdges.GetOutputs(0), grouped.GetOutputs(0));
            Assert.Equal(67, grouped.GetInputClockCount(0));
            List<(long, byte, byte)> Observe(Sn74Ls393 chip)
            {
                var trace = new List<(long, byte, byte)>();
                chip.SetObservedOutputs(0, mask);
                chip.FirstOutputsChanged = (value, changed) =>
                {
                    trace.Add((chip.TimeNanoseconds, value, changed));
                    if (trace.Count == 2) chip.SetObservedOutputs(0, 15);
                    if (trace.Count == 10) chip.FirstOutputsChanged = null;
                };
                return trace;
            }
        }
    }

    [Fact]
    public void AnotherChannelsPendingClearCannotBeSkippedByGroupedClocking()
    {
        var chip = Create(0, 15);
        var trace = new List<(long, byte)>();
        chip.SecondOutputsChanged = (value, _) => trace.Add((chip.TimeNanoseconds, value));
        chip.SetClear(1, true);
        chip.AdvanceClocks(0, 2);
        Assert.Equal(new (long, byte)[] { (24, 0) }, trace);
        Assert.Equal(2, chip.GetOutputs(0));
    }

    [Fact]
    public void HighestRatedClockKeepsRippleThatHasNotSettledAtThePeriodBoundary()
    {
        var chip = new Sn74Ls393(25_000_000, 25_000_000, 15, 0, Sn74Ls393Timing.CalculatedTypical);
        chip.AdvanceClocks(0, 1);
        Assert.Equal(14, chip.GetOutputs(0)); // QA fell at 33 ns; QB is not due until 42 ns.
        Assert.Equal(42, chip.NextEventNanoseconds);
        chip.AdvanceTo(60);
        Assert.Equal(0, chip.GetOutputs(0));
    }

    [Fact]
    public void SimultaneousChannelEventsResolveBeforeNotificationsAndExposeTheirNextRealDeadline()
    {
        var chip = Create(7, 15);
        var deadlines = new List<long?>();
        chip.NextEventNanosecondsChanged = deadlines.Add;
        chip.FirstOutputsChanged = (_, mask) =>
        {
            if (mask != 1) return;
            Assert.Equal(14, chip.GetOutputs(1));
            Assert.Equal(147, chip.NextEventNanoseconds);
        };
        chip.SetClock(0, true);
        chip.SetClock(1, true);
        chip.AdvanceTo(125);
        chip.SetClock(0, false);
        chip.SetClock(1, false);
        chip.SetClock(1, false);
        chip.AdvanceTo(138);
        chip.AdvanceTo(138);
        chip.AdvanceTo(200);
        Assert.Equal(new long?[] { 138, 147, null }, deadlines);
    }

    [Fact]
    public void RepeatedMaximumFrequencyPeriodsMatchTheStableBinaryCountAfterSettling()
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var chip = new Sn74Ls393(25_000_000, 25_000_000, seed, 0, Sn74Ls393Timing.CalculatedTypical);
            chip.AdvanceClocks(0, 1001);
            chip.AdvanceTo(chip.TimeNanoseconds + 60);
            Assert.Equal((seed + 1001) & 15, chip.GetOutputs(0));
            Assert.Equal(1001, chip.GetInputClockCount(0));
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void TimingCanBeReplacedWithoutChangingTheCounterOrInventingAnotherClock()
    {
        var chip = new Sn74Ls393(4_000_000, 4_000_000, 7, 0, new(10, 11, 5, 6, 7, 20));
        var trace = new List<long>();
        chip.FirstOutputsChanged = (_, _) => trace.Add(chip.TimeNanoseconds);
        chip.AdvanceClocks(0, 1);
        Assert.Equal(new long[] { 136, 141, 147, 154 }, trace);
        Assert.Equal(8, chip.GetOutputs(0));
    }

    [Fact]
    public void InvalidInputsAndOverflowAreRejectedWithoutCorruptingState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393(0, 1, 0, 0, Sn74Ls393Timing.CalculatedTypical));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393(25_000_001, 1, 0, 0, Sn74Ls393Timing.CalculatedTypical));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393Timing(21, 13, 9, 9, 9, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393Timing(12, 13, 20, 20, 20, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393Timing(12, 13, 9, 9, 9, 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls393Timing(12, 13, 0, 9, 9, 24));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetClock(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(0, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(-1));
        Assert.Throws<OverflowException>(() => chip.AdvanceClocks(0, long.MaxValue));
        Assert.Equal(0, chip.TimeNanoseconds);
        Assert.Equal(0, chip.GetInputClockCount(0));
        chip.AdvanceTo(long.MaxValue - 5);
        Assert.Throws<OverflowException>(() => chip.SetClear(0, true));
        Assert.False(chip.GetClear(0));
        var fractional = new Sn74Ls393(3_000_000, 1, 0, 0, Sn74Ls393Timing.CalculatedTypical);
        Assert.Throws<InvalidOperationException>(() => fractional.AdvanceClocks(0, 1));
        fractional.SetClock(0, true);
        fractional.AdvanceTo(166);
        fractional.SetClock(0, false);
        fractional.AdvanceTo(333);
        Assert.Equal(1, fractional.GetOutputs(0));
    }

    [Fact]
    public void ARealDeadlineAtLongMaxValueIsNotMistakenForAnEmptySlot()
    {
        var chip = Create(15);
        chip.AdvanceTo(long.MaxValue - 24);
        chip.SetClear(0, true);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(0, chip.GetOutputs(0));
        Assert.Null(chip.NextEventNanoseconds);
    }
}
