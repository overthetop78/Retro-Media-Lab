using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class FieldTransferTests
{
    [Theory]
    [InlineData(0x52, false, 128, 0)]
    [InlineData(0x52, true, 0, 0x20)]
    [InlineData(0x56, true, 128, 0x20)]
    [InlineData(0x5e, true, 0, 0x20)]
    public void DeletedMarksDistinguishReadDataFromReadIncludingDeletedAndVerify(byte command, bool deleted, int bytes, byte result)
    {
        using var disk = CreateDisk();
        disk.Sectors[0] = disk.Sectors[0] with { DeletedDataMark = deleted };
        using var chip = Controller(disk);
        Command(chip, command, 0, 1);
        var transfer = Run(chip);
        Assert.Equal(result, transfer.Result);
        Assert.Equal(bytes, transfer.Bytes.Length);
    }

    [Theory]
    [InlineData(0x4a, false)]
    [InlineData(0x4e, true)]
    public void WriteCommandsSendAnExplicitDataMark(byte command, bool deleted)
    {
        using var disk = CreateDisk();
        using var chip = Controller(disk);
        Command(chip, command, 0, 1);
        Assert.Equal(0, Run(chip, index => (byte)index).Result);
        Assert.Equal(deleted, disk.Sectors[0].DeletedDataMark);
        Assert.Equal(Enumerable.Range(0, 128).Select(index => (byte)index), disk.Sectors[0].Data.ToArray());
    }

    [Fact]
    public void BadHeaderStopsBeforeDataButBadDataStopsAfterTheFailingSector()
    {
        using var disk = CreateDisk();
        disk.Sectors[0] = disk.Sectors[0] with { HeaderIntegrityValid = false };
        using var chip = Controller(disk);
        Command(chip, 0x53, 0, 1, 2);
        var header = Run(chip);
        Assert.Equal(0x0c, header.Result);
        Assert.Empty(header.Bytes);
        disk.Sectors[0] = disk.Sectors[0] with { HeaderIntegrityValid = true, DataIntegrityValid = false };
        chip.MediaChanged();
        Command(chip, 0x53, 0, 1, 2);
        var data = Run(chip);
        Assert.Equal(0x0e, data.Result);
        Assert.Equal(128, data.Bytes.Length);
        Assert.Equal(1, Special(chip, 6));
    }

    [Fact]
    public void RecordedFcsIsCheckedEvenWithoutAnIntegrityFlag()
    {
        using var disk = CreateDisk();
        disk.Sectors[0] = disk.Sectors[0] with { RecordedErrorCheckCode = new byte[] { 0, 0 } };
        using var chip = Controller(disk);
        Command(chip, 0x52, 0, 1);
        Assert.Equal(0x0e, Run(chip).Result);
        var crc = Fcs(disk.Sectors[0].Data.Span);
        disk.Sectors[0] = disk.Sectors[0] with { RecordedErrorCheckCode = new byte[] { (byte)(crc >> 8), (byte)crc } };
        chip.MediaChanged();
        Command(chip, 0x52, 0, 1);
        Assert.Equal(0, Run(chip).Result);
    }

    [Fact]
    public void ReadIdPreservesPhysicalOrderDuplicateNumbersAndRecordedSize()
    {
        using var disk = CreateDisk();
        disk.Sectors[0] = disk.Sectors[0] with { Number = 7, RecordedSizeCode = 3 };
        disk.Sectors[1] = disk.Sectors[1] with { Number = 2, RecordedSizeCode = 1 };
        disk.Sectors.Add(new EmulationDiskSector(2, new byte[128], 0, 0, 7) { RecordedSizeCode = 0 });
        using var chip = Controller(disk);
        Command(chip, 0x5b, 0, 0, 3);
        Assert.Equal(new byte[] { 0, 0, 7, 3, 0, 0, 2, 1, 0, 0, 7, 0 }, Run(chip).Bytes);
    }

    [Theory]
    [InlineData(0, 1, 2, 2)]
    [InlineData(0, 0xff, 2, 2)]
    [InlineData(0x40, 1, 1, 4)]
    [InlineData(0x40, 1, 2, 2)]
    [InlineData(0x80, 1, 3, 4)]
    public void ScanChecksFixedKeysAndIntelResultCodes(byte type, byte key0, byte key1, byte result)
    {
        using var disk = CreateDisk();
        using var chip = Controller(disk, dma: true);
        Command(chip, 0x40, 0, 1, 1, (byte)(type | 1), 2);
        var key = new[] { key0, key1 };
        Assert.Equal(result, Run(chip, index => key[index % key.Length]).Result);
        Assert.Equal(1, Special(chip, 6));
        Assert.Equal(0, Special(chip, 0x14));
        Assert.Equal(126, Special(chip, 0x13));
    }

    [Fact]
    public void ScanContinuesIntoTheNextSectorAndNonMatchingKeysDoNotSlide()
    {
        using var disk = CreateDisk();
        using var chip = Controller(disk, dma: true);
        Command(chip, 0x40, 0, 1, 2, 1, 2);
        var key = new byte[] { 0xaa, 0x55 };
        Assert.Equal(2, Run(chip, index => key[index % 2]).Result);
        Assert.Equal(2, Special(chip, 6));
        Command(chip, 0x40, 0, 1, 1, 1, 2);
        key = [2, 3];
        Assert.Equal(0, Run(chip, index => key[index % 2]).Result);
    }

    [Fact]
    public void ReadDataSkipsDeletedSectorButRetainsItInTheCommandCount()
    {
        using var disk = CreateDisk();
        disk.Sectors[0] = disk.Sectors[0] with { DeletedDataMark = true };
        using var chip = Controller(disk);
        Command(chip, 0x53, 0, 1, 2);
        var result = Run(chip);
        Assert.Equal(0x20, result.Result);
        Assert.Equal(disk.Sectors[1].Data.ToArray(), result.Bytes);
    }

    private static Intel8271DriveFixture Controller(MemoryTrackDisk disk, bool dma = false)
    {
        var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        Command(chip, 0x3a, 0x17, dma ? (byte)0 : (byte)1);
        return chip;
    }

    private static byte Special(Intel8271DriveFixture chip, byte address)
    {
        Command(chip, 0x3d, address);
        return chip.Read(1);
    }

    private static void Command(Intel8271DriveFixture chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var parameter in parameters) chip.Write(1, parameter);
    }

    private static (byte Result, byte[] Bytes) Run(Intel8271DriveFixture chip, Func<int, byte>? source = null)
    {
        var bytes = new List<byte>();
        var written = 0;
        for (var tick = 0; tick < 1_000_000; tick++)
        {
            chip.Tick();
            var status = chip.Read(0);
            if (chip.Drq || (status & 4) != 0)
            {
                if (source is null) bytes.Add(chip.Read(4));
                else chip.Write(4, source(written++));
            }
            if ((status & 0x90) == 0x10) return (chip.Read(1), bytes.ToArray());
        }
        Assert.Fail("8271 command did not reach its result phase.");
        return default;
    }

    private static ushort Fcs(ReadOnlySpan<byte> bytes)
    {
        // Indépendant du helper de production, CRC-16/IBM-3740 avec le data mark FB.
        ushort crc = 0xffff;
        var input = new byte[bytes.Length + 1];
        input[0] = 0xfb;
        bytes.CopyTo(input.AsSpan(1));
        foreach (var value in input)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private static MemoryTrackDisk CreateDisk()
    {
        var disk = new MemoryTrackDisk();
        byte[] Data(byte first, byte second)
        {
            var data = new byte[128];
            data[0] = first;
            data[1] = second;
            return data;
        }
        disk.Sectors.Clear();
        disk.Sectors.Add(new(0, Data(1, 2), 0, 0, 1));
        disk.Sectors.Add(new(1, Data(0xaa, 0x55), 0, 0, 2));
        return disk;
    }
}
