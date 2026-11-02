using RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Services;
using System.Buffers.Binary;
using System.IO;
using Disk = RetroMediaLab.Chips.Tests.Controllers.Hitachi.HDC.HD63463.Hd63463Tests.Disk;

namespace RetroMediaLab.Chips.Tests.Controllers.Generic.ATA.PIO;

public sealed class AtaTests
{
    [Fact]
    public void IdentifyReportsActualCapacityGeometryAndOnlyImplementedTransferCapabilities()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        Issue(device, 0xec);
        Assert.True(device.Irq);
        var data = Read(device, 512);
        Assert.Equal(3, Word(data, 1));
        Assert.Equal(2, Word(data, 3));
        Assert.Equal(4, Word(data, 6));
        Assert.Equal(0x200, Word(data, 49)); // LBA, without advertising DMA.
        Assert.Equal(disk.SectorCount, Word(data, 60) | (long)Word(data, 61) << 16);
        Assert.Equal(16, Word(data, 47));
        Assert.Equal(0x100, Word(data, 59));
        Assert.False(device.Drq);
        Assert.False(device.Busy);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void PioUses512ByteBlocksAcrossTheImageSectorBoundaries(int sectorSize)
    {
        using var disk = new Disk(sectorSize: sectorSize, recordSize: sectorSize);
        var original = Contents(disk);
        var device = Device(disk);
        Address(device, 1, 2);
        Issue(device, 0x20);
        Assert.Equal(original.AsSpan(512, 1024).ToArray(), Read(device, 1024));
        Assert.Equal(2, device.ReadBlocks(0));
        var payload = Enumerable.Range(0, 1024).Select(index => (byte)(index * 7 + 31)).ToArray();
        Address(device, 1, 2);
        Issue(device, 0x30);
        Assert.False(device.Irq); // First write DRQ is polled, not an interrupt.
        Write(device, payload);
        WaitIdle(device);
        var expected = original.ToArray();
        payload.CopyTo(expected.AsSpan(512));
        Assert.Equal(expected, Contents(disk));
        Assert.Equal(2, device.WrittenBlocks(0));
        Assert.Equal(0, device.ReadCommand(2));
        Assert.Equal(2, device.ReadCommand(3)); // Last transferred block, not the next block.
    }

    [Fact]
    public void ReadingStatusAcknowledgesIrqButAlternateStatusDoesNotAndMaskPreservesPendingIrq()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteControl(6, 2);
        Address(device, 0, 1);
        Issue(device, 0x20);
        Assert.False(device.Irq);
        Assert.Equal(0x58, device.ReadControl(6));
        device.WriteControl(6, 0);
        Assert.True(device.Irq);
        Assert.Equal(0x58, device.ReadControl(6));
        Assert.True(device.Irq);
        Assert.Equal(0x58, device.ReadCommand(7));
        Assert.False(device.Irq);
        Read(device, 512);
        Assert.False(device.Irq);
    }

    [Fact]
    public void MasterAndSlaveHaveIndependentDataAndGeometrySettings()
    {
        using var master = new Disk(0x11, sectorSize: 512, recordSize: 512);
        using var slave = new Disk(0x84, sectorSize: 512, recordSize: 512);
        var device = Device(master, slave);
        Address(device, 0, 1, slave: true);
        Issue(device, 0x20);
        Assert.Equal(slave.Record(0), Read(device, 512));
        device.ReadCommand(7);
        Address(device, 0, 1);
        Issue(device, 0x20);
        Assert.Equal(master.Record(0), Read(device, 512));
        Assert.Equal(1, device.ReadBlocks(0));
        Assert.Equal(1, device.ReadBlocks(1));
    }

    [Fact]
    public void ChsTransfersCrossHeadAndCylinderBoundaries()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteCommand(6, 0xa1);
        device.WriteCommand(2, 3);
        device.WriteCommand(3, 4);
        device.WriteCommand(4, 0);
        device.WriteCommand(5, 0);
        Issue(device, 0x20);
        Assert.Equal(Contents(disk).AsSpan(7 * 512, 3 * 512).ToArray(), Read(device, 3 * 512));
        Assert.Equal(1, device.ReadCommand(4));
        Assert.Equal(0, device.ReadCommand(6) & 15);
        Assert.Equal(2, device.ReadCommand(3));
    }

    [Fact]
    public void InitializeParametersChangesTranslationAndInvalidParametersFailOnlyOnDataAccess()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteCommand(6, 0xa0);
        device.WriteCommand(2, 2);
        Issue(device, 0x91);
        Assert.Equal(0, device.ReadCommand(7) & 1);
        device.WriteCommand(2, 1);
        device.WriteCommand(3, 1);
        device.WriteCommand(4, 3);
        Issue(device, 0x20);
        Assert.Equal(disk.Record(6), Read(device, 512));
        Issue(device, 0xec);
        var identity = Read(device, 512);
        Assert.Equal(12, Word(identity, 54));
        Assert.Equal(1, Word(identity, 55));
        Assert.Equal(2, Word(identity, 56));
        device.WriteCommand(2, 0);
        Issue(device, 0x91);
        Assert.Equal(0, device.ReadCommand(7) & 1);
        device.WriteCommand(2, 1);
        Issue(device, 0x20);
        Assert.Equal(0x10, device.ReadCommand(1));
        Assert.False(device.Drq);
    }

    [Fact]
    public void ZeroCountTransfers256SectorsAndLeavesTheLastAddress()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512, sectorsPerTrack: 64);
        var device = Device(disk);
        Address(device, 0, 0);
        Issue(device, 0x20);
        Assert.Equal(Contents(disk).AsSpan(0, 256 * 512).ToArray(), Read(device, 256 * 512));
        Assert.Equal(256, device.ReadBlocks(0));
        Assert.Equal(0, device.ReadCommand(2));
        Assert.Equal(255, device.ReadCommand(3));
        Assert.Equal(0, device.ReadCommand(4));
    }

    [Fact]
    public void ReadMultipleRaisesInterruptsPerBlockIncludingThePartialFinalBlock()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteCommand(2, 2);
        Issue(device, 0xc6);
        device.ReadCommand(7);
        Address(device, 3, 5);
        Issue(device, 0xc4);
        using var received = new MemoryStream();
        foreach (var sectors in new[] { 2, 2, 1 })
        {
            WaitDrq(device);
            Assert.True(device.Irq);
            device.ReadCommand(7);
            received.Write(Read(device, sectors * 512));
            Assert.False(device.Irq);
        }
        Assert.Equal(Contents(disk).AsSpan(3 * 512, 5 * 512).ToArray(), received.ToArray());
        Assert.False(device.Busy);
        Assert.False(device.Drq);
    }

    [Fact]
    public void WriteMultipleBuffersEachBlockAndPublishesItThroughTheExistingImage()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        var before = Contents(disk);
        device.WriteCommand(2, 2);
        Issue(device, 0xc6);
        device.ReadCommand(7);
        Address(device, 2, 3);
        Issue(device, 0xc5);
        var payload = Enumerable.Repeat((byte)0x6d, 3 * 512).ToArray();
        Write(device, payload.AsSpan(0, 1024));
        Assert.Equal(before, Contents(disk)); // Busy until the emulated media clocks advance.
        WaitDrq(device);
        Assert.True(device.Irq);
        Assert.Equal(2, device.WrittenBlocks(0));
        device.ReadCommand(7);
        Write(device, payload.AsSpan(1024));
        WaitIdle(device);
        Assert.True(device.Irq);
        payload.CopyTo(before.AsSpan(2 * 512));
        Assert.Equal(before, Contents(disk));
    }

    [Fact]
    public void InvalidMultipleSettingDisablesMultipleTransfers()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteCommand(2, 3);
        Issue(device, 0xc6);
        Assert.Equal(4, device.ReadCommand(1));
        Address(device, 0, 1);
        Issue(device, 0xc4);
        Assert.Equal(4, device.ReadCommand(1));
        Assert.False(device.Drq);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EightBitBufferTransfersObeyTheSoftwareResetFeatureSelection(bool preserve)
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        device.WriteCommand(1, 1);
        Issue(device, 0xef);
        if (preserve)
        {
            device.WriteCommand(1, 0x66);
            Issue(device, 0xef);
        }
        device.WriteControl(6, 4);
        device.WriteControl(6, 0);
        WaitIdle(device);
        Issue(device, 0xe8);
        for (var index = 0; index < (preserve ? 512 : 256); index++)
            device.WriteCommand(0, preserve ? (ushort)(index & 255) : (ushort)0x7351);
        Assert.False(device.Drq);
        Issue(device, 0xe4);
        for (var index = 0; index < (preserve ? 512 : 256); index++)
            Assert.Equal(preserve ? index & 255 : 0x7351, device.ReadCommand(0));
        Assert.False(device.Drq);
    }

    [Fact]
    public void ResetAbandonsIncompleteWritesAndNeverDisposesTheBorrowedImage()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var original = Contents(disk);
        var device = Device(disk);
        Address(device, 0, 1);
        Issue(device, 0x30);
        device.WriteCommand(0, 0xa321);
        device.WriteControl(6, 4);
        Assert.True(device.Busy);
        Assert.False(device.Drq);
        device.Advance(100);
        Assert.True(device.Busy);
        device.WriteControl(6, 0);
        WaitIdle(device);
        Assert.Equal(original, Contents(disk));
        Assert.False(device.Irq);
        Assert.Equal(1, device.ReadCommand(1));
        Assert.Equal(1, device.ReadCommand(2));
        Assert.Equal(1, device.ReadCommand(3));
        Assert.Equal(0, device.ReadCommand(6));
        device.Reset();
        WaitIdle(device);
        Assert.False(disk.Disposed);
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0x30)]
    public void DisconnectDuringPioWaitStopsActivityAndDoesNotCommitTheOldBuffer(int command)
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        Disk? connected = disk;
        var device = new AtaPio(unit => unit == 0 ? connected : null, ticksPerSecond: 1000);
        device.Advance(100);
        Address(device, 0, 1);
        Issue(device, (ushort)command);
        Assert.True(device.Active(0));
        Assert.True(device.Drq);
        connected = null;
        device.Advance(1);
        Assert.False(device.Active(0));
        Assert.False(device.Drq);
        Assert.False(device.Irq);
        connected = disk;
        Assert.Equal(0x50, device.ReadControl(6));
        Assert.False(disk.HasChanges);
        Assert.False(disk.Disposed);
    }

    [Fact]
    public void CorruptReadDataIsTransferredWithAnErrorWhileVerifyStopsWithoutDrq()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512) { Bad = true };
        var device = Device(disk);
        Address(device, 2, 2);
        Issue(device, 0x20);
        Assert.Equal(0x59, device.ReadCommand(7));
        Assert.Equal(0x40, device.ReadCommand(1));
        Assert.Equal(disk.Record(2), Read(device, 512));
        Assert.False(device.Drq);
        Assert.Equal(1, device.ReadCommand(2));
        Assert.Equal(2, device.ReadCommand(3));
        Address(device, 2, 2);
        Issue(device, 0x40);
        Assert.False(device.Drq);
        Assert.Equal(0x40, device.ReadCommand(1));
        Assert.Equal(2, device.ReadCommand(2));
    }

    [Fact]
    public void EndOfMediaKeepsTheFailingAddressAndTheUntransferredCount()
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        Address(device, 23, 2);
        Issue(device, 0x20);
        Assert.Equal(disk.Record(23), Read(device, 512));
        WaitIdle(device);
        Assert.False(device.Drq);
        Assert.Equal(0x10, device.ReadCommand(1));
        Assert.Equal(1, device.ReadCommand(2));
        Assert.Equal(24, device.ReadCommand(3));
    }

    [Fact]
    public void ProtectedImagesAndHostIoErrorsBecomeTaskFileErrors()
    {
        using var protectedDisk = new Disk(sectorSize: 512, recordSize: 512) { IsReadOnly = true };
        var writer = Device(protectedDisk);
        Address(writer, 0, 1);
        Issue(writer, 0x30);
        Assert.False(writer.Drq);
        Assert.Equal(4, writer.ReadCommand(1));
        Assert.Equal(0x71, writer.ReadCommand(7));
        Assert.False(protectedDisk.HasChanges);
        using var failedDisk = new Disk(sectorSize: 512, recordSize: 512)
        {
            ReadTransform = _ => throw new IOException()
        };
        var reader = Device(failedDisk);
        Address(reader, 0, 1);
        Issue(reader, 0x20);
        Assert.Equal(0x40, reader.ReadCommand(1));
        Assert.False(reader.Drq);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0xc8)]
    [InlineData(0x22)]
    public void UnsupportedCommandsAbortWithoutPretendingToTransferData(int command)
    {
        using var disk = new Disk(sectorSize: 512, recordSize: 512);
        var device = Device(disk);
        Issue(device, (ushort)command);
        Assert.Equal(4, device.ReadCommand(1));
        Assert.Equal(0x51, device.ReadCommand(7));
        Assert.False(device.Drq);
        Assert.False(disk.HasChanges);
    }

    private static AtaPio Device(Disk master, Disk? slave = null)
    {
        var device = new AtaPio(unit => unit == 0 ? master : slave, ticksPerSecond: 1000);
        device.Advance(100);
        return device;
    }

    private static void Address(AtaPio device, int block, int count, bool slave = false)
    {
        device.WriteCommand(6, (ushort)(0xe0 | (slave ? 0x10 : 0) | block >> 24));
        device.WriteCommand(2, (ushort)count);
        device.WriteCommand(3, (byte)block);
        device.WriteCommand(4, (byte)(block >> 8));
        device.WriteCommand(5, (byte)(block >> 16));
    }

    private static void Issue(AtaPio device, ushort command)
    {
        device.WriteCommand(7, command);
        device.Advance(1000);
    }

    private static byte[] Read(AtaPio device, int bytes)
    {
        var result = new byte[bytes];
        for (var offset = 0; offset < bytes; offset += 2)
        {
            WaitDrq(device);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset), device.ReadCommand(0));
        }
        return result;
    }

    private static void Write(AtaPio device, ReadOnlySpan<byte> data)
    {
        for (var offset = 0; offset < data.Length; offset += 2)
        {
            WaitDrq(device);
            device.WriteCommand(0, BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
        }
    }

    private static void WaitDrq(AtaPio device)
    {
        for (var clock = 0; !device.Drq && device.Busy && clock < 10000; clock++) device.Advance(1);
        Assert.True(device.Drq);
    }

    private static void WaitIdle(AtaPio device)
    {
        for (var clock = 0; device.Busy && clock < 10000; clock++) device.Advance(1);
        Assert.False(device.Busy);
    }

    private static ushort Word(byte[] data, int word) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(word * 2));

    private static byte[] Contents(Disk disk)
    {
        using var data = new MemoryStream();
        for (var index = 0; index < disk.SectorCount; index++)
            data.Write(disk.ReadSectorAsync(index).GetAwaiter().GetResult().Data.Span);
        return data.ToArray();
    }
}
