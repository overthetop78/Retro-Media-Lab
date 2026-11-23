using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Services;
using System.Numerics;

namespace RetroMediaLab.Tests.Emulation.Video;
[Collection("WPF")]
public class VideoTests(RetroMediaLab.Tests.Application.TestInfrastructure.StaExecutionScenarios sta)
{
    [Fact]
    public void CrtMasksHaveDistinctPatternsAndMonochromeUsesOnlyLuminance()
    {
        var masks = Enum.GetValues<EmulationCrtMask>()
            .Where(mask => mask != EmulationCrtMask.Monochrome)
            .Select(mask =>
            {
                var settings = CrtPhosphorMask.Parameters(new(Mask: mask));
                return Enumerable.Range(0, 16 * 14)
                    .Select(pixel => CrtPhosphorMask.ApplyPixel(Vector3.One, pixel % 16, pixel / 16, settings))
                    .ToArray();
            }).ToArray();
        for (var i = 0; i < masks.Length; i++)
        for (var j = i + 1; j < masks.Length; j++)
            Assert.False(masks[i].SequenceEqual(masks[j]));

        var trinitron = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.ApertureGrille));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 0, trinitron),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 8, trinitron));
        Assert.NotEqual(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 0, trinitron),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 2, 0, trinitron));
        var slots = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.SlotMask));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 1, slots),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 8, slots));
        Assert.NotEqual(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 1, slots),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 9, slots));

        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 320, 240, 1280, default, 4f / 3, 0, TimeSpan.Zero);
        var raster50 = CrtSignal.From(new(), frame with { SignalFieldRate = 50 });
        var raster60 = CrtSignal.From(new(), frame with { SignalFieldRate = 60 });
        Assert.Equal(576, raster50.ActiveLines);
        Assert.Equal(480, raster60.ActiveLines);
        Assert.Equal(new Vector2(384f, 288f), CrtPhosphorMask.RasterCoordinate(new(.25f, .25f), raster50.ActiveLines));
        Assert.Equal(new Vector2(320f, 240f), CrtPhosphorMask.RasterCoordinate(new(.25f, .25f), raster60.ActiveLines));

        var red = new Vector3(1f, 0f, 0f);
        var green = new Vector3(0f, .2126f / .7152f, 0f);
        foreach (var phosphor in Enum.GetValues<EmulationCrtPhosphorColor>())
        {
            var settings = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.Monochrome, PhosphorColor: phosphor));
            var first = CrtPhosphorMask.ApplyPixel(red, 0, 0, settings);
            var second = CrtPhosphorMask.ApplyPixel(green, 5, 8, settings);
            Assert.InRange(Vector3.Distance(first, second), 0f, .00001f);
            Assert.InRange(Vector3.Distance(first, .2126f * new Vector3(settings.X, settings.Y, settings.Z)), 0f, .00001f);
        }
    }

    [Fact]
    public void CrtResolvesColorAndSyncIndependently()
    {
        var source = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 320, 256, 1280, default, 4f / 3, 2, TimeSpan.Zero)
        {
            SignalStandard = RetroMediaLab.Emulation.Enums.EmulationVideoStandard.Pal,
            SignalFieldRate = 60
        };
        var receiver = new RetroMediaLab.VideoPresentation.Contracts.EmulationCrtVideoConfiguration
        {
            ColorStandards = RetroMediaLab.VideoPresentation.Enums.EmulationCrtColorStandards.Secam,
            FieldRate = RetroMediaLab.VideoPresentation.Enums.EmulationCrtFieldRate.Hz50
        };
        var incompatible = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(receiver, source);
        Assert.Equal(1.2f, incompatible.Tuning.X);
        Assert.Equal(0f, incompatible.Tuning.Y);
        Assert.Equal(288f / 256, incompatible.Raster.X);
        Assert.NotEqual(incompatible.SourceUv(new(.5f, .5f)),
            RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(receiver, source with { Sequence = 3 })
                .SourceUv(new(.5f, .5f)));

        var wrongColor = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(
            receiver with { FieldRate = RetroMediaLab.VideoPresentation.Enums.EmulationCrtFieldRate.Hz50And60 }, source);
        Assert.Equal(0f, wrongColor.Tuning.X);
        Assert.Equal(0f, wrongColor.Tuning.Y);
        Assert.Equal(240f / 256, wrongColor.Raster.X);

        var locked = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(new(), source);
        Assert.Equal(0f, locked.Tuning.X);
        Assert.Equal(2f, locked.Tuning.Y);
        Assert.Equal(new System.Numerics.Vector2(.5f, .5f), locked.SourceUv(new(.5f, .5f)));
    }

    [Fact]
    public void CrtIgnoresLegacyPhosphorAndDecodesTheSourceStandard()
    {
        var oldProfile = System.Text.Json.JsonSerializer.Deserialize<RetroMediaLab.VideoPresentation.Contracts.EmulationCrtVideoConfiguration>(
            """{"ColorMode":1,"BeamIntensity":35,"MaskIntensity":0,"HorizontalCurvature":50,"ScanlinesEnabled":true}""");
        Assert.NotNull(oldProfile);
        var source = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 320, 240, 1280, default, 4f / 3, 0, TimeSpan.Zero)
        {
            SignalFieldRate = 60
        };
        var color = new System.Numerics.Vector3(.6f, .3f, .1f);
        var neighbor = new System.Numerics.Vector3(.2f, .3f, .6f);
        var ntsc = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(oldProfile, source);
        Assert.Equal(1f, ntsc.Tuning.Y);
        var sameColor = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.Decode(color, color, (int)ntsc.Tuning.Y, 0);
        Assert.InRange(System.Numerics.Vector3.Distance(color, sameColor), 0f, .00001f);
        var pal = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(oldProfile,
            source with { SignalStandard = RetroMediaLab.Emulation.Enums.EmulationVideoStandard.Pal });
        var secam = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(oldProfile,
            source with { SignalStandard = RetroMediaLab.Emulation.Enums.EmulationVideoStandard.Secam, SignalFieldRate = 50 });
        Assert.Equal(2f, pal.Tuning.Y);
        Assert.Equal(3f, secam.Tuning.Y);
        var ntscEdge = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.Decode(color, neighbor, (int)ntsc.Tuning.Y, 0);
        var palEdge = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.Decode(color, neighbor, (int)pal.Tuning.Y, 0);
        var secamEdge = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.Decode(color, neighbor, (int)secam.Tuning.Y, 0);
        Assert.NotEqual(ntscEdge, palEdge);
        Assert.NotEqual(palEdge, secamEdge);
        Assert.NotEqual(secamEdge, RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.Decode(color, neighbor, (int)secam.Tuning.Y, 1));

        var forced60 = oldProfile with { FieldRate = RetroMediaLab.VideoPresentation.Enums.EmulationCrtFieldRate.Hz60 };
        var rolled50 = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(forced60, source with { SignalFieldRate = 50, Sequence = 1 });
        Assert.InRange(rolled50.Tuning.X, .83f, .84f);
        Assert.InRange(rolled50.Raster.W, .79f, .81f);
    }

    [Theory]
    [InlineData(90, 60, "001")]
    [InlineData(120, 60, "01")]
    [InlineData(240, 60, "0100")]
    [InlineData(60, 50, "000001")]
    [InlineData(90, 50, "001010101")]
    [InlineData(100, 50, "01")]
    [InlineData(120, 50, "010101001010")]
    [InlineData(240, 50, "010001000010000100001000")]
    public void BlackFrameCadenceKeepsEveryGameFrameAndBlanksAtMostOnce(double hostHz, double emulatedHz, string cycle)
    {
        Assert.True(BlackFrameCadence.IsAvailable(hostHz, emulatedHz));
        var cadence = new BlackFrameCadence();
        var previousImage = -1d;
        for (var refresh = 0; refresh < cycle.Length * 2; refresh++)
        {
            var image = Math.Ceiling((refresh + 1) * emulatedHz / hostHz) - 1;
            Assert.Equal(cycle[refresh % cycle.Length] == '1', cadence.ShouldBlank(refresh, 0, image != previousImage));
            previousImage = image;
        }
    }

    [Fact]
    public void BlackFrameCadenceKeepsAnImageAfterPauseOrMissedRefreshes()
    {
        Assert.False(BlackFrameCadence.IsAvailable(120, 0));
        Assert.False(BlackFrameCadence.IsAvailable(60, 60));
        Assert.False(BlackFrameCadence.IsAvailable(50, 60));
        var cadence = new BlackFrameCadence();
        Assert.False(cadence.ShouldBlank(0, 0, true));
        Assert.True(cadence.ShouldBlank(1, 0, false));
        Assert.False(cadence.ShouldBlank(1, 0, false));
        Assert.False(cadence.ShouldBlank(3, 0, false));
        Assert.False(cadence.ShouldBlank(100, 1, false));
        Assert.False(cadence.ShouldBlank(101, 1, true));
        Assert.True(cadence.ShouldBlank(102, 1, false));
        Assert.False(cadence.ShouldBlank(103, 1, false));
    }

    [Theory]
    [InlineData("""{"processing":{"temporal":{"blackFrameInsertion":true}}}""", true)]
    [InlineData("""{"processing":{"blackFrameInsertion":false,"temporal":{"blackFrameInsertion":true}}}""", false)]
    public void BlackFrameInsertionReadsOlderProfiles(string json, bool enabled)
    {
        var files = new MemoryVideoProfileFiles();
        var id = Guid.NewGuid();
        var store = new VideoPresentationProfileStore("virtual-video", files);
        store.Save("sega", id);
        var path = Assert.Single(files.Files.Keys);
        files.Files[path] = json;

        var reopened = new VideoPresentationProfileStore("virtual-video", files);
        Assert.Equal(enabled, reopened.Get("sega", id).Processing!.BlackFrameInsertion);
        reopened.Save("sega", id);
        Assert.DoesNotContain("\"temporal\"", files.Files[path]);
    }
    [Fact]
    public Task PresentationComposesFieldsBeforeDroppedFramesAndClearsHistoryOnMachineChange() =>
        sta.RunAsync(VideoProcessingScenarios.FieldPresentation);

    [Fact]
    public void NativeFieldsUseTheirParityAndRejectStaleHistory() => VideoProcessingScenarios.NativeFieldDeinterlacing();

    [Fact]
    public void EveryVideoParameterLabelResolvesInFrench()
    {
        var localization = RetroMediaLab.App.Localization.Sources.LocalizationSource.Instance;
        var previous = (localization.Culture, localization.UiCulture);
        try
        {
            var french = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            localization.SetCultures(french, french);
            Assert.Equal("Emulation.Video.Gamma", RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog.ParameterResourceKeys[RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog.Gamma]);
            foreach (var resourceKey in RetroMediaLab.VideoPresentation.Dictionaries
                         .EmulationVideoProcessingCatalog.ParameterResourceKeys.Values)
            {
                var label = RetroMediaLab.App.Localization.Extensions.LocExtension.Get(resourceKey);
                Assert.False(string.IsNullOrWhiteSpace(label), resourceKey);
                Assert.NotEqual($"[{resourceKey}]", label);
            }
        }
        finally
        {
            localization.SetCultures(previous.Culture, previous.UiCulture);
        }
    }

    [Fact] public void VideoProcessingNormalizesEnumsIntensitiesAndPalette() => VideoGeometryScenarios.ProcessingLimits();
    [Fact] public void DeditheringRecognizesSourcePatterns() => VideoGeometryScenarios.DeditheringRecognizesSourcePatterns();
    [Fact] public void VideoRestorationUsesTheSourceAndProtectsEdges() => VideoGeometryScenarios.RestorationUsesTheSourceAndProtectsEdges();
    [Fact] public void VideoProcessingCombinesLowContrastWithGlobalBrightness() => VideoGeometryScenarios.ProcessingOrder();
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public Task PresentationRoutesFramesSettingsAndRendererFailures(int failure) => sta.RunAsync(() => VideoProcessingScenarios.Presentation(failure));
    [Theory] [InlineData(-20,(byte)0,(byte)1)] [InlineData(0,(byte)0,(byte)255)] [InlineData(20,(byte)248,(byte)255)]
    public void SoftwarePipelineAdjustsLinearBrightnessAndPreservesMetadata(int setting,byte dark,byte light) => VideoProcessingScenarios.PipelineBrightness(setting,dark,light);
    [Fact] public void SoftwarePipelineNearestSamplingDuplicatesExpectedPixels() => VideoProcessingScenarios.Resize();
    [Theory] [InlineData(RetroMediaLab.VideoPresentation.Enums.EmulationVideoRenderer.Direct3D11)] [InlineData(RetroMediaLab.VideoPresentation.Enums.EmulationVideoRenderer.OpenGL)] [InlineData(RetroMediaLab.VideoPresentation.Enums.EmulationVideoRenderer.Vulkan)]
    public void GpuRendererRoutingPreservesSourceFrame(RetroMediaLab.VideoPresentation.Enums.EmulationVideoRenderer renderer) => VideoProcessingScenarios.RendererRouting(renderer);
    [Theory] [InlineData(RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888)] [InlineData(RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Rgb565)] [InlineData(RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Rgb1555)]
    public void PackedPixelsAndRowPadding(RetroMediaLab.Emulation.Enums.EmulationPixelFormat format) => VideoProcessingScenarios.Pixels(format);
    [Theory] [InlineData(800, 600, 2, 800, 400)] [InlineData(800, 600, 1, 600, 600)] [InlineData(320, 480, 4d/3, 320, 240)]
    public void AspectRatioFitsAvailableArea(double width, double height, double aspect, double expectedWidth, double expectedHeight) => VideoGeometryScenarios.Fit(width,height,aspect,expectedWidth,expectedHeight);
    [Theory]
    [InlineData(4d / 3, 1440, 1440, 1080)]
    [InlineData(5d / 4, 1350, 1350, 1012.5)]
    [InlineData(16d / 9, 1920, 1440, 1080)]
    public void FourThreeGameFitsInsideTheSelectedScreen(double aspect, double screenWidth, double imageWidth, double imageHeight)
    {
        var (screen, image) = EmulationVideoLayoutFunctions.FitDisplay(1920, 1080, aspect, 4d / 3);
        Assert.Equal(screenWidth, screen.Width, 6);
        Assert.Equal(1080, screen.Height, 6);
        Assert.Equal(imageWidth, image.Width, 6);
        Assert.Equal(imageHeight, image.Height, 6);
    }

    [Theory]
    [InlineData(50d, 576)]
    [InlineData(60d, 480)]
    public void SynchronizedCrtFitsTheWholeGameIntoItsActiveRaster(double fieldRate, int activeLines)
    {
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = fieldRate };
        var signal = CrtSignal.From(new(), frame);
        Assert.Equal(activeLines, signal.ActiveLines);
        Assert.Equal(Vector2.Zero, signal.SourceUv(Vector2.Zero));
        Assert.Equal(Vector2.One, signal.SourceUv(Vector2.One));
    }

    [Theory] [InlineData(0, 100, 1)] [InlineData(100, -1, 1)] [InlineData(100, 100, double.NaN)] [InlineData(100, 100, double.PositiveInfinity)] [InlineData(100, 100, 0)]
    public void InvalidGeometryProducesEmptyArea(double width, double height, double aspect) => VideoGeometryScenarios.InvalidFit(width,height,aspect);
    [Fact] public void PresenterUsesTheAspectRatioReportedByTheEmulator() => VideoGeometryScenarios.FrameAspect();
    [Fact] public void ImageAdjustmentsClampAndPreserveInput()=>VideoGeometryScenarios.Adjustments();
    [Theory]
    [InlineData(-20,0f)]
    [InlineData(0,0f)]
    [InlineData(180,1.8f)]
    [InlineData(190,1.9f)]
    [InlineData(220,2.2f)]
    [InlineData(240,2.4f)]
    [InlineData(1886,-1f)]
    public void GammaSelectsAStandardCurve(int gamma, float expected) =>
        VideoGeometryScenarios.Gamma(gamma, expected);
    [Fact] public void VideoScreenTypesPreserveProfilesAndUseOnlyTheirOwnControls() => VideoGeometryScenarios.ScreenTypeProfiles();
    [Fact] public void ImageAdjustmentEndpointsChangeTheExpectedSignal() => VideoGeometryScenarios.AdjustmentEndpoints();
    [Fact] public void GammaSelectsLuminanceCurveAndSharpnessAffectsOutputPixels() => VideoGeometryScenarios.GammaAndSharpnessPixels();
    [Fact] public void PaletteSelectionIgnoresAlpha()=>VideoProcessingScenarios.Palettes();
}
