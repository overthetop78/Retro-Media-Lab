using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class SyncNotificationTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845, 0)]
    [InlineData(Crtc6845Model.Mc6845, 1)]
    [InlineData(Crtc6845Model.Mc6845, 3)]
    [InlineData(Crtc6845Model.Hd6845S, 0)]
    [InlineData(Crtc6845Model.Hd6845S, 1)]
    [InlineData(Crtc6845Model.Hd6845S, 3)]
    public void NotificationsMatchExactlyTheVisibleClockBoundaryTransitions(Crtc6845Model model, byte mode)
    {
        var crtc = Create(model, mode: mode);
        var notified = new List<(int Clock, bool Level)>();
        var expected = new List<(int Clock, bool Level)>();
        var clock = 0;
        crtc.VSyncChanged = level =>
        {
            Assert.Equal(crtc.VSync, level);
            notified.Add((clock, level));
        };
        for (clock = 1; clock <= 4096; ++clock)
        {
            var previous = crtc.VSync;
            crtc.Tick();
            if (previous != crtc.VSync) expected.Add((clock, crtc.VSync));
        }
        Assert.NotEmpty(expected);
        Assert.Equal(expected, notified);
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845, false)]
    [InlineData(Crtc6845Model.Mc6845, true)]
    [InlineData(Crtc6845Model.Hd6845S, false)]
    [InlineData(Crtc6845Model.Hd6845S, true)]
    public void ResetAndTestMaskTheOutputAtTheClockWithoutRepeatedNotifications(Crtc6845Model model, bool test)
    {
        var crtc = Create(model);
        for (var clock = 0; clock < 4096 && !crtc.VSync; ++clock) crtc.Tick();
        Assert.True(crtc.VSync);
        var levels = new List<bool>();
        crtc.VSyncChanged = levels.Add;
        crtc.SetLightPen(test);
        crtc.SetReset(true);
        Assert.Empty(levels);
        crtc.Tick();
        Assert.Equal(new[] { false }, levels);
        Clocks(crtc, 4);
        Assert.Single(levels);
        crtc.SetReset(false);
        var expected = new List<bool> { false };
        for (var clock = 0; clock < 4096; ++clock)
        {
            var previous = crtc.VSync;
            crtc.Tick();
            if (previous != crtc.VSync) expected.Add(crtc.VSync);
        }
        Assert.Equal(expected, levels);
        if (crtc.VSync)
        {
            crtc.Reset();
            Assert.False(levels[^1]);
        }
        var count = levels.Count;
        crtc.Reset();
        Assert.Equal(count, levels.Count);
    }
}
