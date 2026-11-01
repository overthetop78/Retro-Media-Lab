using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.MediaEngine.Contracts.Migration;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaFileSystems.FileSystems.Apple.Dos;
using MediaFileSystemIds = RetroMediaLab.MediaFileSystems.Definitions.FileSystemIds;

namespace RetroMediaLab.Tests.Media;

public sealed class AppleDosMigrationImageTests
{
    [Fact]
    public void InjectedFileCanBeReadFromTheCreatedDosImage()
    {
        var source = new FileSystemVolume("DOS-001", MediaFileSystemIds.Fat12, 0, 0, null, null, [new FileSystemEntry("HELLO", FileSystemEntryKind.File, 3, null, string.Empty, 0, 0, true, [], new byte[] { 1, 2, 3 })], []);
        var result = new FileSystemMigrationService().CreateImage(source, DiskImageFormatIds.AppleIIAppleDos140);
        Assert.True(result.Report.CanExecute);
        var image = result.Image;
        var reader = new AppleDosFileSystemReader();

        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Equal("DOS-001", volume.Name);
        Assert.Equal(MediaFileSystemIds.AppleDos, volume.FileSystemId);
        var file = Assert.Single(volume.Entries);
        Assert.Equal("HELLO", file.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.Content);
    }
}
