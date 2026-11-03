namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class BusTransferTests
{
    [Fact]
    public void HeldDackWriteAcknowledgesOnReleaseWithoutChipSelect()
    {
        using var disk = new MemoryTrackDisk(fill: 0x5a);
        using var chip = Controller(disk, true);
        AwaitData(chip);
        chip.Chip.SetBus(false, false, true, true, 0, 0x5a);
        for (var tick = 0; tick < 8; tick++) chip.Tick();
        Assert.True(chip.Drq);
        chip.Chip.SetBus(false, false, false, true, 0, 0x5a);
        Assert.False(chip.Drq);
        Assert.Equal(1, chip.Chip.WrittenBytes(0));
        chip.Chip.Reset();
    }

    [Fact]
    public void DackReadOverridesControlAddressAndHasOnlyOneAcknowledgementPerPulse()
    {
        using var disk = new MemoryTrackDisk(fill: 0x5a);
        using var chip = Controller(disk, false);
        AwaitData(chip);
        chip.Chip.SetBus(true, true, false, true, 1);
        Assert.Equal<byte?>(0x5a, chip.Chip.BusData);
        Assert.False(chip.Drq);
        Assert.Equal(1, chip.Chip.ReadBytes(0));
        chip.Chip.SetBus(true, true, false, true, 1);
        Assert.Equal(1, chip.Chip.ReadBytes(0));
        chip.Chip.SetBus(false, false, false, false, 0);
        Assert.Null(chip.Chip.BusData);
        chip.Chip.Reset();
    }

    private static Intel8271DriveFixture Controller(MemoryTrackDisk disk, bool write = false)
    {
        var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        Command(chip, write ? (byte)0x4a : (byte)0x52, 0, 1);
        return chip;
    }

    private static void Command(Intel8271DriveFixture chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
    }

    private static void AwaitData(Intel8271DriveFixture chip)
    {
        for (var tick = 0; tick < 400_000; tick++)
        {
            chip.Tick();
            if (chip.Drq || (chip.Read(0) & 4) != 0) return;
        }
        Assert.Fail("No data request within one revolution.");
    }

}
