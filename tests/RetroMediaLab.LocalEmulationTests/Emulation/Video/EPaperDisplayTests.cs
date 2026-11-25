using System.Numerics;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class EPaperDisplayTests
{
    [Fact]
    public void ReflectivePalettesHaveTheExpectedNumberOfPigments()
    {
        Vector3 Pigment(Vector3 color, EmulationEPaperColorMode mode) =>
            FilterEPaper.Pigment(color, mode, FilterEPaper.Surface(mode));

        var shades = Enumerable.Range(0, 256).Select(value => new Vector3(value / 255f)).ToArray();
        Assert.Equal(2, shades.Select(color => Pigment(color, EmulationEPaperColorMode.Monochrome)).Distinct().Count());
        Assert.Equal(16, shades.Select(color => Pigment(color, EmulationEPaperColorMode.Grayscale16)).Distinct().Count());

        var kaleido = from r in Enumerable.Range(0, 16)
                      from g in Enumerable.Range(0, 16)
                      from b in Enumerable.Range(0, 16)
                      select Pigment(new Vector3(r, g, b) / 15f, EmulationEPaperColorMode.Color4096);
        Assert.Equal(4096, kaleido.Distinct().Count());
    }

    [Theory]
    [InlineData(EmulationEPaperColorMode.Monochrome, 270, 6)]
    [InlineData(EmulationEPaperColorMode.Grayscale16, 390, 6)]
    [InlineData(EmulationEPaperColorMode.TricolorRed, 720, 4)]
    [InlineData(EmulationEPaperColorMode.TricolorYellow, 720, 4)]
    [InlineData(EmulationEPaperColorMode.Color4096, 450, 6)]
    [InlineData(EmulationEPaperColorMode.Gallery, 960, 4)]
    public void PigmentTraceFadesWithoutHoldingTheLiveFrame(EmulationEPaperColorMode mode, int interval, int fullEvery)
    {
        var clock = new EPaperRefreshClock();
        var panel = new EmulationEPaperVideoConfiguration(mode);
        EPaperRefresh Present(int time) => clock.Advance(panel, 1, 1, TimeSpan.FromMilliseconds(time));

        var first = Present(0);
        Assert.True(first.CaptureHistory);
        Assert.False(first.HasPrevious);
        var trace = Present(interval / 2);
        Assert.False(trace.CaptureHistory);
        Assert.True(trace.HasPrevious);
        Assert.InRange(trace.CurrentWeight, .65f, .77f);
        Assert.Equal(trace.CurrentWeight, Present(interval * 2 / 3).CurrentWeight);
        Assert.InRange(Present(interval - 1).CurrentWeight, .99f, 1f);
        for (var count = 1; count <= fullEvery; count++)
            Assert.True(Present(count * interval).CaptureHistory);
        Assert.Equal(.35f, Present(fullEvery * interval + 20).Inversion);
        Assert.Equal(1f, Present(fullEvery * interval + 40).CurrentWeight);
        Assert.Equal(trace.CurrentWeight, Present(fullEvery * interval + 80).CurrentWeight);
        Assert.False(Present(0).HasPrevious);
    }

    [Fact]
    public void GalleryRetainsPigmentsLongerThanKaleido()
    {
        var clock = new EPaperRefreshClock();
        var kaleido = new EmulationEPaperVideoConfiguration(EmulationEPaperColorMode.Color4096);
        var gallery = new EmulationEPaperVideoConfiguration(EmulationEPaperColorMode.Gallery);
        clock.Advance(kaleido, 256, 192, TimeSpan.Zero);
        Assert.True(clock.Advance(kaleido, 256, 192, TimeSpan.FromMilliseconds(450)).CaptureHistory);
        clock.Advance(gallery, 256, 192, TimeSpan.Zero);
        Assert.False(clock.Advance(gallery, 256, 192, TimeSpan.FromMilliseconds(450)).CaptureHistory);
        Assert.True(clock.Advance(gallery, 256, 192, TimeSpan.FromMilliseconds(960)).CaptureHistory);
    }
}
