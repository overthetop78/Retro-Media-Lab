using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS138;

public sealed class Sn74Ls138Tests
{
    [Fact]
    public void ObservedDeadlinesPreserveAllPinsWithInterruptedInputsAndNewReceivers()
    {
        Sn74Ls138Timing[] profiles = [Sn74Ls138Timing.Typical, Sn74Ls138Timing.CharacterizedMaximum,
            new(30, 1, 1, 40, 8, 9, 10, 11)];
        byte[] masks = [0, 1, 2, 4, 8, 16, 32, 64, 128, 255];
        foreach (var timing in profiles)
        foreach (var mask in masks)
        for (byte initial = 0; initial < 8; initial++)
        for (byte target = 0; target < 64; target++)
        {
            var actual = new Sn74Ls138(timing); var reference = new Sn74Ls138(timing);
            actual.SetInputs((byte)(8 | initial), 63); reference.SetInputs((byte)(8 | initial), 63);
            actual.AdvanceTo(100); reference.AdvanceTo(100);
            actual.SetObservedOutputs(mask); reference.SetObservedOutputs(mask);
            var observed = new List<(long, int, bool?)>(); var expected = new List<(long, int, bool?)>();
            actual.OutputChanged = (pin, high) => observed.Add((actual.TimeNanoseconds, pin, high));
            reference.OutputChanged = (pin, high) => expected.Add((reference.TimeNanoseconds, pin, high));
            actual.SetInputs(target, 63); reference.SetInputs(target, 63);
            Advance(105);
            actual.SetInputs((byte)(target & 37), 37); reference.SetInputs((byte)(target & 37), 37);
            Advance(121);
            // Wiring changes occur at a synchronized transaction time, even
            // when no previously connected output requested this catch-up.
            actual.SetObservedOutputs((byte)~mask); reference.SetObservedOutputs((byte)~mask);
            Advance(150);
            actual.SetInputs((byte)(target ^ 8), 63); reference.SetInputs((byte)(target ^ 8), 63);
            Advance(200);

            void Advance(long end)
            {
                while (actual.NextOutputEventNanoseconds is { } next && next <= end)
                {
                    Assert.True(next > actual.TimeNanoseconds);
                    actual.AdvanceTo(next);
                }
                actual.AdvanceTo(end);
                for (var time = reference.TimeNanoseconds + 1; time <= end; time++) reference.AdvanceTo(time);
                Assert.Equal(reference.OutputLevels, actual.OutputLevels);
                Assert.Equal(reference.OutputKnown, actual.OutputKnown);
                Assert.Equal(reference.NextEventNanoseconds, actual.NextEventNanoseconds);
                Assert.Equal(expected, observed);
            }
        }
    }

    [Fact]
    public void OutputDeadlineTracksMasksCancellationAndFeedbackWithoutLosingInternalResponses()
    {
        var chip = Create(); chip.SetObservedOutputs(2);
        var deadlines = new List<long?>();
        chip.NextOutputEventNanosecondsChanged = next =>
        {
            deadlines.Add(next);
            if (next is null && chip.TimeNanoseconds == 120) chip.SetInput(Sn74Ls138Input.G1, false);
        };
        chip.SetInput(Sn74Ls138Input.A, true);
        Assert.Equal(111, chip.NextEventNanoseconds); Assert.Equal(120, chip.NextOutputEventNanoseconds);
        chip.AdvanceTo(105); chip.SetObservedOutputs(1);
        Assert.Equal(111, chip.NextOutputEventNanoseconds);
        chip.SetObservedOutputs(0); Assert.Null(chip.NextOutputEventNanoseconds);
        Assert.Equal(111, chip.NextEventNanoseconds); // Unconnected response retained.
        chip.SetObservedOutputs(2); chip.AdvanceTo(120);
        Assert.True(chip.GetOutput(0)); Assert.False(chip.GetOutput(1));
        Assert.Equal(134, chip.NextOutputEventNanoseconds);
        chip.AdvanceTo(134);
        Assert.Equal(byte.MaxValue, chip.OutputLevels); Assert.Null(chip.NextOutputEventNanoseconds);
        Assert.Equal(new long?[] { 120, 111, null, 120, null, 134, null }, deadlines);
    }

    [Fact]
    public void GlobalDeadlineFeedbackDoesNotPublishAnObsoleteObservedDate()
    {
        var chip = Create(); var dates = new List<long?>();
        chip.NextOutputEventNanosecondsChanged = dates.Add;
        chip.NextEventNanosecondsChanged = next => { if (next.HasValue) chip.SetObservedOutputs(2); };
        chip.SetInput(Sn74Ls138Input.A, true);
        Assert.Equal(new long?[] { 120 }, dates);
        chip.AdvanceTo(105); chip.SetInput(Sn74Ls138Input.A, false);
        Assert.Null(chip.NextEventNanoseconds); Assert.Null(chip.NextOutputEventNanoseconds);
        Assert.Equal(new long?[] { 120, null }, dates);
    }

    private static Sn74Ls138 Create(byte inputs = 8)
    {
        var chip = new Sn74Ls138(Sn74Ls138Timing.Typical);
        chip.SetInputs(inputs, 63);
        chip.AdvanceTo(100);
        return chip;
    }

    [Fact]
    public void CompleteTruthTableCoversAllAddressesAndAllThreeEnables()
    {
        for (byte input = 0; input < 64; input++)
        {
            var chip = Create(input);
            var enabled = (input & 56) == 8;
            var expected = enabled ? (byte)~(1 << (input & 7)) : byte.MaxValue;
            Assert.Equal(byte.MaxValue, chip.OutputKnown);
            Assert.Equal(expected, chip.OutputLevels);
            for (var output = 0; output < 8; output++)
                Assert.Equal((expected & (1 << output)) != 0, chip.GetOutput(output));
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void EveryPartialInputAssignmentMatchesAllConsistentPhysicalTruthTableRows()
    {
        for (byte known = 0; known < 64; known++)
        for (byte levels = 0; levels < 64; levels++)
        {
            if ((levels & ~known) != 0) continue;
            byte allHigh = 255, allLow = 255;
            for (var row = 0; row < 64; row++)
            {
                if ((row & known) != levels) continue;
                var outputs = (row & 56) == 8 ? (byte)~(1 << (row & 7)) : byte.MaxValue;
                allHigh &= outputs; allLow &= (byte)~outputs;
            }
            var chip = new Sn74Ls138(Sn74Ls138Timing.Typical);
            chip.SetInputs(levels, known);
            chip.AdvanceTo(100);
            Assert.Equal(allHigh, chip.OutputLevels);
            Assert.Equal((byte)(allHigh | allLow), chip.OutputKnown);
        }
    }

    [Theory]
    [InlineData(Sn74Ls138Input.A, 1)]
    [InlineData(Sn74Ls138Input.B, 2)]
    [InlineData(Sn74Ls138Input.C, 4)]
    public void SelectPathsRetainTwoAndThreeLogicLevelsAndOutputDirections(Sn74Ls138Input pin, int selected)
    {
        var chip = Create();
        var trace = new List<(long, int, bool?)>();
        chip.OutputChanged = (output, high) => trace.Add((chip.TimeNanoseconds, output, high));
        chip.SetInput(pin, true);
        chip.AdvanceTo(110);
        Assert.False(chip.GetOutput(0)); Assert.True(chip.GetOutput(selected));
        chip.AdvanceTo(111); Assert.True(chip.GetOutput(0));
        chip.AdvanceTo(120); Assert.False(chip.GetOutput(selected));
        chip.AdvanceTo(200);
        chip.SetInput(pin, false);
        chip.AdvanceTo(218); Assert.False(chip.GetOutput(0));
        chip.AdvanceTo(221); Assert.True(chip.GetOutput(selected));
        Assert.Equal(new (long, int, bool?)[] { (111, 0, true), (120, selected, false), (218, 0, false), (221, selected, true) }, trace);
    }

    [Theory]
    [InlineData(Sn74Ls138Input.G1, false, 14, 13)]
    [InlineData(Sn74Ls138Input.G2A, true, 12, 20)]
    [InlineData(Sn74Ls138Input.G2B, true, 12, 20)]
    public void EachEnableCanDemultiplexDataAtItsOwnPropagationDates(Sn74Ls138Input pin, bool disable, int rise, int fall)
    {
        var chip = Create(13); // CBA=101.
        var trace = new List<(long, int, bool?)>();
        chip.OutputChanged = (output, high) => trace.Add((chip.TimeNanoseconds, output, high));
        chip.SetInput(pin, disable);
        chip.AdvanceTo(100 + rise - 1); Assert.False(chip.GetOutput(5));
        chip.AdvanceTo(100 + rise); Assert.True(chip.GetOutput(5));
        chip.AdvanceTo(200);
        chip.SetInput(pin, !disable);
        chip.AdvanceTo(200 + fall); Assert.False(chip.GetOutput(5));
        Assert.Equal(new (long, int, bool?)[] { (100 + rise, 5, true), (200 + fall, 5, false) }, trace);
        Assert.Equal(0xdf, chip.OutputLevels);
    }

    [Fact]
    public void OppositeResponseCancelsPendingSelectionWithoutRetimingUnchangedOutputs()
    {
        var chip = Create();
        var trace = new List<(long, int, bool?)>();
        chip.OutputChanged = (output, high) => trace.Add((chip.TimeNanoseconds, output, high));
        chip.SetInput(Sn74Ls138Input.A, true);
        chip.AdvanceTo(105);
        chip.SetInput(Sn74Ls138Input.A, false);
        Assert.Null(chip.NextEventNanoseconds);
        chip.AdvanceTo(150);
        Assert.Empty(trace);
        Assert.Equal(0xfe, chip.OutputLevels);
        chip.SetInput(Sn74Ls138Input.G1, false);
        chip.AdvanceTo(155);
        chip.SetInput(Sn74Ls138Input.C, true); // Already disabled: no postponed release.
        Assert.Equal(164, chip.NextEventNanoseconds);
        chip.AdvanceTo(164);
        Assert.Equal(byte.MaxValue, chip.OutputLevels);
    }

    [Fact]
    public void AnUnknownSelectLeavesOnlyCompatibleOutputsIndeterminate()
    {
        var chip = Create(13);
        chip.SetInput(Sn74Ls138Input.A, null);
        chip.AdvanceTo(150);
        Assert.Equal(0xcf, chip.OutputKnown); // Only /Y4 and /Y5 may be selected.
        Assert.Null(chip.GetOutput(4)); Assert.Null(chip.GetOutput(5));
        chip.SetInput(Sn74Ls138Input.G2A, true);
        chip.AdvanceTo(200);
        Assert.Equal(byte.MaxValue, chip.OutputKnown);
        Assert.Equal(byte.MaxValue, chip.OutputLevels);
    }

    [Fact]
    public void CommonResponseDateResolvesAllOutputsBeforePublishingAndRefreshesRecipients()
    {
        var chip = new Sn74Ls138(new(7, 7, 7, 7, 7, 7, 7, 7));
        var seen = new List<int>();
        chip.OutputChanged = (output, _) =>
        {
            Assert.Equal(byte.MaxValue, chip.OutputKnown);
            Assert.Equal(0xfe, chip.OutputLevels);
            seen.Add(output);
            if (output == 0) chip.SetObservedOutputs(128);
        };
        chip.SetInputs(8, 63);
        chip.AdvanceTo(7);
        Assert.Equal(new[] { 0, 7 }, seen);
    }

    [Fact]
    public void FeedbackArmsTheNextRealResponseAndDeadlineNotificationsRemainLocal()
    {
        var chip = Create();
        var dates = new List<long?>();
        chip.NextEventNanosecondsChanged = dates.Add;
        chip.OutputChanged = (output, high) =>
        {
            Assert.Throws<InvalidOperationException>(() => chip.AdvanceTo(chip.TimeNanoseconds + 1));
            if (output == 0 && high == true) chip.SetInput(Sn74Ls138Input.G1, false);
        };
        chip.SetInput(Sn74Ls138Input.A, true);
        chip.AdvanceTo(150);
        Assert.Equal(new long?[] { 111, null }, dates); // Feedback cancels selection before it propagates.
        Assert.Equal(byte.MaxValue, chip.OutputLevels);
        chip.AdvanceTo(150);
        Assert.Equal(2, dates.Count);
    }

    [Fact]
    public void MaximumCharacterizationPreservesTheDistinctSelectTrajectories()
    {
        var chip = new Sn74Ls138(Sn74Ls138Timing.CharacterizedMaximum);
        chip.SetInputs(8, 63); chip.AdvanceTo(100);
        chip.SetInput(Sn74Ls138Input.A, true);
        chip.AdvanceTo(120); Assert.True(chip.GetOutput(0));
        chip.AdvanceTo(138); Assert.True(chip.GetOutput(1));
        chip.AdvanceTo(139); Assert.False(chip.GetOutput(1));
    }

    [Fact]
    public void SimultaneousBlockingTermsUseTheFirstBlockerAndSelectionNeedsTheLastRequiredTerm()
    {
        var chip = Create(13); // /Y5 selected, A and C high.
        chip.SetInputs(0, 63); // A/C falling and G1 disabling at the same instant.
        Assert.Equal(114, chip.NextEventNanoseconds); // G1's 14 ns, not the unrelated 21 ns path.
        chip.AdvanceTo(114);
        Assert.Equal(byte.MaxValue, chip.OutputLevels);
        chip.AdvanceTo(200);
        chip.SetInputs(9, 63); // A rises and G1 enables: /Y1 needs both.
        Assert.Equal(220, chip.NextEventNanoseconds); // A's 20 ns, after G1's 13 ns.
        chip.AdvanceTo(220);
        Assert.False(chip.GetOutput(1));
        chip.SetInputs(17, 63); // G1 disables and /G2A disables; earliest blocker wins.
        Assert.Equal(232, chip.NextEventNanoseconds);
        chip.AdvanceTo(232);
        Assert.True(chip.GetOutput(1));
    }

    [Fact]
    public void InvalidInputsAndOverflowDoNotPartiallyProgramAnotherOutput()
    {
        Assert.Throws<ArgumentNullException>(() => new Sn74Ls138(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls138Timing(0, 1, 1, 1, 1, 1, 1, 1));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.GetOutput(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.GetOutput(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInput((Sn74Ls138Input)6, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputs(64, 63));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(99));
        chip.AdvanceTo(long.MaxValue - 11);
        Assert.Throws<OverflowException>(() => chip.SetInput(Sn74Ls138Input.A, true));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Equal(0xfe, chip.OutputLevels);
        var last = new Sn74Ls138(new(11, 11, 11, 11, 11, 11, 11, 11));
        last.AdvanceTo(long.MaxValue - 11);
        last.SetInputs(8, 63);
        Assert.Equal(long.MaxValue, last.NextEventNanoseconds);
        last.AdvanceTo(long.MaxValue);
        Assert.Equal(0xfe, last.OutputLevels);
        Assert.Null(last.NextEventNanoseconds);
    }

    [Fact]
    public void EveryStableSingleSelectTransitionPreservesBothPhysicalResponsesForEveryProfile()
    {
        Sn74Ls138Timing[] profiles = [Sn74Ls138Timing.Typical, Sn74Ls138Timing.CharacterizedMaximum,
            new(30, 1, 1, 40, 8, 9, 10, 11)];
        foreach (var timing in profiles)
        for (var before = 0; before < 8; before++)
        for (var bit = 1; bit <= 4; bit <<= 1)
        {
            var chip = new Sn74Ls138(timing);
            chip.SetInputs((byte)(8 | before), 63);
            chip.AdvanceTo(100);
            var after = before ^ bit;
            var riseAt = 100 + ((before & bit) == 0 ? timing.SelectTwoRise : timing.SelectThreeRise);
            var fallAt = 100 + ((after & bit) == 0 ? timing.SelectTwoFall : timing.SelectThreeFall);
            var trace = new List<(long Time, int Pin, bool? High)>();
            chip.OutputChanged = (pin, high) => trace.Add((chip.TimeNanoseconds, pin, high));
            chip.SetInputs((byte)(8 | after), 63);
            for (var time = 100; time <= 150; time++)
            {
                chip.AdvanceTo(time);
                var expected = 255;
                if (time < riseAt) expected &= ~(1 << before);
                if (time >= fallAt) expected &= ~(1 << after);
                Assert.Equal(expected, chip.OutputLevels);
                Assert.Equal(255, chip.OutputKnown);
                for (var pin = 0; pin < 8; pin++)
                    Assert.Equal((expected & (1 << pin)) != 0, chip.GetOutput(pin));
                long? next = time < riseAt ? riseAt : null;
                if (time < fallAt && (!next.HasValue || fallAt < next)) next = fallAt;
                Assert.Equal(next, chip.NextEventNanoseconds);
            }
            (long Time, int Pin, bool? High)[] expectedTrace = [(riseAt, before, true), (fallAt, after, false)];
            Assert.Equal(expectedTrace.OrderBy(edge => edge.Time).ThenBy(edge => edge.Pin), trace);
        }
    }
}
