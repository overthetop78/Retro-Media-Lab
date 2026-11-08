using System.IO;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Tape;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Images.Reading;
using MediaVolumeOrigins = RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins;
using RetroMediaLab.MediaFileSystems.Definitions;
using MediaVolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.Tests.Media;

public sealed class AtomTapMediaTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(65535)]
    public async Task NativeFilesRoundTripThroughCommonCodecs(int size)
    {
        var data = new byte[size]; new Random(42).NextBytes(data);
        var bytes = File("SAME", data).Concat(File("SAME", [1, 2, 3])).ToArray();
        var document = AtomTapReader.ReadBytes(new("memory.tap", []), bytes);
        var composition = SequentialMediaComposition.CreateDefault();
        var decoded = await composition.Decoders.DecodeAsync(document);
        Assert.Empty(decoded.Diagnostics); Assert.Empty(decoded.UndecodedSegments);
        Assert.Equal(2, decoded.Blocks.Count);
        Assert.Equal(data, decoded.Blocks[0].Data.ToArray());
        Assert.Equal("2900", decoded.Blocks[0].Metadata["loadAddress"]);
        Assert.All(decoded.Blocks, block => Assert.True(block.IntegrityValid));
        var volume = new MediaVolumeDescriptor(0, bytes.Length, MediaVolumeOrigins.SequentialContent, fileSystemId: FileSystemIds.SequentialContent);
        var explored = new SequentialContentDecoderAdapter(composition.Decoders).Read(document, volume);
        Assert.Equal(2, explored.Entries.Count);
        Assert.Equal(data, explored.Entries[0].Content);
        var encoded = await composition.Encoders.EncodeAsync(new(TapeImageFormatIds.AtomTap, "acorn-atom", decoded.Blocks));
        var rebuilt = MediaImageDocument.CreateUnexplored(new("memory.tap", []), TapeImageFormatIds.AtomTap,
            MediaKind.Tape, encoded, [], new Dictionary<string, string>());
        var output = new MemoryWriter();
        await new AtomTapWriter(output).WriteAsync(rebuilt, "copy.tap", TapeImageFormatIds.AtomTap);
        Assert.Equal(bytes, output.Bytes);
        Assert.NotEmpty(MediaWritingComposition.CreateDefault().Registry.FindTargetCandidates(TapeImageFormatIds.AtomTap, ".tap"));
    }

    [Fact]
    public async Task CorruptedChecksumRemainsVisibleAndCannotBeExportedAsValidTap()
    {
        var document = AtomTapReader.ReadBytes(new("memory.tap", []), File("TEST", [1, 2, 3]));
        var original = (SequentialMediaImageRepresentation)document.Representation;
        var segments = original.Segments!.ToArray();
        var damaged = new byte[] { 8, (byte)'N', 2, 1, 2, 3, 0 };
        var segment = segments[3];
        segments[3] = new(segment.Position, segment.Kind,
            dataRange: new(0, damaged.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(damaged)), metadata: segment.Metadata);
        var bad = MediaImageDocument.CreateUnexplored(document.Source, document.FormatId, MediaKind.Tape,
            new SequentialMediaImageRepresentation(null, segments: segments), [], new Dictionary<string, string>());
        var decoded = await SequentialMediaComposition.CreateDefault().Decoders.DecodeAsync(bad);
        Assert.False(Assert.Single(decoded.Blocks).IntegrityValid);
        Assert.NotEmpty(decoded.Diagnostics);
        await Assert.ThrowsAsync<InvalidDataException>(() => new AtomTapWriter(new MemoryWriter()).WriteAsync(bad, "copy.tap", TapeImageFormatIds.AtomTap));
    }

    [Fact]
    public void TruncatedNativeFilesAreRejected()
    {
        var bytes = File("TEST", [1, 2, 3]);
        Assert.False(AtomTapReader.Validate(bytes.AsSpan(0, bytes.Length - 1)));
        Assert.False(AtomTapReader.Validate([]));
    }

    private static byte[] File(string name, byte[] data)
    {
        var bytes = new byte[22 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(name, bytes);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16), 0x2900);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(18), 0xc2b2);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20), checked((ushort)data.Length));
        data.CopyTo(bytes, 22); return bytes;
    }
    private sealed class MemoryWriter : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken); Bytes = stream.ToArray();
        }
    }
}
