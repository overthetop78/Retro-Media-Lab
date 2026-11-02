using System.Buffers.Binary;
using Codes = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Functions.Hd63463ErrorCodeFunctions;

namespace RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463;

public sealed partial class Hd63463Tests
{
    [Fact]
    public void ErrorCodesMatchACapturedHd63463FormatOnARealDrive()
    {
        // timw, 3 June 2026, raw logic-analyzer capture and decoded first
        // sector: https://stardot.org.uk/forums/viewtopic.php?start=30&t=33019
        Assert.Equal(0x84c0, Codes.Crc([], new byte[4], 0x1021, 0xffff));
        Assert.Equal(0xbcc1e785u, Codes.Fire([0xf8], Enumerable.Repeat((byte)0x4e, 256).ToArray()));
    }

    [Fact]
    public void RecordedErrorCodesMatchIndependentPolynomialDivisionVectors()
    {
        // Remainders obtained by division of the whole input polynomial,
        // independent of the byte-at-a-time implementation used by the chip.
        ReadOnlySpan<byte> data = "123456789"u8;
        Assert.Equal(0x51693c0cu, Codes.Fire([], data));
        Assert.Equal(0x29b1, Codes.Crc([], data, 0x1021, 0xffff));
        Assert.Equal(0x0839, Codes.Crc([], data, 0x0001, 0));
    }

    [Theory]
    [InlineData(256, 0, 0x800000u)]
    [InlineData(256, 13, 0x03ff80u)]
    [InlineData(256, 253, 0x000001u)]
    [InlineData(1024, 700, 0x01ff00u)]
    [InlineData(4096, 4093, 0x03ff80u)]
    public void FireLocatesBurstsAtTheBeginningMiddleAndEndOfTheData(int bytes, int position, uint error)
    {
        var corrupt = new byte[bytes];
        ApplyPattern(corrupt, position, error);
        var syndrome = Codes.Fire([], corrupt);
        Assert.True(Codes.Locate(syndrome, bytes, out var address, out var pattern));
        ApplyPattern(corrupt, address, pattern);
        Assert.All(corrupt, value => Assert.Equal(0, value));
    }

    [Fact]
    public void AutomaticEccCorrectsBeforeDmaAndEndsAbnormallyAtTheAffectedSector()
    {
        using var disk = new Disk { ReadTransform = sector => RecordedFire(sector, 13, 0x03ff80) };
        var controller = Controller(_ => disk, mode: 0x82);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 2));
        Assert.Equal(disk.Block(0), ReadClocked(controller));
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(new byte[] { 0, 0x48, 1, 0, 0, 0, 0, 0, 0, 2 }, Results(controller, 10));
        Assert.False(disk.HasChanges); // Correction belongs to controller RAM, not the source image.
        Assert.Equal(1, controller.ReadBlocks(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HostEccReceivesErroneousDataAndCkeSurvivesRecallAndBufferAccess(bool erroneousRead)
    {
        using var disk = new Disk { ReadTransform = sector => RecordedFire(sector, 17, 0x03ff80) };
        var controller = Controller(_ => disk, mode: 0x82, operationMode: erroneousRead ? (byte)0x1f : (byte)0x1e);
        Issue(controller, erroneousRead ? (byte)0x70 : (byte)0x40,
            erroneousRead ? [1, 0, 0, 1] : Transfer(1, 0, 0, 0, 1));
        var received = ReadClocked(controller);
        var expected = disk.Block(0);
        Assert.NotEqual(expected, received);
        Assert.Equal(0x40, controller.ReadParameter());
        Assert.Equal(received, ReadBuffer(controller, 0));
        Issue(controller, 0x20, []);
        var result = Results(controller, 8);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(0, result[0]);
        Assert.Equal(0, result[1]);
        var address = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(2));
        for (var index = 0; index < 3; index++) received[address + index] ^= result[4 + index];
        Assert.Equal(expected, received);
    }

    [Fact]
    public void LongDataCanBeCorrectedByTheHostWithoutEnlargingTheTwoBuffers()
    {
        using var disk = new Disk(sectorSize: 1024, recordSize: 1024)
        { ReadTransform = sector => RecordedFire(sector, 700, 0x01ff00) };
        var controller = Controller(_ => disk, mode: 0x82, recordLength: 3);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        var received = ReadClocked(controller);
        Assert.Equal(1024, received.Length);
        Assert.Equal(0x40, controller.ReadParameter());
        Issue(controller, 0x20, []);
        var result = Results(controller, 8);
        var address = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(2));
        for (var index = 0; index < 3; index++) received[address + index] ^= result[4 + index];
        Assert.Equal(disk.Record(0), received);
    }

    [Fact]
    public void AutomaticUncorrectableEccDoesNotTransferTheDamagedSector()
    {
        using var disk = new Disk { ReadTransform = UncorrectableFire };
        var controller = Controller(_ => disk, mode: 0x82);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        Assert.Empty(ReadClocked(controller));
        Assert.Equal(0x4c, controller.ReadParameter());
        Assert.False(controller.Drq);
    }

    [Fact]
    public void HostProvisionalEccStatusCanBeFollowedByAnUncorrectableCkeResult()
    {
        using var disk = new Disk { ReadTransform = UncorrectableFire };
        var controller = Controller(_ => disk, mode: 0x82, operationMode: 0x1e);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        Assert.Equal(256, ReadClocked(controller).Length);
        Assert.Equal(0x40, controller.ReadParameter());
        Issue(controller, 0x20, []);
        Assert.Equal(0x4c, controller.ReadParameter());
        controller.Reset();
        Issue(controller, 0x20, []);
        Assert.Equal(0x4c, controller.ReadParameter());
    }

    [Fact]
    public void PioAutomaticCorrectionKeepsTheCorrectedDataInTheSelectedBuffer()
    {
        using var disk = new Disk { ReadTransform = sector => RecordedFire(sector, 253, 0x03ff80) };
        var controller = Controller(_ => disk, mode: 0x02);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        controller.Advance(100000);
        Assert.Equal(0x48, controller.ReadParameter());
        Assert.False(controller.Drq);
        Assert.Equal(disk.Block(0), ReadBuffer(controller, 0));
    }

    [Fact]
    public void EccDuringCheckDataDoesNotReplaceTheBufferContents()
    {
        using var disk = new Disk { ReadTransform = sector => RecordedFire(sector, 17, 0x03ff80) };
        var controller = Controller(_ => disk, mode: 0x82);
        Issue(controller, 0x38, [0, 0]);
        controller.Advance(10000);
        controller.WriteParameter(0x1937);
        Issue(controller, 0x48, Transfer(1, 0, 0, 0, 1));
        controller.Advance(100000);
        Assert.Equal(0x48, controller.ReadParameter());
        Assert.Equal(0x1937, BinaryPrimitives.ReadUInt16LittleEndian(ReadBuffer(controller, 0)));
    }

    [Fact]
    public void CompareReportsRecordedEccInsteadOfMistakingItForADataMismatch()
    {
        using var disk = new Disk { ReadTransform = sector => RecordedFire(sector, 17, 0x03ff80) };
        var controller = Controller(_ => disk, mode: 0x82);
        Issue(controller, 0x88, Transfer(1, 0, 0, 0, 1));
        WriteClocked(controller, disk.Block(0));
        Assert.Equal(0x48, controller.ReadParameter());
        Assert.False(disk.HasChanges);
    }

    [Theory]
    [InlineData(0x10)]
    [InlineData(0x12)]
    [InlineData(0x14)]
    [InlineData(0x16)]
    public void CrcModeUsesTheSelectedPolynomialAndInitialValue(byte operation)
    {
        using var disk = new Disk { ReadTransform = sector => RecordedCrc(sector, operation, excludeAddressMark: true) };
        var controller = Controller(_ => disk, mode: 0x82, operationMode: operation);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        Assert.Equal(disk.Block(0), ReadClocked(controller));
        Assert.Equal(0x60, controller.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrcScopeIncludesTheRecordedAddressMarkOnlyWhenConfigured(bool exclude)
    {
        using var disk = new Disk { ReadTransform = sector => RecordedCrc(sector, 0x16, exclude) };
        var controller = Controller(_ => disk, mode: exclude ? (byte)0x82 : (byte)0x80, operationMode: 0x16);
        Issue(controller, 0x40, Transfer(1, 0, 0, 0, 1));
        Assert.Equal(disk.Block(0), ReadClocked(controller));
        var incompatible = Controller(_ => disk, mode: exclude ? (byte)0x80 : (byte)0x82, operationMode: 0x16);
        Issue(incompatible, 0x40, Transfer(1, 0, 0, 0, 1));
        Assert.Empty(ReadClocked(incompatible));
        Assert.Equal(0x44, incompatible.ReadParameter());
    }

    private static DiskSector RecordedFire(DiskSector sector, int position, uint pattern)
    {
        var prefix = new byte[] { 0xa1, 0xfb };
        var stored = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(stored, Codes.Fire(prefix.AsSpan(1), sector.Data.Span));
        var received = sector.Data.ToArray();
        ApplyPattern(received, position, pattern);
        return sector with { Data = received, IntegrityValid = false,
            RecordedErrorCheckCode = stored, RecordedDataPrefix = prefix };
    }

    private static DiskSector UncorrectableFire(DiskSector sector)
    {
        var coded = RecordedFire(sector, 0, 0x010000);
        var received = coded.Data.ToArray();
        received[100] ^= 0x40;
        received[200] ^= 0x80;
        return coded with { Data = received };
    }

    private static DiskSector RecordedCrc(DiskSector sector, byte mode, bool excludeAddressMark)
    {
        var prefix = new byte[] { 0xa1, 0xfb };
        var polynomial = (mode & 4) != 0 ? (ushort)0x1021 : (ushort)1;
        var initial = (mode & 2) != 0 ? ushort.MaxValue : (ushort)0;
        var stored = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(stored, Codes.Crc(prefix.AsSpan(excludeAddressMark ? 1 : 0), sector.Data.Span, polynomial, initial));
        return sector with { RecordedErrorCheckCode = stored, RecordedDataPrefix = prefix };
    }

    private static void ApplyPattern(Span<byte> data, int address, uint pattern)
    {
        data[address] ^= (byte)(pattern >> 16);
        data[address + 1] ^= (byte)(pattern >> 8);
        data[address + 2] ^= (byte)pattern;
    }
}
