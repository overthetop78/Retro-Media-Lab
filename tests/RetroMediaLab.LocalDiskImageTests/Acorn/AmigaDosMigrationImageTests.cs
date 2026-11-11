using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.MediaEngine.Contracts.Migration;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaFileSystems.FileSystems.Amiga;
using MediaFileSystemIds = RetroMediaLab.MediaFileSystems.Definitions.FileSystemIds;
using System.IO;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Detection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Interfaces;
using MigrationPlan = RetroMediaLab.MediaFileSystems.Migration.MigrationPlan;
using MigrationEntry = RetroMediaLab.MediaFileSystems.Migration.MigrationEntry;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Images.Reading;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AmigaDosMigrationImageTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.AmigaDos, 901120, 11)]
    [InlineData(DiskImageFormatIds.AmigaDosHighDensity, 1802240, 22)]
    public async Task AmigaAdfRecognitionWritingAndExplorationRemainSeparateFromAcorn(string format, int length, int sectorsPerTrack)
    {
        var plan = new MigrationPlan(MediaFileSystemIds.Fat12, MediaFileSystemIds.AmigaDosFfs, "AMIGA",
            [new MigrationEntry("HELLO", "HELLO", RetroMediaLab.MediaFileSystems.FileSystemEntryKind.File, new byte[] { 1, 2, 3 }, null, "", 0, true, [])]);
        var written = new AmigaDosVolumeWriter().Create(plan, AmigaDosVariant.Ffs, new(format, 512, 80, 2, sectorsPerTrack, length / 512));
        var bytes = written.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(), _ => new MemoryStream(bytes, writable: false));
        Assert.Equal(format, detector.Detect("amiga.adf", bytes.Length).Format!.Id);
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("amiga.adf");
        Assert.Equal(format, image.FormatId);
        Assert.Equal(512, image.BlockSize);
        Assert.Equal(sectorsPerTrack, image.SectorsPerTrack);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("amiga.adf", []), image);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(document);
        Assert.Contains(destinations, item => item.FormatId == format && item.Extension == ".adf");
        Assert.All(destinations.Where(item => item.WriterId == MediaImageWriterIds.AmigaAdf), item => Assert.Equal(format, item.FormatId));
        Assert.DoesNotContain(destinations, item => item.WriterId == MediaImageWriterIds.AcornAdf);
        var volume = new AmigaDosFileSystemReader().Read(image);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.Single(volume.Entries).Content);
        var output = new MemoryWriter();
        await new AmigaAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(image, "roundtrip.adf");
        Assert.Equal(bytes, output.Bytes);
        var reopened = await new AdfReader((_, _) => Task.FromResult(output.Bytes)).ReadAsync("roundtrip.adf");
        Assert.Equal(format, reopened.FormatId);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.Single(new AmigaDosFileSystemReader().Read(reopened).Entries).Content);
    }

    [Fact]
    public async Task AmigaDdCapacityKeepsItsExistingPriorityOverAcornPartialRecognition()
    {
        // A recognisable F-format prefix must not take over the previously recognised Amiga DD size.
        var bytes = AcornStorageImageTests.CreateFFormatBytes(true)[..AmigaAdfGeometry.DoubleDensityCapacity];
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(), _ => new MemoryStream(bytes, writable: false));
        Assert.Equal(DiskImageFormatIds.AmigaDos, detector.Detect("size.adf", bytes.Length).Format!.Id);
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("size.adf");
        Assert.Equal(DiskImageFormatIds.AmigaDos, image.FormatId);
        Assert.Equal(512, image.BlockSize);
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

    [Fact]
    public void InjectedFileCanBeReadFromTheCreatedAdfImage()
    {
        var source = new FileSystemVolume("TEST", MediaFileSystemIds.Fat12, 0, 0, null, null, [new FileSystemEntry("HELLO", FileSystemEntryKind.File, 3, null, string.Empty, 0, 0, true, [], new byte[] { 1, 2, 3 })], []);
        var result = new FileSystemMigrationService().CreateImage(source, DiskImageFormatIds.AmigaDos);
        Assert.True(result.Report.CanExecute);
        var image = result.Image;
        var reader = new AmigaDosFileSystemReader();

        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Equal("TEST", volume.Name);
        Assert.Equal(MediaFileSystemIds.AmigaDosFfs, volume.FileSystemId);
        var file = Assert.Single(volume.Entries);
        Assert.Equal("HELLO", file.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.Content);
    }
}
