using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class VramScheduleTests
{
    [Fact]
    public void WholeLineSchedulesRetainEveryBackgroundSpriteRefreshAndCpuSelection()
    {
        Tms9918VramSlot[] Line(Tms9918DisplayMode mode, bool refresh = false) => Enumerable.Range(0, 171)
            .Select(i => Tms9918VramSchedule.At(i * 2, mode, refresh)).ToArray();
        var graphics = Line(Tms9918DisplayMode.GraphicsI);
        Assert.Equal(graphics, Line(Tms9918DisplayMode.GraphicsII));
        Assert.Equal(19, graphics.Count(x => x.Kind == K.Cpu));
        foreach (K kind in new[] { K.BackgroundName, K.BackgroundColour, K.BackgroundPattern, K.SpriteVisibility })
            Assert.Equal(Enumerable.Range(0, 32), graphics.Where(x => x.Kind == kind).Select(x => x.Index).Order());
        foreach (K kind in new[] { K.SpriteY, K.SpriteX, K.SpriteName, K.SpriteColour, K.SpritePatternLeft, K.SpritePatternRight })
            Assert.Equal(Enumerable.Range(0, 4), graphics.Where(x => x.Kind == kind).Select(x => x.Index).Order());
        var cpu = graphics.Select((slot, index) => (slot, index))
            .Where(x => x.slot.Kind == K.Cpu).Select(x => x.index * 2).ToArray();
        Assert.Equal(32, Enumerable.Range(0, cpu.Length)
            .Max(i => (cpu[(i + 1) % cpu.Length] - cpu[i] + 342) % 342));
        Assert.Equal(new Tms9918VramSlot(K.BackgroundName, 0), Tms9918VramSchedule.At(332, Tms9918DisplayMode.GraphicsI, false));
        Assert.Equal(new Tms9918VramSlot(K.BackgroundPattern, 0), Tms9918VramSchedule.At(338, Tms9918DisplayMode.GraphicsI, false));
        // Traversée de ligne lue sur le dessin : cinq créneaux CPU, puis N2/C2/P2a/P2b.
        foreach (int dot in new[] { 278, 280, 282, 284, 286 })
            Assert.Equal(K.Cpu, Tms9918VramSchedule.At(dot, Tms9918DisplayMode.GraphicsI, false).Kind);
        Assert.Equal(new Tms9918VramSlot(K.SpriteName, 2), Tms9918VramSchedule.At(288, Tms9918DisplayMode.GraphicsI, false));
        Assert.Equal(new Tms9918VramSlot(K.SpriteColour, 2), Tms9918VramSchedule.At(290, Tms9918DisplayMode.GraphicsI, false));
        Assert.Equal(new Tms9918VramSlot(K.SpritePatternLeft, 2), Tms9918VramSchedule.At(292, Tms9918DisplayMode.GraphicsI, false));
        Assert.Equal(new Tms9918VramSlot(K.SpritePatternRight, 2), Tms9918VramSchedule.At(294, Tms9918DisplayMode.GraphicsI, false));
        var text = Line(Tms9918DisplayMode.Text);
        foreach (var mode in new[] { Tms9918DisplayMode.GraphicsIIText, Tms9918DisplayMode.TextMulticolor,
            Tms9918DisplayMode.GraphicsIITextMulticolor }) Assert.Equal(text, Line(mode));
        Assert.Equal(91, text.Count(x => x.Kind == K.Cpu));
        Assert.Equal(Enumerable.Range(0, 40), text.Where(x => x.Kind == K.BackgroundName).Select(x => x.Index).Order());
        Assert.Equal(Enumerable.Range(0, 40), text.Where(x => x.Kind == K.BackgroundPattern).Select(x => x.Index).Order());
        var multicolor = Line(Tms9918DisplayMode.Multicolor);
        Assert.Equal(multicolor, Line(Tms9918DisplayMode.GraphicsIIMulticolor));
        Assert.Equal(51, multicolor.Count(x => x.Kind == K.Cpu));
        Assert.DoesNotContain(multicolor, x => x.Kind == K.BackgroundColour);
        var refresh = Line(Tms9918DisplayMode.GraphicsI, true);
        Assert.Equal(64, refresh.Count(x => x.Kind == K.Refresh));
        Assert.Equal(107, refresh.Count(x => x.Kind == K.Cpu));
        Assert.Equal(Enumerable.Range(0, 64), refresh.Where(x => x.Kind == K.Refresh).Select(x => x.Index).Order());
    }
}
