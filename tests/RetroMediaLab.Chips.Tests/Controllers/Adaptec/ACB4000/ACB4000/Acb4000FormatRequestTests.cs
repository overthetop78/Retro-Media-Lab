using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000FormatRequestTests
{
    [Theory]
    [InlineData(0x00, false, 0x6c)]
    [InlineData(0x04, false, 0x6c)]
    [InlineData(0x06, false, 0xe5)]
    [InlineData(0x1c, true, 0x6c)]
    [InlineData(0x1e, true, 0xe5)]
    public void OptionsSelectFillAndDefectReception(byte options, bool defects, byte fill)
    {
        for (var unit = 0; unit < 8; unit++)
        {
            byte[] cdb = [4, (byte)(unit << 5 | options), 0xe5, 0, 2, 0];
            Assert.True(Acb4000FormatRequest.TryDecode(cdb, Acb4000DriveParameters.Default, out var request));
            Assert.Equal(unit, request.Unit);
            Assert.Equal(fill, request.Fill);
            Assert.Equal(defects, request.HasDefectList);
            Assert.Equal(33, request.Geometry.SectorsPerTrack);
        }
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x02)]
    [InlineData(0x08)]
    [InlineData(0x0c)]
    [InlineData(0x10)]
    [InlineData(0x14)]
    [InlineData(0x18)]
    [InlineData(0x1f)]
    public void InvalidOptionsCannotStartFormatting(byte options)
    {
        Assert.False(Acb4000FormatRequest.TryDecode([4, options, 0, 0, 2, 0], Acb4000DriveParameters.Default, out var request));
        Assert.Equal(default, request);
    }

    [Fact]
    public void InvalidCdbAndGeometryAreRejected()
    {
        foreach (var cdb in new byte[][] { [], [4], [4, 0, 0, 0, 2], [4, 0, 0, 0, 2, 0, 0],
            [5, 0, 0, 0, 2, 0], [4, 0, 0, 0, 2, 1], [4, 0, 0, 1, 2, 0], [4, 0, 0, 0, 33, 0] })
            Assert.False(Acb4000FormatRequest.TryDecode(cdb, Acb4000DriveParameters.Default, out _));
        Assert.False(Acb4000FormatRequest.TryDecode([4, 0, 0, 0, 2, 0], default, out _));
    }

    [Fact]
    public void DefectCoordinatesAreDecodedInPhysicalOrderAtTheFormatBoundaries()
    {
        var bytes = List((0, 1, 0), (0, 1, 10416), (1, 0, 0), (305, 1, 0x1234));
        Assert.True(Acb4000FormatDefects.TryDecode(bytes, Acb4000DriveParameters.Default, out var defects));
        Assert.Equal(4, defects.Count);
        Assert.Equal((305, (byte)1, 0x1234u), defects[3]);
        Assert.Equal((0, (byte)1, 10416u), defects[1]);
        bytes[11] = 0x55;
        Assert.Equal((0, (byte)1, 0x55u), defects[0]); // Borrowed storage, not a hidden copy.
    }

    [Fact]
    public void EmptyAndMaximumListsAreAccepted()
    {
        Assert.True(Acb4000FormatDefects.TryDecode([0, 0, 0, 0], Acb4000DriveParameters.Default, out var empty));
        Assert.Equal(0, empty.Count);
        var bytes = List(Enumerable.Range(0, 127).Select(i => (0, (byte)1, (uint)i)).ToArray());
        Assert.Equal(1020, bytes.Length);
        Assert.True(Acb4000FormatDefects.TryReadLength(bytes.AsSpan(0, 4), out var length));
        Assert.Equal(bytes.Length, length);
        Assert.True(Acb4000FormatDefects.TryDecode(bytes, Acb4000DriveParameters.Default, out var defects));
        Assert.Equal(127, defects.Count);
    }

    [Fact]
    public void UnsortedAndOutOfGeometryListsAreRejected()
    {
        foreach (var bytes in new[] { List((1, 0, 0), (0, 1, 0)), List((0, 1, 0), (0, 0, 1)),
            List((0, 0, 20), (0, 0, 10)), List((306, 0, 0)), List((0, 2, 0)), List((0xffffff, 0, 0)) })
            Assert.False(Acb4000FormatDefects.TryDecode(bytes, Acb4000DriveParameters.Default, out _));
    }

    [Fact]
    public void MalformedLengthsAndReservedHeaderBytesAreRejected()
    {
        var bytes = List((0, 0, 1));
        Assert.False(Acb4000FormatDefects.TryDecode(bytes.AsSpan(0, 11), Acb4000DriveParameters.Default, out _));
        foreach (var length in new[] { 1, 7, 9, 1024, 65535 })
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)length);
            Assert.False(Acb4000FormatDefects.TryReadLength(bytes, out _));
        }
        Assert.False(Acb4000FormatDefects.TryReadLength([0, 0, 0], out _));
        Assert.False(Acb4000FormatDefects.TryReadLength([1, 0, 0, 0], out _));
        Assert.False(Acb4000FormatDefects.TryReadLength([0, 1, 0, 0], out _));
    }

    [Fact]
    public void ParameterTrackAndPositionsBeyondTheFirmwareLimitAreRejected()
    {
        foreach (var bytes in new[] { List((0, 0, 0)), List((1, 0, 10417)), List((1, 0, uint.MaxValue)) })
            Assert.False(Acb4000FormatDefects.TryDecode(bytes, Acb4000DriveParameters.Default, out _));
    }

    private static byte[] List(params (int Cylinder, byte Head, uint Position)[] defects)
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
