using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.MediaEngine.Contracts.Migration;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaFileSystems.Definitions;

namespace RetroMediaLab.Tests.Media;

public sealed class CommodoreDosMigrationImageTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.Commodore1541)]
    [InlineData(DiskImageFormatIds.Commodore1571)]
    [InlineData(DiskImageFormatIds.Commodore1581)]
    public void MigratedFileCanBeReadFromCommodoreDosImage(string formatId)
    {
        var source = new FileSystemVolume("VOLUME", FileSystemIds.Fat12, 0, 0, null, null, [new FileSystemEntry("HELLO", FileSystemEntryKind.File, 3, null, string.Empty, 0, 0, true, [], new byte[] { 1, 2, 3 })], []);
        var result = new FileSystemMigrationService().CreateImage(source, formatId);
        Assert.True(result.Report.CanExecute);
        var image = result.Image;
        var reader = Assert.Single(RetroMediaLab.MediaFileSystems.Exploration.FileSystemReaderCatalog.CreateDefault(), item => item.Id == FileSystemIds.CommodoreDos);
        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Equal("VOLUME", volume.Name);
        var file = Assert.Single(volume.Entries);
        Assert.Equal("HELLO", file.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsAssignableFrom<IReadOnlyList<byte>>(file.Content));
    }
}
