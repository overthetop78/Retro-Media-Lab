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
using RetroMediaLab.MediaEngine.Images.Conversion.Sequential;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.Uef;
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
        var payloadIndex = Array.FindLastIndex(segments, item => item.Kind == SequentialSegmentKind.DataBlock);
        var segment = segments[payloadIndex];
        segments[payloadIndex] = new(segment.Position, segment.Kind,
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

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public async Task RecordedSignalAndNativeTapConvertBothWays(int sampleRate)
    {
        var bytes = File("SIGNAL", Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
        var source = AtomTapReader.ReadBytes(new("memory.tap", []), bytes);
        var sequential = SequentialMediaComposition.CreateDefault();
        var writing = MediaWritingComposition.CreateDefault();
        var service = new SequentialMediaConversionService(MediaRecognitionComposition.CreateDefault().ReadingService,
            sequential.Decoders, sequential.Encoders, writing.Registry, writing.WritingService);
        var wave = await service.PlanAsync(source, TapeImageFormatIds.Wav, "acorn-atom", new Dictionary<string, string> { ["sampleRate"] = sampleRate.ToString() });
        Assert.Equal(sampleRate.ToString(), wave.TargetDocument.Metadata["sampleRate"]);
        var back = await service.PlanAsync(wave.TargetDocument, TapeImageFormatIds.AtomTap, "acorn-atom");
        Assert.Contains(back.Losses, loss => loss.Contains("source signal timing"));
        var recordedToUef = await service.PlanAsync(wave.TargetDocument, TapeImageFormatIds.Uef, "acorn-atom");
        Assert.Contains(recordedToUef.Losses, loss => loss.Contains("standard reconstruction"));
        var output = new MemoryWriter();
        await new AtomTapWriter(output).WriteAsync(back.TargetDocument, "copy.tap", TapeImageFormatIds.AtomTap);
        Assert.Equal(bytes, output.Bytes);
        var uef = await service.PlanAsync(source, TapeImageFormatIds.Uef, "acorn-atom");
        await new UefWriter(output).WriteAsync(uef.TargetDocument, "copy.uef", TapeImageFormatIds.Uef);
        Assert.Equal(UefConstants.BaudRate, BinaryPrimitives.ReadUInt16LittleEndian(output.Bytes.AsSpan(UefConstants.HeaderSize)));
        Assert.Equal(300, BinaryPrimitives.ReadUInt16LittleEndian(output.Bytes.AsSpan(UefConstants.HeaderSize + UefConstants.ChunkHeaderSize)));
        var fromUef = await service.PlanAsync(uef.TargetDocument, TapeImageFormatIds.AtomTap, "acorn-atom");
        await new AtomTapWriter(output).WriteAsync(fromUef.TargetDocument, "copy.tap", TapeImageFormatIds.AtomTap);
        Assert.Equal(bytes, output.Bytes);
    }

    [Fact]
    public async Task UnknownSerialBytesProduceADiagnosticWithoutInventingTapeBlocks()
    {
        var decoded = await AtomTapCodec.DecodeStreamAsync([1, 2, 3, 4], CancellationToken.None);
        Assert.Empty(decoded.Blocks);
        Assert.Empty(decoded.UndecodedSegments);
        Assert.Contains(decoded.Diagnostics, diagnostic => diagnostic.Contains("Unrecognized bytes"));
    }

    [Fact]
    public async Task IncompatibleUefFramingIsRetainedInsteadOfSilentlyDropped()
    {
        byte[] bytes = [8, (byte)'N', 0, 42];
        var segment = new SequentialMediaSegment(0, SequentialSegmentKind.DataBlock,
            dataRange: new(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes)),
            metadata: new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = "0104" });
        var document = MediaImageDocument.CreateUnexplored(new("memory.uef", []), TapeImageFormatIds.Uef, MediaKind.Tape,
            new SequentialMediaImageRepresentation(null, segments: [segment]), [], new Dictionary<string, string>());
        var decoded = await SequentialMediaComposition.CreateDefault().Decoders.DecodeAsync(document, "acorn-atom");
        Assert.Empty(decoded.Blocks);
        Assert.Same(segment, Assert.Single(decoded.UndecodedSegments));
        Assert.NotEmpty(decoded.Diagnostics);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UefAcceptsOneStopAndHistoricalTwoStopFraming(int stopBits)
    {
        byte[] expected = [0, 42, 255];
        var native = AtomTapReader.ReadBytes(new("memory.tap", []), File("P", expected));
        var original = (SequentialMediaImageRepresentation)native.Representation;
        var segments = new List<SequentialMediaSegment>();
        foreach (var segment in original.Segments!)
        {
            if (segment.Kind != SequentialSegmentKind.DataBlock) { segments.Add(segment); continue; }
            var bytes = (await RetroMediaLab.MediaEngine.Images.Reading.Blocks.BlockMediaDataReader.ReadRangeAsync(segment.DataRange, CancellationToken.None))!;
            Assert.Equal(1, bytes[2]); // The canonical reconstruction follows Acorn §19.
            bytes[2] = (byte)stopBits;
            segments.Add(new(segment.Position, segment.Kind, dataRange: new(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes)), metadata: segment.Metadata));
        }
        var uef = MediaImageDocument.CreateUnexplored(new("memory.uef", []), TapeImageFormatIds.Uef, MediaKind.Tape,
            new SequentialMediaImageRepresentation(null, segments: segments), [], new Dictionary<string, string>());
        var decoded = await SequentialMediaComposition.CreateDefault().Decoders.DecodeAsync(uef, "acorn-atom");
        var block = Assert.Single(decoded.Blocks);
        Assert.True(block.IntegrityValid); Assert.Equal(expected, block.Data.ToArray());
        Assert.Empty(decoded.Diagnostics); Assert.Empty(decoded.UndecodedSegments);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RomRecordedBlockIncludesSyncBytesInChecksum(bool corrupt)
    {
        // Actual SAVE "P" serial bytes, selected Acorn BASIC/OS ROM,
        // 20261111-final-tape-base-035329770; no encoder-under-test constructs this fixture.
        var serial = Convert.FromHexString("2A2A2A2A500D4500002AC2B229000D000A203F23334246463D34320D0014205052494E54224D454449412050415353220D001E20454E440DFF45");
        var expected = serial.AsSpan(14, 43).ToArray();
        if (corrupt) serial[20] ^= 1;
        var result = await AtomTapCodec.DecodeStreamAsync(serial, CancellationToken.None);
        var block = Assert.Single(result.Blocks);
        Assert.Equal(!corrupt, block.IntegrityValid);
        Assert.Equal("P", block.Metadata["fileName"]);
        Assert.Equal("2900", block.Metadata["loadAddress"]);
        if (!corrupt) { Assert.Equal(expected, block.Data.ToArray()); Assert.Empty(result.Diagnostics); }
        else Assert.NotEmpty(result.Diagnostics);
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
