using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;
using RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS165;

public sealed class Sn74Ls165Tests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ConstantLoadedPeriodsKeepTheAdditionalHighClockLoadEdges(bool high, bool phase)
    {
        foreach (var periods in new[] { 0, 1, 3, 17, 101 })
        {
            Sn74Ls165 Create()
            {
                var chip = new Sn74Ls165(12_000_000, high ? byte.MaxValue : (byte)0);
                chip.SetSerialInput(high); chip.SetParallelData(high ? byte.MaxValue : (byte)0); chip.SetClock(phase);
                return chip;
            }
            var actual = Create(); var reference = Create();
            Assert.True(actual.TryAdvanceConstantLoadedPeriods(periods * 6, periods));
            for (var i = 0; i < periods; i++)
            {
                for (var clock = 0; clock < 3; clock++) { reference.SetClock(!phase); reference.SetClock(phase); }
                reference.SetShiftLoad(false); reference.SetShiftLoad(true);
                for (var clock = 0; clock < 3; clock++) { reference.SetClock(!phase); reference.SetClock(phase); }
            }
            Assert.Equal(reference.InputClockCount, actual.InputClockCount);
            Assert.Equal(reference.ShiftClockCount, actual.ShiftClockCount);
            Assert.Equal(reference.StoredData, actual.StoredData);
            Assert.Equal(reference.GatedClockHigh, actual.GatedClockHigh);
            actual.SetParallelData(high ? (byte)0 : byte.MaxValue);
            Assert.False(actual.TryAdvanceConstantLoadedPeriods(6, 1));
        }
    }

    [Fact]
    public void ConstantLoadsRejectUnknownInputsAndCounterOverflowAtomically()
    {
        var chip = new Sn74Ls165(12_000_000, 0);
        Assert.False(chip.TryAdvanceConstantLoadedPeriods(6, 1));
        chip.SetSerialInput(false); chip.SetParallelData(0);
        chip.AdvanceStableClocks(long.MaxValue - 1);
        Assert.Throws<OverflowException>(() => chip.TryAdvanceConstantLoadedPeriods(2, 1));
        Assert.Equal(long.MaxValue - 1, chip.InputClockCount);
        chip.SetClock(true);
        Assert.Throws<OverflowException>(() => chip.TryAdvanceConstantLoadedPeriods(0, 1));
        Assert.Equal(long.MaxValue, chip.InputClockCount);
        Assert.Equal(long.MaxValue, chip.ShiftClockCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.TryAdvanceConstantLoadedPeriods(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.TryAdvanceConstantLoadedPeriods(0, -1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StablePrefixMatchesEveryWordUntilItsFirstKnownOrUnknownOutputChange(bool startHigh)
    {
        foreach (var (value, mask) in TriStateWords())
        foreach (var serial in new bool?[] { null, false, true })
        {
            var probe = StableFixture((byte)value, mask, serial);
            probe.SetClock(startHigh);
            var original = probe.SerialOutput;
            long expected = 10;
            for (var clock = 0; clock < 10; clock++)
            {
                Period(probe);
                if (probe.SerialOutput != original) { expected = clock; break; }
            }
            var grouped = StableFixture((byte)value, mask, serial);
            var unit = StableFixture((byte)value, mask, serial);
            grouped.SetClock(startHigh); unit.SetClock(startHigh);
            var changes = new List<bool?>();
            grouped.OutputsChanged = (high, _) => changes.Add(high);
            Assert.Equal(expected == 10 ? long.MaxValue : expected, grouped.StableClockPeriodsAtCurrentPhase);
            Assert.Equal(expected, startHigh ? grouped.AdvanceStableClockPeriods(10) : grouped.AdvanceStableClocks(10));
            for (var clock = 0; clock < expected; clock++) Period(unit);
            Assert.Empty(changes);
            Assert.Equal(unit.KnownBits, grouped.KnownBits);
            Assert.Equal(unit.StoredData, grouped.StoredData);
            Assert.Equal(unit.SerialOutput, grouped.SerialOutput);
            Assert.Equal(unit.ComplementaryOutput, grouped.ComplementaryOutput);
            Assert.Equal(unit.InputClockCount, grouped.InputClockCount);
            Assert.Equal(unit.ShiftClockCount, grouped.ShiftClockCount);
            Assert.Equal(unit.GatedClockHigh, grouped.GatedClockHigh);
            Assert.Equal(startHigh, grouped.ClockHigh);
            if (expected < 10)
            {
                Period(grouped);
                Assert.Single(changes);
                Assert.Equal(probe.SerialOutput, changes[0]);
                Assert.Equal(probe.InputClockCount, grouped.InputClockCount);
            }
        }

        void Period(Sn74Ls165 chip) { chip.SetClock(!startHigh); chip.SetClock(startHigh); }
    }

    [Fact]
    public void SerialAndParallelChangesRearmTheRetainedOutputDeadline()
    {
        var chip = new Sn74Ls165(12_000_000, 0);
        chip.SetSerialInput(false);
        Assert.Equal(long.MaxValue, chip.StableClockPeriods);
        chip.SetSerialInput(true);
        Assert.Equal(7, chip.StableClockPeriods);
        Assert.Equal(3, chip.AdvanceStableClocks(3));
        Assert.Equal(4, chip.StableClockPeriods);
        chip.SetParallelData(0xff);
        chip.SetShiftLoad(false); chip.SetShiftLoad(true);
        Assert.Equal(long.MaxValue, chip.StableClockPeriods);
        chip.SetSerialInput(null);
        Assert.Equal(7, chip.AdvanceStableClocks(100));
        Assert.Equal(0x80, chip.KnownBits);
        Assert.True(chip.SerialOutput);
        Assert.Equal(0, chip.StableClockPeriods);
        chip.Clock();
        Assert.Null(chip.SerialOutput);
        Assert.Equal(long.MaxValue, chip.StableClockPeriods);
    }

    private static IEnumerable<(byte Data, byte Known)> TriStateWords()
    {
        for (var word = 0; word < 6561; word++) // 3^8 physically distinct known-low/high/unknown words.
        {
            byte data = 0, known = 0;
            var digits = word;
            for (var bit = 0; bit < 8; bit++)
            {
                var level = digits % 3;
                digits /= 3;
                if (level != 0) known |= (byte)(1 << bit);
                if (level == 2) data |= (byte)(1 << bit);
            }
            yield return (data, known);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void EstablishedSerialLevelsAdvanceLongSpansWithoutInventingTransitions(bool? serial)
    {
        var chip = new Sn74Ls165(12_000_000, serial.HasValue ? serial.Value ? (byte)255 : (byte)0 : null);
        chip.SetSerialInput(serial);
        var notifications = 0;
        chip.OutputsChanged = (_, _) => notifications++;
        Assert.Equal(1_000_000_000_000L, chip.AdvanceStableClocks(1_000_000_000_000L));
        Assert.Equal(1_000_000_000_000L, chip.InputClockCount);
        Assert.Equal(1_000_000_000_000L, chip.ShiftClockCount);
        Assert.Equal(serial, chip.SerialOutput);
        Assert.Equal(0, notifications);
        Assert.False(chip.ClockHigh);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void StablePrefixPreservesTheLoadAndInhibitClockGates(bool loadHigh, bool inhibit)
    {
        var grouped = StableFixture(0xa5, 0xff, false);
        var unit = StableFixture(0xa5, 0xff, false);
        foreach (var chip in new[] { grouped, unit })
        {
            chip.SetShiftLoad(loadHigh);
            chip.SetClockInhibit(inhibit);
        }
        Assert.Equal(20, grouped.AdvanceStableClocks(20));
        for (var clock = 0; clock < 20; clock++) unit.Clock();
        Assert.Equal(unit.StoredData, grouped.StoredData);
        Assert.Equal(unit.KnownBits, grouped.KnownBits);
        Assert.Equal(unit.InputClockCount, grouped.InputClockCount);
        Assert.Equal(unit.ShiftClockCount, grouped.ShiftClockCount);
        Assert.Equal(unit.GatedClockHigh, grouped.GatedClockHigh);
    }

    [Fact]
    public void StablePrefixRejectsNegativeCountsAndAnUnresolvedHighClock()
    {
        var chip = new Sn74Ls165(12_000_000, 0);
        Assert.Equal(0, chip.AdvanceStableClocks(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceStableClocks(-1));
        chip.CaptureClock(true);
        Assert.Throws<InvalidOperationException>(() => chip.AdvanceStableClocks(1));
        chip.ResolveClock();
        chip.SetClock(false);
        Assert.False(chip.ClockHigh);
    }

    private static Sn74Ls165 StableFixture(byte data, byte known, bool? serial)
    {
        var chip = new Sn74Ls165(12_000_000);
        chip.SetSerialInput(serial);
        chip.SetParallelData(data, known);
        chip.SetShiftLoad(false);
        chip.SetShiftLoad(true);
        return chip;
    }

    [Fact]
    public void CompleteInputEdgesMatchSharedCaptureWithChangingGateAndUnknownInputs()
    {
        var complete = new Sn74Ls165(12_000_000);
        var shared = new Sn74Ls165(12_000_000);
        var first = new List<(bool?, bool?, long, long, bool)>();
        var second = new List<(bool?, bool?, long, long, bool)>();
        complete.OutputsChanged = (q, n) => first.Add((q, n, complete.InputClockCount, complete.ShiftClockCount, complete.ClockHigh));
        shared.OutputsChanged = (q, n) => second.Add((q, n, shared.InputClockCount, shared.ShiftClockCount, shared.ClockHigh));
        for (var step = 0; step < 128; step++)
        {
            foreach (var chip in new[] { complete, shared })
            {
                chip.SetParallelData((byte)(step * 37), step % 3 == 0 ? (byte)0x83 : byte.MaxValue);
                chip.SetSerialInput(step % 7 == 0 ? null : (step & 4) != 0);
                chip.SetShiftLoad((step & 8) != 0);
                chip.SetClockInhibit((step & 2) != 0);
            }
            var high = (step & 1) != 0;
            complete.SetClock(high);
            shared.CaptureClock(high);
            shared.ResolveClock();
            Assert.Equal(shared.StoredData, complete.StoredData);
            Assert.Equal(shared.KnownBits, complete.KnownBits);
            Assert.Equal(shared.InputClockCount, complete.InputClockCount);
            Assert.Equal(shared.ShiftClockCount, complete.ShiftClockCount);
            Assert.Equal(shared.GatedClockHigh, complete.GatedClockHigh);
            Assert.Equal(second, first);
        }
    }

    [Fact]
    public void CompleteInputCanResolveAPendingSharedEdgeWithoutCreatingAnotherClock()
    {
        var chip = new Sn74Ls165(12_000_000, 0x40);
        var changes = new List<bool?>();
        chip.OutputsChanged = (q, _) => changes.Add(q);
        chip.CaptureClock(true);
        Assert.Empty(changes);
        Assert.Throws<InvalidOperationException>(() => chip.SetClock(false));
        chip.SetClock(true);
        Assert.Equal(new bool?[] { true }, changes);
        Assert.Equal(1, chip.InputClockCount);
        Assert.Equal(1, chip.ShiftClockCount);
        chip.SetClock(false);
        Assert.False(chip.ClockHigh);
    }

    [Fact]
    public void AParallelLoadKeepsKnownGroundPinsSeparateFromUnknownRomPins()
    {
        var chip = new Sn74Ls165(12_000_000);
        chip.SetSerialInput(false);
        chip.SetParallelData(0, 0x83); // A, B and H grounded; C..G unknown.
        chip.SetShiftLoad(false);
        Assert.Equal(0x83, chip.ParallelKnownBits);
        Assert.Equal(0x83, chip.KnownBits);
        Assert.Null(chip.StoredData);
        Assert.False(chip.SerialOutput);
        chip.SetShiftLoad(true);
        for (var i = 0; i < 5; i++)
        {
            chip.Clock();
            Assert.Null(chip.SerialOutput);
            Assert.Null(chip.ComplementaryOutput);
        }
        chip.Clock();
        Assert.False(chip.SerialOutput); // B is grounded in this fixture.
        Assert.Equal(byte.MaxValue, chip.KnownBits);
    }

    [Fact]
    public void ResolvingAParallelInputWhileLoadIsLowUpdatesTheOutputWithoutAClock()
    {
        var chip = new Sn74Ls165(12_000_000);
        chip.SetParallelData(0xff, 0x7f);
        chip.SetShiftLoad(false);
        Assert.Null(chip.SerialOutput);
        var outputs = new List<(bool?, bool?)>();
        chip.OutputsChanged = (high, inverse) => outputs.Add((high, inverse));
        chip.SetParallelData(0xff);
        Assert.Equal(byte.MaxValue, chip.ParallelKnownBits);
        Assert.Equal(byte.MaxValue, chip.StoredData);
        Assert.Equal(new (bool?, bool?)[] { (true, false) }, outputs);
        Assert.Equal(0, chip.InputClockCount);
        Assert.Equal(0, chip.ShiftClockCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(64)]
    public void PartialParallelKnowledgeSurvivesGroupedAndExplicitClocking(int periods)
    {
        var grouped = new Sn74Ls165(12_000_000);
        var unit = new Sn74Ls165(12_000_000);
        var groupEdges = new List<(long, bool?)>();
        var unitEdges = new List<(long, bool?)>();
        foreach (var chip in new[] { grouped, unit })
        {
            chip.SetParallelData(0x80, 0x83);
            chip.SetSerialInput(false);
            chip.SetShiftLoad(false);
            chip.SetShiftLoad(true);
        }
        grouped.OutputsChanged = (high, _) => groupEdges.Add((grouped.InputClockCount, high));
        unit.OutputsChanged = (high, _) => unitEdges.Add((unit.InputClockCount, high));
        grouped.AdvanceClocks(periods);
        for (var i = 0; i < periods; i++) { unit.SetClock(true); unit.SetClock(false); }
        Assert.Equal(unit.StoredData, grouped.StoredData);
        Assert.Equal(unit.KnownBits, grouped.KnownBits);
        Assert.Equal(unit.ShiftClockCount, grouped.ShiftClockCount);
        Assert.Equal(unitEdges, groupEdges);
    }

    [Fact]
    public void RomNotificationsResolveOnlyTheFiveConnectedParallelPins()
    {
        var contents = new byte[2048]; contents[0] = 0xa8; contents[1] = 0x50;
        // Synthetic timing fixture; no VDU80 timing profile is selected here.
        var rom = new Mm52116(contents, 0, 10, 4, 3);
        var shift = new Sn74Ls165(12_000_000);
        void Receive(byte? data, bool driven)
        {
            var known = driven && data.HasValue;
            shift.SetParallelData(known ? (byte)((data!.Value >> 1) & 0x7c) : (byte)0,
                known ? byte.MaxValue : (byte)0x83);
        }
        rom.OutputsChanged = Receive;
        try
        {
            Receive(rom.OutputData, rom.OutputsDriven);
            shift.SetSerialInput(false);
            shift.SetShiftLoad(false);
            Assert.False(shift.SerialOutput); // H is grounded even before ROM responds.
            Assert.Equal(0x83, shift.KnownBits);
            rom.AdvanceTo(4);
            Assert.Equal(0x83, shift.KnownBits);
            rom.AdvanceTo(10);
            Assert.Equal((byte)0x54, shift.StoredData);
            shift.SetShiftLoad(true);
            rom.SetAddress(1); rom.AdvanceTo(20);
            Assert.Equal((byte)0x54, shift.StoredData); // Parallel change cannot reload in shift mode.
            Assert.Equal((byte)0x28, shift.ParallelData);
            shift.SetShiftLoad(false);
            Assert.Equal((byte)0x28, shift.StoredData);
        }
        finally { rom.OutputsChanged = null; }
    }

    [Fact]
    public void EveryParallelPatternLoadsDirectlyAndSerializesFromHToA()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var chip = new Sn74Ls165(12_000_000);
            chip.SetSerialInput(false);
            chip.SetParallelData((byte)value);
            chip.SetShiftLoad(false);
            Assert.Equal((byte)value, chip.StoredData);
            Assert.Equal(byte.MaxValue, chip.KnownBits);
            chip.SetShiftLoad(true);
            for (var bit = 7; bit >= 0; bit--)
            {
                var expected = (value & (1 << bit)) != 0;
                Assert.Equal(expected, chip.SerialOutput);
                Assert.Equal(!expected, chip.ComplementaryOutput);
                chip.Clock();
            }
            Assert.Equal((byte)0, chip.StoredData);
            Assert.Equal(8, chip.InputClockCount);
            Assert.Equal(8, chip.ShiftClockCount);
        }
    }

    [Fact]
    public void ParallelInputsOverrideSerialAndBothClockInputsWhileLoadIsLow()
    {
        var chip = new Sn74Ls165(12_000_000);
        chip.SetShiftLoad(false);
        chip.SetClockInhibit(true);
        chip.SetClock(true);
        chip.SetParallelData(0x5a);
        chip.SetSerialInput(true);
        Assert.Equal((byte)0x5a, chip.StoredData);
        chip.SetParallelData(0xa5);
        Assert.Equal((byte)0xa5, chip.StoredData);
        Assert.True(chip.SerialOutput);
        Assert.Equal(0, chip.ShiftClockCount);
        chip.SetClock(false);
        chip.SetClockInhibit(false);
        chip.SetShiftLoad(true);
        Assert.Equal((byte)0xa5, chip.StoredData);
    }

    [Fact]
    public void OriginalNorClockGateIncludesInhibitAndLoadRatherThanOnlyClockPin()
    {
        var chip = new Sn74Ls165(12_000_000, 1);
        chip.SetSerialInput(false);
        chip.SetClockInhibit(true); // CLK low: NOR output falls, clocking the stages.
        Assert.Equal((byte)2, chip.StoredData);
        Assert.Equal(0, chip.InputClockCount);
        Assert.Equal(1, chip.ShiftClockCount);
        chip.Clock(); // Inhibit already high: no new internal edge.
        Assert.Equal((byte)2, chip.StoredData);
        chip.SetClockInhibit(false);
        chip.SetClock(true);
        chip.SetClockInhibit(true); // CLK high: no extra edge, as TI recommends.
        Assert.Equal((byte)4, chip.StoredData);
        chip.SetClock(false);
        chip.SetShiftLoad(false);
        chip.SetParallelData(1);
        Assert.False(chip.GatedClockHigh);
        chip.SetShiftLoad(true); // Load also gates the NOR in the original diagram.
        Assert.True(chip.GatedClockHigh);
        Assert.Equal((byte)2, chip.StoredData);
        Assert.Equal(3, chip.ShiftClockCount);
    }

    [Fact]
    public void UnknownPowerOnBitsAndUnknownSerialInputPropagateOneStagePerEdge()
    {
        var chip = new Sn74Ls165(12_000_000);
        Assert.Null(chip.StoredData);
        Assert.Null(chip.SerialOutput);
        chip.SetSerialInput(true);
        for (var count = 1; count <= 8; count++)
        {
            chip.Clock();
            Assert.Equal((byte)((1 << count) - 1), chip.KnownBits);
            if (count < 8) Assert.Null(chip.SerialOutput);
        }
        Assert.Equal(byte.MaxValue, chip.StoredData);
        chip.SetSerialInput(null);
        chip.AdvanceClocks(8);
        Assert.Equal(0, chip.KnownBits);
        Assert.Null(chip.SerialOutput);
        Assert.Null(chip.ComplementaryOutput);
    }

    [Fact]
    public void CascadedRegistersCaptureTheSameClockBeforePublishingEitherNewOutput()
    {
        var first = new Sn74Ls165(12_000_000, 0x80);
        var second = new Sn74Ls165(12_000_000, 0);
        first.SetSerialInput(false);
        second.SetSerialInput(first.SerialOutput);
        first.OutputsChanged = (high, _) => second.SetSerialInput(high);
        first.CaptureClock(true);
        second.CaptureClock(true);
        first.ResolveClock();
        second.ResolveClock();
        first.SetClock(false); second.SetClock(false);
        Assert.Equal((byte)0, first.StoredData);
        Assert.Equal((byte)1, second.StoredData);
        Assert.Equal(1, first.ShiftClockCount);
        Assert.Equal(1, second.ShiftClockCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(133)]
    public void GroupedClocksMatchExplicitLevelsAndAllFeedbackTransitions(int periods)
    {
        var grouped = new Sn74Ls165(12_000_000, 0xa5);
        var unit = new Sn74Ls165(12_000_000, 0xa5);
        var groupEdges = new List<(long, bool?, bool?)>();
        var unitEdges = new List<(long, bool?, bool?)>();
        void Wire(Sn74Ls165 chip, List<(long, bool?, bool?)> edges)
        {
            chip.SetSerialInput(chip.ComplementaryOutput);
            chip.OutputsChanged = (high, inverse) =>
            {
                edges.Add((chip.InputClockCount, high, inverse));
                chip.SetSerialInput(inverse);
            };
        }
        Wire(grouped, groupEdges); Wire(unit, unitEdges);
        grouped.AdvanceClocks(periods);
        for (var i = 0; i < periods; i++) { unit.SetClock(true); unit.SetClock(false); }
        Assert.Equal(unit.StoredData, grouped.StoredData);
        Assert.Equal(unit.KnownBits, grouped.KnownBits);
        Assert.Equal(unit.ShiftClockCount, grouped.ShiftClockCount);
        Assert.Equal(periods, grouped.InputClockCount);
        Assert.Equal(unitEdges, groupEdges);
        Assert.False(grouped.ClockHigh);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadAndInhibitRetainStorageButStillCountRawClockEdges(bool load)
    {
        var chip = new Sn74Ls165(12_000_000, 0x5a);
        if (load) { chip.SetParallelData(0xa5); chip.SetShiftLoad(false); }
        else chip.SetClockInhibit(true);
        var before = chip.StoredData;
        var shifts = chip.ShiftClockCount;
        chip.AdvanceClocks(1001);
        Assert.Equal(before, chip.StoredData);
        Assert.Equal(shifts, chip.ShiftClockCount);
        Assert.Equal(1001, chip.InputClockCount);
    }

    [Fact]
    public void DuplicateLevelsAndInvalidPeriodRequestsDoNotInventAClock()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn74Ls165(0));
        var chip = new Sn74Ls165(12_000_000);
        chip.AdvanceClocks(0);
        Assert.Equal(0, chip.KnownBits);
        chip.SetClock(true); chip.SetClock(true);
        Assert.Equal(1, chip.InputClockCount);
        Assert.Throws<InvalidOperationException>(() => chip.Clock());
        Assert.Throws<InvalidOperationException>(() => chip.AdvanceClocks(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceClocks(-1));
        chip.SetClock(false);
        chip.CaptureClock(true);
        Assert.Throws<InvalidOperationException>(() => chip.CaptureClock(false));
        chip.ResolveClock(); chip.SetClock(false);
        Assert.Equal(2, chip.InputClockCount);
    }
}
