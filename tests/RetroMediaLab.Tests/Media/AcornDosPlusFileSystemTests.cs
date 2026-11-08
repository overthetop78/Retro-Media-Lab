using System.Buffers.Binary;
using System.Text;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaFileSystems.Exploration;
using RetroMediaLab.MediaFileSystems.FileSystems.Fat12;

namespace RetroMediaLab.Tests.Media;

public sealed class AcornDosPlusFileSystemTests
{
    [Fact]
    public void KeepsLegacyIbm512ByteSectorsAndTwoFatCopiesReadable()
    {
        var data = new byte[368640];
        data[0] = 0xeb;
        data[21] = 0xfd;
        foreach (var offset in new[] { 512, 1536 })
        {
            data[offset] = 0xfd; data[offset + 1] = data[offset + 2] = 0xff;
            SetFat(data.AsSpan(offset), 2, 0xfff);
        }
        Entry(data.AsSpan(5 * 512, 32), "TEST    TXT", 0, 2, 3);
        Encoding.ASCII.GetBytes("IBM").CopyTo(data, 12 * 512);
        var blocks = Enumerable.Range(0, 720).Select(block => new SectorBlock(block,
            new(block / 18, block / 9 % 2, block % 9 + 1), data.AsSpan(block * 512, 512).ToArray()));
        var image = new SectorImage(DiskImageFormatIds.Ibm360, 512, 40, 2, 9, blocks);
        var reader = new Fat12FileSystemReader();
        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Empty(volume.Warnings);
        var file = Assert.Single(volume.Entries);
        Assert.True(file.MetadataValid);
        Assert.Equal("IBM", Encoding.ASCII.GetString(file.Content!.ToArray()));
        Assert.Equal(353L * 1024, volume.FreeBytes);
    }

    [Fact]
    public async Task ExplorerReadsFragmentedFilesAndSubdirectoriesFromNative1024ByteSectors()
    {
        var image = await CreateImage();
        var registry = new SectorFileSystemRegistry();
        Assert.True(registry.TryRead(image, image.FormatId, out var match));
        var volume = match!.Volume;
        Assert.Equal("DOSPLUS", volume.Name);
        Assert.Equal(788L * 1024, volume.FreeBytes);
        Assert.Empty(volume.Warnings);
        var file = Assert.Single(volume.Entries, entry => entry.Name == "TEST.BIN");
        Assert.True(file.MetadataValid);
        Assert.Equal(Payload(), file.Content);
        var directory = Assert.Single(volume.Entries, entry => entry.Name == "SUBDIR");
        var child = Assert.Single(directory.Children);
        Assert.Equal("CHILD.TXT", child.Name);
        Assert.Equal("CHILD", Encoding.ASCII.GetString(child.Content!.ToArray()));
    }

    [Fact]
    public async Task MissingDataSectorProducesAnInvalidFileAndAWarning()
    {
        var original = await CreateImage();
        var image = new SectorImage(original.FormatId, 1024, 80, 2, 5,
            original.AvailableBlocks.Where(block => block.LogicalBlock != 13));
        var reader = new Fat12FileSystemReader();
        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.NotEmpty(volume.Warnings);
        Assert.False(Assert.Single(volume.Entries, entry => entry.Name == "TEST.BIN").MetadataValid);
    }

    [Fact]
    public async Task NativeFatIsNotMisinterpretedAsAnIbmBootSector()
    {
        var image = await CreateImage();
        var wrong = new SectorImage(DiskImageFormatIds.Ibm800, 1024, 80, 2, 5, image.AvailableBlocks);
        Assert.False(new Fat12FileSystemReader().CanRead(wrong));
        Assert.False(Fat12LayoutReader.TryRead(new byte[1024], 800, image.FormatId, out _));
    }

    private static byte[] Payload() => Enumerable.Range(0, 1500).Select(value => (byte)(value * 17)).ToArray();

    private static async Task<SectorImage> CreateImage()
    {
        var data = new byte[819200];
        data[0] = 0xfd; data[1] = data[2] = 0xff;
        SetFat(data, 2, 7); SetFat(data, 7, 0xfff);
        SetFat(data, 4, 0xfff); SetFat(data, 5, 0xfff);
        Entry(data.AsSpan(2048, 32), "DOSPLUS    ", 8, 0, 0);
        Entry(data.AsSpan(2080, 32), "TEST    BIN", 0, 2, 1500);
        Entry(data.AsSpan(2112, 32), "SUBDIR     ", 16, 4, 0);
        Entry(data.AsSpan(10 * 1024, 32), "CHILD   TXT", 0, 5, 5);
        var payload = Payload();
        payload.AsSpan(0, 1024).CopyTo(data.AsSpan(8 * 1024));
        payload.AsSpan(1024).CopyTo(data.AsSpan(13 * 1024));
        Encoding.ASCII.GetBytes("CHILD").CopyTo(data, 11 * 1024);
        return await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory.800");
    }

    private static void Entry(Span<byte> entry, string name, byte attributes, ushort cluster, uint size)
    {
        Encoding.ASCII.GetBytes(name, entry);
        entry[11] = attributes;
        BinaryPrimitives.WriteUInt16LittleEndian(entry[26..], cluster);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[28..], size);
    }

    private static void SetFat(Span<byte> bytes, int cluster, int value)
    {
        var offset = cluster * 3 / 2;
        var pair = BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]);
        pair = (ushort)((cluster & 1) == 0 ? pair & 0xf000 | value : pair & 0x000f | value << 4);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..], pair);
    }
}
