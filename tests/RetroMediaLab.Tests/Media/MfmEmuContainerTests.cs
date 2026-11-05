using System.IO;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaFileSystems.Interfaces;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuContainerTests
{
    [Fact]
    public async Task ReplacedTracksAreOwnedAndOldSnapshotsKeepTheirData()
    {
        var bytes = Image();
        var original = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes));
        var replacement = Enumerable.Repeat((byte)0xa7, 8).ToArray();
        var first = original.WithTrackBytes(1, 1, replacement);
        Array.Clear(replacement);
        var second = first.WithTrackBytes(0, 0, replacement);
        var window = new byte[3];
        await first.ReadTrackBytesAsync(1, 1, 2, window);
        Assert.Equal(new byte[] { 0xa7, 0xa7, 0xa7 }, window);
        await original.ReadTrackBytesAsync(1, 1, 2, window);
        Assert.Equal(new byte[] { 0xab, 0x89, 0x67 }, window);
        var expected = bytes.ToArray();
        expected.AsSpan((int)original.Header.TrackOffset(1, 1) + original.Header.TrackHeaderLength, 8).Fill(0xa7);
        var output = new MemoryStream();
        try
        {
            await first.CopyToAsync(output);
            Assert.Equal(expected, output.ToArray());
            output.SetLength(0);
            output.Position = 0;
            expected.AsSpan((int)original.Header.TrackOffset(0, 0) + original.Header.TrackHeaderLength, 8).Clear();
            await second.CopyToAsync(output);
            Assert.Equal(expected, output.ToArray());
        }
        finally { await output.DisposeAsync(); }
    }

    [Fact]
    public async Task ReplacementsRequireAnExistingTrackAndExactlyItsStoredLength()
    {
        var original = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(Image()));
        Assert.Throws<ArgumentException>(() => original.WithTrackBytes(0, 0, new byte[7]));
        Assert.Throws<ArgumentOutOfRangeException>(() => original.WithTrackBytes(2, 0, new byte[8]));
        Assert.Throws<ArgumentOutOfRangeException>(() => original.WithTrackBytes(0, -1, new byte[8]));
    }

    [Fact]
    public async Task ReadingOneWindowDoesNotReadOtherTracks()
    {
        var bytes = Image();
        var source = new ObservedSource(bytes);
        var image = await MfmEmuContainer.OpenAsync(source);
        source.Reads.Clear();
        var buffer = new byte[4];
        await image.ReadTrackBytesAsync(1, 1, 4, buffer);
        Assert.Equal(new byte[] { 0x67, 0x45, 0x23, 0x01 }, buffer);
        Assert.Equal(new[] { (image.Header.TrackOffset(1, 1), 12), (image.Header.TrackOffset(1, 1) + 20, 4) }, source.Reads);
    }

    [Fact]
    public async Task CopyPreservesHeaderExtensionsTrackExtensionsAndEveryMfmBit()
    {
        var bytes = Image();
        var image = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes));
        var output = new MemoryStream();
        try
        {
            await image.CopyToAsync(output);
            Assert.True(output.CanWrite);
            Assert.Equal(bytes, output.ToArray());
            var reopened = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(output.ToArray()));
            Assert.Equal(image.Header.Bytes.ToArray(), reopened.Header.Bytes.ToArray());
        }
        finally { await output.DisposeAsync(); }
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(9, 0)]
    [InlineData(8, 1)]
    public async Task WindowCannotCrossTheTrackBoundary(int offset, int count)
    {
        var image = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(Image()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => image.ReadTrackBytesAsync(0, 0, offset, new byte[count]).AsTask());
    }

    [Fact]
    public async Task ACorruptTrackIsRejectedBeforeDataOrCopyIsWritten()
    {
        var bytes = Image();
        Assert.True(MfmEmuHeader.TryDecode(bytes, out var header));
        bytes[(int)header.TrackOffset(1, 0)] = 0;
        var image = await MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes));
        var destination = new byte[] { 0xa5 };
        await Assert.ThrowsAsync<InvalidDataException>(() => image.ReadTrackBytesAsync(1, 0, 0, destination).AsTask());
        Assert.Equal(0xa5, destination[0]);
        var output = new MemoryStream();
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => image.CopyToAsync(output).AsTask());
            Assert.Equal(0, output.Length);
            Assert.True(output.CanWrite);
        }
        finally { await output.DisposeAsync(); }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(12)]
    public async Task IncorrectFileLengthIsRejected(int change)
    {
        var bytes = Image();
        Array.Resize(ref bytes, bytes.Length + change);
        await Assert.ThrowsAsync<InvalidDataException>(() => MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes)).AsTask());
    }

    [Fact]
    public async Task EndMarkerMustHaveBothNegativeCoordinatesAndItsSignature()
    {
        var bytes = Image();
        bytes[^1] = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes)).AsTask());
        bytes = Image();
        bytes[^12] = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => MfmEmuContainer.OpenAsync(new MemoryRandomAccessData(bytes)).AsTask());
    }

    [Fact]
    public async Task CancellationDoesNotConsumeOrDisposeBorrowedResources()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new ObservedSource(Image());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MfmEmuContainer.OpenAsync(source, cancellation.Token).AsTask());
        Assert.Empty(source.Reads);
        var image = await MfmEmuContainer.OpenAsync(source);
        source.Reads.Clear();
        var output = new MemoryStream();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => image.CopyToAsync(output, cancellation.Token).AsTask());
            Assert.Empty(source.Reads);
            Assert.Equal(0, output.Length);
            Assert.True(output.CanWrite);
        }
        finally { await output.DisposeAsync(); }
    }

    private static byte[] Image()
    {
        var original = MfmEmuHeader.Create(2, 2, 8, 10000000, 1234, "capture", "original");
        var headerBytes = new byte[original.HeaderLength + 4];
        original.Bytes.CopyTo(headerBytes);
        Write(headerBytes, 12, (uint)headerBytes.Length);
        Write(headerBytes, 20, 16);
        headerBytes[^1] = 0xa7;
        Assert.True(MfmEmuHeader.TryDecode(headerBytes, out var header));
        var bytes = new byte[(int)header.EndMarkerOffset + 12];
        headerBytes.CopyTo(bytes, 0);
        for (var cylinder = 0; cylinder < 2; cylinder++)
            for (var head = 0; head < 2; head++)
            {
                var track = (int)header.TrackOffset(cylinder, head);
                Write(bytes, track, 0x12345678);
                Write(bytes, track + 4, (uint)cylinder);
                Write(bytes, track + 8, (uint)head);
                Write(bytes, track + 12, 0xf1e2d3c4);
                Write(bytes, track + 16, 0x89abcdef);
                Write(bytes, track + 20, 0x01234567);
            }
        Write(bytes, (int)header.EndMarkerOffset, 0x12345678);
        Write(bytes, (int)header.EndMarkerOffset + 4, uint.MaxValue);
        Write(bytes, (int)header.EndMarkerOffset + 8, uint.MaxValue);
        return bytes;
    }

    private static void Write(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);

    private sealed class ObservedSource(byte[] bytes) : IMediaRandomAccessData
    {
        public List<(long Offset, int Count)> Reads { get; } = [];
        public long Length => bytes.Length;
        public ValueTask ReadExactlyAsync(long offset, Memory<byte> destination, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Reads.Add((offset, destination.Length));
            bytes.AsMemory(checked((int)offset), destination.Length).CopyTo(destination);
            return ValueTask.CompletedTask;
        }
    }
}
