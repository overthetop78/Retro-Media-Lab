namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class DriveInputTests
{
    [Fact]
    public void ReadyLossIsLatchedUntilReadDriveStatusNotReadInputPort()
    {
        using var disk = new MemoryTrackDisk(fill: 0);
        using var chip = Controller(disk);
        Command(chip, 0x52, 0, 1);
        AwaitData(chip);
        chip.Chip.SetReady(0, false);
        Assert.Equal(0x10, chip.Read(1));
        Assert.False(chip.Drq);
        chip.Chip.SetReady(0, true);
        Assert.NotEqual(0, Special(chip, 0x22) & 4);
        Command(chip, 0x69, 0);
        Assert.Equal(0x10, chip.Read(1));
        Command(chip, 0x6c);
        Assert.NotEqual(0, chip.Read(1) & 4);
        Command(chip, 0x69, 0);
        Assert.Equal(0, Finish(chip));
    }

    [Fact]
    public void OtherDriveReadyDoesNotAbortTheSelectedDrive()
    {
        using var disk = new MemoryTrackDisk(fill: 0);
        using var chip = Controller(disk);
        Command(chip, 0x52, 0, 1);
        AwaitData(chip);
        chip.Chip.SetReady(1, false);
        Assert.True(chip.Drq);
        Assert.False(chip.Nmi);
    }

    [Fact]
    public void WriteProtectCanChangeDuringTheDataTransfer()
    {
        using var disk = new MemoryTrackDisk(fill: 0);
        using var chip = Controller(disk);
        Command(chip, 0x4a, 0, 1);
        AwaitData(chip);
        Assert.NotEqual(0, chip.DriveControl & 1);
        chip.Chip.SetWriteProtected(0, true);
        Assert.Equal(0x12, chip.Read(1));
        Assert.False(chip.Drq);
        Assert.Equal(0, chip.DriveControl & 1);
    }

    private static Intel8271DriveFixture Controller(MemoryTrackDisk disk)
    {
        var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
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

    private static byte Special(Intel8271DriveFixture chip, byte register)
    {
        Command(chip, 0x3d, register);
        return chip.Read(1);
    }

    private static byte Finish(Intel8271DriveFixture chip)
    {
        for (var tick = 0; tick < 400_000; tick++)
        {
            chip.Tick();
            if ((chip.Read(0) & 0x90) == 0x10) return chip.Read(1);
        }
        Assert.Fail("No result within one revolution.");
        return byte.MaxValue;
    }
}
