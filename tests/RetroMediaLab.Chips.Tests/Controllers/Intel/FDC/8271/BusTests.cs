using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class BusTests
{
    public static TheoryData<bool, bool, bool, bool> SelectionLevels
    {
        get
        {
            var data = new TheoryData<bool, bool, bool, bool>();
            foreach (var selected in new[] { false, true })
                foreach (var dack in new[] { false, true })
                    foreach (var read in new[] { false, true })
                        foreach (var write in new[] { false, true })
                            data.Add(selected, dack, read, write);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SelectionLevels))]
    public void DataBusDrivesOnlyASelectedReadCycle(bool selected, bool dack, bool read, bool write)
    {
        var chip = new Intel8271();
        chip.SetBus(selected, read, write, dack, 0);
        Assert.Equal((selected || dack) && read && !write ? (byte?)0 : null, chip.BusData);
        Assert.Equal(0, chip.Read(0)); // A held WR has not yet committed its data.
        chip.SetBus(false, false, false, false, 0);
        Assert.Null(chip.BusData);
        Assert.Equal(0, chip.Read(0));
    }

    [Fact]
    public void CommandAndParameterRemainFullUntilIndividuallyConsumed()
    {
        var chip = new Intel8271();
        chip.WriteBus(true, false, 0, 0x3d);
        chip.WriteBus(true, false, 1, 0x17);
        Assert.Equal(0xe0, chip.Read(0));
        chip.Tick();
        Assert.Equal(0xa0, chip.Read(0));
        chip.Tick();
        Assert.Equal(0x10, chip.Read(0));
        Assert.Equal(0xc0, chip.Read(1));
        Assert.Equal(0, chip.Read(0));
    }

    [Fact]
    public void ParameterFullOverwriteLosesThePreviousParameterAsDocumented()
    {
        var chip = new Intel8271();
        chip.WriteBus(true, false, 0, 0x3a);
        chip.Tick();
        chip.WriteBus(true, false, 1, 0x17);
        chip.WriteBus(true, false, 1, 0x12);
        Assert.Equal(0xa0, chip.Read(0));
        chip.Tick();
        chip.WriteBus(true, false, 1, 0x2a);
        chip.Tick();
        Assert.Equal(0, chip.Read(0));
        Assert.Equal(0x2a, Special(chip, 0x12));
        Assert.Equal(0xc0, Special(chip, 0x17));
    }

    [Theory]
    [InlineData(4_000_000, 2_000_000, 2)]
    [InlineData(2_000_000, 2_000_000, 1)]
    [InlineData(2_000_000, 4_000_000, 1)]
    public void NominalBufferConsumptionUsesTheControllerClock(int ownerClock, int chipClock, int ticks)
    {
        var chip = new Intel8271(ownerClock, chipClock);
        chip.WriteBus(true, false, 0, 0x2c);
        for (var tick = 1; tick < ticks; tick++) { chip.Tick(); Assert.Equal(0xc0, chip.Read(0)); }
        chip.Tick();
        Assert.Equal(0x10, chip.Read(0));
    }

    [Fact]
    public void HeldWriteLatchesOnceOnReleaseAndUsesTheLastStableValue()
    {
        var chip = new Intel8271();
        chip.SetBus(true, false, true, false, 0, 0x2c);
        for (var tick = 0; tick < 8; tick++) chip.Tick();
        Assert.Equal(0, chip.Read(0));
        chip.SetBus(true, false, true, false, 0, 0x3d);
        chip.SetBus(true, false, false, false, 0, 0x3d);
        Assert.Equal(0xc0, chip.Read(0));
        chip.Tick();
        Assert.Equal(0x80, chip.Read(0));
        chip.WriteBus(true, false, 1, 0x17);
        chip.Tick();
        Assert.Equal(0xc0, chip.Read(1));
    }

    [Fact]
    public void LosingSelectBeforeWriteReleaseDoesNotLatchACommand()
    {
        var chip = new Intel8271();
        chip.SetBus(true, false, true, false, 0, 0x2c);
        chip.SetBus(false, false, true, false, 0, 0x2c);
        chip.SetBus(false, false, false, false, 0, 0x2c);
        chip.Tick();
        Assert.Equal(0, chip.Read(0));
    }

    [Fact]
    public void ResetDropsPendingBuffersAndInProgressBusOutput()
    {
        var chip = new Intel8271();
        chip.WriteBus(true, false, 0, 0x3d);
        chip.WriteBus(true, false, 1, 0x17);
        chip.SetBus(true, true, false, false, 0);
        Assert.Equal<byte?>(0xe0, chip.BusData);
        chip.SetReset(true);
        Assert.Null(chip.BusData);
        Assert.Equal(0, chip.Read(0));
        chip.WriteBus(true, false, 0, 0x3a);
        chip.WriteBus(true, false, 1, 0xff);
        for (var tick = 0; tick < 20; tick++) chip.Tick();
        chip.SetReset(false);
        chip.Tick();
        Assert.Equal(0, chip.Read(0));
        Assert.Equal(0xc0, Special(chip, 0x17));
    }

    [Fact]
    public void ResultReadClearsResultFullAndCompletionInterrupt()
    {
        var chip = new Intel8271();
        chip.SetReady(0, true);
        chip.SetTrackZero(0, true);
        chip.Write(0, 0x35);
        foreach (var value in new byte[] { 0x0d, 1, 0, 0x10 }) chip.Write(1, value);
        chip.Write(0, 0x69);
        chip.Write(1, 0);
        chip.Tick();
        Assert.True(chip.Nmi);
        chip.SetBus(true, true, false, false, 1);
        Assert.Equal<byte?>(0, chip.BusData);
        Assert.False(chip.Nmi);
        Assert.Equal(0, chip.Read(0));
        chip.SetBus(false, false, false, false, 0);
        chip.Reset();
    }

    private static byte Special(Intel8271 chip, byte register)
    {
        chip.Write(0, 0x3d);
        chip.Write(1, register);
        return chip.Read(1);
    }

}
