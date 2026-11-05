using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000TrackLayoutTests
{
    [Fact]
    public void InterleaveTwoReproducesThePhysicalSlotsAndLastSectorFlag()
    {
        var parameters = Parameters();
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, 2, out var geometry));
        var map = new byte[33];
        Assert.True(Acb4000TrackLayout.TryBuild(parameters, geometry, 0, 0, default, map, out var count));
        Assert.Equal(33, count);
        Assert.Equal(new byte[] { 0, 17, 1, 18, 2, 19, 3, 20, 4, 21, 5, 22, 6, 23, 7, 24,
            8, 25, 9, 26, 10, 27, 11, 28, 12, 29, 13, 30, 14, 31, 15, 0x60, 16 }, map);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void EveryPermittedInterleaveAssignsEachLogicalSectorOnce(ushort size)
    {
        var parameters = Parameters() with { BlockSize = size };
        for (byte interleave = 1; interleave < 33; interleave++)
        {
            if (!Acb4000FormatGeometry.TryCreate(parameters, 0, interleave, out var geometry)) continue;
            var map = Enumerable.Repeat((byte)0xcc, 35).ToArray();
            Assert.True(Acb4000TrackLayout.TryBuild(parameters, geometry, 0, 1, default, map, out var count));
            Assert.Equal(geometry.SectorsPerTrack, count);
            var used = map[..count];
            Assert.Equal(Enumerable.Range(0, count), used.Select(value => value & 0x3f).Order());
            Assert.Single(used, value => (value & 0x40) != 0);
            Assert.Equal(count - 1, Assert.Single(used, value => (value & 0x40) != 0) & 0x3f);
            Assert.All(map[count..], value => Assert.Equal(0xcc, value));
        }
    }

    [Theory]
    [InlineData(256, 1, 320)]
    [InlineData(256, 2, 310)]
    [InlineData(512, 1, 576)]
    [InlineData(512, 2, 566)]
    [InlineData(1024, 1, 1088)]
    [InlineData(1024, 2, 1078)]
    public void SlotBoundariesAndDuplicateDefectsUseTheFirmwareDivisor(ushort size, byte interleave, int stride)
    {
        var parameters = Parameters() with { BlockSize = size };
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, interleave, out var geometry));
        var bytes = Defects((1, 1, (uint)stride - 1), (1, 1, (uint)stride),
            (1, 1, (uint)stride + 1), (1, 1, 10416));
        Assert.True(Acb4000FormatDefects.TryDecode(bytes, parameters, out var defects));
        var map = new byte[geometry.SectorsPerTrack];
        Assert.True(Acb4000TrackLayout.TryBuild(parameters, geometry, 1, 1, defects, map, out var count));
        Assert.Equal(geometry.SectorsPerTrack - 2, count);
        Assert.Equal(0x80, map[0]);
        Assert.Equal(0x80, map[1]);
        Assert.Equal(2, map.Count(value => value == 0x80));
        Assert.True(Acb4000TrackLayout.TryBuild(parameters, geometry, 0, 1, defects, map, out count));
        Assert.Equal(geometry.SectorsPerTrack, count);
    }

    [Fact]
    public void AllDefectiveSlotsAndInvalidDestinationsAreRejectedWithoutMutation()
    {
        var parameters = Parameters();
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, 2, out var geometry));
        var bytes = Defects(Enumerable.Range(0, 33).Select(i => (1, (byte)0, (uint)(i * 310))).ToArray());
        Assert.True(Acb4000FormatDefects.TryDecode(bytes, parameters, out var defects));
        var map = Enumerable.Repeat((byte)0x95, 33).ToArray();
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, geometry, 1, 0, defects, map, out var count));
        Assert.Equal(0, count);
        Assert.All(map, value => Assert.Equal(0x95, value));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, geometry, 0, 0, default, map.AsSpan(0, 32), out _));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, geometry, -1, 0, default, map, out _));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, geometry, 2, 0, default, map, out _));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, geometry, 0, 2, default, map, out _));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters, default, 0, 0, default, map, out _));
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters with { Cylinders = 1, BlockSize = 1024 }, 0, 2, out var other));
        Assert.False(Acb4000TrackLayout.TryBuild(parameters with { BlockSize = 512 }, other, 0, 0, default, map, out _));
        Assert.All(map, value => Assert.Equal(0x95, value));
    }

    private static Acb4000DriveParameters Parameters() => Acb4000DriveParameters.Default with { Cylinders = 2, Heads = 2 };

    private static byte[] Defects(params (int Cylinder, byte Head, uint Position)[] defects)
    {
        var bytes = new byte[4 + defects.Length * 8];
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)(defects.Length * 8));
        for (var i = 0; i < defects.Length; i++)
        {
            var entry = bytes.AsSpan(4 + i * 8, 8);
            BinaryPrimitives.WriteUInt32BigEndian(entry, (uint)defects[i].Cylinder << 8 | defects[i].Head);
            BinaryPrimitives.WriteUInt32BigEndian(entry[4..], defects[i].Position);
        }
        return bytes;
    }
}
