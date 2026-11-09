using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN74LS165;

public sealed class Sn74Ls165Tests
{
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
