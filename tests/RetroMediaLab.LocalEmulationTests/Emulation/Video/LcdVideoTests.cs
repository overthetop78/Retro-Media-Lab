using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Functions;

namespace RetroMediaLab.Tests.Emulation.Video;

public sealed class LcdVideoTests
{
    [Fact]
    public void PixelStructureFollowsOrientationAndChannelOrder()
    {
        byte[] Pixel(EmulationLcdPixelStructure structure, EmulationLcdPixelOrientation orientation, int x, int y)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            var settings = Lcd(new() { PixelStructure = structure, Orientation = orientation });
            var output = pipeline.Process(settings, Frame(180), new(1, 1), new(9, 9));
            return output.Pixels.Span.Slice(y * output.Pitch + x * 4, 3).ToArray();
        }

        var vertical = Pixel(EmulationLcdPixelStructure.Rgb, EmulationLcdPixelOrientation.Vertical, 1, 4);
        var horizontal = Pixel(EmulationLcdPixelStructure.Rgb, EmulationLcdPixelOrientation.Horizontal, 4, 1);
        Assert.True(vertical[2] > vertical[0]);
        Assert.True(horizontal[2] > horizontal[0]);
        Assert.Equal(vertical, horizontal);
        var bgr = Pixel(EmulationLcdPixelStructure.Bgr, EmulationLcdPixelOrientation.Horizontal, 4, 1);
        Assert.True(bgr[0] > bgr[2]);
        var noGrid = Pixel(EmulationLcdPixelStructure.None, EmulationLcdPixelOrientation.Vertical, 1, 4);
        Assert.All(noGrid, value => Assert.Equal(noGrid[0], value));
    }

    [Theory]
    [InlineData(16, 12)]
    [InlineData(12, 9)]
    public void LcdGridIsDisabledWhenNeitherVideoDimensionExceedsThePanel(int width, int height)
    {
        var lcd = new EmulationLcdVideoConfiguration { PixelStructure = EmulationLcdPixelStructure.Rgb };
        var colors = new float[width * height * 3];
        Array.Fill(colors, .5f);
        FilterLcdPixelStructure.Apply(colors, 16, 12, width, height, lcd);
        Assert.All(colors, channel => Assert.Equal(.5f, channel));
    }

    [Theory]
    [InlineData(31, 12, EmulationLcdPixelOrientation.Vertical, false)]
    [InlineData(32, 12, EmulationLcdPixelOrientation.Vertical, true)]
    [InlineData(32, 12, EmulationLcdPixelOrientation.Horizontal, false)]
    [InlineData(16, 23, EmulationLcdPixelOrientation.Horizontal, false)]
    [InlineData(16, 24, EmulationLcdPixelOrientation.Horizontal, true)]
    [InlineData(16, 24, EmulationLcdPixelOrientation.Vertical, false)]
    public void LcdGridUsesTheScaleOfEachVideoAxis(int width, int height,
        EmulationLcdPixelOrientation orientation, bool expectedSubpixels)
    {
        var colors = new float[width * height * 3];
        Array.Fill(colors, .5f);
        FilterLcdPixelStructure.Apply(colors, 16, 12, width, height,
            new() { PixelStructure = EmulationLcdPixelStructure.Rgb, Orientation = orientation });
        var hasSubpixels = false;
        for (var index = 0; index < colors.Length; index += 3)
        {
            hasSubpixels |= Math.Abs(colors[index] - colors[index + 2]) > .001f;
            var x = index / 3 % width;
            var y = index / 3 / width;
            var sample = width == 16 ? (y * width) * 3 : x * 3;
            for (var channel = 0; channel < 3; channel++)
                Assert.Equal(colors[sample + channel], colors[index + channel]);
        }
        Assert.Equal(expectedSubpixels, hasSubpixels);
        Assert.True(colors.Min() < .5f);
    }

    [Theory]
    [InlineData(256, 192, 1f)]
    [InlineData(640, 480, .08f)]
    [InlineData(800, 600, 0f)]
    [InlineData(1280, 720, 0f)]
    public void LcdCellSeparationsAreVisibleOnA1080pOutput(int panelWidth, int panelHeight, float subpixelWeight)
    {
        const int width = 1440;
        const int height = 1080;
        var colors = new float[width * height * 3];
        Array.Fill(colors, .25f);
        FilterLcdPixelStructure.Apply(colors, panelWidth, panelHeight, width, height,
            new() { PixelStructure = EmulationLcdPixelStructure.Rgb });

        var row = colors.AsSpan(width * 3, width * 3);
        var strongestRedStripe = 0f;
        var strongestBlueStripe = 0f;
        for (var index = 0; index < row.Length; index += 3)
        {
            strongestRedStripe = Math.Max(strongestRedStripe, row[index] - row[index + 2]);
            strongestBlueStripe = Math.Max(strongestBlueStripe, row[index + 2] - row[index]);
        }

        if (subpixelWeight > 0f)
        {
            Assert.True(strongestRedStripe > .14f * subpixelWeight);
            Assert.True(strongestBlueStripe > .14f * subpixelWeight);
        }
        else
        {
            Assert.Equal(0f, strongestRedStripe);
            Assert.Equal(0f, strongestBlueStripe);
        }
        Assert.True(colors.Max() - colors.Min() > .003f);
        var expected = .25f * .94f * .94f * (1f - 2f / 3f * subpixelWeight);
        Assert.Equal(expected, colors.Average(), .0001f);
    }

    [Theory]
    [InlineData(EmulationLcdDefinition.Vga, 1956, 1467)]
    [InlineData(EmulationLcdDefinition.Svga, 1956, 1467)]
    [InlineData(EmulationLcdDefinition.Hd, 1956, 1467)]
    [InlineData(EmulationLcdDefinition.Hd, 1440, 1080)]
    public void LcdHorizontalGridKeepsLightPinkUniform(
        EmulationLcdDefinition definition, int width, int height)
    {
        var frame = Frame(0) with { Pixels = new byte[] { 155, 155, 244, 255 } };
        var lcd = new EmulationLcdVideoConfiguration
        {
            Definition = definition,
            Orientation = EmulationLcdPixelOrientation.Horizontal
        };
        using var plain = new SoftwareEmulationVideoProcessingPipeline();
        var reference = plain.Process(Lcd(lcd with { PixelStructure = EmulationLcdPixelStructure.None }),
            frame, new(1, 1), new(width, height)).Pixels.Span;
        using var masked = new SoftwareEmulationVideoProcessingPipeline();
        var pixels = masked.Process(Lcd(lcd with { PixelStructure = EmulationLcdPixelStructure.Rgb }),
            frame, new(1, 1), new(width, height)).Pixels.Span;

        var minGreen = 255;
        var maxGreen = 0;
        for (var first = 0; first < height; first += 24)
        {
            var count = Math.Min(24, height - first);
            var redGreen = 0;
            var blueGreen = 0;
            for (var y = first; y < first + count; y++)
            {
                var offset = (y * width + width / 2) * 4;
                redGreen += (pixels[offset + 2] - pixels[offset + 1]) -
                    (reference[offset + 2] - reference[offset + 1]);
                blueGreen += pixels[offset + 1] - pixels[offset];
                minGreen = Math.Min(minGreen, pixels[offset + 1]);
                maxGreen = Math.Max(maxGreen, pixels[offset + 1]);
            }
            Assert.InRange(redGreen / (float)count, -9f, 2f);
            Assert.InRange(blueGreen / (float)count, -2f, 2f);
        }
        Assert.True(maxGreen > minGreen);
    }

    [Fact]
    public void LcdDefinitionResamplesOnThePanelBeforeScalingToTheHost()
    {
        const int width = 1280;
        var pixels = new byte[width * 4];
        for (var x = 1; x < width; x += 2) pixels.AsSpan(x * 4, 3).Fill(255);
        var frame = Frame(0) with { Pixels = pixels, Width = width, Pitch = width * 4 };

        byte[] Render(EmulationLcdDefinition definition)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            return pipeline.Process(Lcd(new() { Definition = definition, BacklightIntensity = 100,
                    PixelStructure = EmulationLcdPixelStructure.None }),
                frame, new(width, 1), new(width, 1)).Pixels.ToArray();
        }

        var native = Render(EmulationLcdDefinition.Native);
        var vga = Render(EmulationLcdDefinition.Vga);
        Assert.True(native[0] < native[4]);
        Assert.Equal(native[4], vga[0]);
        Assert.Equal(vga[0], vga[4]);
        Assert.Equal((256, 192), EmulationVideoProcessingCatalog.LcdDefinitionDimensions(EmulationLcdDefinition.Native, 256, 192));
        Assert.Equal((3840, 2160), EmulationVideoProcessingCatalog.LcdDefinitionDimensions(EmulationLcdDefinition.Uhd4K, 256, 192));
        Assert.Equal(10, EmulationVideoProcessingCatalog.LcdDefinitionResourceKeys.Count);
    }

    [Theory]
    [InlineData(192, 1d)]
    [InlineData(480, .08d)]
    [InlineData(540, .08d)]
    [InlineData(600, 0d)]
    [InlineData(768, 0d)]
    [InlineData(720, 0d)]
    public void LcdHorizontalGridIntegratesAllThreeOpeningsOverThePanel(int panelHeight, double subpixelWeight)
    {
        const int width = 12;
        const int height = 1080;
        var colors = new float[width * height * 3];
        Array.Fill(colors, .25f);
        FilterLcdPixelStructure.Apply(colors, width - 1, panelHeight, width, height,
            new() { PixelStructure = EmulationLcdPixelStructure.Rgb, Orientation = EmulationLcdPixelOrientation.Horizontal });

        var sum = 0d;
        var hasColorDetail = false;
        for (var index = 0; index < colors.Length; index += 3)
        {
            sum += colors[index];
            hasColorDetail |= Math.Abs(colors[index] - colors[index + 2]) > .00001f;
        }
        var expected = .25 * .94 * .94 * (1d - 2d / 3d * subpixelWeight);
        Assert.Equal(expected, sum / (width * height), .00002d);
        Assert.Equal(subpixelWeight > 0d, hasColorDetail);
    }

    [Fact]
    public void LcdBlueSignalOnlyPassesThroughBlueOpenings()
    {
        const int width = 180;
        const int height = 90;
        var colors = new float[width * height * 3];
        for (var index = 2; index < colors.Length; index += 3) colors[index] = 1f;

        FilterLcdPixelStructure.Apply(colors, 2, 1, width, height,
            new() { PixelStructure = EmulationLcdPixelStructure.Rgb });

        Assert.Equal(0f, colors[(45 * width + 15) * 3 + 2]);
        Assert.Equal(0f, colors[(45 * width + 45) * 3 + 2]);
        Assert.Equal(1f, colors[(45 * width + 75) * 3 + 2], .00001f);
        Assert.Equal(1f, colors[(45 * width + 165) * 3 + 2], .00001f);
        Assert.Equal(0f, colors[(45 * width + 89) * 3 + 2]);
        Assert.Equal(0f, colors[75 * 3 + 2]);
        for (var index = 0; index < colors.Length; index += 3)
        {
            Assert.Equal(0f, colors[index]);
            Assert.Equal(0f, colors[index + 1]);
        }
    }

    [Fact]
    public void FixedLcdGridIsIndependentOfGameResolution()
    {
        byte[] Render(int width, int height, EmulationLcdDefinition definition)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            var pixels = new byte[width * height * 4];
            Array.Fill(pixels, (byte)180);
            var frame = new VideoFrame(pixels, width, height, width * 4,
                EmulationPixelFormat.Xrgb8888, 4f / 3f, 1, TimeSpan.Zero);
            var profile = Lcd(new() { Definition = definition, PixelStructure = EmulationLcdPixelStructure.Rgb });
            return pipeline.Process(profile, frame, new(width, height), new(1440, 1080)).Pixels.ToArray();
        }

        var vga = Render(16, 12, EmulationLcdDefinition.Vga);
        Assert.Equal(vga, Render(32, 24, EmulationLcdDefinition.Vga));
        Assert.False(vga.SequenceEqual(Render(32, 24, EmulationLcdDefinition.FullHd)));
        Assert.False(Render(16, 12, EmulationLcdDefinition.Native)
            .SequenceEqual(Render(32, 24, EmulationLcdDefinition.Native)));
    }

    [Fact]
    public void LcdMonochromeGridUsesNeutralLuminance()
    {
        var colors = Enumerable.Repeat(new[] { .7f, .25f, .1f }, 81).SelectMany(pixel => pixel).ToArray();
        FilterLcdPixelStructure.Apply(colors, 1, 1, 9, 9, new() { PixelStructure = EmulationLcdPixelStructure.Monochrome });
        for (var index = 0; index < colors.Length; index += 3)
        {
            Assert.Equal(colors[index], colors[index + 1]);
            Assert.Equal(colors[index], colors[index + 2]);
        }
        Assert.True(colors[0] < colors[(4 * 9 + 4) * 3]);
    }

    [Fact]
    public void PanelControlsBlackDepthAndDarkResponseAndBacklightCanTurnOff()
    {
        byte Sample(EmulationLcdPanelType panel, int light, byte level)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            return pipeline.Process(Lcd(new() { PanelType = panel, BacklightIntensity = light }),
                Frame(level), new(1, 1), new(1, 1)).Pixels.Span[0];
        }
        Assert.True(Sample(EmulationLcdPanelType.Tn, 100, 0) > Sample(EmulationLcdPanelType.Ips, 100, 0));
        Assert.True(Sample(EmulationLcdPanelType.Ips, 100, 0) > Sample(EmulationLcdPanelType.Va, 100, 0));
        Assert.Equal(0, Sample(EmulationLcdPanelType.Ips, 0, 255));
        Assert.True(FilterLcdDisplay.ResponseTime(EmulationLcdPanelType.Va, .05f, .1f)
            > FilterLcdDisplay.ResponseTime(EmulationLcdPanelType.Va, .4f, .5f));
    }

    [Fact]
    public void ConnectionDecodesTheIncomingSignalWithoutCrtMaskAndKeepsSeparateProfiles()
    {
        var pixels = new byte[] { 19, 85, 196, 255 };
        var frame = Frame(0) with { Pixels = pixels, SignalStandard = EmulationVideoStandard.Pal, SignalFieldRate = 50 };
        var lcd = new EmulationLcdVideoConfiguration
        {
            ColorStandards = EmulationCrtColorStandards.Ntsc,
            Connection = EmulationCrtConnection.SVideo
        };
        var filter = new FilterDisplayConnection();
        var settings = Lcd(lcd) with
        {
            Crt = new(EmulationCrtMask.Monochrome, EmulationCrtColorStandards.Pal) { Curvature = 20 }
        };
        var output = filter.Apply(pixels, frame, settings)!;
        Assert.Equal(output[0], output[1]);
        Assert.Equal(output[1], output[2]);
        var compatible = filter.Apply(pixels, frame, settings with { Lcd = lcd with { ColorStandards = EmulationCrtColorStandards.Pal } })!;
        Assert.NotEqual(compatible[0], compatible[2]);
        var wrongFrequency = CrtSignal.ForLcd(lcd with { FieldRate = EmulationCrtFieldRate.Hz60 }, frame);
        Assert.True(wrongFrequency.Tuning.X > 0);
        var led = EmulationVideoProcessingConfigurationFunctions.WithScreenType(settings, EmulationVideoScreenType.LedBacklitLcd);
        var ledPanel = lcd with { ColorStandards = EmulationCrtColorStandards.Pal, BacklightType = EmulationLedBacklightType.MiniLed };
        led = led with { Led = ledPanel };
        Assert.NotNull(filter.Apply(pixels, frame, led));
        Assert.Equal(ledPanel, EmulationVideoProcessingConfigurationFunctions.LcdPanel(led));
        var restored = EmulationVideoProcessingConfigurationFunctions.WithScreenType(led, EmulationVideoScreenType.Lcd);
        Assert.Equal(lcd, restored.Lcd);
        Assert.Equal(settings.Crt, restored.Crt);
    }

    [Fact]
    public void LedBacklightModesPreserveHighlightsAndReduceLightAroundThemByZone()
    {
        const int width = 64;
        const int height = 36;
        var pixels = new byte[width * height * 4];
        var highlight = (18 * width + 32) * 4;
        pixels.AsSpan(highlight, 4).Fill(255);
        var frame = new VideoFrame(pixels, width, height, width * 4,
            EmulationPixelFormat.Xrgb8888, 16f / 9f, 1, TimeSpan.Zero);

        byte[] Render(EmulationLedBacklightType type)
        {
            var configuration = EmulationVideoProcessingConfigurationFunctions.WithScreenType(
                new() { Led = new() { BacklightType = type, BacklightIntensity = 100, PanelType = EmulationLcdPanelType.Ips } },
                EmulationVideoScreenType.LedBacklitLcd);
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            return pipeline.Process(configuration, frame, new(width, height), new(width, height)).Pixels.ToArray();
        }

        var edge = Render(EmulationLedBacklightType.Edge);
        var direct = Render(EmulationLedBacklightType.Direct);
        var fullArray = Render(EmulationLedBacklightType.FullArray);
        var mini = Render(EmulationLedBacklightType.MiniLed);
        var besideHighlight = (18 * width + 36) * 4;
        Assert.True(edge[0] > direct[0]);
        Assert.True(direct[0] > fullArray[0]);
        Assert.True(fullArray[0] > mini[0]);
        Assert.True(fullArray[besideHighlight] > fullArray[0]);
        Assert.True(fullArray[besideHighlight] > mini[besideHighlight]);
        Assert.Equal(255, fullArray[highlight]);
        Assert.Equal(255, mini[highlight]);
    }

    [Theory]
    [InlineData(EmulationVideoStandard.Pal, EmulationCrtColorStandards.Pal)]
    [InlineData(EmulationVideoStandard.Secam, EmulationCrtColorStandards.Secam)]
    [InlineData(EmulationVideoStandard.Ntsc, EmulationCrtColorStandards.Ntsc)]
    public void ColorLockWorksOnLcdAndCrtRegardlessOfTheConnection(
        EmulationVideoStandard source, EmulationCrtColorStandards compatible)
    {
        byte[] pixels = [20, 85, 196, 255];
        var frame = Frame(0) with { Pixels = pixels, SignalStandard = source };
        var filter = new FilterDisplayConnection();
        var lcd = new EmulationLcdVideoConfiguration();
        Assert.Equal(EmulationCrtConnection.Digital, lcd.Connection);
        foreach (var connection in Enum.GetValues<EmulationCrtConnection>())
        foreach (var selection in new[] { EmulationCrtColorStandards.Pal, EmulationCrtColorStandards.Secam, EmulationCrtColorStandards.Ntsc })
        {
            var crt = new EmulationCrtVideoConfiguration(ColorStandards: selection) { Connection = connection };
            var output = filter.Apply(pixels, frame, Lcd(lcd with
            {
                ColorStandards = selection, Connection = connection, MonitorPalette = EmulationCrtMonitorPalette.Ega
            }))!;
            var hasColor = output[0] != output[1] || output[1] != output[2];
            Assert.Equal(selection == compatible, hasColor);
            Assert.Equal(CrtSignal.From(crt, frame).Tuning.Y > 0, hasColor);
        }
    }

    [Fact]
    public void BacklightLeakIsUnevenAlongTheEdgesAndFadesInward()
    {
        const int size = 100;
        float[] Render(EmulationLcdPanelType panel, float level)
        {
            var colors = new float[size * size * 3];
            Array.Fill(colors, level);
            FilterLcdDisplay.Apply(colors, size, size, new() { PanelType = panel, BacklightIntensity = 100 });
            return colors;
        }
        static float At(float[] colors, int x, int y) => colors[(y * size + x) * 3];

        var ips = Render(EmulationLcdPanelType.Ips, 0);
        var va = Render(EmulationLcdPanelType.Va, 0);
        var bright = Render(EmulationLcdPanelType.Ips, .75f);
        Assert.True(At(ips, 28, 0) > At(ips, 0, 0));
        Assert.True(At(ips, 28, 0) > At(ips, 28, 10));
        Assert.True(At(ips, 28, 10) > At(ips, 28, 50));
        Assert.True(At(ips, 28, 0) > At(ips, 28, 99));
        Assert.True(At(ips, 63, 99) > At(ips, 63, 0));
        Assert.True(At(ips, 28, 0) - At(ips, 28, 50) > At(va, 28, 0) - At(va, 28, 50));
        Assert.Equal(At(bright, 28, 0), At(bright, 28, 50));
    }

    [Fact]
    public void LightLeakFollowsHostOutputWhenEmulatedResolutionChanges()
    {
        static byte[] Render(int width, int height)
        {
            using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
            var input = new VideoFrame(new byte[width * height * 4], width, height, width * 4,
                EmulationPixelFormat.Xrgb8888, 4f / 3f, 1, TimeSpan.Zero);
            return pipeline.Process(Lcd(new() { BacklightIntensity = 100 }), input,
                new(width, height), new(120, 90)).Pixels.ToArray();
        }

        var first = Render(4, 3);
        var larger = Render(40, 30);
        Assert.Equal(first, larger);
        Assert.True(first[34 * 4] > first[(45 * 120 + 34) * 4]);
    }

    private static EmulationVideoProcessingConfiguration Lcd(EmulationLcdVideoConfiguration configuration) =>
        new() { DisplayTechnology = EmulationVideoDisplayTechnology.FixedPixel, Lcd = configuration };

    private static VideoFrame Frame(byte level) =>
        new(new byte[] { level, level, level, 255 }, 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1, 1, TimeSpan.Zero);
}
