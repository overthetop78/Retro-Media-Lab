using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class VerticalGatesTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void RewritingVerticalDisplayedEndsDisplayAtTheNextCharacter(Crtc6845Model model)
    {
        var crtc = Create(model);
        NextFrame(crtc);
        Clocks(crtc, 16); // Sur Hitachi, la première ligne du champ masque la comparaison R6.
        Clocks(crtc, 4);
        Assert.True(crtc.DisplayEnabled);
        Write(crtc, 6, 0);
        Assert.True(crtc.DisplayEnabled);
        crtc.Tick();
        Assert.Equal(5, crtc.Column);
        Assert.False(crtc.DisplayEnabled);
        Write(crtc, 6, 2);
        crtc.Tick();
        Assert.False(crtc.DisplayEnabled);
        NextFrame(crtc);
        Assert.True(crtc.DisplayEnabled);
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void RewritingVsyncRowDoesNotPublishUntilTheCharacterClock(Crtc6845Model model)
    {
        var crtc = Create(model);
        Write(crtc, 7, 127); // Éviter une impulsion MC6845 de 16 lignes déjà en cours au début du champ.
        NextFrame(crtc);
        Clocks(crtc, 4);
        var notifications = new List<bool>();
        crtc.VSyncChanged = notifications.Add;
        Write(crtc, 7, 0);
        Assert.False(crtc.VSync);
        Assert.Empty(notifications);
        crtc.Tick();
        Assert.True(crtc.VSync);
        Assert.Equal(new[] { true }, notifications);
        Write(crtc, 7, 0);
        crtc.Tick();
        Assert.Equal(new[] { true }, notifications);
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void OddInterlaceFieldUsesTheRewrittenHalfLineComparison(Crtc6845Model model)
    {
        var crtc = Create(model, mode: 1);
        Write(crtc, 7, 127);
        NextFrame(crtc);
        Clocks(crtc, 2);
        Write(crtc, 7, 0);
        Write(crtc, 0, 31);
        Clocks(crtc, 13);
        Assert.Equal(15, crtc.Column);
        Assert.False(crtc.VSync);
        crtc.Tick();
        Assert.Equal(16, crtc.Column);
        Assert.True(crtc.VSync);
    }
}
