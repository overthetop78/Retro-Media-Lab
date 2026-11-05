using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuFormattedDataTests
{
    private const int TrackBytes = 20_836;
    private const uint Rate = 10_000_000;
    private const int BlockSize = 256;
    private const int Sectors = 33;

    [Fact]
    public async Task GeneratedTracksExposeRecordedGeometryAndPreparedAdfsBytes()
    {
        var geometry = new HardDiskGeometry(1, 2, Sectors, BlockSize);
        var prefix = AcornAdfsFormatter.CreateHeader(checked((int)geometry.LogicalSectorCount));
        var header = MfmEmuHeader.Create(1, 2, TrackBytes, Rate);
        var representation = new MfmEmuImageRepresentation(MfmEmuContainer.CreateFormatted(header, geometry, prefix));
        Assert.Equal(geometry.Capacity, representation.LogicalLength);
        var bytes = new byte[prefix.Length];
        await representation.Sectors.ReadExactlyAsync(0, bytes, CancellationToken.None);
        Assert.Equal(prefix, bytes);
        var last = await representation.Sectors.ReadSectorAsync(geometry.LogicalSectorCount - 1);
        Assert.True(last.IntegrityValid);
        Assert.Equal(1, last.Head);
        Assert.All(last.Data.ToArray(), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task PartialContainerReadsMatchTheCompleteStreamIncludingTrackHeadersAndEndMarker()
    {
        var geometry = new HardDiskGeometry(1, 2, Sectors, BlockSize);
        var header = MfmEmuHeader.Create(1, 2, TrackBytes, Rate);
        var source = new MfmEmuFormattedData(header, geometry, ReadOnlyMemory<byte>.Empty);
        var container = MfmEmuContainer.CreateFormatted(header, geometry, ReadOnlyMemory<byte>.Empty);
        using var stream = new MemoryStream();
        await container.CopyToAsync(stream);
        var expected = stream.ToArray();
        Assert.Equal(source.Length, expected.Length);
        var actual = new byte[expected.Length];
        for (var offset = 0; offset < actual.Length; offset += 997)
            await source.ReadExactlyAsync(offset, actual.AsMemory(offset, Math.Min(997, actual.Length - offset)));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CancellationAndBoundsDoNotLeavePartiallyPublishedState()
    {
        var geometry = new HardDiskGeometry(1, 1, Sectors, BlockSize);
        var header = MfmEmuHeader.Create(1, 1, TrackBytes, Rate);
        var source = new MfmEmuFormattedData(header, geometry, ReadOnlyMemory<byte>.Empty);
        var destination = Enumerable.Repeat((byte)0x5a, 20).ToArray();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await source.ReadExactlyAsync(0, destination, new CancellationToken(true)));
        Assert.All(destination, value => Assert.Equal(0x5a, value));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await source.ReadExactlyAsync(source.Length - 1, destination));
        Assert.All(destination, value => Assert.Equal(0x5a, value));
    }
}
