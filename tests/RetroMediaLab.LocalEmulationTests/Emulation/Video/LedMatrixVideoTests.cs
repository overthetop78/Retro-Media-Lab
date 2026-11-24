using System.Numerics;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class LedMatrixVideoTests
{
    [Fact]
    public void DefinitionsUseFixedGridSizesAndNativeUsesTheInput()
    {
        var choices = EmulationVideoProcessingCatalog.LedMatrixDefinitionResourceKeys.Keys;
        Assert.Equal(new[] { (256, 192), (16, 16), (32, 16), (32, 32), (64, 32), (64, 64),
            (128, 64), (128, 128), (256, 128), (256, 256) },
            choices.Select(choice => EmulationVideoProcessingCatalog.LedMatrixDefinitionDimensions(choice, 256, 192)));

        var image = FilterLedMatrixCellStructure.Render([1f, 0f, 0f, 0f, 0f, 1f], 2, 1, 40, 20, new());
        var first = Pixel(image, 40, 10, 10);
        var second = Pixel(image, 40, 30, 10);
        Assert.True(first.X > .95f && first.Z < .02f);
        Assert.True(second.Z > .95f && second.X < .02f);
    }

    [Fact]
    public void TwoEmittersProduceRedGreenAndAmber()
    {
        Assert.Equal(Vector3.UnitX, FilterLedMatrixColor.Emission(Vector3.UnitX, EmulationLedMatrixColor.RedGreen));
        Assert.Equal(new Vector3(0f, .5f, 0f), FilterLedMatrixColor.Emission(Vector3.UnitY, EmulationLedMatrixColor.RedGreen));
        Assert.Equal(new Vector3(1f, .5f, 0f), FilterLedMatrixColor.Emission(Vector3.One, EmulationLedMatrixColor.RedGreen));
        Assert.Equal(Vector3.Zero, FilterLedMatrixColor.Emission(Vector3.UnitZ, EmulationLedMatrixColor.RedGreen));
        Assert.True(FilterLedMatrixColor.Emission(Vector3.UnitY, EmulationLedMatrixColor.White).X >
            FilterLedMatrixColor.Emission(Vector3.UnitZ, EmulationLedMatrixColor.White).X);
    }

    [Fact]
    public void LensesRemainVisibleAndDiffusionIncreasesWithEmission()
    {
        var off = Render(0f, EmulationLedMatrixShape.Round);
        var dim = Render(.2f, EmulationLedMatrixShape.Round);
        var bright = Render(.8f, EmulationLedMatrixShape.Round);
        Assert.True(Pixel(off, 320, 10, 10).X > Pixel(off, 320, 19, 10).X);
        Assert.True(Pixel(bright, 320, 18, 10).X > Pixel(dim, 320, 18, 10).X * 2);
        Assert.True(Pixel(bright, 320, 10, 10).X > Pixel(bright, 320, 18, 10).X);
    }

    [Fact]
    public void RoundedSquarePreservesMoreOfTheCornersThanACircle()
    {
        var round = Pixel(Render(.8f, EmulationLedMatrixShape.Round), 320, 15, 16).X;
        var roundedSquare = Pixel(Render(.8f, EmulationLedMatrixShape.RoundedSquare), 320, 15, 16).X;
        var square = Pixel(Render(.8f, EmulationLedMatrixShape.Square), 320, 15, 16).X;
        Assert.True(round < roundedSquare && roundedSquare < square);
    }

    private static float[] Render(float level, EmulationLedMatrixShape shape) =>
        FilterLedMatrixCellStructure.Render([level, 0f, 0f], 1, 1, 320, 320,
            new(Definition: EmulationLedMatrixDefinition.Size16x16, Shape: shape));

    private static Vector3 Pixel(float[] image, int width, int x, int y) =>
        new(image.AsSpan((y * width + x) * 3, 3));
}
