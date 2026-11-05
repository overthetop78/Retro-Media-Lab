using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

namespace RetroMediaLab.Tests.Media;

public sealed class AdaptecMfmGeometryTests
{
    [Theory]
    [InlineData(256, 33)]
    [InlineData(512, 18)]
    [InlineData(1024, 9)]
    public void EncodedParametersSurvivePhysicalInterleaveAndFieldDecoding(int size, int count)
    {
        var geometry = new AdaptecMfmGeometry(size, 306, 4, count);
        var flags = new byte[9];
        geometry.WriteInitialParameters(flags, reducedWriteCurrent: 150, landingZone: 0x87, stepCode: 1);
        var order = Enumerable.Range(0, count).Where(index => index % 2 == 0)
            .Concat(Enumerable.Range(0, count).Where(index => index % 2 != 0));
        var sectors = order.Select(address => new MediaDiskSector(address, new byte[size], 0, 0, address)).ToArray();
        var track = AdaptecMfmTrackCodec.Encode(sectors, false, 16_666_667, size, flags);
        var fields = AdaptecMfmTrackCodec.Decode(track.Revolutions[0], 0, 0, size);
        Assert.True(AdaptecMfmGeometry.TryRead(fields, out var actual));
        Assert.Equal(geometry, actual);
        for (var index = 0; index < count; index++)
        {
            Assert.Equal(index < 9 ? flags[index] : 0, fields[index].Id.Span[5] & 0x7f);
            Assert.True(fields[index].Sector.HeaderIntegrityValid);
            Assert.True(fields[index].Sector.DataIntegrityValid);
        }
    }

    [Fact]
    public void GeometryUsesPhysicalParameterOrderRatherThanLogicalSectorOrder()
    {
        var fields = Fields([0, 0x48, 0x7f, 0x6f, 0x7f, 0x6f, 0x7f, 0x4f, 0x7f]);
        Assert.True(AdaptecMfmGeometry.TryRead(fields, out var geometry));
        Assert.Equal(new AdaptecMfmGeometry(1024, 2048, 16, 9), geometry);
        var rotated = fields.Skip(3).Concat(fields.Take(3)).ToArray();
        Assert.True(AdaptecMfmGeometry.TryRead(rotated, out var afterRotation));
        Assert.Equal(geometry, afterRotation);
    }

    [Fact]
    public void BlankOrIncompleteTracksDoNotReceiveGuessedGeometry()
    {
        Assert.False(AdaptecMfmGeometry.TryRead([], out _));
        Assert.False(AdaptecMfmGeometry.TryRead(Fields(new byte[9]), out _));
        Assert.False(AdaptecMfmGeometry.TryRead(Fields([0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0])[..8], out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AmbiguousOrInvalidIdentitiesCannotSupplyGeometry(int corruption)
    {
        var fields = MutableFields([0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0]);
        if (corruption == 0) fields[1].Id[2] = 0x80;
        if (corruption == 1) fields[1] = (fields[1].Id, fields[1].Sector with { HeaderIntegrityValid = false });
        if (corruption == 2) fields[1] = (fields[1].Id, fields[1].Sector with { Number = 0 });
        if (corruption == 3) fields[0].Id[5] |= 0x80;
        Assert.False(AdaptecMfmGeometry.TryRead(fields.Select(field => ((ReadOnlyMemory<byte>)field.Id, field.Sector)).ToArray(), out _));
    }

    private static (byte[] Id, MediaDiskSector Sector)[] MutableFields(byte[] flags)
    {
        int[] addresses = [0, 2, 4, 6, 8, 1, 3, 5, 7];
        return addresses.Select((address, position) =>
        {
            var id = new byte[10];
            id[0] = 0xa1;
            id[1] = 0xfe;
            id[4] = (byte)address;
            id[5] = (byte)(flags[position] | (address == 8 ? 0x80 : 0));
            return (id, new MediaDiskSector(address, ReadOnlyMemory<byte>.Empty, 0, 0, address)
                { HeaderIntegrityValid = true });
        }).ToArray();
    }

    private static (ReadOnlyMemory<byte> Id, MediaDiskSector Sector)[] Fields(byte[] flags) =>
        MutableFields(flags).Select(field => ((ReadOnlyMemory<byte>)field.Id, field.Sector)).ToArray();
}
