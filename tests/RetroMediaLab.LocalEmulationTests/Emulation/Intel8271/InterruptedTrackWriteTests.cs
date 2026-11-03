using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class InterruptedTrackWriteTests
{
    private const int MaximumTicks = 800_000;
    private const int SectorLength = 128;

    public static TheoryData<int, int> BitBoundaries
    {
        get
        {
            var values = new TheoryData<int, int>();
            foreach (var clock in new[] { 2_000_000, 4_000_000 })
                for (var bits = 1; bits <= 8; bits++) values.Add(clock, bits);
            return values;
        }
    }

    [Theory]
    [MemberData(nameof(BitBoundaries))]
    public void ResetPreservesOnlyTheDataBitsAlreadyEmitted(int clock, int bits)
    {
        using var disk = CreateDisk();
        using var chip = Writing(disk, clock);
        AwaitRequest(chip);
        chip.Write(4, 0);
        AwaitRequest(chip); // First payload byte has just been loaded, not emitted.
        var payloadClock = chip.ElapsedNanoseconds;
        chip.Write(4, 0x77); // Acknowledged ahead of time; this is not yet on the medium.
        var halfBitTicks = clock == 2_000_000 ? 8 : 4;
        for (var tick = 0; tick < (bits * 2 - 1) * halfBitTicks; tick++) chip.Tick();
        chip.SetReset(true);

        var halfBitNanoseconds = (uint)halfBitTicks * 500u;
        for (var bit = 0; bit < bits; bit++)
        {
            var clockOffset = checked((uint)(payloadClock - disk.LastWriteStart)) + (uint)bit * 2u * halfBitNanoseconds;
            Assert.Contains(clockOffset, disk.LastWritePulses);
            Assert.DoesNotContain(clockOffset + halfBitNanoseconds, disk.LastWritePulses);
        }
        AssertPhysicalWindow(disk);
        Assert.All(disk.Sectors[0].Data.ToArray()[1..], value => Assert.Equal(0xff, value));
        Assert.All(disk.Sectors[1].Data.ToArray(), value => Assert.Equal(0x5a, value));
        Assert.False(disk.Sectors[0].DataIntegrityValid);
        // Les CRC physiques sont hors de la porte, donc contrôlés par AssertPhysicalWindow.
        // Un raccordement déphasé ne garantit pas le même décodage des anciens octets.
        Assert.Equal(1, disk.WindowWrites);
        Assert.False(chip.Drq);
        Assert.False(chip.Chip.WriteData);
        chip.SetReset(true);
        Assert.Equal(1, disk.WindowWrites);
    }

    [Fact]
    public void LateDmaKeepsThePreviousByteButNeverWritesTheMissingByte()
    {
        using var disk = CreateDisk();
        using var chip = Writing(disk);
        AwaitRequest(chip);
        chip.Write(4, 0x12);
        AwaitRequest(chip);
        for (var tick = 0; tick < 128; tick++) chip.Tick();
        Assert.Equal(0x0a, chip.Read(1));
        Assert.Equal(0x12, disk.Sectors[0].Data.Span[0]);
        Assert.All(disk.Sectors[0].Data.ToArray()[1..], value => Assert.Equal(0xff, value));
        Assert.False(disk.Sectors[0].DataIntegrityValid);
        Assert.Equal(OriginalCrc(), disk.Sectors[0].RecordedErrorCheckCode.ToArray());
    }

    [Fact]
    public void ResetBeforeTheFirstDataMarkBitPreservesPayloadButKeepsTheWrittenSync()
    {
        using var disk = CreateDisk();
        using var chip = Writing(disk);
        AwaitRequest(chip);
        chip.Write(4, 0);
        chip.Reset();
        Assert.Equal(1, disk.WindowWrites);
        AssertPhysicalWindow(disk);
        Assert.All(disk.Sectors[0].Data.ToArray(), value => Assert.Equal(0xff, value));
    }



    [Fact]
    public void InterruptedDataIsReadBackWithTheOriginalMismatchingCrc()
    {
        using var disk = CreateDisk();
        using var chip = Writing(disk);
        AwaitRequest(chip);
        chip.Write(4, 0x12);
        AwaitRequest(chip);
        for (var tick = 0; tick < 128; tick++) chip.Tick();
        Assert.Equal(0x0a, chip.Read(1));
        Command(chip, 0x52, 0, 1);
        var bytes = new List<byte>();
        byte? result = null;
        for (var tick = 0; tick < MaximumTicks; tick++)
        {
            chip.Tick();
            if (chip.Drq) bytes.Add(chip.Read(4));
            if ((chip.Read(0) & 0x90) != 0x10) continue;
            result = chip.Read(1);
            break;
        }
        Assert.Equal<byte?>(0x0e, result);
        Assert.Equal(SectorLength, bytes.Count);
        Assert.Equal(0x12, bytes[0]);
        Assert.All(bytes.Skip(1), value => Assert.Equal(0xff, value));
    }

    [Fact]
    public void FormatResetBeforeIndexLeavesTheTrackUntouched()
    {
        using var disk = CreateDisk();
        using var chip = Formatting(disk, 3);
        for (var tick = 0; tick < 1_000; tick++) chip.Tick();
        chip.Reset();
        Assert.Equal(0, disk.WindowWrites);
        Assert.Equal(new[] { 1, 2 }, disk.Sectors.Select(sector => sector.Number));
    }

    [Fact]
    public void FormatResetKeepsTheCompletedNewFieldAndTheOldFieldNotReachedYet()
    {
        using var disk = CreateDisk();
        using var chip = Formatting(disk, 3);
        Header(chip, 9);
        AwaitRequest(chip); // First word of the next ID mark; the old second ID is further ahead.
        chip.Reset();
        Assert.Equal(new[] { 9, 2 }, disk.Sectors.Select(sector => sector.Number));
        Assert.True(disk.Sectors[0].HeaderIntegrityValid);
        Assert.True(disk.Sectors[0].DataIntegrityValid);
        Assert.All(disk.Sectors[0].Data.ToArray(), value => Assert.Equal(0xe5, value));
        Assert.All(disk.Sectors[1].Data.ToArray(), value => Assert.Equal(0x5a, value));
    }

    [Fact]
    public void FormatResetDuringHeaderPreservesActualBitsAndSignalsBadHeaderCrc()
    {
        using var disk = CreateDisk();
        using var chip = Formatting(disk, 1);
        AwaitRequest(chip);
        chip.Write(4, 0);
        AwaitRequest(chip); // The C byte has been queued, H is requested ahead of time.
        for (var tick = 0; tick < 120; tick++) chip.Tick();
        chip.Reset();
        var partial = disk.Sectors[0];
        Assert.Equal(0, partial.Cylinder);
        Assert.Equal(0xff, partial.Head);
        Assert.Equal(0xff, partial.Number);
        Assert.Equal<byte?>(0xff, partial.RecordedSizeCode);
        Assert.False(partial.HeaderIntegrityValid);
        Assert.Equal(new[] { 0xff, 1, 2 }, disk.Sectors.Select(sector => sector.Number));
    }

    [Fact]
    public void FormatResetDuringPayloadKeepsOnlyTheEmittedFillBytes()
    {
        using var disk = CreateDisk();
        using var chip = Formatting(disk, 1);
        Header(chip, 9);
        // At the last CPU request R has just been queued. N, CRC, gaps, sync and DAM precede DATA.
        for (var tick = 0; tick < (22 + 4) * 128; tick++) chip.Tick();
        chip.Reset();
        Assert.Equal(new[] { 9, 2 }, disk.Sectors.Select(sector => sector.Number));
        var partial = disk.Sectors[0];
        Assert.True(partial.HeaderIntegrityValid);
        Assert.False(partial.DataIntegrityValid);
        Assert.All(partial.Data.ToArray()[..4], value => Assert.Equal(0xe5, value));
        Assert.Equal(0xff, partial.Data.Span[4]);
        Assert.All(disk.Sectors[1].Data.ToArray(), value => Assert.Equal(0x5a, value));
    }

    [Fact]
    public void FormatLateDmaAfterTheMarkDoesNotRestoreTheOldTrack()
    {
        using var disk = CreateDisk();
        using var chip = Formatting(disk, 1);
        AwaitRequest(chip);
        for (var tick = 0; tick < 128; tick++) chip.Tick();
        Assert.Equal(0x0a, chip.Read(1));
        Assert.Equal(1, disk.WindowWrites);
        Assert.False(disk.Sectors[0].HeaderIntegrityValid);
        Assert.Equal(new[] { 0xff, 1, 2 }, disk.Sectors.Select(sector => sector.Number));
    }

    private static Intel8271DriveFixture Formatting(MemoryTrackDisk disk, byte count)
    {
        var chip = new Intel8271DriveFixture(disk);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        Command(chip, 0x63, 0, 11, count, 0, 0);
        return chip;
    }

    private static void Header(Intel8271DriveFixture chip, byte number)
    {
        foreach (var value in new byte[] { 0, 0, number, 0 })
        {
            AwaitRequest(chip);
            chip.Write(4, value);
        }
    }

    private static Intel8271DriveFixture Writing(MemoryTrackDisk disk, int clock = 2_000_000)
    {
        var chip = new Intel8271DriveFixture(disk, clock);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        Command(chip, 0x4a, 0, 1);
        return chip;
    }

    private static void Command(Intel8271DriveFixture chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
    }

    private static void AwaitRequest(Intel8271DriveFixture chip)
    {
        for (var tick = 0; tick < MaximumTicks; tick++)
        {
            chip.Tick();
            if (chip.Drq) return;
        }
        Assert.Fail("No write request before two revolutions.");
    }

    private static byte[] OriginalCrc()
    {
        ushort crc = 0xffff;
        foreach (var value in new[] { (byte)0xfb }.Concat(Enumerable.Repeat((byte)0xff, SectorLength)))
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? crc << 1 ^ 0x1021 : crc << 1);
        }
        return [(byte)(crc >> 8), (byte)crc];
    }

    private static MemoryTrackDisk CreateDisk()
    {
        var disk = new MemoryTrackDisk(fill: 0xff);
        disk.Sectors.Add(new(1, Enumerable.Repeat((byte)0x5a, SectorLength).ToArray(), 0, 0, 2));
        return disk;
    }

    private static void AssertPhysicalWindow(MemoryTrackDisk disk)
    {
        Assert.NotNull(disk.BeforeWrite);
        Assert.NotNull(disk.AfterWrite);
        uint[] Positions(IReadOnlyList<uint> intervals)
        {
            uint position = 0;
            return intervals.Select(interval => position += interval).ToArray();
        }
        var before = Positions(disk.BeforeWrite.FluxIntervalsNanoseconds);
        var after = Positions(disk.AfterWrite.FluxIntervalsNanoseconds);
        var start = disk.LastWriteStart;
        var end = start + disk.LastWriteDuration;
        Assert.Equal(before.Where(position => position < start || position > end),
            after.Where(position => position < start || position > end));
        Assert.Equal(disk.LastWritePulses.Select(offset => start + offset),
            after.Where(position => position >= start && position <= end));
    }
}
