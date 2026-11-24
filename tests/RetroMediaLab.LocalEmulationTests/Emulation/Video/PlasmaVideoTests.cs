using System.Numerics;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Functions;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class PlasmaVideoTests
{
    [Fact]
    public void PlasmaUsesOnlyTheRequestedDefinitionsAndConnections()
    {
        Assert.Equal(new[] { EmulationLcdDefinition.Native, EmulationLcdDefinition.Vga, EmulationLcdDefinition.Xga,
            EmulationLcdDefinition.Hd, EmulationLcdDefinition.HdReady, EmulationLcdDefinition.FullHd },
            EmulationVideoProcessingCatalog.PlasmaDefinitionResourceKeys.Keys);
        Assert.Equal(new[] { EmulationCrtConnection.Digital, EmulationCrtConnection.Scart, EmulationCrtConnection.Composite,
            EmulationCrtConnection.Component, EmulationCrtConnection.SVideo, EmulationCrtConnection.AnalogRf },
            EmulationVideoProcessingCatalog.PlasmaConnectionResourceKeys.Keys);
        var old = Plasma(new() { Definition = EmulationLcdDefinition.Uhd4K, Connection = EmulationCrtConnection.LegacyMonitor });
        var normalized = EmulationVideoProcessingConfigurationFunctions.Normalize(old).Plasma;
        Assert.Equal(EmulationLcdDefinition.Native, normalized.Definition);
        Assert.Equal(EmulationCrtConnection.Digital, normalized.Connection);
    }

    [Fact]
    public void PlasmaDecodesPalInColorAndRejectsAnIncompatibleStandardOverDigital()
    {
        var pixels = new byte[] { 0, 0, 255, 255 };
        var frame = new VideoFrame(pixels, 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1f, 1, TimeSpan.Zero)
            { SignalStandard = EmulationVideoStandard.Pal, SignalFieldRate = 50 };
        var pal = new EmulationPlasmaVideoConfiguration
            { ColorStandards = EmulationCrtColorStandards.Pal, FieldRate = EmulationCrtFieldRate.Hz50 };
        var filter = new FilterDisplayConnection();
        Assert.Equal(pixels, filter.Apply(pixels, frame, Plasma(pal)));
        var incompatible = filter.Apply(pixels, frame, Plasma(pal with { ColorStandards = EmulationCrtColorStandards.Ntsc }))!;
        Assert.Equal(incompatible[0], incompatible[1]);
        Assert.Equal(incompatible[1], incompatible[2]);
        Assert.True(CrtSignal.ForLcd(pal with { FieldRate = EmulationCrtFieldRate.Hz60 }, frame).Tuning.X > 0);
    }

    [Fact]
    public void GridUsesThePanelAndLeavesUnresolvedColorsIntact()
    {
        const int size = 40;
        Vector3 Pixel(EmulationPlasmaCellStructure layout, int x, int y, int panelCells = 1)
        {
            var colors = new float[size * size * 3];
            Array.Fill(colors, 1f);
            FilterPlasmaCellStructure.Apply(colors, panelCells, panelCells, size, size, layout);
            var offset = (y * size + x) * 3;
            return new Vector3(colors[offset], colors[offset + 1], colors[offset + 2]);
        }

        Assert.Equal(Vector3.One, Pixel(EmulationPlasmaCellStructure.Delta, 7, 11, size));
        Assert.Equal(Vector3.UnitX, Pixel(EmulationPlasmaCellStructure.Stripe, 7, 0));
        Assert.True(Pixel(EmulationPlasmaCellStructure.Waffle, 7, 0).X < .1f);
        Assert.InRange(Vector3.Distance(Vector3.UnitY, Pixel(EmulationPlasmaCellStructure.Delta, 20, 10)), 0f, .00001f);
        Assert.InRange(Vector3.Distance(Vector3.UnitX, Pixel(EmulationPlasmaCellStructure.Delta, 10, 30)), 0f, .00001f);
        Assert.InRange(Vector3.Distance(Vector3.UnitZ, Pixel(EmulationPlasmaCellStructure.Delta, 30, 30)), 0f, .00001f);
        var amber = Pixel(EmulationPlasmaCellStructure.Monochrome, 7, 11, size);
        Assert.True(amber.X > amber.Y && amber.Y > amber.Z);
    }

    [Fact]
    public void SoftwarePlasmaLiftsBlackAndDimsLargeBrightAreas()
    {
        const int width = 24;
        var scene = new byte[width * width * 4];
        Array.Fill(scene, (byte)255, 4, 4);
        var fullWhite = new byte[scene.Length];
        Array.Fill(fullWhite, (byte)255);

        byte[] Render(byte[] pixels)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            var frame = new VideoFrame(pixels, width, width, width * 4,
                EmulationPixelFormat.Xrgb8888, 1f, 1, TimeSpan.Zero);
            return pipeline.Process(Plasma(new()), frame, new(width, width), new(width, width)).Pixels.ToArray();
        }

        var smallHighlight = Render(scene);
        Assert.InRange(smallHighlight[0], 1, 16);
        Assert.Equal(255, smallHighlight[4]);
        Assert.InRange(Render(fullWhite)[4], 128, 254);
    }

    private static EmulationVideoProcessingConfiguration Plasma(EmulationPlasmaVideoConfiguration configuration) =>
        EmulationVideoProcessingConfigurationFunctions.WithScreenType(
            new() { Plasma = configuration }, EmulationVideoScreenType.Plasma);
}
