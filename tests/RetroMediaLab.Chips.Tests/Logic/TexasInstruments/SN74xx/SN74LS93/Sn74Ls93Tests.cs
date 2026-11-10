using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS93;

public sealed class Sn74Ls93Tests
{
    private static Sn74Ls93 Create(byte initial = 0) => new(4_000_000, 2_000_000, initial, Sn74Ls93Timing.CalculatedTypical);

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(79)]
    public void ConservativeConnectedDeadlinesPreserveEveryObservedResponseAndStateAcrossMasks(int phase)
    {
        for (byte seed = 0; seed < 16; seed++)
        for (byte mask = 0; mask < 16; mask++)
        {
            var chip = Create(seed); var reference = Create(seed);
            chip.SetObservedOutputs(mask); reference.SetObservedOutputs(mask);
            chip.SetClock(0, true); reference.SetClock(0, true);
            chip.SetClock(1, true); reference.SetClock(1, true);
            chip.ConnectPeriodicClock(0, 125, 250, 125);
            chip.ConnectPeriodicClock(1, 250 - phase, 500, 250);
            var observed = mask;
            var actual = new List<(long Time, byte Levels, byte Changed)>();
            var expected = new List<(long Time, byte Levels, byte Changed)>();
            chip.OutputsChanged = (level, changed) => actual.Add((chip.TimeNanoseconds, (byte)(level & observed), changed));
            reference.OutputsChanged = (level, changed) => expected.Add((reference.TimeNanoseconds, (byte)(level & observed), changed));
            long nextA = 125, nextB = 250 - phase; bool a = true, b = true;
            void Advance(long target)
            {
                while (Math.Min(nextA, nextB) <= target)
                {
                    var at = Math.Min(nextA, nextB); reference.AdvanceTo(at);
                    if (at == nextA) { a = !a; reference.SetClock(0, a); nextA += 125; }
                    if (at == nextB) { b = !b; reference.SetClock(1, b); nextB += 250; }
                }
                reference.AdvanceTo(target); chip.AdvanceTo(target);
                Assert.Equal(expected, actual); Assert.Equal(reference.Outputs, chip.Outputs);
                for (var clock = 0; clock < 2; clock++)
                {
                    Assert.Equal(reference.GetInputClockCount(clock), chip.GetInputClockCount(clock));
                    Assert.Equal(reference.GetClock(clock), chip.GetClock(clock));
                }
            }
            while (chip.TimeNanoseconds < 5000)
            {
                var next = chip.NextOutputEventNanoseconds ?? 5000;
                Assert.True(next > chip.TimeNanoseconds);
                var target = Math.Min(next, 5000);
                Advance(target);
                Assert.All(expected, item => Assert.Equal(target, item.Time));
                actual.Clear(); expected.Clear();
            }
            chip.SetResets(true, true); reference.SetResets(true, true);
            Advance(5007);
            chip.SetResets(false, false); reference.SetResets(false, false);
            Advance(6019);
            actual.Clear(); expected.Clear();
            observed = (byte)(mask ^ 15); chip.SetObservedOutputs(observed); reference.SetObservedOutputs(observed);
            Advance(7031);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(79)]
    public void FinalUnobservedRippleMatchesExplicitEdgesAtEveryNanosecondAndResumes(int phase)
    {
        for (byte seed = 0; seed < 16; seed++)
        for (var section = 0; section < 2; section++)
        for (var tail = 0; tail <= 80; tail++)
        {
            var chip = Create(seed); var reference = Create(seed);
            chip.SetObservedOutputs(0);
            chip.SetClock(0, true); reference.SetClock(0, true);
            chip.SetClock(1, true); reference.SetClock(1, true);
            chip.ConnectPeriodicClock(0, 125, 250, 125);
            chip.ConnectPeriodicClock(1, 250 - phase, 500, 250);
            long nextA = 125, nextB = 250 - phase;
            bool a = true, b = true;
            void Advance(long target)
            {
                while (Math.Min(nextA, nextB) <= target)
                {
                    var at = Math.Min(nextA, nextB); reference.AdvanceTo(at);
                    if (at == nextA) { a = !a; reference.SetClock(0, a); nextA += 125; }
                    if (at == nextB) { b = !b; reference.SetClock(1, b); nextB += 250; }
                }
                reference.AdvanceTo(target); chip.AdvanceTo(target);
                Assert.Equal(reference.Outputs, chip.Outputs);
                var nextFallA = a ? nextA : nextA + 125;
                var nextFallB = b ? nextB : nextB + 250;
                Assert.Equal(Math.Min(reference.NextEventNanoseconds ?? long.MaxValue,
                    Math.Min(nextFallA, nextFallB)), chip.NextEventNanoseconds);
                for (var clock = 0; clock < 2; clock++)
                {
                    Assert.Equal(reference.GetInputClockCount(clock), chip.GetInputClockCount(clock));
                    Assert.Equal(reference.GetClock(clock), chip.GetClock(clock));
                }
            }
            var target = (section == 0 ? 10_125 : 10_250 - phase) + tail;
            Advance(target);
            var expected = new List<(long, byte, byte)>(); var actual = new List<(long, byte, byte)>();
            reference.OutputsChanged = (levels, changed) => expected.Add((reference.TimeNanoseconds, levels, changed));
            chip.OutputsChanged = (levels, changed) => actual.Add((chip.TimeNanoseconds, levels, changed));
            chip.SetObservedOutputs(15);
            // A new asynchronous input may arrive while the last ripple is pending.
            if ((tail & 1) == 0) { reference.SetResets(true, true); chip.SetResets(true, true); }
            Advance(target + 550);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(79)]
    public void UnobservedIndependentSectionsMatchExplicitEdgesAndResumeTheirPartialRipple(int phase)
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var chip = Create(seed); var reference = Create(seed);
            chip.SetObservedOutputs(0);
            chip.SetClock(0, true); reference.SetClock(0, true);
            chip.SetClock(1, true); reference.SetClock(1, true);
            chip.ConnectPeriodicClock(0, 125, 250, 125);
            chip.ConnectPeriodicClock(1, 250 - phase, 500, 250);
            long nextA = 125, nextB = 250 - phase;
            bool a = true, b = true;
            void Advance(long target)
            {
                while (Math.Min(nextA, nextB) <= target)
                {
                    var at = Math.Min(nextA, nextB); reference.AdvanceTo(at);
                    if (at == nextA) { a = !a; reference.SetClock(0, a); nextA += 125; }
                    if (at == nextB) { b = !b; reference.SetClock(1, b); nextB += 250; }
                }
                reference.AdvanceTo(target); chip.AdvanceTo(target);
                Assert.Equal(reference.Outputs, chip.Outputs);
                var nextFallA = a ? nextA : nextA + 125;
                var nextFallB = b ? nextB : nextB + 250;
                Assert.Equal(Math.Min(reference.NextEventNanoseconds ?? long.MaxValue, Math.Min(nextFallA, nextFallB)), chip.NextEventNanoseconds);
                for (var clock = 0; clock < 2; clock++)
                {
                    Assert.Equal(reference.GetInputClockCount(clock), chip.GetInputClockCount(clock));
                    Assert.Equal(reference.GetClock(clock), chip.GetClock(clock));
                }
            }
            foreach (var target in new long[] { 7, 125, 135, 251, 263, 3_017, 100_001, 100_011 }) Advance(target);
            reference.SetResets(true, true); chip.SetResets(true, true);
            Advance(110_012); reference.SetResets(false, false); chip.SetResets(false, false);
            Advance(120_023);
            var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
            chip.OutputsChanged = (levels, changed) => actual.Add((chip.TimeNanoseconds, levels, changed));
            reference.OutputsChanged = (levels, changed) => expected.Add((reference.TimeNanoseconds, levels, changed));
            chip.SetObservedOutputs(15); Advance(125_000);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ExternalPeriodicQaToCkbWireMatchesTheScalarCascadeAndReset()
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var chip = Create(seed); var scalar = Create(seed);
            chip.SetClock(1, (seed & 1) != 0); scalar.SetClock(1, (seed & 1) != 0);
            var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
            var described = false;
            chip.OutputsChanged = (levels, changed) =>
            {
                if (!described && (changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
                if ((changed & 14) != 0) actual.Add((chip.TimeNanoseconds, levels, (byte)(changed & 14)));
            };
            chip.QaPeriodicClockChanged = (fall, period, high) =>
            {
                if (period == 0)
                {
                    chip.DisconnectPeriodicClock(1); described = false; chip.SetObservedOutputs(15);
                }
                else
                {
                    chip.ConnectPeriodicClock(1, fall, period, high); described = true; chip.SetObservedOutputs(14);
                }
            };
            scalar.OutputsChanged = (levels, changed) =>
            {
                if ((changed & 1) != 0) scalar.SetClock(1, (levels & 1) != 0);
                if ((changed & 14) != 0) expected.Add((scalar.TimeNanoseconds, levels, (byte)(changed & 14)));
            };
            chip.SetClock(0, true); scalar.SetClock(0, true);
            chip.ConnectPeriodicClock(0, 125, 250, 125);
            long previous = 0;
            foreach (var end in new long[] { 711, 717, 1_233, 1_259, 10_000, 10_100, 30_380, 30_880, 50_000 })
            {
                for (var time = previous + 1; time <= end; time++)
                {
                    scalar.AdvanceTo(time); scalar.SetClock(0, time % 250 < 125);
                }
                chip.AdvanceTo(end);
                Assert.Equal(expected, actual);
                Assert.Equal(scalar.Outputs, chip.Outputs);
                Assert.Equal(scalar.GetInputClockCount(0), chip.GetInputClockCount(0));
                Assert.Equal(scalar.GetInputClockCount(1), chip.GetInputClockCount(1));
                var reset = end is 711 or 1_233 or 10_000 or 30_380;
                scalar.SetResets(reset, reset); chip.SetResets(reset, reset);
                previous = end;
            }
        }
    }

    [Fact]
    public void PeriodicQaWirePreservesDownstreamTransitionsAcrossAsynchronousResets()
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var source = Create(seed); var explicitSource = Create(seed);
            var receiver = Create(); var explicitReceiver = Create();
            receiver.SetClock(0, (seed & 1) != 0); explicitReceiver.SetClock(0, (seed & 1) != 0);
            var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
            receiver.OutputsChanged = (pins, changed) => actual.Add((receiver.TimeNanoseconds, pins, changed));
            explicitReceiver.OutputsChanged = (pins, changed) => expected.Add((explicitReceiver.TimeNanoseconds, pins, changed));
            var represented = false;
            source.OutputsChanged = (pins, changed) =>
            {
                if ((changed & 1) == 0 || represented) return;
                receiver.AdvanceTo(source.TimeNanoseconds); receiver.SetClock(0, (pins & 1) != 0);
            };
            source.QaPeriodicClockChanged = (fall, period, high) =>
            {
                receiver.AdvanceTo(source.TimeNanoseconds);
                if (period == 0)
                {
                    receiver.DisconnectPeriodicClock(0); represented = false;
                    source.SetObservedOutputs(1);
                }
                else
                {
                    receiver.ConnectPeriodicClock(0, fall, period, high); represented = true;
                    source.SetObservedOutputs(0);
                }
            };
            explicitSource.OutputsChanged = (pins, changed) =>
            {
                if ((changed & 1) == 0) return;
                explicitReceiver.AdvanceTo(explicitSource.TimeNanoseconds);
                explicitReceiver.SetClock(0, (pins & 1) != 0);
            };
            source.SetClock(0, true); explicitSource.SetClock(0, true);
            source.ConnectPeriodicClock(0, 125, 250, 125);
            for (long time = 1; time <= 8_000; time++)
            {
                explicitSource.AdvanceTo(time);
                explicitSource.SetClock(0, time % 250 < 125);
                source.AdvanceTo(time);
                if (time is 711 or 777 or 1234 or 1240 or 1511 or 1537)
                {
                    var reset = time is 711 or 1234 or 1511;
                    explicitSource.SetResets(reset, reset); source.SetResets(reset, reset);
                }
                receiver.AdvanceTo(time); explicitReceiver.AdvanceTo(time);
                Assert.Equal(explicitSource.Outputs, source.Outputs);
                Assert.Equal(explicitReceiver.Outputs, receiver.Outputs);
                Assert.Equal(explicitReceiver.GetClock(0), receiver.GetClock(0));
            }
            Assert.Equal(expected, actual);
            // A large interval now exercises the deferred source-state path.
            for (long time = 8_001; time <= 50_000; time++)
            {
                explicitSource.AdvanceTo(time); explicitSource.SetClock(0, time % 250 < 125);
                explicitReceiver.AdvanceTo(time);
            }
            source.AdvanceTo(50_000); receiver.AdvanceTo(50_000);
            Assert.Equal(explicitSource.Outputs, source.Outputs);
            Assert.Equal(explicitSource.GetInputClockCount(0), source.GetInputClockCount(0));
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeriodicExternalInputsPreserveIndependentSectionsResetAndRipple(bool cascade)
    {
        for (byte initial = 0; initial < 16; initial++)
        {
            var periodic = Create(initial); var scalar = Create(initial);
            var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
            void Wire(Sn74Ls93 chip, List<(long, byte, byte)> trace)
            {
                if (cascade) chip.SetClock(1, (initial & 1) != 0);
                chip.OutputsChanged = (levels, changed) =>
                {
                    trace.Add((chip.TimeNanoseconds, levels, changed));
                    if (cascade && (changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
                };
            }
            Wire(periodic, actual); Wire(scalar, expected);
            periodic.SetClock(0, true); scalar.SetClock(0, true);
            periodic.ConnectPeriodicClock(0, 125, 250, 125);
            if (!cascade) periodic.ConnectPeriodicClock(1, 370, 500, 200);
            for (long time = 0; time <= 5_000; time++)
            {
                // Resolve old responses before presenting the next physical input.
                scalar.AdvanceTo(time);
                scalar.SetClock(0, time % 250 < 125);
                if (!cascade) scalar.SetClock(1, time >= 170 && (time - 170) % 500 < 200);
                periodic.AdvanceTo(time);
                if (time is 711 or 777 or 1234 or 1511)
                {
                    var reset = time is 711 or 1234;
                    scalar.SetResets(reset, reset); periodic.SetResets(reset, reset);
                }
                Assert.Equal(scalar.Outputs, periodic.Outputs);
                Assert.Equal(scalar.GetClock(0), periodic.GetClock(0));
                Assert.Equal(scalar.GetClock(1), periodic.GetClock(1));
                Assert.Equal(scalar.GetInputClockCount(0), periodic.GetInputClockCount(0));
                Assert.Equal(scalar.GetInputClockCount(1), periodic.GetInputClockCount(1));
            }
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void PeriodicInputCanBeReplacedOrDisconnectedFromAResolvedOutput()
    {
        var chip = Create();
        chip.SetClock(0, true);
        chip.ConnectPeriodicClock(0, 125, 250, 125);
        chip.OutputsChanged = (_, _) =>
        {
            if (chip.TimeNanoseconds != 135) return;
            Assert.False(chip.GetClock(0));
            chip.ConnectPeriodicClock(0, 375, 400, 150);
        };
        chip.AdvanceTo(550);
        Assert.Equal(2, chip.GetInputClockCount(0));
        Assert.False(chip.GetClock(0));
        chip.DisconnectPeriodicClock(0);
        chip.AdvanceTo(5_000);
        Assert.Equal(2, chip.GetInputClockCount(0));
        Assert.False(chip.GetClock(0));
        chip.SetClock(0, true); chip.SetClock(0, false);
        chip.AdvanceTo(5_100);
        Assert.Equal(3, chip.GetInputClockCount(0));
    }

    [Fact]
    public void PeriodicInputRetainsItsLastRepresentableFallingEdge()
    {
        var chip = Create();
        chip.AdvanceTo(long.MaxValue - 200);
        chip.SetClock(0, true);
        chip.ConnectPeriodicClock(0, long.MaxValue - 100, 250, 125);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(1, chip.GetInputClockCount(0));
        Assert.Equal(1, chip.Outputs);
        Assert.False(chip.GetClock(0));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void SectionsAreIndependentAndAdvanceOnlyOnTheirOwnFallingInput()
    {
        var chip = Create();
        Assert.Equal(4_000_000, chip.GetClockFrequency(0));
        Assert.Equal(2_000_000, chip.GetClockFrequency(1));
        chip.SetClock(0, false); chip.SetClock(0, true);
        chip.AdvanceTo(100);
        Assert.Equal(0, chip.Outputs);
        chip.SetClock(0, false); chip.SetClock(0, false);
        chip.AdvanceTo(110);
        Assert.Equal(1, chip.Outputs);
        chip.AdvanceClocks(0, 31);
        Assert.Equal(0, chip.Outputs);
        Assert.Equal(32, chip.GetInputClockCount(0));
        Assert.Equal(0, chip.GetInputClockCount(1));
        chip.AdvanceClocks(1, 3);
        Assert.Equal(6, chip.Outputs);
        Assert.Equal(3, chip.GetInputClockCount(1));
    }

    [Fact]
    public void RipplePreservesEachPhysicalIntermediateWordAndCumulativeDelay()
    {
        var chip = Create(14);
        var trace = new List<(long, byte, byte)>();
        chip.OutputsChanged = (levels, changed) => trace.Add((chip.TimeNanoseconds, levels, changed));
        chip.SetClock(1, true); chip.SetClock(1, false);
        chip.AdvanceTo(34);
        Assert.Equal(new (long, byte, byte)[] { (14, 12, 2), (23, 8, 4), (34, 0, 8) }, trace);
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void ExternalQaToCkbWireReproducesTheCharacterizedFortySixNanosecondCarry()
    {
        var chip = Create(7);
        var trace = new List<(long, byte, byte)>();
        chip.SetClock(1, true); // Initialize the wire from the seeded high QA.
        chip.OutputsChanged = (levels, changed) =>
        {
            trace.Add((chip.TimeNanoseconds, levels, changed));
            if ((changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
        };
        chip.SetClock(0, true); chip.SetClock(0, false);
        chip.AdvanceTo(46);
        Assert.Equal(new (long, byte, byte)[] { (12, 6, 1), (26, 4, 2), (35, 0, 4), (46, 8, 8) }, trace);
        Assert.Equal(1, chip.GetInputClockCount(1));
    }

    [Fact]
    public void BothResetInputsAreRequiredAndResetDoesNotGenerateInternalCarries()
    {
        var chip = Create(15);
        var trace = new List<(long, byte, byte)>();
        chip.OutputsChanged = (levels, changed) => trace.Add((chip.TimeNanoseconds, levels, changed));
        chip.SetReset(0, true); chip.AdvanceTo(100);
        Assert.Equal(15, chip.Outputs);
        chip.SetReset(1, true);
        chip.AdvanceTo(125); Assert.Equal(15, chip.Outputs);
        chip.AdvanceTo(126); Assert.Equal(0, chip.Outputs);
        Assert.Equal(new (long, byte, byte)[] { (126, 0, 15) }, trace);
        chip.AdvanceClocks(0, 100); chip.AdvanceClocks(1, 100);
        Assert.Equal(0, chip.Outputs);
        Assert.Equal(100, chip.GetInputClockCount(0)); Assert.Equal(100, chip.GetInputClockCount(1));
        chip.SetReset(0, false);
        chip.AdvanceTo(chip.TimeNanoseconds + 25);
        chip.AdvanceClocks(1, 1);
        Assert.Equal(2, chip.Outputs);
    }

    [Fact]
    public void ResetOverridesAnInFlightCarryAndDoesNotRearmWhenOnlyTheOtherInactivePinChanges()
    {
        var chip = Create(6);
        chip.SetClock(1, true); chip.SetClock(1, false);
        chip.AdvanceTo(14); // QB fell and QC's carry is due at23.
        chip.SetResets(true, true);
        Assert.Equal(40, chip.NextEventNanoseconds);
        chip.AdvanceTo(23); Assert.Equal(4, chip.Outputs);
        chip.AdvanceTo(40); Assert.Equal(0, chip.Outputs);
        Assert.Null(chip.NextEventNanoseconds);
        chip.SetReset(1, false);
        chip.AdvanceTo(100);
        chip.SetReset(0, false);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void EveryPhaseAndObservedMaskMatchesExplicitClockLevels()
    {
        for (byte seed = 0; seed < 16; seed++)
        for (byte observed = 0; observed < 16; observed++)
        {
            var grouped = Create(seed); var detailed = Create(seed);
            var actual = new List<(long, byte, byte)>();
            var expected = new List<(long, byte, byte)>();
            grouped.SetObservedOutputs(observed); detailed.SetObservedOutputs(observed);
            grouped.OutputsChanged = (levels, changed) => actual.Add((grouped.TimeNanoseconds, levels, changed));
            detailed.OutputsChanged = (levels, changed) => expected.Add((detailed.TimeNanoseconds, levels, changed));
            grouped.AdvanceClocks(0, 32); Explicit(detailed, 0, 32);
            grouped.AdvanceClocks(1, 32); Explicit(detailed, 1, 32);
            Assert.Equal(expected, actual);
            Assert.Equal(detailed.Outputs, grouped.Outputs);
            Assert.Equal(detailed.TimeNanoseconds, grouped.TimeNanoseconds);
            Assert.Equal(32, grouped.GetInputClockCount(0)); Assert.Equal(32, grouped.GetInputClockCount(1));
            Assert.False(grouped.GetClock(0)); Assert.False(grouped.GetClock(1));
            Assert.Null(grouped.NextEventNanoseconds);
        }
    }

    [Fact]
    public void ExternalDivideBySixteenCascadeMatchesExplicitClocksForEveryPhase()
    {
        for (byte seed = 0; seed < 16; seed++)
        {
            var grouped = Create(seed); var detailed = Create(seed);
            var a = new List<(long, byte, byte)>(); var b = new List<(long, byte, byte)>();
            Wire(grouped, a); Wire(detailed, b);
            grouped.AdvanceClocks(0, 64); Explicit(detailed, 0, 64);
            Assert.Equal(b, a);
            Assert.Equal(seed, grouped.Outputs);
            Assert.Equal(32, grouped.GetInputClockCount(1));
        }
    }

    [Fact]
    public void SameDateOutputsResolveBeforePublicationAndRecipientsCanChangeObservation()
    {
        var chip = new Sn74Ls93(4_000_000, 2_000_000, 0, new(10, 12, 10, 14, 7, 9, 11, 11, 26));
        var trace = new List<(long, byte, byte)>();
        chip.OutputsChanged = (levels, changed) =>
        {
            Assert.Equal(3, chip.Outputs);
            trace.Add((chip.TimeNanoseconds, levels, changed));
            chip.SetObservedOutputs(0);
        };
        chip.SetClock(0, true); chip.SetClock(1, true);
        chip.SetClock(0, false); chip.SetClock(1, false);
        chip.AdvanceTo(10);
        Assert.Equal(new (long, byte, byte)[] { (10, 3, 3) }, trace);
        chip.AdvanceClocks(0, 5);
        Assert.Single(trace);
    }

    [Fact]
    public void DeadlineListenerKeepsIntermediateEventsAndFeedbackCanAssertReset()
    {
        var chip = Create(6);
        var dates = new List<long?>();
        chip.NextEventNanosecondsChanged = dates.Add;
        chip.OutputsChanged = (_, _) =>
        {
            Assert.Throws<InvalidOperationException>(() => chip.AdvanceTo(chip.TimeNanoseconds + 1));
            Assert.Throws<InvalidOperationException>(() => chip.AdvanceClocks(0, 1));
            chip.SetResets(true, true);
        };
        chip.AdvanceClocks(1, 1);
        Assert.Equal(0, chip.Outputs);
        Assert.Equal(new long?[] { 264, null }, dates); // CKB half-period250 ns + QB falling delay14 ns.
    }

    [Fact]
    public void MaximumRecommendedInputRatesPreserveTheExternallyCascadedBinaryCount()
    {
        var chip = new Sn74Ls93(32_000_000, 16_000_000, 0, Sn74Ls93Timing.CalculatedTypical);
        chip.OutputsChanged = (levels, changed) =>
        {
            if ((changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
        };
        for (var cycle = 0; cycle < 64; cycle++)
        {
            chip.AdvanceTo(cycle * 1_000_000_000L / 32_000_000);
            chip.SetClock(0, true);
            chip.AdvanceTo((2 * cycle + 1) * 1_000_000_000L / 64_000_000);
            chip.SetClock(0, false);
        }
        chip.AdvanceTo(2100);
        Assert.Equal(0, chip.Outputs);
        Assert.Equal(64, chip.GetInputClockCount(0));
        Assert.Equal(32, chip.GetInputClockCount(1));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void ObservationChangesDuringAGroupedIntervalPreserveTheLaterDetailedResponses()
    {
        var grouped = Create(); var detailed = Create();
        var a = new List<(long, byte, byte)>(); var b = new List<(long, byte, byte)>();
        void Observe(Sn74Ls93 chip, List<(long, byte, byte)> trace)
        {
            chip.SetObservedOutputs(8);
            chip.OutputsChanged = (levels, changed) =>
            {
                trace.Add((chip.TimeNanoseconds, levels, changed));
                chip.SetObservedOutputs(14);
            };
        }
        Observe(grouped, a); Observe(detailed, b);
        grouped.AdvanceClocks(1, 64); Explicit(detailed, 1, 64);
        Assert.Equal(b, a);
        Assert.Equal(detailed.Outputs, grouped.Outputs);
        Assert.Equal(detailed.TimeNanoseconds, grouped.TimeNanoseconds);
    }

    [Fact]
    public void ResetQaTransitionStillReachesTheExternalClockWireWithoutCountingInternally()
    {
        var chip = Create(15);
        chip.SetClock(1, true);
        chip.OutputsChanged = (levels, changed) =>
        {
            if ((changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
        };
        chip.SetResets(true, true);
        chip.AdvanceTo(100);
        Assert.Equal(0, chip.Outputs);
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void ARecipientSuppliedFallingLevelDuringTheHalfPeriodIsNotReplayedByTheHelper()
    {
        var chip = Create();
        chip.OutputsChanged = (_, changed) =>
        {
            if ((changed & 1) != 0) chip.SetClock(1, false);
        };
        chip.SetClock(0, true); chip.SetClock(0, false); // QA response at10 ns.
        chip.AdvanceClocks(1, 1); // CKB rises now, receiver supplies its fall at10 ns.
        Assert.Equal(1, chip.GetInputClockCount(1));
        Assert.Equal(3, chip.Outputs);
        Assert.Equal(500, chip.TimeNanoseconds);
    }

    [Fact]
    public void ParameterErrorsAndRepresentationLimitsAreExplicit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls93(32_000_001, 0, 0, Sn74Ls93Timing.CalculatedTypical));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls93(0, 16_000_001, 0, Sn74Ls93Timing.CalculatedTypical));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls93(0, 0, 16, Sn74Ls93Timing.CalculatedTypical));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls93Timing(10, 12, 10, 14, 30, 9, 11, 11, 26));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetClock(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetReset(2, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(0, -1));
        chip.AdvanceTo(long.MaxValue - 10);
        chip.SetClock(0, true); chip.SetClock(0, false);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue); Assert.Equal(1, chip.Outputs);
        chip.SetClock(0, true);
        Assert.Throws<OverflowException>(() => chip.SetClock(0, false));
        Assert.True(chip.GetClock(0)); Assert.Equal(1, chip.GetInputClockCount(0));
        var fractional = new Sn74Ls93(32_000_000, 16_000_000, 0, Sn74Ls93Timing.CalculatedTypical);
        Assert.Throws<InvalidOperationException>(() => fractional.AdvanceClocks(0, 1));
        Assert.Throws<InvalidOperationException>(() => fractional.AdvanceClocks(1, 1));
    }

    private static void Wire(Sn74Ls93 chip, List<(long, byte, byte)> trace)
    {
        chip.SetClock(1, (chip.Outputs & 1) != 0);
        chip.OutputsChanged = (levels, changed) =>
        {
            trace.Add((chip.TimeNanoseconds, levels, changed));
            if ((changed & 1) != 0) chip.SetClock(1, (levels & 1) != 0);
        };
    }
    private static void Explicit(Sn74Ls93 chip, int clock, int periods)
    {
        var period = 1_000_000_000 / chip.GetClockFrequency(clock);
        for (var i = 0; i < periods; i++)
        {
            chip.SetClock(clock, true); chip.AdvanceTo(chip.TimeNanoseconds + period / 2);
            chip.SetClock(clock, false); chip.AdvanceTo(chip.TimeNanoseconds + period - period / 2);
        }
    }
}
