using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine.Images.Creation;
using MediaSector = RetroMediaLab.MediaEngine.Contracts.MediaDiskSector;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuFloppyFormattingTests
{
    [Theory]
    [InlineData("acorn.dfs.ds")]
    [InlineData("acorn.dfs.ds80")]
    public async Task DfsCreationAndReadingSeparatePhysicalSideFromRecordedHead(string format)
    {
        var created = BlankSectorImageFactory.Create(format);
        var reader = new BbcDfsReader((_, _) => Task.FromResult(new byte[created.Capacity]));
        var read = await reader.ReadAsync("memory.dsd");
        foreach (var image in new[] { created, read })
        {
            Assert.Contains(image.AvailableBlocks, block => block.Address.Head == 1);
            Assert.All(image.AvailableBlocks, block =>
            {
                Assert.NotNull(block.RecordedAddress);
                Assert.Equal(BbcDfsLayout.RecordedHead, block.RecordedAddress!.Head);
                Assert.Equal(block.Address.Cylinder, block.RecordedAddress.Cylinder);
                Assert.Equal(block.Address.Number, block.RecordedAddress.Number);
            });
        }
    }

    [Theory]
    [InlineData("acorn.dfs.ds")]
    [InlineData("acorn.dfs.ds80")]
    public void DfsSecondSideFormattingKeepsPhysicalLocationAndOtherSide(string format)
    {
        var original = BlankSectorImageFactory.Create(format);
        var fields = original.AvailableBlocks.Where(block => block.Address.Cylinder == 0 && block.Address.Head == 1)
            .Select(block => new MediaSector(block.LogicalBlock, Enumerable.Repeat((byte)0xe5, BbcDfsGeometry.SectorSize).ToArray(),
                block.Address.Cylinder, BbcDfsLayout.RecordedHead, block.Address.Number, true) { RecordedSizeCode = 1 }).ToArray();
        var updated = SectorTrackFunctions.Replace(original, 0, 1, fields, retainGeometry: true);
        Assert.Equal(original.Capacity, updated.Capacity);
        foreach (var block in updated.AvailableBlocks.Where(block => block.Address.Cylinder == 0 && block.Address.Head == 1))
        {
            Assert.Equal(BbcDfsLayout.RecordedHead, block.RecordedAddress!.Head);
            Assert.All(block.Data, value => Assert.Equal(0xe5, value));
        }
        foreach (var block in original.AvailableBlocks.Where(block => block.Address.Head == 0))
            Assert.Equal(block.Data, updated.AvailableBlocks.Single(candidate => candidate.LogicalBlock == block.LogicalBlock).Data);
        var invalid = fields.ToArray();
        invalid[0] = invalid[0] with { Head = 1 };
        Assert.Throws<NotSupportedException>(() => SectorTrackFunctions.Replace(original, 0, 1, invalid, retainGeometry: true));
    }

    [Fact]
    public void AGeometryPreservingImageAcceptsCanonicalFieldsWithoutTouchingOtherTracks()
    {
        var original = new SectorImage(CpcDskFormat.FormatId, 512, 2, 1, 3,
            Enumerable.Range(0, 6).Select(index => new SectorBlock(index, new(index / 3, 0, index % 3),
                Enumerable.Repeat((byte)(0x11 + index), 512).ToArray())));
        var replacement = Enumerable.Range(0, 3).Select(index => new MediaSector(index,
            Enumerable.Repeat((byte)0xa5, 512).ToArray(), 0, 0, index, true) { RecordedSizeCode = 2 }).ToArray();
        var updated = SectorTrackFunctions.Replace(original, 0, 0, replacement, retainGeometry: true);
        Assert.Equal(original.Capacity, updated.Capacity);
        Assert.Equal(original.BlockCount, updated.BlockCount);
        Assert.All(updated.AvailableBlocks.Where(block => block.Address.Cylinder == 0).SelectMany(block => block.Data), value => Assert.Equal(0xa5, value));
        foreach (var block in original.AvailableBlocks)
        {
            Assert.All(block.Data, value => Assert.Equal((byte)(0x11 + block.LogicalBlock), value));
            if (block.Address.Cylinder == 1)
                Assert.Equal(block.Data, updated.AvailableBlocks.Single(other => other.LogicalBlock == block.LogicalBlock).Data);
        }
        var invalid = replacement.ToArray();
        invalid[^1] = invalid[^1] with { Number = invalid[0].Number };
        Assert.Throws<NotSupportedException>(() => SectorTrackFunctions.Replace(original, 0, 0, invalid, retainGeometry: true));
        invalid = replacement.ToArray();
        invalid[^1] = invalid[^1] with { Cylinder = 9 };
        Assert.Throws<NotSupportedException>(() => SectorTrackFunctions.Replace(original, 0, 0, invalid, retainGeometry: true));
    }

}
