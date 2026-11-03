namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class RegisterTransferTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void DataRequestsUseDmaOrNonDmaWithoutConflatingCompletion(byte mode, bool nonDma)
    {
        using var disk = new MemoryTrackDisk(fill: 0xa5);
        using var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        Command(chip, 0x3a, 0x17, mode);
        Command(chip, 0x52, 0, 1);
        AwaitData(chip);
        Assert.Equal(!nonDma, chip.Drq);
        Assert.Equal(nonDma, chip.Nmi);
        Assert.Equal(nonDma ? 0x8c : 0x80, chip.Read(0));
        Assert.Equal<byte?>(0xa5, chip.Chip.ReadBus(false, true, 0));
        Assert.False(chip.Drq);
        Assert.False(chip.Nmi);
        for (var i = 1; i < 128; i++) { AwaitData(chip); chip.Read(4); }
        for (var i = 0; i < 128; i++) chip.Tick();
        Assert.Equal(0x80, chip.Read(0)); // The high CRC byte cannot finish the field.
        for (var i = 0; i < 128; i++) chip.Tick();
        Assert.Equal(0x18, chip.Read(0));
        Assert.True(chip.Nmi);
        Assert.False(chip.Drq);
        chip.Read(4);
        Assert.True(chip.Nmi); // Reading data cannot acknowledge a completion result.
        Assert.Equal(0, chip.Read(1));
        Assert.False(chip.Nmi);
    }

    [Theory]
    [InlineData(2_000_000, 128)]
    [InlineData(4_000_000, 64)]
    public void ByteCadenceFollowsControllerClockWithoutChangingOwnerClock(int chipClock, int interval)
    {
        using var disk = new MemoryTrackDisk(fill: 0xa5);
        using var chip = new Intel8271DriveFixture(disk, chipClock);
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        Command(chip, 0x52, 0, 1);
        AwaitData(chip);
        chip.Read(4);
        for (var i = 1; i < interval; i++) { chip.Tick(); Assert.False(chip.Drq); }
        chip.Tick();
        Assert.True(chip.Drq);
    }

    [Fact]
    public void DataRegistersAreMirroredAcrossAllDackAddresses()
    {
        using var disk = new MemoryTrackDisk(fill: 0xa5);
        using var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        Command(chip, 0x52, 0, 1);
        for (var register = 4; register < 8; register++)
        {
            AwaitData(chip);
            Assert.Equal(0xa5, chip.Read(register));
            Assert.False(chip.Drq);
        }
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
