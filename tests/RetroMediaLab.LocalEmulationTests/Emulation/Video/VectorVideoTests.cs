using System.Numerics;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Functions;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class VectorVideoTests
{
    [Theory]
    [InlineData(321, 241)]
    [InlineData(641, 481)]
    public void NativeVectorsKeepTheirCoordinatesAndAddAtCrossings(int width, int height)
    {
        var horizontal = new VideoVectorSegment(new(.1f, .5f), new(.9f, .5f), Vector3.UnitX, .25f);
        var vertical = new VideoVectorSegment(new(.5f, .1f), new(.5f, .9f), Vector3.UnitY, .50f);
        var frame = Frame(1, [horizontal, vertical, horizontal]);
        var pixels = Render(new(), frame, width, height);

        var center = Pixel(pixels, width, width / 2, height / 2);
        var redLine = Pixel(pixels, width, width / 4, height / 2);
        Assert.True(center.X > .49f && center.Y > .49f && center.Z == 0);
        Assert.True(redLine.X > .49f && redLine.Y == 0);
        Assert.Equal(Vector3.Zero, Pixel(pixels, width, 0, 0));
    }

    [Fact]
    public void BeamWidthBrightnessAndPhosphorAffectEmission()
    {
        var frame = Frame(1, [new(new(.1f, .5f), new(.9f, .5f), Vector3.One, .4f)]);
        var screen = new EmulationVectorVideoConfiguration(EmulationCrtColorMode.Amber);
        var narrow = Render(new(), frame, 301, 301, screen with { BeamWidth = -20 });
        var wide = Render(new(), frame, 301, 301, screen with { BeamWidth = 20 });
        var bright = Render(new(), frame, 301, 301, screen with { BeamWidth = 20 }, brightness: 10);
        var offAxis = Pixel(wide, 301, 150, 151);
        Assert.True(offAxis.X > Pixel(narrow, 301, 150, 151).X * 2f);
        Assert.True(offAxis.X > offAxis.Y && offAxis.Y > offAxis.Z);
        Assert.True(Pixel(bright, 301, 150, 151).X > offAxis.X);
    }

    [Fact]
    public void RepaintingTheSameFrameDoesNotReexciteThePhosphor()
    {
        var filter = new FilterVectorDisplay();
        var frame = Frame(1, [new(new(.5f), new(.5f), Vector3.One, .3f)]);
        var first = Render(filter, frame, 101, 101);
        Assert.Equal(first, Render(filter, frame, 101, 101));
        var next = Render(filter, Frame(2, []), 101, 101);
        Assert.InRange(Pixel(next, 101, 50, 50).X / Pixel(first, 101, 50, 50).X, .24f, .26f);
    }

    [Fact]
    public void RasterFallbackKeepsTheOriginalLineAndCurvatureAppliesLast()
    {
        const int size = 101;
        var raster = new float[size * size * 3];
        for (var x = 0; x < size; x++) raster[(size * 20 + x) * 3 + 2] = .6f;
        var output = new float[raster.Length];
        var filter = new FilterVectorDisplay();
        var frame = Frame(1, null);
        filter.Render(output, raster, frame, size, size, new(), 0);
        var center = Pixel(output, size, 50, 20);
        Assert.True(center.Z > .4f && center.X == 0 && center.Y == 0);
        Assert.True(Pixel(output, size, 50, 19).Z < center.Z);
        Assert.True(Pixel(output, size, 0, 20).Z > .4f);
        filter.Render(output, raster, frame, size, size, new(Curvature: 20), 0);
        Assert.Equal(Vector3.Zero, Pixel(output, size, 0, 20));
    }

    [Theory]
    [InlineData(10, 0, 0, 0, 0)]
    [InlineData(0, 10, 0, 0, 0)]
    [InlineData(0, 0, 240, 0, 0)]
    [InlineData(0, 0, 0, -10, 0)]
    [InlineData(0, 0, 0, 0, -10)]
    public void GeneralImageAdjustmentsApplyToThePausedVectorFrame(int brightness, int contrast, int gamma, int saturation, int sharpness)
    {
        var frame = Frame(1, [new(new(.1f, .5f), new(.9f, .5f), new(.4f, .2f, .1f), .7f)]);
        var original = EmulationVideoProcessingConfigurationFunctions.WithScreenType(new(), EmulationVideoScreenType.Vector);
        var edited = EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(original, EmulationVideoScreenType.Vector,
            _ => new(brightness, contrast, gamma, saturation, sharpness));

        using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
        var before = pipeline.Process(original, frame, new(1, 1), new(101, 101));
        var after = pipeline.Process(edited, frame, new(1, 1), new(101, 101));

        Assert.False(before.Pixels.Span.SequenceEqual(after.Pixels.Span));
    }

    private static VideoFrame Frame(long sequence, VideoVectorSegment[]? vectors) =>
        new(ReadOnlyMemory<byte>.Empty, 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1f, sequence, TimeSpan.FromMilliseconds(20 * sequence))
        {
            VectorSegments = vectors is null ? (ReadOnlyMemory<VideoVectorSegment>?)null : vectors.AsMemory()
        };

    private static float[] Render(FilterVectorDisplay filter, VideoFrame frame, int width, int height,
        EmulationVectorVideoConfiguration? configuration = null, int brightness = 0)
    {
        var result = new float[width * height * 3];
        filter.Render(result, null, frame, width, height, configuration ?? new(), brightness);
        return result;
    }

    private static Vector3 Pixel(float[] colors, int width, int x, int y) =>
        new(colors.AsSpan((y * width + x) * 3, 3));
}
