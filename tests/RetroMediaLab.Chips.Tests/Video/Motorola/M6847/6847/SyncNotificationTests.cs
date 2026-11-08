using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class SyncNotificationTests
{
    [Fact]
    public void FieldSyncNotificationsMatchEveryPhysicalEdgeWithoutDuplicates()
    {
        var chip = new Mc6847(new byte[280]);
        var edges = new List<(long Field, int Line, int HalfClock, bool Level)>();
        var received = chip.FieldSync;
        chip.FieldSyncChanged = level =>
        {
            Assert.NotEqual(received, level);
            Assert.Equal(chip.FieldSync, level);
            received = level;
            edges.Add((chip.Field, chip.Scanline, chip.HalfClock, level));
        };
        for (var edge = 0; edge < 2 * 456 * 262; edge++)
        {
            chip.SetClock(!chip.Clock);
            Assert.Equal(chip.FieldSync, received);
            chip.SetClock(chip.Clock);
        }
        Assert.Equal(new[] { (0L, 230, 0, false), (1L, 0, 0, true), (1L, 230, 0, false), (2L, 0, 0, true) }, edges);
    }

    [Fact]
    public void ResetPublishesOnlyTheChangedLevelAfterResettingItsTiming()
    {
        var chip = new Mc6847(new byte[280]);
        var edges = new List<bool>();
        chip.FieldSyncChanged = edges.Add;
        chip.Reset();
        Assert.Empty(edges);
        while (chip.FieldSync) chip.SetClock(!chip.Clock);
        chip.FieldSyncChanged = level =>
        {
            Assert.Equal(0, chip.Field);
            Assert.Equal(0, chip.Scanline);
            Assert.Equal(0, chip.HalfClock);
            Assert.True(chip.FieldSync);
            edges.Add(level);
        };
        chip.Reset();
        chip.Reset();
        Assert.Equal(new[] { false, true }, edges);
    }

    [Fact]
    public void ExistingVideoReceiverForwardsTheSameFieldSyncEdges()
    {
        var video = new AtomVideo(_ => 0, new byte[280]);
        var levels = new List<bool>();
        video.FrameSyncChanged = level =>
        {
            Assert.Equal(video.FrameSync, level);
            levels.Add(level);
        };
        while (!video.Tick()) { }
        Assert.Equal(new[] { false, true }, levels);
        Assert.Equal(1, video.Frame);
    }
}
