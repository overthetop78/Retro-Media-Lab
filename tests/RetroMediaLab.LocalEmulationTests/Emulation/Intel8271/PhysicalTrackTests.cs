namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class PhysicalTrackTests
{
    private const int RevolutionTicks = 400_000;

    [Fact]
    public void OptionalOutputHasNoIntrinsicHeadSelectionWithoutBoardWiring()
    {
        using var disk = new MemoryTrackDisk(128);
        using var chip = Controller(disk);
        Command(chip, 0x3a, 0x23, 0x20);
        Command(chip, 0x52, 0, 1);
        Assert.Equal(0, Read(chip).Result);
        Assert.Equal(0, disk.RequestedHead);
        Assert.NotEqual(0, chip.DriveControl & 0x20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SectorSizesUseTheSameFmReceiverAndCrc(byte sizeCode)
    {
        using var disk = new MemoryTrackDisk(128 << sizeCode);
        using var chip = Controller(disk);
        Command(chip, 0x53, 0, 1, (byte)((sizeCode << 5) | 1));
        var transfer = Read(chip);
        Assert.Equal(0, transfer.Result);
        Assert.Equal(disk.Sectors[0].Data.ToArray(), transfer.Data);
    }

    [Fact]
    public void StandardClockFitsA4096ByteFieldWithinOneRevolution()
    {
        using var disk = new MemoryTrackDisk(4096);
        using var chip = Controller(disk, 4_000_000);
        Command(chip, 0x53, 0, 1, 0xa1);
        var transfer = Read(chip);
        Assert.Equal(0, transfer.Result);
        Assert.Equal(disk.Sectors[0].Data.ToArray(), transfer.Data);
    }

    [Fact]
    public void OverfullTrackDoesNotInventASlowerRotationToFitTheField()
    {
        using var disk = new MemoryTrackDisk(4096);
        using var chip = Controller(disk);
        Command(chip, 0x53, 0, 1, 0xa1);
        var transfer = Read(chip);
        Assert.Equal(0x08, transfer.Result); // The next revolution's ID mark interrupts the truncated DATA field.
        Assert.InRange(transfer.Elapsed, RevolutionTicks, RevolutionTicks + 5_000);
        Assert.InRange(transfer.Data.Length, 3_000, 3_124);
    }

    [Fact]
    public void SectorSearchWaitsForTheNextPhysicalPassInsteadOfReturningImmediately()
    {
        using var disk = new MemoryTrackDisk(128);
        using var first = Controller(disk);
        Command(first, 0x3a, 0x23, 0x48);
        Command(first, 0x52, 0, 1);
        var immediate = Read(first);
        Assert.Equal(0, immediate.Result);
        Assert.InRange(immediate.FirstRequest, 5_000, 8_000);

        using var rotated = Controller(disk);
        Command(rotated, 0x3a, 0x23, 0x48);
        for (var tick = 0; tick < 8_000; tick++) rotated.Tick();
        Command(rotated, 0x52, 0, 1);
        var nextPass = Read(rotated);
        Assert.Equal(0, nextPass.Result);
        Assert.Equal(RevolutionTicks - 8_000, nextPass.FirstRequest - immediate.FirstRequest);
        Assert.Equal(immediate.Data, nextPass.Data);
    }

    [Fact]
    public void EmptyTrackSearchExpiresOnIndexWithoutFabricatingADataRequest()
    {
        using var disk = new MemoryTrackDisk(128);
        disk.Sectors.Clear();
        using var chip = Controller(disk);
        Command(chip, 0x52, 0, 1);
        var transfer = Read(chip);
        Assert.Equal(0x18, transfer.Result);
        Assert.Equal(-1, transfer.FirstRequest);
        Assert.Empty(transfer.Data);
        Assert.InRange(transfer.Elapsed, RevolutionTicks, 2 * RevolutionTicks);
    }

    [Fact]
    public void DuplicateIdWriteTargetsThePhysicalOccurrenceWhoseHeaderWasRead()
    {
        using var disk = new MemoryTrackDisk(128);
        disk.Sectors.Add(disk.Sectors[0] with { Index = 1, Data = new byte[128] });
        var untouched = disk.Sectors[0].Data.ToArray();
        using var chip = Controller(disk);
        Command(chip, 0x3a, 0x23, 0x48);
        // The first ID has passed; the duplicate still lies ahead on this rotation.
        for (var tick = 0; tick < 5_000; tick++) chip.Tick();
        Command(chip, 0x4a, 0, 1);
        var count = 0;
        byte? result = null;
        for (var tick = 0; tick < 2 * RevolutionTicks; tick++)
        {
            chip.Tick();
            if (chip.Drq) chip.Write(4, (byte)count++);
            if ((chip.Read(0) & 0x90) != 0x10) continue;
            result = chip.Read(1);
            break;
        }
        Assert.Equal<byte?>(0, result);
        Assert.Equal(128, count);
        Assert.Equal(untouched, disk.Sectors[0].Data.ToArray());
        Assert.Equal(Enumerable.Range(0, 128).Select(index => (byte)index), disk.Sectors[1].Data.ToArray());
    }

    [Fact]
    public void RecordedSizeMismatchTimesOutWithoutTransferringTheWrongLength()
    {
        using var disk = new MemoryTrackDisk(128);
        disk.Sectors[0] = disk.Sectors[0] with { RecordedSizeCode = 1 };
        using var chip = Controller(disk);
        Command(chip, 0x52, 0, 1);
        var transfer = Read(chip);
        Assert.Equal(0x18, transfer.Result);
        Assert.Empty(transfer.Data);
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(2, 0, 2)]
    [InlineData(3, 0x18, 2)]
    public void WrongCylinderRetriesTheNextTwoTracksOnly(int matchingPhysicalTrack, byte result, int steps)
    {
        using var disk = new MemoryTrackDisk(128) { HeaderCylinder = physical => physical == matchingPhysicalTrack ? 0 : 9 };
        using var chip = Controller(disk);
        chip.MechanicalSpeedPercent = 0;
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        Command(chip, 0x52, 0, 1);
        var transfer = Read(chip, 3);
        Assert.Equal(result, transfer.Result);
        Assert.Equal(steps, chip.SeekSteps(0));
        if (result == 0) Assert.Equal(disk.Sectors[0].Data.ToArray(), transfer.Data);
        else Assert.Empty(transfer.Data);
    }

    private static Intel8271DriveFixture Controller(MemoryTrackDisk disk, int chipClock = 2_000_000)
    {
        var chip = new Intel8271DriveFixture(disk, chipClock);
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        return chip;
    }

    private static void Command(Intel8271DriveFixture chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
    }

    private static (byte Result, byte[] Data, int FirstRequest, int Elapsed) Read(Intel8271DriveFixture chip, int maximumRevolutions = 2)
    {
        var data = new List<byte>();
        var firstRequest = -1;
        for (var tick = 1; tick <= maximumRevolutions * RevolutionTicks; tick++)
        {
            chip.Tick();
            if (chip.Drq)
            {
                if (firstRequest < 0) firstRequest = tick;
                data.Add(chip.Read(4));
            }
            if ((chip.Read(0) & 0x90) == 0x10) return (chip.Read(1), data.ToArray(), firstRequest, tick);
        }
        Assert.Fail("No result within the bounded search and retry interval.");
        return default;
    }

}
