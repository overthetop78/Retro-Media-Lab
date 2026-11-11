using System.IO;
using System.IO.Compression;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaFileSystems.Exploration;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.Tests.Media;

public sealed class JfdMediaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeExportPreservesHeadersTablesErrorsAndObey(bool gzip)
    {
        var bytes = Native();
        var input = gzip ? Compress(bytes) : bytes;
        var document = JfdReader.ReadBytes(new("memory.jfd", [], input.Length), input);
        var image = Assert.IsType<SectorMediaImageRepresentation>(document.Representation).Image;
        Assert.Equal(128, image.BlockSize);
        Assert.False(Assert.Single(image.AvailableBlocks).HeaderIntegrityValid);
        Assert.True(Assert.Single(image.AvailableBlocks).DataIntegrityValid);
        var writer = new MemoryWriter();
        await new JfdWriter(writer).WriteAsync(document, "copy.jfd", DiskImageFormatIds.Jfd);
        Assert.Equal(bytes, JfdCodec.Unpack(writer.Bytes));
        Assert.NotEmpty(MediaWritingComposition.CreateDefault().Registry.FindTargetCandidates(DiskImageFormatIds.Jfd, ".jfd"));
        Assert.Contains(".jfd", MediaRecognitionComposition.CreateDefault().SupportedExtensions);
        Assert.True(await new JfdReader((_, _) => Task.FromResult(input)).CanReadAsync(new(new("memory.jfd", [], input.Length)), CancellationToken.None));
    }

    [Fact]
    public async Task EditsUpdateDataAndPreserveNativeInformation()
    {
        var bytes = Native(); var document = JfdReader.ReadBytes(new("memory.jfd", []), bytes);
        var image = ((SectorMediaImageRepresentation)document.Representation).Image;
        var block = Assert.Single(image.AvailableBlocks) with { Data = Enumerable.Repeat((byte)0xa5, 128).ToArray() };
        var edited = new SectorImage(image.FormatId, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack, [block]);
        var changed = MediaImageDocument.CreateUnexplored(document.Source, document.FormatId, MediaKind.Floppy,
            new SectorMediaImageRepresentation(edited), document.Diagnostics, document.Metadata);
        var output = new MemoryWriter();
        await new JfdWriter(output).WriteAsync(changed, "copy.jfd", DiskImageFormatIds.Jfd);
        var decoded = JfdCodec.Decode(output.Bytes);
        Assert.All(Assert.Single(decoded.Sectors.AvailableBlocks).Data, value => Assert.Equal(0xa5, value));
        Assert.Equal(bytes.AsSpan(0, 336).ToArray(), decoded.Bytes.AsSpan(0, 336).ToArray());
        Assert.Equal(bytes[^4..], decoded.Bytes[^4..]);
    }

    [Fact]
    public void ErrorStubIsNotInventedReadableData()
    {
        var bytes = Native(); Put(bytes, 328, uint.MaxValue);
        var decoded = JfdCodec.Decode(bytes);
        Assert.Empty(decoded.Sectors.AvailableBlocks);
        Assert.Null(Assert.Single(decoded.Records).Offset);
        Assert.Equal(bytes, JfdCodec.Rewrite(decoded, decoded.Sectors));
    }

    [Fact]
    public void TruncatedTablesAndDataAreRejected()
    {
        var bytes = Native(); Put(bytes, 328, 1000);
        Assert.Throws<InvalidDataException>(() => JfdCodec.Decode(bytes));
        bytes = Native(); Put(bytes, 24, 325);
        Assert.Throws<InvalidDataException>(() => JfdCodec.Decode(bytes));
    }

    [Fact]
    public void DeltaTrackOverridesTheBaseWithoutDestroyingItsHistory()
    {
        var original = Native(); var bytes = new byte[original.Length + 144]; original.CopyTo(bytes, 0);
        var dt = original.Length; var ds = dt + 4; var dd = ds + 12;
        Put(bytes, 8, (uint)bytes.Length); Put(bytes, 36, (uint)dt); Put(bytes, 40, (uint)ds); Put(bytes, 44, (uint)dd);
        Put(bytes, dt, 0); Put(bytes, ds, 0xff020010); Put(bytes, ds + 4, 0); Put(bytes, ds + 8, uint.MaxValue);
        Array.Fill(bytes, (byte)0x77, dd, 128);
        var decoded = JfdCodec.Decode(bytes);
        Assert.All(Assert.Single(decoded.Sectors.AvailableBlocks).Data, value => Assert.Equal(0x77, value));
        Assert.Equal(bytes, JfdCodec.Rewrite(decoded, decoded.Sectors));
    }

    [Fact]
    public void NativeSectorDensitySelectsTheExistingEncoder()
    {
        var decoded = JfdCodec.Decode(Native());
        var encoder = new SectorImageTrackEncoder();
        Assert.True(encoder.CanEncode(decoded.Sectors));
        Assert.NotEmpty(encoder.Encode(decoded.Sectors));
    }

    [Theory]
    [InlineData("acorn.dfs.ss", 1)]
    [InlineData("ibm.360", 2)]
    public async Task NewImagesCanBeCreatedFromDecodedSectorsWithoutAnExistingJfd(string format, byte density)
    {
        var blocks = Enumerable.Range(0, 2).Select(cylinder => new SectorBlock(cylinder, new(cylinder, 0, 1),
            Enumerable.Repeat((byte)(10 + cylinder), 256).ToArray()) { HeaderIntegrityValid = true, DataIntegrityValid = true }).ToArray();
        var image = new SectorImage(format, 256, 2, 1, 1, blocks);
        var document = MediaImageDocument.CreateUnexplored(new("captured.scp", []), format, MediaKind.Floppy,
            new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var output = new MemoryWriter();
        await new JfdWriter(output).WriteAsync(document, "created.jfd", DiskImageFormatIds.Jfd);
        var decoded = JfdCodec.Decode(output.Bytes);
        // The native table contains at least 160 side slots. Empty trailing
        // tracks remain represented, without fabricating their sector data.
        Assert.Equal(1, decoded.Sectors.Heads); Assert.Equal(80, decoded.Sectors.Cylinders);
        Assert.Equal(80, decoded.Sectors.BlockCount);
        Assert.Equal(2, decoded.Sectors.AvailableBlocks.Count);
        for (var absent = 2; absent < decoded.Sectors.BlockCount; absent++)
            Assert.False(decoded.Sectors.TryGetBlock(absent, out _));
        Assert.All(decoded.Sectors.AvailableBlocks, block => Assert.Equal(density, block.FormatCode));
        foreach (var block in decoded.Sectors.AvailableBlocks) Assert.Equal(blocks[block.LogicalBlock].Data, block.Data);
    }

    [Fact]
    public async Task CommonConversionsPrepareFluxForWritingAndRecoverJfdSectors()
    {
        var bytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var image = new SectorImage("ibm.360", 256, 1, 1, 1, [new(0, new(0, 0, 1), bytes)]);
        var source = MediaImageDocument.CreateUnexplored(new("source.img", []), image.FormatId, MediaKind.Floppy,
            new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var engine = MediaEngineComposition.CreateDefault();
        var toFlux = engine.Conversion.Registry.Resolve(source, DiskImageFormatIds.RawHxcStream, MediaRepresentationKind.Flux);
        Assert.NotNull(toFlux);
        var flux = await toFlux.ConvertAsync(source, DiskImageFormatIds.RawHxcStream, MediaRepresentationKind.Flux, new Dictionary<string, string>());
        Assert.NotNull(engine.Visualization.Registry.CreateDescriptor(flux.Document));
        var toSectors = engine.Conversion.Registry.Resolve(flux.Document, DiskImageFormatIds.Jfd, MediaRepresentationKind.Sectors);
        Assert.NotNull(toSectors);
        var sectors = await toSectors.ConvertAsync(flux.Document, DiskImageFormatIds.Jfd, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        var output = new MemoryWriter();
        await new JfdWriter(output).WriteAsync(sectors.Document, "captured.jfd", DiskImageFormatIds.Jfd);
        Assert.Equal(bytes, Assert.Single(JfdCodec.Decode(output.Bytes).Sectors.AvailableBlocks).Data);
    }

    [Fact]
    public void DosPlusFilesRemainExplorableInsideANativeJfd()
    {
        var bytes = new byte[800 * 1024];
        bytes[0] = 0xfd; bytes[1] = 0xff; bytes[2] = 0xff; bytes[3] = 0xff; bytes[4] = 0x0f;
        var root = 2048; System.Text.Encoding.ASCII.GetBytes("HELLO   TXT", bytes.AsSpan(root));
        bytes[root + 11] = 0x20; bytes[root + 26] = 2; bytes[root + 28] = 5;
        "12345"u8.CopyTo(bytes.AsSpan(8192));
        var blocks = Enumerable.Range(0, 800).Select(at => new SectorBlock(at, new(at / 10, at / 5 % 2, at % 5 + 1), bytes.AsSpan(at * 1024, 1024).ToArray()));
        var image = new SectorImage(DiskImageFormatIds.AcornDosPlus800, 1024, 80, 2, 5, blocks);
        var native = JfdCodec.Decode(JfdCodec.Create(image));
        var report = new SectorFileSystemRegistry().ReadAll(native.Sectors);
        var file = Assert.Single(Assert.Single(report.Matches).Volume.Entries);
        Assert.Equal("HELLO.TXT", file.Name); Assert.Equal("12345"u8.ToArray(), file.Content);
    }

    [Fact]
    public void NewJfdCannotSilentlyDiscardIncompatibleRecordedFields()
    {
        var block = new SectorBlock(0, new(0, 0, 1), new byte[256]);
        foreach (var invalid in new[] { block with { DeletedDataMark = true }, block with { RecordedSizeCode = 3 }, block with { Address = new(0, 1, 1) } })
            Assert.False(JfdCodec.CanCreate(new SectorImage("ibm.360", 256, 1, 1, 1, [invalid])));
    }

    [Fact]
    public void SectorEditsUpdateTheNativeCrcFlagsWithoutRewritingOtherHeaderBits()
    {
        var native = JfdCodec.Decode(Native());
        var block = Assert.Single(native.Sectors.AvailableBlocks) with { HeaderIntegrityValid = true, DataIntegrityValid = false };
        var image = new SectorImage(DiskImageFormatIds.Jfd, 128, 1, 1, 1, [block]);
        var rewritten = JfdCodec.Rewrite(native, image);
        Assert.Equal(0xff020020u, BinaryPrimitives.ReadUInt32LittleEndian(rewritten.AsSpan(324)));
        var decoded = Assert.Single(JfdCodec.Decode(rewritten).Sectors.AvailableBlocks);
        Assert.True(decoded.HeaderIntegrityValid);
        Assert.False(decoded.DataIntegrityValid);
    }

    private static byte[] Native()
    {
        var bytes = new byte[336 + 128 + 4];
        "JFDI"u8.CopyTo(bytes); Put(bytes, 4, 204); Put(bytes, 8, (uint)bytes.Length);
        Put(bytes, 24, 320); Put(bytes, 28, 324); Put(bytes, 32, 336);
        Put(bytes, 304, 0x00010101); Put(bytes, 312, 4);
        Put(bytes, 324, 0xff020010); Put(bytes, 328, 0); Put(bytes, 332, uint.MaxValue);
        Array.Fill(bytes, (byte)0x4d, 336, 128);
        "OBEY"u8.CopyTo(bytes.AsSpan(464)); return bytes;
    }
    private static void Put(byte[] bytes, int at, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), value);
    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var compressed = new GZipStream(output, CompressionLevel.Optimal, true)) compressed.Write(bytes);
        return output.ToArray();
    }
    private sealed class MemoryWriter : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(); await write(stream, cancellationToken); Bytes = stream.ToArray();
        }
    }
}
