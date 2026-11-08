using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Detection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Conversion.Acorn;

namespace RetroMediaLab.Tests.Media;

public sealed class AcornDosPlus800Tests
{
    [Theory]
    [InlineData("acorn.dosplus.800", ".img", true)]
    [InlineData("ACORN.DOSPLUS.800", ".IMG", true)]
    [InlineData("acorn.dosplus.800", ".800", true)]
    [InlineData("ACORN.DOSPLUS.800", ".800", true)]
    [InlineData("acorn.dosplus.800", ".adf", false)]
    [InlineData("ACORN.DOSPLUS.800", ".ADF", false)]
    public void OutputSelectionKeepsThePhysicalFormatRegardlessOfCase(string format, string extension, bool expected)
        => Assert.Equal(expected, AcornAdfConversionService.CanCreate(format, extension));

    [Theory]
    [InlineData("acorn.dosplus.800", 819200, true)]
    [InlineData("ACORN.DOSPLUS.800", 819200, true)]
    [InlineData("acorn.dosplus.800", 655360, false)]
    [InlineData("ACORN.DOSPLUS.800", 655360, false)]
    public async Task ChecksTheExplicitGeometryRegardlessOfCase(string format, int size, bool expected)
    {
        IMediaImageReader reader = new AdfReader();
        var context = new MediaRecognitionContext(new MediaSourceDescriptor("memory", [], size, format));
        Assert.Equal(expected, await reader.CanReadAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task EmptyCreatedMediaRemainsIdentifiableAfterReopeningItsRawBytes()
    {
        var blank = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornDosPlus800);
        var output = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(blank, "memory");
        Assert.Equal(new byte[] { 0xfd, 0xff, 0xff }, output.Bytes[..3]);
        Assert.All(output.Bytes[3..], value => Assert.Equal(0, value));
        var reopened = await new AdfReader((_, _) => Task.FromResult(output.Bytes)).ReadAsync("memory");
        Assert.Equal(DiskImageFormatIds.AcornDosPlus800, reopened.FormatId);
        Assert.True(reopened.TryGetBlock(0, out var first));
        Assert.Equal(1, first.Address.Number);
    }

    [Fact]
    public async Task ProducesThePhysicalMfmTrackUsedByTheFloppyController()
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        data[0] = 0xfd; data[1] = data[2] = 0xff;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        var encoder = new SectorImageTrackEncoder();
        Assert.True(encoder.CanEncode(image));
        var track = encoder.EncodePhysicalTrack(image, 0, 0, 200_000_000);
        Assert.Equal("iso.mfm", track.EncoderId);
        Assert.Equal(100_000, track.Bits.Count);
        Assert.Equal(200_000_000u, track.Revolution.IndexTimeTicks);
        Assert.NotEmpty(track.Revolution.FluxIntervals);
    }

    [Fact]
    public async Task WritesTheOriginalPayloadWithoutAddingASectorOrBootBlock()
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        new Random(512).NextBytes(data);
        data[0] = 0xfd; data[1] = data[2] = 0xff;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        var output = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(image, "memory");
        Assert.Equal(data, output.Bytes);
    }

    private sealed class MemoryWriter : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken);
            Bytes = stream.ToArray();
        }
    }

    [Theory]
    [InlineData(".img")]
    [InlineData(".800")]
    [InlineData(".adf")]
    public void DetectsTheDosPlusFatBeforeCapacityBasedFormats(string extension)
    {
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(),
            _ => new MemoryStream([0xfd, 0xff, 0xff], writable: false));
        Assert.Equal(DiskImageFormatIds.AcornDosPlus800,
            detector.Detect("memory" + extension, AcornAdfGeometry.Capacity).Format!.Id);
    }

    [Theory]
    [InlineData(".img", "ibm.800")]
    [InlineData(".adf", "acorn.adfs.800")]
    public void KeepsOther800KFormatsWhenTheDosPlusHeaderIsAbsent(string extension, string expected)
    {
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(),
            _ => new MemoryStream([0, 0, 0], writable: false));
        Assert.Equal(expected, detector.Detect("memory" + extension, AcornAdfGeometry.Capacity).Format!.Id);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task PreservesPayloadAndUsesThePhysicalNumberingOfEachFormat(bool dosPlus, int firstSector)
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        if (dosPlus) { data[0] = 0xfd; data[1] = data[2] = 0xff; }
        for (int block = 1; block < 800; block++) data[block * 1024] = (byte)block;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        Assert.Equal(dosPlus ? DiskImageFormatIds.AcornDosPlus800 : DiskImageFormatIds.AcornAdfs800, image.FormatId);
        Assert.Equal(1024, image.BlockSize);
        Assert.Equal(5, image.SectorsPerTrack);
        foreach (var (block, cylinder, head, sector) in new[]
        {
            (0, 0, 0, firstSector), (4, 0, 0, firstSector + 4),
            (5, 0, 1, firstSector), (10, 1, 0, firstSector), (799, 79, 1, firstSector + 4)
        })
        {
            Assert.True(image.TryGetBlock(block, out var found));
            Assert.Equal(cylinder, found.Address.Cylinder);
            Assert.Equal(head, found.Address.Head);
            Assert.Equal(sector, found.Address.Number);
            Assert.Equal(data.AsSpan(block * 1024, 1024).ToArray(), found.Data.ToArray());
        }
    }
}
