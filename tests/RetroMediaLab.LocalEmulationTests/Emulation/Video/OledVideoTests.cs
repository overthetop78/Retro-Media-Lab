using System.Numerics;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Functions;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class OledVideoTests
{
    [Fact]
    public void OledLimitsLargeBrightAreasWithoutLiftingBlack()
    {
        const int size = 24;
        var pixels = new byte[size * size * 4];
        pixels.AsSpan(4, 4).Fill(255);
        var frame = new VideoFrame(pixels, size, size, size * 4,
            EmulationPixelFormat.Xrgb8888, 1f, 1, TimeSpan.Zero);

        byte[] Render(EmulationOledPanelType panel, VideoFrame input)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            return pipeline.Process(Oled(new() { PanelType = panel, PixelStructure = EmulationOledPixelStructure.None }),
                input, new(size, size), new(size, size)).Pixels.ToArray();
        }

        var white = new byte[pixels.Length];
        Array.Fill(white, (byte)255);
        foreach (var panel in Enum.GetValues<EmulationOledPanelType>())
        {
            var result = Render(panel, frame);
            Assert.All(result[..3], channel => Assert.Equal(0, channel));
            Assert.Equal(255, result[4]);
            Assert.InRange(Render(panel, frame with { Pixels = white })[0], 1, 254);
        }
        Assert.True(Render(EmulationOledPanelType.QdOled, frame with { Pixels = white })[0] >
            Render(EmulationOledPanelType.Woled, frame with { Pixels = white })[0]);
    }

    [Fact]
    public void OledSpecializedStructuresUseDistinctOpenings()
    {
        const int width = 80;
        const int height = 40;
        Vector3 Pixel(EmulationOledPixelStructure structure, int x, int y, Vector3 source)
        {
            var image = new float[width * height * 3];
            for (var offset = 0; offset < image.Length; offset += 3) source.CopyTo(image, offset);
            FilterOledPixelStructure.Apply(image, 2, 1, width, height, structure);
            var index = (y * width + x) * 3;
            return new Vector3(image[index], image[index + 1], image[index + 2]);
        }

        Assert.True(Vector3.Distance(new Vector3(.6f),
            Pixel(EmulationOledPixelStructure.Wrgb, 6, 20, Vector3.One)) < .00001f);
        Assert.Equal(Vector3.Zero, Pixel(EmulationOledPixelStructure.Wrgb, 6, 20, Vector3.UnitZ));
        Assert.True(Vector3.Distance(Vector3.UnitY,
            Pixel(EmulationOledPixelStructure.TriangularRgb, 20, 10, Vector3.One)) < .00001f);
        Assert.True(Vector3.Distance(Vector3.UnitX,
            Pixel(EmulationOledPixelStructure.TriangularRgb, 10, 30, Vector3.One)) < .00001f);
        Assert.True(Vector3.Distance(Vector3.UnitZ,
            Pixel(EmulationOledPixelStructure.TriangularRgb, 30, 30, Vector3.One)) < .00001f);
        Assert.Equal(Vector3.UnitX, Pixel(EmulationOledPixelStructure.PenTileDiamond, 27, 20, Vector3.One));
        Assert.Equal(Vector3.UnitZ, Pixel(EmulationOledPixelStructure.PenTileDiamond, 67, 20, Vector3.One));
    }

    [Theory]
    [InlineData(EmulationCrtConnection.Scart)]
    [InlineData(EmulationCrtConnection.SVideo)]
    public void OledConnectionOptionsExcludeUnsupportedInputs(EmulationCrtConnection connection)
    {
        Assert.DoesNotContain(connection, EmulationVideoProcessingCatalog.OledConnectionResourceKeys.Keys);
        var configuration = EmulationVideoProcessingConfigurationFunctions.Normalize(Oled(new()
        {
            Connection = connection,
            PanelType = EmulationOledPanelType.Woled,
            PixelStructure = EmulationOledPixelStructure.RgbVertical
        }));
        Assert.Equal(EmulationCrtConnection.Digital, configuration.Oled.Connection);
        Assert.Equal(EmulationOledPanelType.Woled, configuration.Oled.PanelType);
        Assert.Equal(EmulationOledPixelStructure.RgbVertical, configuration.Oled.PixelStructure);
    }

    private static EmulationVideoProcessingConfiguration Oled(EmulationOledVideoConfiguration configuration) =>
        EmulationVideoProcessingConfigurationFunctions.WithScreenType(
            new() { Oled = configuration }, EmulationVideoScreenType.Oled);
}
