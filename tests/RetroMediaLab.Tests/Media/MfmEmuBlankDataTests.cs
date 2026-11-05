using System.IO;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Visualization;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuBlankDataTests
{
    [Fact]
    public void VisualizationDescriptorEnumeratesPhysicalTracksWithoutDecodingThem()
    {
        var representation = MfmEmuImageRepresentation.CreateUnformatted(2, 3, 65536, 10000000);
        var document = MediaImageDocument.CreateUnexplored(new("virtual.emu", []), HardDiskImageFormatIds.MfmEmu,
            MediaKind.HardDisk, representation, [], new Dictionary<string, string>());
        var descriptor = MediaVisualizationComposition.CreateDefault().Registry.CreateDescriptor(document);
        Assert.Equal(MediaRepresentationKind.Flux, descriptor.RepresentationKind);
        Assert.Equal(MediaVisualizationProgressUnit.Track, descriptor.ProgressUnit);
        Assert.Equal(new[] { 0, 1, 2 }, descriptor.Surfaces);
        Assert.Equal(new (long, int?)[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2) },
            descriptor.Elements.Select(element => (element.Position, element.Surface)));
    }

    [Fact]
    public async Task VirtualSourceHasEveryTrackHeaderAndNoInventedSectorFields()
    {
        var header = MfmEmuHeader.Create(2, 3, 4, 10000000);
        var source = new MfmEmuBlankData(header);
        var bytes = new byte[(int)source.Length];
        await source.ReadExactlyAsync(0, bytes);
        Assert.Equal(header.Bytes.ToArray(), bytes[..header.HeaderLength]);
        for (var cylinder = 0; cylinder < 2; cylinder++)
            for (var head = 0; head < 3; head++)
            {
                var offset = (int)header.TrackOffset(cylinder, head);
                Assert.Equal(0x12345678u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset)));
                Assert.Equal(cylinder, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 4)));
                Assert.Equal(head, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 8)));
                Assert.Equal(new byte[4], bytes.AsSpan(offset + 12, 4).ToArray());
            }
        Assert.True(header.TryReadTrackAddress(bytes.AsSpan((int)header.EndMarkerOffset), out _, out _, out var end));
        Assert.True(end);
        var reopened = await MfmEmuContainer.OpenAsync(source);
        var stream = new MemoryStream();
        try
        {
            await reopened.CopyToAsync(stream);
            Assert.Equal(bytes, stream.ToArray());
        }
        finally { await stream.DisposeAsync(); }
    }

    [Fact]
    public async Task SmallReadsAcrossVirtualHeadersMatchTheWholeImage()
    {
        var source = new MfmEmuBlankData(MfmEmuHeader.Create(2, 2, 8, 10000000));
        var expected = new byte[(int)source.Length];
        await source.ReadExactlyAsync(0, expected);
        for (var offset = 0; offset < expected.Length; offset++)
        {
            var window = new byte[Math.Min(7, expected.Length - offset)];
            await source.ReadExactlyAsync(offset, window);
            Assert.Equal(expected.AsSpan(offset, window.Length).ToArray(), window);
        }
    }

    [Fact]
    public async Task LastTrackPastTwoGiBDoesNotRequireAWholeDiskBuffer()
    {
        var image = MfmEmuImageRepresentation.CreateUnformatted(2048, 16, 65536, 10000000);
        var window = new byte[8];
        await image.ReadTrackBytesAsync(2047, 15, 65528, window);
        Assert.Equal(new byte[8], window);
        var header = MfmEmuHeader.Create(2048, 16, 65536, 10000000);
        Assert.True(header.EndMarkerOffset > int.MaxValue);
        var source = new MfmEmuBlankData(header);
        var end = new byte[12];
        await source.ReadExactlyAsync(header.EndMarkerOffset, end);
        Assert.True(header.TryReadTrackAddress(end, out _, out _, out var eof));
        Assert.True(eof);
    }

    [Theory]
    [InlineData(0, 1, 4, 10000000u)]
    [InlineData(1, 0, 4, 10000000u)]
    [InlineData(1, 1, 0, 10000000u)]
    [InlineData(1, 1, 5, 10000000u)]
    [InlineData(1, 1, 4, 0u)]
    public void InvalidDimensionsAreRejected(int cylinders, int heads, int length, uint rate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MfmEmuImageRepresentation.CreateUnformatted(cylinders, heads, length, rate));

    [Fact]
    public async Task InvalidOrCancelledReadsDoNotClearTheDestination()
    {
        var source = new MfmEmuBlankData(MfmEmuHeader.Create(1, 1, 4, 10000000));
        var window = new byte[] { 0xa5 };
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => source.ReadExactlyAsync(source.Length, window).AsTask());
        Assert.Equal(0xa5, window[0]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ReadExactlyAsync(0, window, cancellation.Token).AsTask());
        Assert.Equal(0xa5, window[0]);
    }
}
