using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Functions;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class ProjectionVideoTests
{
    [Fact]
    public void ProjectionNormalizesOnlyItsOwnOptions()
    {
        Assert.Equal(new[] { EmulationCrtConnection.Digital, EmulationCrtConnection.Composite,
            EmulationCrtConnection.Component, EmulationCrtConnection.SVideo, EmulationCrtConnection.Scart },
            EmulationVideoProcessingCatalog.ProjectionConnectionResourceKeys.Keys);
        var settings = EmulationVideoProcessingConfigurationFunctions.Normalize(Projector(new()
        {
            AmbientLight = 40,
            Connection = EmulationCrtConnection.AnalogRf,
            Definition = EmulationLcdDefinition.FullHd
        }));
        Assert.Equal(20, settings.Projection.AmbientLight);
        Assert.Equal(EmulationCrtConnection.Digital, settings.Projection.Connection);
        Assert.Equal(EmulationLcdDefinition.FullHd, settings.Projection.Definition);
        Assert.Equal(EmulationLcdDefinition.Native, settings.Lcd.Definition);
    }

    [Fact]
    public void ProjectionUsesThePanelDecoderForColorAndSynchronization()
    {
        var pixels = new byte[] { 0, 0, 255, 255 };
        var frame = new VideoFrame(pixels, 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1f, 1, TimeSpan.Zero)
            { SignalStandard = EmulationVideoStandard.Pal, SignalFieldRate = 50 };
        var pal = new EmulationProjectionVideoConfiguration
            { ColorStandards = EmulationCrtColorStandards.Pal, FieldRate = EmulationCrtFieldRate.Hz50 };
        var filter = new FilterDisplayConnection();
        Assert.Equal(pixels, filter.Apply(pixels, frame, Projector(pal)));
        var incompatible = filter.Apply(pixels, frame, Projector(pal with { ColorStandards = EmulationCrtColorStandards.Ntsc }))!;
        Assert.Equal(incompatible[0], incompatible[1]);
        Assert.Equal(incompatible[1], incompatible[2]);
        Assert.True(CrtSignal.ForLcd(pal with { FieldRate = EmulationCrtFieldRate.Hz60 }, frame).Tuning.X > 0);
    }

    [Fact]
    public void ProjectionGridIsNeutralAndAbsentFromCrtOptics()
    {
        const int output = 32;
        float[] Render(EmulationProjectionTechnology technology, int panelSource)
        {
            var colors = new float[output * output * 3];
            Array.Fill(colors, .8f);
            FilterProjection.Apply(colors, panelSource, panelSource, output, output, new() { Technology = technology });
            return colors;
        }

        var lcd = Render(EmulationProjectionTechnology.ThreeLcd, 2);
        var border = 7 * output * 3;
        var center = border + 7 * 3;
        Assert.True(lcd[border] < lcd[center]);
        for (var index = 0; index < lcd.Length; index += 3)
        {
            Assert.Equal(lcd[index], lcd[index + 1]);
            Assert.Equal(lcd[index], lcd[index + 2]);
        }
        foreach (var uniform in new[] { Render(EmulationProjectionTechnology.Crt, 2),
            Render(EmulationProjectionTechnology.ThreeLcd, output) })
            Assert.All(uniform, value => Assert.Equal(uniform[0], value));
        var dlp = Render(EmulationProjectionTechnology.ThreeChipDlp, 2);
        Assert.True(dlp[center] - dlp[border] < lcd[center] - lcd[border]);
    }

    [Fact]
    public void ProjectionSurfaceAndAmbientLightChangeTheReflectedImage()
    {
        float[] Render(EmulationProjectionSurface surface, int ambient)
        {
            var colors = new[] { 0f, 0f, 0f, 1f, .3f, .1f, 1f, .3f, .1f };
            FilterProjection.Apply(colors, 3, 1, 3, 1, new() { Surface = surface, AmbientLight = ambient });
            return colors;
        }

        var white = Render(EmulationProjectionSurface.WhiteScreen, 0);
        var gray = Render(EmulationProjectionSurface.GrayScreen, 0);
        var lit = Render(EmulationProjectionSurface.WhiteScreen, 20);
        var wall = Render(EmulationProjectionSurface.WhiteWall, 0);
        Assert.True(gray[0] < white[0] && gray[3] < white[3]);
        Assert.True(lit[0] > white[0] && lit[3] - lit[5] < white[3] - white[5]);
        Assert.Equal(white[3], white[6]);
        Assert.NotEqual(wall[3], wall[6]);
    }

    [Theory]
    [InlineData(EmulationProjectionTechnology.SingleChipDlp, true, false)]
    [InlineData(EmulationProjectionTechnology.SingleChipDlp, true, true)]
    [InlineData(EmulationProjectionTechnology.ThreeChipDlp, false, false)]
    [InlineData(EmulationProjectionTechnology.ThreeChipDlp, false, true)]
    public void OnlySingleChipDlpSeparatesColorsOnAMovingEdge(EmulationProjectionTechnology technology, bool splits, bool vertical)
    {
        const int width = 8;
        VideoFrame Edge(int position, long sequence)
        {
            var bytes = new byte[width * width * 4];
            for (var y = 0; y < width; y++)
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                bytes.AsSpan(offset, 3).Fill((vertical ? y : x) < position ? (byte)0 : (byte)255);
                bytes[offset + 3] = 255;
            }
            return new VideoFrame(bytes, width, width, width * 4, EmulationPixelFormat.Xrgb8888,
                1f, sequence, TimeSpan.FromMilliseconds(sequence * 20)) { SignalStandard = EmulationVideoStandard.Pal };
        }

        var filter = new FilterDisplayConnection();
        var settings = Projector(new() { Technology = technology });
        var initial = Edge(2, 1);
        var staticFrame = Edge(2, 2);
        var moving = Edge(3, 3);
        Assert.Equal(initial.Pixels.ToArray(), filter.Apply(initial.Pixels.Span, initial, settings));
        Assert.Equal(staticFrame.Pixels.ToArray(), filter.Apply(staticFrame.Pixels.Span, staticFrame, settings));
        var rendered = filter.Apply(moving.Pixels.Span, moving, settings)!;
        var edge = (2 * width + 2) * 4;
        Assert.Equal(splits, rendered[edge] != rendered[edge + 2]);
        filter.ResetHistory();
        Assert.Equal(moving.Pixels.ToArray(), filter.Apply(moving.Pixels.Span, moving, settings));
    }

    private static EmulationVideoProcessingConfiguration Projector(EmulationProjectionVideoConfiguration projection) =>
        new() { DisplayTechnology = EmulationVideoDisplayTechnology.Projection, Projection = projection };
}
