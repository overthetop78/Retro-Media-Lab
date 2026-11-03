using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using MediaSector = RetroMediaLab.MediaEngine.Contracts.MediaDiskSector;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuFloppyFormattingTests
{
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
