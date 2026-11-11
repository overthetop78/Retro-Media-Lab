using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class WinchesterDescriptorTests
{
    [Theory]
    [InlineData(22)]
    [InlineData(33)]
    public void UnchangedLegacyGeometryRetainsEveryDescriptorByte(int length)
    {
        var descriptor = Legacy(length);
        var expected = descriptor.ToArray();
        var geometry = new HardDiskGeometry(2, 4, 33, 256);
        AcornWinchesterFormat.UpdateGeometryIfChanged(descriptor, geometry, geometry.Capacity);
        Assert.Equal(expected, descriptor);
    }

    [Fact]
    public void ChangedGeometryUpdatesRequiredFieldsAndRetainsOtherDescriptorBytes()
    {
        var descriptor = Legacy();
        var geometry = new HardDiskGeometry(4, 2, 33, 256);
        AcornWinchesterFormat.UpdateGeometryIfChanged(descriptor, geometry, geometry.Capacity);
        var decoded = AcornWinchesterFormat.Geometry(descriptor, geometry.Capacity);
        Assert.NotNull(decoded);
        Assert.Equal((4L, 2, 33, 256), (decoded.Cylinders, decoded.Heads, decoded.SectorsPerTrack, decoded.SectorSize));
        Assert.Equal(0xa5, descriptor[^1]);
    }

    private static byte[] Legacy(int length = AcornWinchesterFormat.DescriptorSize)
    {
        var descriptor = new byte[length];
        BinaryPrimitives.WriteUInt16BigEndian(descriptor.AsSpan(AcornWinchesterFormat.CylinderOffset), 2);
        descriptor[AcornWinchesterFormat.HeadOffset] = 4;
        descriptor[^1] = 0xa5;
        return descriptor;
    }
}
