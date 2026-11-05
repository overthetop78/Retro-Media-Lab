using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;

namespace RetroMediaLab.Tests.Media;

/// <summary>Vecteurs DSC et validation des dispositions DAT, sans fichier ni ressource native.</summary>
public sealed class AcornWinchesterFormatTests
{
    [Fact]
    public void DescriptorParametersProduceTheOriginalNinePhysicalFlagsWithoutChangingTheDescriptor()
    {
        var descriptor = Descriptor(256);
        BinaryPrimitives.WriteUInt16BigEndian(descriptor.AsSpan(16), 150);
        var before = descriptor.ToArray();
        var flags = new byte[9];
        Assert.True(AcornWinchesterFormat.TryWritePhysicalParameters(descriptor, 306L * 2 * 33 * 256, flags));
        Assert.Equal(new byte[] { 0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0 }, flags);
        Assert.Equal(before, descriptor);
    }

    [Fact]
    public void PhysicalProjectionPreservesStepHeadBitsParkingAndTheFirmwareDerivedPair()
    {
        var descriptor = new byte[33];
        AcornWinchesterFormat.WriteGeometry(descriptor, new(2048, 16, 9, 1024));
        BinaryPrimitives.WriteUInt16BigEndian(descriptor.AsSpan(16), 2047);
        descriptor[20] = 0xff;
        descriptor[21] = 2;
        var flags = new byte[9];
        Assert.True(AcornWinchesterFormat.TryWritePhysicalParameters(descriptor, 2048L * 16 * 9 * 1024, flags));
        Assert.Equal(new byte[] { 0, 0x48, 0x7f, 0x6f, 0x7f, 0x6f, 0x7f, 0x4f, 0x7f }, flags);
    }

    [Fact]
    public void PhysicalProjectionAlsoAcceptsTheLegacyZeroBlockSize()
    {
        var descriptor = Descriptor(0);
        var flags = new byte[9];
        Assert.True(AcornWinchesterFormat.TryWritePhysicalParameters(descriptor, 306L * 2 * 33 * 256, flags));
        Assert.Equal(new byte[] { 0, 2 }, flags[..2]);
        Assert.Equal(0, descriptor[3]);
        Assert.Equal(0, descriptor[12]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UnrepresentablePhysicalParametersAreRejectedBeforeChangingTheOutput(int corruption)
    {
        var descriptor = Descriptor(256);
        if (corruption == 0) descriptor[21] = 3;
        if (corruption == 1) BinaryPrimitives.WriteUInt16BigEndian(descriptor.AsSpan(16), 2048);
        if (corruption == 2) BinaryPrimitives.WriteUInt16BigEndian(descriptor.AsSpan(13), 2049);
        if (corruption == 3) descriptor[15] = 17;
        var flags = Enumerable.Repeat((byte)0x5a, 9).ToArray();
        Assert.False(AcornWinchesterFormat.TryWritePhysicalParameters(descriptor, 0, flags));
        Assert.All(flags, value => Assert.Equal(0x5a, value));
    }

    [Theory]
    [InlineData(256, 32)]
    [InlineData(256, 33)]
    [InlineData(512, 17)]
    [InlineData(512, 18)]
    [InlineData(1024, 9)]
    public void CompleteDatLengthPreservesTheSupportedLayout(int size, int sectors)
    {
        var geometry = new HardDiskGeometry(306, 2, sectors, size);
        var descriptor = new byte[33];
        AcornWinchesterFormat.WriteGeometry(descriptor, geometry);
        Assert.Equal(new byte[] { 0, 0, 0, 8, 0, 0, 0, 0, 0,
            (byte)(size >> 16), (byte)(size >> 8), (byte)size, 1, 1, 0x32, 2 }, descriptor[..16]);
        var decoded = Assert.IsType<HardDiskGeometry>(AcornWinchesterFormat.Geometry(descriptor, geometry.Capacity));
        Assert.Equal(geometry.Capacity, decoded.Capacity);
        Assert.Equal(size, decoded.SectorSize);
        Assert.Equal(sectors, decoded.SectorsPerTrack);
        Assert.True(WriterAccepts(geometry, size));
        Assert.False(WriterAccepts(geometry, size / 2));
    }

    [Theory]
    [InlineData(0, 256, 33)]
    [InlineData(256, 256, 33)]
    [InlineData(512, 512, 18)]
    [InlineData(1024, 1024, 9)]
    public void MissingOrPartialDatKeepsTheUsualLayout(int declaredSize, int size, int sectors)
    {
        var descriptor = Descriptor(declaredSize);
        var decoded = Assert.IsType<HardDiskGeometry>(AcornWinchesterFormat.Geometry(descriptor));
        Assert.Equal(size, decoded.SectorSize);
        Assert.Equal(sectors, decoded.SectorsPerTrack);
        var partial = Assert.IsType<HardDiskGeometry>(AcornWinchesterFormat.Geometry(descriptor, size));
        Assert.Equal(decoded.Capacity, partial.Capacity);
        Assert.Equal(sectors, partial.SectorsPerTrack);
    }

    [Fact]
    public void LegacyDescriptorDoesNotNeedAModeSelectHeader()
    {
        var descriptor = Descriptor(0);
        Assert.Equal(0, descriptor[3]);
        Assert.Equal(0, descriptor[12]);
        var decoded = Assert.IsType<HardDiskGeometry>(AcornWinchesterFormat.Geometry(descriptor));
        Assert.Equal(306L * 2 * 33 * 256, decoded.Capacity);
    }

    [Fact]
    public void DescriptorEncodingPreservesUnrelatedControllerAndTrailingFields()
    {
        var descriptor = Enumerable.Repeat((byte)0x95, 33).ToArray();
        var before = descriptor.ToArray();
        AcornWinchesterFormat.WriteGeometry(descriptor, new(2048, 16, 9, 1024));
        foreach (var offset in Enumerable.Range(0, 33).Except(new[] { 3, 9, 10, 11, 12, 13, 14, 15 }))
            Assert.Equal(before[offset], descriptor[offset]);
        Assert.Equal(new byte[] { 0, 4, 0 }, descriptor[9..12]);
        Assert.Equal(new byte[] { 8, 0, 16 }, descriptor[13..16]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(257)]
    [InlineData(2048)]
    [InlineData(0x100100)]
    public void UnsupportedBlockSizeIsNotSilentlyReplaced(int size)
    {
        Assert.Null(AcornWinchesterFormat.Geometry(Descriptor(size)));
    }

    [Fact]
    public void TruncatedOrEmptyGeometryIsRejected()
    {
        Assert.Null(AcornWinchesterFormat.Geometry(Descriptor(256).AsSpan(0, 21)));
        var descriptor = Descriptor(256);
        descriptor[13] = descriptor[14] = 0;
        Assert.Null(AcornWinchesterFormat.Geometry(descriptor));
        descriptor = Descriptor(256);
        descriptor[15] = 0;
        Assert.Null(AcornWinchesterFormat.Geometry(descriptor));
    }

    [Theory]
    [InlineData(1, 2, 34, 256)]
    [InlineData(1, 2, 33, 512)]
    [InlineData(1, 2, 18, 1024)]
    [InlineData(1, 2, 9, 2048)]
    [InlineData(65536, 2, 33, 256)]
    [InlineData(1, 256, 33, 256)]
    public void WriterRejectsUnrepresentableGeometry(long cylinders, int heads, int sectors, int size)
    {
        var geometry = new HardDiskGeometry(cylinders, heads, sectors, size);
        Assert.False(AcornWinchesterFormat.SupportsGeometry(geometry));
        Assert.False(WriterAccepts(geometry, size));
        Assert.Throws<ArgumentOutOfRangeException>(() => AcornWinchesterFormat.WriteGeometry(new byte[33], geometry));
    }

    [Fact]
    public void WriterStillRejectsUnavailableOrInconsistentData()
    {
        var geometry = new HardDiskGeometry(1, 2, 18, 512);
        Assert.False(WriterAccepts(geometry, 512, MediaDataRangeKind.Unavailable));
        Assert.False(WriterAccepts(geometry, 512, capacity: geometry.Capacity + 512));
    }

    private static byte[] Descriptor(int size)
    {
        var bytes = new byte[33];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(9), (uint)size << 8);
        bytes[13] = 1;
        bytes[14] = 0x32;
        bytes[15] = 2;
        return bytes;
    }

    private static bool WriterAccepts(HardDiskGeometry geometry, int logicalSize,
        MediaDataRangeKind kind = MediaDataRangeKind.Zero, long? capacity = null)
    {
        var length = capacity ?? geometry.Capacity;
        var document = MediaImageDocument.CreateUnexplored(new("memory.dat", []), HardDiskImageFormatIds.AcornWinchester,
            MediaKind.HardDisk, new BlockMediaImageRepresentation(length, logicalSize, [new(0, length, kind)], geometry),
            [], new Dictionary<string, string>());
        return new AcornWinchesterWriter().CanWrite(document, HardDiskImageFormatIds.AcornWinchester, ".dat");
    }
}
