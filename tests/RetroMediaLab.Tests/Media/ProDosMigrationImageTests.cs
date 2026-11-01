using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.MediaEngine.Contracts.Migration;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaFileSystems.Definitions;

namespace RetroMediaLab.Tests.Media;

public sealed class ProDosMigrationImageTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.AppleIIProDos140)]
    [InlineData(DiskImageFormatIds.AppleIIProDos800)]
    public void MigratedFileCanBeReadFromProDosImage(string formatId)
    {
        var image = new FileSystemMigrationService().CreateImage(CreateSource(), formatId).Image;
        AssertFile(image, FileSystemIds.ProDos);
    }

    [Fact]
    public void MigratedFileCanBeReadFromSosImage()
    {
        var image = new FileSystemMigrationService().CreateImage(CreateSource(), DiskImageFormatIds.AppleIIISos).Image;
        AssertFile(image, FileSystemIds.Sos);
    }

    private static FileSystemVolume CreateSource() => new("VOLUME", FileSystemIds.Fat12, 0, 0, null, null, [new FileSystemEntry("HELLO", FileSystemEntryKind.File, 3, null, string.Empty, 0, 0, true, [], new byte[] { 1, 2, 3 })], []);

    private static void AssertFile(RetroMediaLab.MediaEngine.Images.Models.Sectors.SectorImage image, string expectedFileSystemId)
    {
        var reader = Assert.Single(RetroMediaLab.MediaFileSystems.Exploration.FileSystemReaderCatalog.CreateDefault(), item => item.Id == FileSystemIds.ProDos);
        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Equal(expectedFileSystemId, volume.FileSystemId);
        var file = Assert.Single(volume.Entries);
        Assert.Equal("HELLO", file.Name);
        var content = Assert.IsAssignableFrom<IReadOnlyList<byte>>(file.Content);
        Assert.Equal(new byte[] { 1, 2, 3 }, content);
    }
}
