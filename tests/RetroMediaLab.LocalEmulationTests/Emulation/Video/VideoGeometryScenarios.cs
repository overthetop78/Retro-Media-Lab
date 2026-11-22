using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using static RetroMediaLab.VideoPresentation.Enums.EmulationImageAdjustmentControls;
using RetroMediaLab.VideoPresentation.Functions;
using RetroMediaLab.App.Functions.Rendering.Emulation;
namespace RetroMediaLab.Tests.Emulation.Video;
internal static class VideoGeometryScenarios
{
    public static void ProcessingLimits()
    {
        var input=new EmulationVideoProcessingConfiguration
        {
            DisplayTechnology=(RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology)999,
            Sampling=(RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling)999,
            Crt=new(HorizontalCurvature:-500,VerticalCurvature:500,BeamWidth:-10,MaskIntensity:200),
            FixedPixel=new(ResponseTimeMilliseconds:5000,GridIntensity:-1,PersistenceIntensity:101,MonochromePalette:RetroMediaLab.VideoPresentation.Enums.EmulationMonochromePalette.White,BacklightIntensity:101,BlackDepth:-1),
            Restoration=new(Dedithering:-1,Debanding:101),Temporal=new(GeneralPersistence:101,MotionBlur:-1)
        };
        var output=EmulationVideoProcessingConfigurationFunctions.Normalize(input);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology.Normal,output.DisplayTechnology);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Nearest,output.Sampling);
        Assert.Equal(-100,output.Crt.HorizontalCurvature); Assert.Equal(100,output.Crt.VerticalCurvature); Assert.Equal(0,output.Crt.BeamWidth); Assert.Equal(100,output.Crt.MaskIntensity);
        Assert.Equal(1000,output.FixedPixel.ResponseTimeMilliseconds); Assert.Equal(0,output.FixedPixel.GridIntensity); Assert.Equal(100,output.FixedPixel.PersistenceIntensity);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationMonochromePalette.White,output.FixedPixel.MonochromePalette);
        Assert.Equal(100,output.FixedPixel.BacklightIntensity); Assert.Equal(0,output.FixedPixel.BlackDepth); Assert.Equal(0,output.Restoration.Dedithering); Assert.Equal(100,output.Restoration.Debanding);
        Assert.Equal(100,output.Temporal.GeneralPersistence); Assert.Equal(0,output.Temporal.MotionBlur);
        Assert.Equal(5000,input.FixedPixel.ResponseTimeMilliseconds); Assert.Equal(output,EmulationVideoProcessingConfigurationFunctions.Normalize(output));
    }
    public static void DeditheringRecognizesSourcePatterns()
    {
        const int size = 16;
        var patterns = new Func<int, int, int>[]
        {
            (x, y) => (x + y) & 1,
            (x, _) => x & 1,
            (_, y) => y & 1,
            (x, _) => (x / 2) & 1,
            (_, y) => (y / 2) & 1
        };
        byte[]? workspace = null;
        foreach (var pattern in patterns)
        {
            var input = new byte[size * size * 4];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var offset = (y * size + x) * 4;
                var color = pattern(x, y) == 0 ? (byte)48 : (byte)132;
                input.AsSpan(offset, 3).Fill(color);
                input[offset + 3] = 255;
            }

            // A thin, high-contrast mark interrupts the periodic region.
            var text = (10 * size + 10) * 4;
            input.AsSpan(text, 3).Fill(250);
            var result = EmulationImageRestorationFunctions.ApplyDedithering(input, size, size, 100, ref workspace);
            Assert.NotNull(result);
            var first = (5 * size + 5) * 4;
            var second = (6 * size + 6) * 4;
            Assert.InRange(result[first], 90, 110);
            Assert.Equal(result[first], result[second]);
            Assert.Equal((byte)250, result[text]);
            Assert.Equal(input[text - 4], result[text - 4]);
            Assert.Equal(input[0], result[0]);
            Assert.Equal((byte)255, result[first + 3]);
        }

        // A valid two-color pattern can span the full range of the source palette.
        var contrasted = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var offset = (y * size + x) * 4;
            contrasted.AsSpan(offset, 3).Fill(((x + y) & 1) == 0 ? (byte)0 : (byte)255);
            contrasted[offset + 3] = 255;
        }
        var mark = (10 * size + 10) * 4;
        contrasted.AsSpan(mark, 3).Fill(120);
        var merged = EmulationImageRestorationFunctions.ApplyDedithering(contrasted, size, size, 100, ref workspace);
        Assert.NotNull(merged);
        Assert.Equal((byte)188, merged[(5 * size + 5) * 4]);
        Assert.Equal((byte)188, merged[(5 * size + 6) * 4]);
        Assert.Equal((byte)120, merged[mark]);
        Assert.Equal((byte)255, merged[mark - 4]);

        var host = new EmulationVideoProcessingConfiguration { Restoration = new(Dedithering: 100) };
        var crt = EmulationVideoProcessingConfigurationFunctions.WithScreenType(host, EmulationVideoScreenType.Crt);
        Assert.Equal(100, EmulationVideoProcessingConfigurationFunctions.ForRendering(crt).Restoration.Dedithering);
        crt = crt with { SignalSimulation = crt.SignalSimulation with
            { Connection = EmulationSignalConnection.Composite, ConnectionIntensity = 50 } };
        Assert.Equal(0, EmulationVideoProcessingConfigurationFunctions.ForRendering(crt).Restoration.Dedithering);
        Assert.Equal(100, crt.Restoration.Dedithering);
        foreach (var screen in Enum.GetValues<EmulationVideoScreenType>())
        {
            var profile = EmulationVideoProcessingConfigurationFunctions.WithScreenType(host, screen);
            Assert.Equal(EmulationVideoProcessingConfigurationFunctions.SupportsDedithering(profile) ? 100 : 0,
                EmulationVideoProcessingConfigurationFunctions.ForRendering(profile).Restoration.Dedithering);
        }
    }

    public static void RestorationUsesTheSourceAndProtectsEdges()
    {
        const int width = 120;
        const int height = 24;
        var bands = new float[width * height * 3];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 3;
            var step = x / 12 * .017f;
            bands[offset] = .2f + step;
            bands[offset + 1] = .4f - step * .2126f / .7152f;
            bands[offset + 2] = .25f;
            if (x is >= 53 and <= 56 && y is >= 2 and <= 7) bands[offset + 2] = .95f;
        }

        var original = bands.ToArray();
        EmulationImageRestorationFunctions.ApplyDebanding(bands, width, height, 100);
        float RedAt(int x) => Enumerable.Range(10, 10).Average(y => bands[(y * width + x) * 3]);
        Assert.True(RedAt(57) - RedAt(50) > .005f);
        for (var x = 52; x <= 57; x++)
        {
            var offset = (4 * width + x) * 3;
            Assert.Equal(original.AsSpan(offset, 3).ToArray(), bands.AsSpan(offset, 3).ToArray());
        }

        const int sourceSize = 5;
        const int outputSize = 15;
        var source = Enumerable.Repeat(.2f, sourceSize * sourceSize * 3).ToArray();
        var scaled = Enumerable.Repeat(.2f, outputSize * outputSize * 3).ToArray();
        var sourceCenter = (2 * sourceSize + 2) * 3;
        var outputCenter = (7 * outputSize + 7) * 3;
        for (var channel = 0; channel < 3; channel++)
        {
            source[sourceCenter + channel] = .65f;
            scaled[outputCenter + channel] = .4f;
        }

        scaled[outputCenter + 3] = .65f;
        EmulationImageRestorationFunctions.ApplyDetailRecovery(source, sourceSize, sourceSize,
            scaled, outputSize, outputSize, 100);
        Assert.InRange(scaled[outputCenter], .649f, .65f);
        Assert.Equal(.65f, scaled[outputCenter + 3]);
        Assert.Equal(.2f, scaled[0]);

        var twoPixelLine = Enumerable.Repeat(.2f, 7 * 7 * 3).ToArray();
        var scaledLine = Enumerable.Repeat(.2f, 21 * 21 * 3).ToArray();
        for (var y = 0; y < 7; y++)
        for (var x = 3; x <= 4; x++)
            twoPixelLine.AsSpan((y * 7 + x) * 3, 3).Fill(.65f);
        var lineCenter = (10 * 21 + 10) * 3;
        scaledLine.AsSpan(lineCenter, 3).Fill(.4f);
        EmulationImageRestorationFunctions.ApplyDetailRecovery(twoPixelLine, 7, 7, scaledLine, 21, 21, 100);
        Assert.InRange(scaledLine[lineCenter], .649f, .65f);

        var configured = new EmulationVideoProcessingConfiguration { Restoration = new(Debanding: 75, DetailRecovery: 50) };
        var crt = EmulationVideoProcessingConfigurationFunctions.WithScreenType(configured, EmulationVideoScreenType.Crt);
        Assert.Equal(75, crt.Restoration.Debanding);
        Assert.Equal(0, EmulationVideoProcessingConfigurationFunctions.ForRendering(crt).Restoration.Debanding);
        Assert.Equal(0, EmulationVideoProcessingConfigurationFunctions.ForRendering(crt).Restoration.DetailRecovery);
        Assert.Equal(50, EmulationVideoProcessingConfigurationFunctions.ForRendering(configured).Restoration.DetailRecovery);
    }

    public static void ProcessingOrder()
    {
        using var pipeline=new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        var frame=new RetroMediaLab.Emulation.Contracts.VideoFrame(new byte[]{0,0,0,255,255,255,255,255},2,1,8,RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888,2,1,TimeSpan.Zero);
        byte[] Render(int brightness) =>
            pipeline.Process(new() { Adjustments = new(Brightness: brightness, Contrast: -20) },
                frame, new(2, 1), new(2, 1)).Pixels.ToArray();

        Assert.InRange(Render(0)[0], 126, 128);
        Assert.InRange(Render(0)[4], 130, 132);
        Assert.InRange(Render(-20)[4], 0, 2);
        Assert.InRange(Render(20)[0], 248, 251);
    }
    public static void Fit(double width, double height, double aspect, double expectedWidth, double expectedHeight)
    {
        var result = EmulationVideoLayoutFunctions.Fit(width, height, aspect);
        Assert.Equal(expectedWidth, result.Width, 8); Assert.Equal(expectedHeight, result.Height, 8);
        Assert.True(result.Width <= width && result.Height <= height);
    }
    public static void InvalidFit(double width, double height, double aspect) => Assert.True(EmulationVideoLayoutFunctions.Fit(width, height, aspect).IsEmpty);
    public static void FrameAspect()
    {
        var pixels = new byte[640 * 240 * 4];
        var reported = new RetroMediaLab.Emulation.Contracts.VideoFrame(pixels, 640, 240, 640 * 4, RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 4f / 3f, 1, TimeSpan.Zero);
        Assert.Equal(4d / 3d, RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter.FrameAspectRatio(reported), 6);
        Assert.Equal(640d / 240d, RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter.FrameAspectRatio(reported with { AspectRatio = 0 }), 6);
    }
    public static void Adjustments()
    {
        var original=new EmulationImageAdjustments(-100,100,3,-2,20);
        var result=EmulationImageAdjustmentFunctions.Normalize(original);
        Assert.Equal(new EmulationImageAdjustments(-20,20,0,-2,20),result);
        Assert.Equal(-100,original.Brightness);
        Assert.Equal(new EmulationImageAdjustments(),EmulationImageAdjustmentFunctions.Normalize(null));
    }
    public static void Gamma(int gamma, float expected) => Assert.Equal(expected, EmulationImageAdjustmentFunctions.GammaParameter(gamma));

    public static void ScreenTypeProfiles()
    {
        var host = new EmulationVideoProcessingConfiguration
        {
            Sampling = EmulationVideoSampling.Xbrz,
            Adjustments = new(Brightness: 20, Saturation: -20)
        };
        foreach (var screen in Enum.GetValues<EmulationVideoScreenType>())
        {
            var selected = EmulationVideoProcessingConfigurationFunctions.WithScreenType(host, screen);
            Assert.Equal(screen, EmulationVideoProcessingConfigurationFunctions.ScreenType(selected));
            Assert.Equal(EmulationVideoSampling.Xbrz, selected.Sampling);
            var rendering = EmulationVideoProcessingConfigurationFunctions.ForRendering(selected);
            Assert.Equal(screen == EmulationVideoScreenType.Host ? EmulationVideoSampling.Xbrz : EmulationVideoSampling.Nearest,
                rendering.Sampling);
            Assert.Equal(screen == EmulationVideoScreenType.Host ? host.Adjustments : new(), rendering.Adjustments);
        }

        var edited = host;
        foreach (var screen in Enum.GetValues<EmulationVideoScreenType>())
            edited = EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(edited, screen,
                _ => new(Brightness: (int)screen + 1, Contrast: -(int)screen, Gamma: 240, Saturation: 5, Sharpness: -5));

        var reloaded = System.Text.Json.JsonSerializer.Deserialize<EmulationVideoProcessingConfiguration>(
            System.Text.Json.JsonSerializer.Serialize(edited))!;
        foreach (var screen in Enum.GetValues<EmulationVideoScreenType>())
        {
            var selected = EmulationVideoProcessingConfigurationFunctions.WithScreenType(reloaded, screen);
            var stored = new EmulationImageAdjustments((int)screen + 1, -(int)screen, 240, 5, -5);
            Assert.Equal(stored, EmulationVideoProcessingConfigurationFunctions.ImageAdjustments(selected, screen));
            var expectedControls = screen switch
            {
                EmulationVideoScreenType.Vector or EmulationVideoScreenType.Vfd => Brightness,
                EmulationVideoScreenType.DotMatrix or EmulationVideoScreenType.SegmentDisplay => Brightness | Contrast,
                EmulationVideoScreenType.LedMatrix => Brightness | Contrast | EmulationImageAdjustmentControls.Gamma | Saturation,
                EmulationVideoScreenType.EPaper => Contrast,
                _ => All
            };
            Assert.Equal(expectedControls, EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(selected));
            Assert.Equal(new EmulationImageAdjustments(
                    expectedControls.HasFlag(Brightness) ? stored.Brightness : 0,
                    expectedControls.HasFlag(Contrast) ? stored.Contrast : 0,
                    expectedControls.HasFlag(EmulationImageAdjustmentControls.Gamma) ? stored.Gamma : 0,
                    expectedControls.HasFlag(Saturation) ? stored.Saturation : 0,
                    expectedControls.HasFlag(Sharpness) ? stored.Sharpness : 0),
                EmulationVideoProcessingConfigurationFunctions.ForRendering(selected).Adjustments);
        }

        var rgb = EmulationVideoProcessingConfigurationFunctions.WithScreenType(reloaded, EmulationVideoScreenType.LedMatrix);
        var mono = rgb with { LedMatrix = rgb.LedMatrix with { Color = EmulationLedMatrixColor.Red } };
        Assert.Equal(0, EmulationVideoProcessingConfigurationFunctions.ForRendering(mono).Adjustments.Saturation);
        Assert.Equal(5, EmulationVideoProcessingConfigurationFunctions.ForRendering(rgb).Adjustments.Saturation);

        var dots = EmulationVideoProcessingConfigurationFunctions.WithScreenType(reloaded, EmulationVideoScreenType.DotMatrix);
        var grayDots = dots with { DotMatrix = dots.DotMatrix with { Palette = EmulationDotMatrixPalette.Gray } };
        var rgbDots = dots with { DotMatrix = dots.DotMatrix with { Palette = EmulationDotMatrixPalette.Rgb } };
        Assert.Equal(Brightness | Contrast | EmulationImageAdjustmentControls.Gamma, EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(grayDots));
        Assert.Equal(Brightness | Contrast | EmulationImageAdjustmentControls.Gamma | Saturation,
            EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(rgbDots));

        var lcd = EmulationVideoProcessingConfigurationFunctions.WithScreenType(host, EmulationVideoScreenType.Lcd);
        lcd = lcd with { FixedPixel = lcd.FixedPixel with { BacklightIntensity = 27 } };
        var led = EmulationVideoProcessingConfigurationFunctions.WithScreenType(lcd, EmulationVideoScreenType.LedBacklitLcd);
        Assert.Null(led.FixedPixel.BacklightIntensity);
        led = led with { FixedPixel = led.FixedPixel with { BacklightIntensity = 81 } };
        var oled = EmulationVideoProcessingConfigurationFunctions.WithScreenType(led, EmulationVideoScreenType.Oled);
        oled = oled with { FixedPixel = oled.FixedPixel with { OledEmissionIntensity = 50 } };
        var reopened = System.Text.Json.JsonSerializer.Deserialize<EmulationVideoProcessingConfiguration>(
            System.Text.Json.JsonSerializer.Serialize(oled))!;
        var lcdAgain = EmulationVideoProcessingConfigurationFunctions.WithScreenType(reopened, EmulationVideoScreenType.Lcd);
        Assert.Equal(27, lcdAgain.FixedPixel.BacklightIntensity);
        var ledAgain = EmulationVideoProcessingConfigurationFunctions.WithScreenType(lcdAgain, EmulationVideoScreenType.LedBacklitLcd);
        Assert.Equal(81, ledAgain.FixedPixel.BacklightIntensity);
        var oledAgain = EmulationVideoProcessingConfigurationFunctions.WithScreenType(ledAgain, EmulationVideoScreenType.Oled);
        Assert.Equal(50, oledAgain.FixedPixel.OledEmissionIntensity);

        using var pipeline = new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(
            new byte[] { 0, 0, 0, 255, 255, 255, 255, 255 }, 2, 1, 8,
            RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 2, 1, TimeSpan.Zero);
        byte[] Render(EmulationVideoProcessingConfiguration processing) =>
            pipeline.Process(processing, frame, new(2, 1), new(8, 4)).Pixels.ToArray();
        var dimmed = Render(oledAgain);
        Assert.Equal(dimmed, Render(oledAgain with { Sampling = EmulationVideoSampling.Nearest, Adjustments = new() }));
        Assert.Equal(0, dimmed[0]);
        Assert.True(dimmed[16] < Render(oledAgain with { FixedPixel = oledAgain.FixedPixel with { OledEmissionIntensity = 100 } })[16]);
        var adjustedOled = EmulationVideoProcessingConfigurationFunctions.WithImageAdjustments(oledAgain,
            EmulationVideoScreenType.Oled, current => current with { Brightness = -20 });
        Assert.True(Render(adjustedOled)[16] < dimmed[16]);
        Assert.Equal(host.Adjustments, adjustedOled.Adjustments);
    }

    public static void AdjustmentEndpoints()
    {
        float[] lowContrast = [0f, .125f, .75f, 1f];
        var highContrast = lowContrast.ToArray();
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoContrastParameterFunctions.Apply(lowContrast, -20);
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoContrastParameterFunctions.Apply(highContrast, 20);
        Assert.All(lowContrast, value => Assert.InRange(value, .21f, .23f));
        Assert.Equal([0f, 0f, 1f, 1f], highContrast);

        float[] saturated = [.6f, .4f, .2f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSaturationParameterFunctions.Apply(saturated, -20);
        Assert.All(saturated, component => Assert.Equal(saturated[0], component));
        Assert.Equal(.42808f, saturated[0], 5);
        Assert.Equal(1f, EmulationImageAdjustmentFunctions.SaturationGain(0));
        Assert.Equal(3f, EmulationImageAdjustmentFunctions.SaturationGain(20));
        Assert.Equal("-20", EmulationImageAdjustmentFunctions.FormatSaturation(-20));
        Assert.Equal("+20", EmulationImageAdjustmentFunctions.FormatSaturation(20));
        float[] boosted = [.9f, .4f, .2f, .4f, .4f, .4f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSaturationParameterFunctions.Apply(boosted, 20);
        Assert.Equal(1f, boosted[0]);
        Assert.Equal(0f, boosted[2]);
        Assert.Equal([.4f, .4f, .4f], boosted[3..]);

        float[] edge = [.25f, .25f, .25f, .75f, .75f, .75f];
        var softened = edge.ToArray();
        var sharpened = edge.ToArray();
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSharpnessParameterFunctions.Apply(softened, 2, 1, -20);
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSharpnessParameterFunctions.Apply(sharpened, 2, 1, 20);
        Assert.InRange(softened[0], .4f, .45f);
        Assert.Equal(0f, sharpened[0]);
        Assert.InRange(softened[3], .55f, .6f);
        Assert.Equal(1f, sharpened[3]);
    }

    public static void GammaAndSharpnessPixels()
    {
        using var pipeline = new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(
            new byte[] { 0, 0, 0, 255, 128, 128, 128, 255, 255, 255, 255, 255 }, 3, 1, 12,
            RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 3, 1, TimeSpan.Zero);
        byte[] Render(EmulationImageAdjustments adjustments, int width) =>
            pipeline.Process(new() { Adjustments = adjustments }, frame, new(3, 1), new(width, 1)).Pixels.ToArray();

        var original = Render(new(), 3);
        var light = Render(new(Gamma: 180), 3);
        var reference = Render(new(Gamma: 220), 3);
        var dark = Render(new(Gamma: 240), 3);
        var bt1886 = Render(new(Gamma: EmulationImageAdjustmentFunctions.Bt1886Gamma), 3);
        Assert.Equal(frame.Pixels.ToArray(), original);
        Assert.True(light[4] > reference[4]);
        Assert.True(reference[4] > dark[4]);
        Assert.True(bt1886[0] > dark[0]);
        Assert.True(bt1886[4] > dark[4]);
        Assert.Equal(0, light[0]);
        Assert.Equal(0, dark[0]);
        Assert.Equal(255, light[8]);
        Assert.Equal(255, dark[8]);
        Assert.True(light[4] > 128);
        Assert.True(dark[4] < 128);
        float[] red = [1f, 0f, 0f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoGammaParameterFunctions.Apply(red, 240);
        Assert.InRange(red[0], .85f, .90f);
        Assert.Equal([0f, 0f], red[1..]);

        var scaled = Render(new(), 12);
        var softened = Render(new(Sharpness: -20), 12);
        var sharpened = Render(new(Sharpness: 20), 12);
        Assert.Equal(scaled[0], softened[0]);
        Assert.Equal(scaled[44], softened[44]);
        Assert.True(softened[12] > scaled[12]);
        Assert.True(softened[32] < scaled[32]);
        Assert.Equal(scaled[20], sharpened[20]);
        Assert.True(sharpened[16] > scaled[16]);
        Assert.True(sharpened[28] < scaled[28]);
    }
}
