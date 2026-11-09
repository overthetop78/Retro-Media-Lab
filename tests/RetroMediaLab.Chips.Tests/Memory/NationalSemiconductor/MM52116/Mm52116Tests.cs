using RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Services;

namespace RetroMediaLab.Chips.Tests.Memory.NationalSemiconductor.MM52116;

public sealed class Mm52116Tests
{
    [Fact]
    public void AllAddressesAndAllProgrammedSelectCombinationsReadTheMaskImage()
    {
        var contents = Enumerable.Range(0, 2048).Select(i => (byte)(i * 73 + (i >> 8))).ToArray();
        for (byte programmed = 0; programmed < 8; programmed++)
        {
            var chip = new Mm52116(contents, programmed, 450, 120, 100);
            chip.SetChipSelects(programmed);
            for (var address = 0; address < 2048; address++)
            {
                chip.SetAddress((ushort)(address | 0xf800));
                chip.AdvanceTo(chip.TimeNanoseconds + 450);
                Assert.Equal(address, chip.Address);
                Assert.True(chip.OutputsDriven);
                Assert.Equal(contents[address], chip.OutputData);
            }
            for (byte pins = 0; pins < 8; pins++)
            {
                chip.SetChipSelects(pins);
                chip.AdvanceTo(chip.TimeNanoseconds + 450);
                Assert.Equal(pins == programmed, chip.OutputsDriven);
                Assert.Equal(pins == programmed ? contents[^1] : (byte?)null, chip.OutputData);
            }
        }
    }

    [Fact]
    public void DecodeSelectionAndTurnOffHaveIndependentLocalDeadlines()
    {
        var contents = new byte[2048]; contents[0] = 0x5a; contents[1] = 0xa5;
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        var edges = new List<(long, byte?, bool)>();
        chip.OutputsChanged = (data, driven) => edges.Add((chip.TimeNanoseconds, data, driven));
        Assert.Equal(4, chip.NextEventNanoseconds);
        chip.AdvanceTo(3);
        Assert.False(chip.OutputsDriven);
        chip.AdvanceTo(4);
        Assert.True(chip.OutputsDriven);
        Assert.Null(chip.OutputData);
        chip.AdvanceTo(10);
        Assert.Equal((byte)0x5a, chip.OutputData);
        chip.SetAddress(1);
        chip.AdvanceTo(11);
        chip.SetChipSelects(1);
        Assert.Equal(14, chip.NextEventNanoseconds);
        chip.AdvanceTo(100);
        Assert.False(chip.OutputsDriven);
        Assert.Null(chip.NextEventNanoseconds);
        chip.SetChipSelects(0);
        chip.AdvanceTo(104);
        Assert.Equal((byte)0xa5, chip.OutputData);
        Assert.Equal(new (long, byte?, bool)[]
        {
            (4, null, true), (10, 0x5a, true), (14, null, false), (104, 0xa5, true)
        }, edges);
    }

    [Fact]
    public void StableAddressBeforeSelectionNeedsOnlyTheSelectDelay()
    {
        var contents = new byte[2048]; contents[17] = 0xc3;
        var chip = new Mm52116(contents, 7, 450, 120, 100);
        chip.SetAddress(17);
        chip.AdvanceTo(450);
        Assert.False(chip.OutputsDriven);
        chip.SetChipSelects(7);
        Assert.Equal(570, chip.NextEventNanoseconds);
        chip.AdvanceTo(569);
        Assert.Null(chip.OutputData);
        chip.AdvanceTo(570);
        Assert.Equal((byte)0xc3, chip.OutputData);
    }

    [Fact]
    public void SimultaneousDecodeAndDriverEnablePublishOnlyTheResolvedBusLevel()
    {
        var contents = new byte[2048]; contents[0] = 0x81;
        var chip = new Mm52116(contents, 0, 4, 4, 3);
        var edges = new List<(byte?, bool)>();
        chip.OutputsChanged = (data, driven) => edges.Add((data, driven));
        chip.AdvanceTo(4);
        Assert.Equal(new (byte?, bool)[] { (0x81, true) }, edges);
    }

    [Fact]
    public void ChangedAddressRestartsItsOwnDecodeWithoutChangingTheDriverDeadline()
    {
        var contents = new byte[2048]; contents[1] = 0x11; contents[2] = 0x22;
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        var values = new List<(long, byte?)>();
        chip.OutputsChanged = (data, _) => values.Add((chip.TimeNanoseconds, data));
        chip.AdvanceTo(2); chip.SetAddress(1);
        chip.AdvanceTo(5); chip.SetAddress(2);
        chip.AdvanceTo(14);
        Assert.Null(chip.OutputData);
        chip.AdvanceTo(15);
        Assert.Equal(new (long, byte?)[] { (4, null), (15, 0x22) }, values);
    }

    [Fact]
    public void ReturningToCurrentDriverLevelCancelsOnlyThePendingDriverChange()
    {
        var contents = new byte[2048]; contents[0] = 0x77;
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        chip.AdvanceTo(10);
        chip.SetChipSelects(1);
        chip.AdvanceTo(11);
        chip.SetChipSelects(0);
        Assert.Null(chip.NextEventNanoseconds);
        chip.AdvanceTo(100);
        Assert.True(chip.OutputsDriven);
        Assert.Equal((byte)0x77, chip.OutputData);
    }

    [Fact]
    public void EveryDeadlineIsNotifiedInOrderAndDataFeedbackCanArmTheNextAccess()
    {
        var contents = new byte[2048]; contents[0] = 1; contents[1] = 2;
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        var dataEvents = new List<(long, byte?)>();
        var deadlines = new List<(long, long?)>();
        chip.NextEventNanosecondsChanged = next => deadlines.Add((chip.TimeNanoseconds, next));
        chip.OutputsChanged = (data, _) =>
        {
            dataEvents.Add((chip.TimeNanoseconds, data));
            if (data == 1) chip.SetAddress(1);
        };
        chip.AdvanceTo(30);
        Assert.Equal(new (long, byte?)[] { (4, null), (10, 1), (20, 2) }, dataEvents);
        Assert.Equal(new (long, long?)[] { (4, 10), (10, 20), (20, null) }, deadlines);
        Assert.Equal(30, chip.TimeNanoseconds);
    }

    [Fact]
    public void RepeatedInputsAndEqualDataDoNotPublishDuplicateLevelsOrRestartAccess()
    {
        var contents = Enumerable.Repeat((byte)0x66, 2048).ToArray();
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        chip.AdvanceTo(10);
        var notifications = 0;
        chip.OutputsChanged = (_, _) => notifications++;
        chip.SetAddress(0xf800); chip.SetChipSelects(0xf8);
        Assert.Null(chip.NextEventNanoseconds);
        chip.SetAddress(1); chip.AdvanceTo(15); chip.SetAddress(1);
        Assert.Equal(20, chip.NextEventNanoseconds);
        chip.AdvanceTo(20);
        Assert.Equal(0, notifications);
        Assert.Null(chip.NextEventNanoseconds);
    }

    [Fact]
    public void MaskContentsAreCopiedAndInputsAreBoundedByPhysicalPins()
    {
        var contents = new byte[2048]; contents[0] = 0xa5;
        var chip = new Mm52116(contents, 0, 10, 4, 3);
        contents[0] = 0;
        chip.AdvanceTo(10);
        Assert.Equal((byte)0xa5, chip.OutputData);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.AdvanceTo(9));
        Assert.Throws<ArgumentException>(() => new Mm52116(new byte[2047], 0, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mm52116(contents, 8, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mm52116(contents, 0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mm52116(contents, 0, 451, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mm52116(contents, 0, 1, 121, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mm52116(contents, 0, 1, 1, 101));
    }
}
