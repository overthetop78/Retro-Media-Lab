using RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;
using System.Buffers.Binary;
namespace RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463;

public sealed partial class Hd63463Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormatCollectsBothBuffersOrDoneBeforeWritingTheSelectedTrack(bool done)
    {
        using var disk = new Disk();
        var otherHead = disk.Record(0);
        var controller = FormatController(disk);
        Issue(controller, 0xa3, [1, 1, 0, 4]);
        controller.Advance(5000);
        Assert.True(controller.Drq);
        var identifiers = FormatIdentifiers(0, 1, 4);
        var length = done ? 16 : identifiers.Length;
        for (var offset = 0; offset < length; offset += 2)
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset)), done && offset == length - 2);
        Assert.True(controller.Busy);
        Assert.False(controller.Drq);
        Assert.False(disk.HasChanges);
        controller.Advance(100000);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(new byte[] { 0, 0, 1, 1, 0, 0 }, Results(controller, 6));
        Assert.Equal(4, controller.WrittenBlocks(0));
        Assert.Equal(otherHead, disk.Record(0));
        for (var record = 4; record < 8; record++)
            Assert.All(disk.Record(record), value => Assert.Equal(0x4e, value));
        Assert.False(disk.Disposed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void PioFormatUsesThePreparedIdsAndFillsTheWholeRecord(byte length)
    {
        var recordBytes = 256 << (length - 1);
        using var disk = new Disk(recordSize: recordBytes);
        var controller = FormatController(disk, length, dma: false);
        var identifiers = FormatIdentifiers(0, 0, 4);
        Issue(controller, 0x38, [0, 0]);
        controller.Advance(13);
        for (var offset = 0; offset < 256; offset += 2)
            controller.WriteParameter(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset)));
        Issue(controller, 0xa3, [1, 0, 0, 4]);
        controller.Advance(100000);
        Assert.False(controller.Drq);
        Assert.Equal(0x60, controller.Status);
        for (var record = 0; record < 4; record++)
            Assert.All(disk.Record(record), value => Assert.Equal(0x4e, value));
        Assert.Equal(4, controller.WrittenBlocks(0));
    }

    [Fact]
    public void FormatDmaUsesTheSecondIdentifierBufferAndValidatesItBeforeWriting()
    {
        using var disk = new Disk(sectorsPerTrack: 70);
        var controller = FormatController(disk);
        var identifiers = FormatIdentifiers(0, 0, 70);
        identifiers[69 * 4 + 2] = 1; // The final ID in DBUF1 cannot describe this physical head.
        SendFormatIds(controller, identifiers, 70);
        Assert.Equal(0x44, controller.ReadParameter());
        Assert.False(disk.HasChanges);
        SendFormatIds(controller, FormatIdentifiers(0, 0, 70), 70);
        controller.Advance(200000);
        Assert.Equal(0x60, controller.Status);
        Assert.Equal(70, controller.WrittenBlocks(0));
        Assert.All(disk.Record(69), value => Assert.Equal(0x4e, value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FormatNeverSilentlyLosesNonstandardIdsInAFlatImage(int idByte)
    {
        using var disk = new Disk();
        var controller = FormatController(disk);
        var identifiers = FormatIdentifiers(0, 0, 4);
        identifiers[idByte] = 7;
        SendFormatIds(controller, identifiers, 4);
        Assert.Equal(0x64, controller.Status);
        Assert.Equal(0x44, controller.ReadParameter());
        Assert.False(disk.HasChanges);
        Assert.Equal(0, controller.WrittenBlocks(0));
    }

    [Theory]
    [InlineData(3, 0x44)]
    [InlineData(129, 0x0c)]
    public void FormatRejectsUnrepresentableGeometryBeforeAskingForIds(int count, byte error)
    {
        using var disk = new Disk();
        var controller = FormatController(disk);
        Issue(controller, 0xa3, [1, 0, (byte)(count >> 8), (byte)count]);
        Assert.False(controller.Drq);
        Assert.False(controller.Busy);
        Assert.Equal(error, controller.ReadParameter());
        Assert.False(disk.HasChanges);
    }

    [Fact]
    public void ProtectedFormatDoesNotRequestOrWriteAnyData()
    {
        using var disk = new Disk { IsReadOnly = true };
        var controller = FormatController(disk);
        Issue(controller, 0xa3, [1, 0, 0, 4]);
        Assert.Equal(0x64, controller.ReadParameter());
        Assert.False(controller.Drq);
        Assert.False(disk.HasChanges);
    }

    [Fact]
    public void StandaloneDoneStartsFormattingAndAbortStopsAfterTheWrittenSector()
    {
        using var disk = new Disk();
        var untouched = disk.Record(1);
        var controller = FormatController(disk);
        Issue(controller, 0xa3, [1, 0, 0, 4]);
        controller.Advance(5000);
        var identifiers = FormatIdentifiers(0, 0, 4);
        for (var offset = 0; offset < 16; offset += 2)
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset)));
        controller.DmaDone();
        Assert.False(controller.Drq);
        for (var tick = 0; tick < 40000 && controller.WrittenBlocks(0) == 0; tick++) controller.Advance(1);
        Assert.Equal(1, controller.WrittenBlocks(0));
        controller.WriteCommand(0xf0);
        controller.Advance(100000);
        Assert.Equal(4, controller.ReadParameter());
        Assert.Equal(untouched, disk.Record(1));
        Assert.False(disk.Disposed);
    }

    private static Hd63463 FormatController(Disk disk, byte length = 1, bool dma = true)
    {
        var controller = Controller(_ => disk, mode: dma ? (byte)0x80 : (byte)0, recordLength: length);
        var records = disk.SectorsPerTrack * disk.SectorSize / (256 << (length - 1));
        Issue(controller, 0xe8, [length == 1 ? (byte)0x1f : (byte)0x1e, dma ? (byte)0x82 : (byte)2,
            0, 6, 16, 2, 1, (byte)(records - 1), length, 10, 13, 12, 0, 0, 0, 0]);
        var rpm = 3600;
        while ((long)records * ((256 << (length - 1)) + 128) + 64 >= 625000L * 60 / rpm) rpm /= 2;
        controller.Configure(0, 0, 100, rotationsPerMinute: rpm);
        return controller;
    }

    private static byte[] FormatIdentifiers(int cylinder, int head, int count)
    {
        var identifiers = new byte[512];
        for (var sector = 0; sector < count; sector++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(identifiers.AsSpan(sector * 4), (ushort)cylinder);
            identifiers[sector * 4 + 2] = (byte)head;
            identifiers[sector * 4 + 3] = (byte)sector;
        }
        return identifiers;
    }

    private static void SendFormatIds(Hd63463 controller, byte[] identifiers, byte count)
    {
        Issue(controller, 0xa3, [1, 0, 0, count]);
        controller.Advance(5000);
        Assert.True(controller.Drq);
        foreach (var offset in Enumerable.Range(0, identifiers.Length / 2))
            controller.WriteDma(BinaryPrimitives.ReadUInt16LittleEndian(identifiers.AsSpan(offset * 2)));
    }
}
