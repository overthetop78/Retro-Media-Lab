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
