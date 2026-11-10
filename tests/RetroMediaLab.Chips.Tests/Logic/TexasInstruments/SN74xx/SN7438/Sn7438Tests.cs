using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Services;

namespace RetroMediaLab.Chips.Tests.Logic.TexasInstruments.SN74xx.SN7438;

public sealed class Sn7438Tests
{
    private static Sn7438 Create() => new(Sn7438Timing.Typical);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryGateResolvesTheFullTableWithoutActivelyDrivingHigh(int gate)
    {
        foreach (var a in new bool?[] { null, false, true })
        foreach (var b in new bool?[] { null, false, true })
        {
            var chip = Create();
            chip.SetInputs(gate, a, b);
            chip.AdvanceTo(22);
            var expected = a == false || b == false ? Sn7438OutputDrive.Released
                : a == true && b == true ? Sn7438OutputDrive.Sinking : Sn7438OutputDrive.Unknown;
            Assert.Equal(expected, chip.GetOutputDrive(gate));
            for (var other = 0; other < 4; other++)
                if (other != gate) Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(other));
            Assert.Null(chip.NextEventNanoseconds);
        }
    }

    [Fact]
    public void ReleaseRequiresExternalPolarizationAndCannotOverrideAnotherCollector()
    {
        var chip = Create();
        bool? wire = null;
        var pullUp = false;
        void Resolve()
        {
            var a = chip.GetOutputDrive(0);
            var b = chip.GetOutputDrive(1);
            wire = a == Sn7438OutputDrive.Sinking || b == Sn7438OutputDrive.Sinking ? false
                : a == Sn7438OutputDrive.Unknown || b == Sn7438OutputDrive.Unknown ? null
                : pullUp ? true : null;
        }
        chip.OutputDriveChanged = (_, _) => Resolve();
        chip.SetInputs(0, false, true);
        chip.SetInputs(1, false, true);
        chip.AdvanceTo(14);
        Assert.Null(wire); // Both collectors released, no source of a high voltage.
        pullUp = true; Resolve();
        Assert.True(wire);
        chip.SetInputs(1, true, true);
        chip.AdvanceTo(24);
        Assert.True(wire);
        chip.AdvanceTo(25);
        Assert.False(wire); // Gate zero remains released, gate one sinks the shared wire.
        chip.SetInputs(0, true, null);
        chip.AdvanceTo(39);
        Assert.False(wire); // A known sink dominates an unknown driver.
        chip.SetInputA(1, false);
        chip.AdvanceTo(53);
        Assert.Null(wire);
    }

    [Theory]
    [InlineData(14, 11)]
    [InlineData(22, 18)]
    [InlineData(35, 27)]
    public void CharacterizedAndExternalLoadProfilesPreserveBothPropagationDates(long rise, long fall)
    {
        var chip = new Sn7438(new(rise, fall));
        var trace = new List<(long, Sn7438OutputDrive)>();
        chip.OutputDriveChanged = (_, drive) => trace.Add((chip.TimeNanoseconds, drive));
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(rise - 1);
        Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(0));
        chip.AdvanceTo(rise);
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(rise + fall - 1);
        Assert.Equal(Sn7438OutputDrive.Released, chip.GetOutputDrive(0));
        chip.AdvanceTo(rise + fall);
        Assert.Equal(new[] { (rise, Sn7438OutputDrive.Released), (rise + fall, Sn7438OutputDrive.Sinking) }, trace);
    }

    [Fact]
    public void UnchangedTargetDoesNotPostponeAndReturningToEstablishedDriveCancels()
    {
        var chip = Create();
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(5);
        chip.SetInputB(0, false);
        Assert.Equal(14, chip.NextEventNanoseconds);
        chip.AdvanceTo(20);
        chip.SetInputs(0, true, true);
        Assert.Equal(31, chip.NextEventNanoseconds);
        chip.AdvanceTo(25);
        chip.SetInputA(0, false);
        Assert.Null(chip.NextEventNanoseconds);
        chip.AdvanceTo(40);
        Assert.Equal(Sn7438OutputDrive.Released, chip.GetOutputDrive(0));
    }

    [Fact]
    public void ReplacingAResponseUsesTheNewTargetsOwnDate()
    {
        var chip = Create();
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(5);
        chip.SetInputs(0, true, true);
        Assert.Equal(16, chip.NextEventNanoseconds);
        chip.AdvanceTo(14);
        Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(0));
        chip.AdvanceTo(16);
        Assert.Equal(Sn7438OutputDrive.Sinking, chip.GetOutputDrive(0));
    }

    [Fact]
    public void SharedFrontResolvesEveryGateBeforeCallbacksAndRefreshesObservers()
    {
        var chip = Create();
        var trace = new List<int>();
        chip.OutputDriveChanged = (gate, _) =>
        {
            for (var other = 0; other < 4; other++)
                Assert.Equal(Sn7438OutputDrive.Released, chip.GetOutputDrive(other));
            trace.Add(gate);
            if (gate == 0) chip.SetObservedOutputs(8);
        };
        chip.SetInputLevels(0, 15, 15, 15);
        chip.AdvanceTo(14);
        Assert.Equal(new[] { 0, 3 }, trace);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(13));
    }

    [Fact]
    public void ResolvedWireFeedbackArmsTheNextPhysicalPropagationWithoutAClock()
    {
        var chip = Create();
        chip.SetInputB(1, true);
        chip.OutputDriveChanged = (gate, drive) =>
        {
            if (gate == 0)
                chip.SetInputA(1, drive == Sn7438OutputDrive.Unknown ? null : drive == Sn7438OutputDrive.Released);
        };
        chip.SetInputs(0, true, true);
        chip.AdvanceTo(11);
        Assert.Equal(25, chip.NextEventNanoseconds);
        Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(1));
        chip.AdvanceTo(25);
        Assert.Equal(Sn7438OutputDrive.Released, chip.GetOutputDrive(1));
    }

    [Fact]
    public void UnknownInputAndUnknownTransistorNeverBecomeAnAssumedHigh()
    {
        var chip = Create();
        chip.SetInputs(0, false, null);
        chip.AdvanceTo(14);
        chip.SetInputA(0, true);
        chip.AdvanceTo(27);
        Assert.Equal(Sn7438OutputDrive.Released, chip.GetOutputDrive(0));
        chip.AdvanceTo(28);
        Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(0));
    }

    [Fact]
    public void LocalDeadlineTracksArmAndCompletionAndDisallowsTimeAdvanceFromARecipient()
    {
        var chip = Create();
        var dates = new List<long?>();
        chip.NextEventNanosecondsChanged = dates.Add;
        chip.OutputDriveChanged = (_, _) => Assert.Throws<InvalidOperationException>(() => chip.AdvanceTo(100));
        chip.SetInputs(0, false, true);
        chip.SetInputs(0, false, true);
        chip.AdvanceTo(14);
        chip.AdvanceTo(14);
        Assert.Equal(new long?[] { 14, null }, dates);
    }

    [Fact]
    public void KnownNullableAndParallelInputsKeepEveryOutputAndDateEquivalent()
    {
        var known = Create(); var nullable = Create(); var packed = Create();
        var a = new List<(long, int, Sn7438OutputDrive)>();
        var b = new List<(long, int, Sn7438OutputDrive)>();
        var c = new List<(long, int, Sn7438OutputDrive)>();
        known.OutputDriveChanged = (gate, drive) => a.Add((known.TimeNanoseconds, gate, drive));
        nullable.OutputDriveChanged = (gate, drive) => b.Add((nullable.TimeNanoseconds, gate, drive));
        packed.OutputDriveChanged = (gate, drive) => c.Add((packed.TimeNanoseconds, gate, drive));
        for (var step = 0; step < 1024; step++)
        {
            var al = (byte)(step & 15); var bl = (byte)((step >> 4) & 15);
            packed.SetInputLevels(al, 15, bl, 15);
            for (var gate = 0; gate < 4; gate++)
            {
                known.SetInputs(gate, (al & (1 << gate)) != 0, (bl & (1 << gate)) != 0);
                nullable.SetInputs(gate, (bool?)((al & (1 << gate)) != 0), (bl & (1 << gate)) != 0);
            }
            var date = (step + 1) * 25L;
            known.AdvanceTo(date); nullable.AdvanceTo(date); packed.AdvanceTo(date);
        }
        Assert.Equal(a, b); Assert.Equal(a, c);
        for (var gate = 0; gate < 4; gate++)
            Assert.Equal(known.GetOutputDrive(gate), packed.GetOutputDrive(gate));
    }

    [Fact]
    public void ParallelUnknownPinsMatchTheIndependentTruthTable()
    {
        for (byte ak = 0; ak < 16; ak++)
        for (byte bk = 0; bk < 16; bk++)
        {
            var chip = Create();
            chip.SetInputLevels(5, ak, 10, bk);
            chip.AdvanceTo(22);
            for (var gate = 0; gate < 4; gate++)
            {
                var mask = 1 << gate;
                bool? av = (ak & mask) == 0 ? null : (5 & mask) != 0;
                bool? bv = (bk & mask) == 0 ? null : (10 & mask) != 0;
                var expected = av == false || bv == false ? Sn7438OutputDrive.Released
                    : av == true && bv == true ? Sn7438OutputDrive.Sinking : Sn7438OutputDrive.Unknown;
                Assert.Equal(expected, chip.GetOutputDrive(gate));
            }
        }
    }

    [Fact]
    public void InvalidArgumentsAndOverflowDoNotPartiallyMutateInputsOrDeadlines()
    {
        Assert.Throws<ArgumentNullException>(() => new Sn7438(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn7438Timing(0, 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sn7438Timing(14, -1));
        var chip = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputA(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.GetOutputDrive(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetObservedOutputs(16));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.SetInputLevels(16, 15, 15, 15));
        chip.AdvanceTo(long.MaxValue - 11);
        Assert.Throws<OverflowException>(() => chip.SetInputLevels(2, 3, 3, 3));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Equal(Sn7438OutputDrive.Unknown, chip.GetOutputDrive(1));
        chip.SetInputs(1, true, true);
        Assert.Equal(long.MaxValue, chip.NextEventNanoseconds);
        chip.AdvanceTo(long.MaxValue);
        Assert.Equal(Sn7438OutputDrive.Sinking, chip.GetOutputDrive(1));
        Assert.Throws<OverflowException>(() => chip.SetInputB(1, false));
        Assert.Null(chip.NextEventNanoseconds);
        Assert.Equal(Sn7438OutputDrive.Sinking, chip.GetOutputDrive(1));
    }
}
