using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.VideoPresentation.Services;
using System.Numerics;

namespace RetroMediaLab.Tests.Emulation.Video;
[Collection("WPF")]
public class VideoTests(RetroMediaLab.Tests.Application.TestInfrastructure.StaExecutionScenarios sta)
{
    [Fact]
    public void CrtConnectionAppliesDistinctAnalogProfilesAndCleanDigitalPassThrough()
    {
        const int width = 40, height = 8;
        var input = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            input[i * 4] = i % 2 == 0 ? (byte)20 : (byte)220;
            input[i * 4 + 1] = i % 3 == 0 ? (byte)235 : (byte)30;
            input[i * 4 + 2] = i % 4 < 2 ? (byte)255 : (byte)4;
            input[i * 4 + 3] = 255;
        }
        var frame = new VideoFrame(input, width, height, width * 4, EmulationPixelFormat.Xrgb8888,
            4f / 3f, 9, TimeSpan.Zero);
        var settings = new EmulationVideoProcessingConfiguration { DisplayTechnology = EmulationVideoDisplayTechnology.Crt };
        byte[]? scratch = null;
        Assert.Null(FilterCrtConnection.Apply(input, frame, settings, ref scratch));
        byte[] Apply(EmulationCrtConnection connection, int degradation, VideoFrame source)
        {
            var active = settings with { Crt = new() { Connection = connection, ConnectionDegradation = degradation } };
            return FilterCrtConnection.Apply(input, source, active, ref scratch)?.ToArray() ?? input;
        }

        Assert.Same(input, Apply(EmulationCrtConnection.Scart, 0, frame));
        Assert.Same(input, Apply(EmulationCrtConnection.Component, 0, frame));
        var profiles = Enum.GetValues<EmulationCrtConnection>()
            .Where(connection => connection is >= EmulationCrtConnection.Scart and <= EmulationCrtConnection.AnalogRf)
            .Select(connection => Apply(connection, 20, frame)).ToArray();
        Assert.All(profiles, profile => Assert.False(input.SequenceEqual(profile)));
        for (var i = 0; i < profiles.Length; i++)
        for (var j = i + 1; j < profiles.Length; j++)
            Assert.False(profiles[i].SequenceEqual(profiles[j]));

        Assert.False(input.SequenceEqual(Apply(EmulationCrtConnection.Composite, 0, frame)));
        var rf = Apply(EmulationCrtConnection.AnalogRf, 20, frame);
        Assert.Equal(rf, Apply(EmulationCrtConnection.AnalogRf, 20, frame));
        Assert.False(rf.SequenceEqual(Apply(EmulationCrtConnection.AnalogRf, 20, frame with { Sequence = 10 })));
    }

    [Theory]
    [InlineData(EmulationCrtColorStandards.Pal, EmulationVideoStandard.Pal)]
    [InlineData(EmulationCrtColorStandards.Secam, EmulationVideoStandard.Secam)]
    [InlineData(EmulationCrtColorStandards.Ntsc, EmulationVideoStandard.Ntsc)]
    public void CrtRfUsesTheSelectedStandardAndMatchesIncomingSignalOnMultistandardReceivers(
        EmulationCrtColorStandards standard, EmulationVideoStandard sourceStandard)
    {
        byte[] pixels = Enumerable.Repeat(new byte[] { 43, 65, 188, 255, 205, 81, 26, 255 }, 256)
            .SelectMany(pair => pair).ToArray();
        var frame = new VideoFrame(pixels, 32, 16, 128, EmulationPixelFormat.Xrgb8888, 2f, 29, TimeSpan.Zero)
            { SignalStandard = sourceStandard };
        var settings = new EmulationVideoProcessingConfiguration
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.Crt,
            Crt = new() { Connection = EmulationCrtConnection.AnalogRf, ConnectionDegradation = 20 }
        };
        byte[] Render(EmulationCrtColorStandards selection, EmulationCrtConnection connection = EmulationCrtConnection.AnalogRf)
        {
            byte[]? scratch = null;
            return FilterCrtConnection.Apply(pixels, frame,
                settings with { Crt = settings.Crt with { Connection = connection, ColorStandards = selection } }, ref scratch)!;
        }

        var selected = Render(standard);
        const EmulationCrtColorStandards all = EmulationCrtColorStandards.PalSecamNtsc;
        Assert.Equal(selected, Render(all));
        foreach (var other in new[] { EmulationCrtColorStandards.Pal, EmulationCrtColorStandards.Secam, EmulationCrtColorStandards.Ntsc })
        {
            if (other == standard) continue;
            Assert.False(selected.SequenceEqual(Render(other)));
            Assert.Equal(Render(standard, EmulationCrtConnection.Composite), Render(other, EmulationCrtConnection.Composite));
            Assert.Equal(0f, CrtSignal.From(settings.Crt with { ColorStandards = other }, frame).Tuning.Y);
        }
    }

    [Theory]
    [InlineData(EmulationCrtMonitorPalette.Mda, 127, 127, 127, 145, 145, 145)]
    [InlineData(EmulationCrtMonitorPalette.Cga, 168, 83, 3, 170, 85, 0)]
    [InlineData(EmulationCrtMonitorPalette.Ega, 181, 100, 20, 170, 85, 0)]
    public void CrtLegacyMonitorsQuantizeTheSourceBeforeThePhosphors(EmulationCrtMonitorPalette palette,
        byte r, byte g, byte b, byte expectedR, byte expectedG, byte expectedB)
    {
        byte[] source = [b, g, r, 255];
        var frame = new VideoFrame(source, 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1f, 0, TimeSpan.Zero);
        var configuration = new EmulationVideoProcessingConfiguration
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.Crt,
            Crt = new() { Connection = EmulationCrtConnection.LegacyMonitor, MonitorPalette = palette }
        };
        byte[]? scratch = null;
        Assert.Equal(new byte[] { expectedB, expectedG, expectedR, 255 },
            FilterCrtConnection.Apply(source, frame, configuration, ref scratch));
        using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
        var monochrome = configuration with { Crt = configuration.Crt with { Mask = EmulationCrtMask.Monochrome } };
        var output = pipeline.Process(monochrome, frame, new(1, 1), new(1, 1)).Pixels.Span;
        Assert.True(output[0] > 0);
        Assert.Equal(output[0], output[1]);
        Assert.Equal(output[1], output[2]);
    }

    [Theory]
    [InlineData(-20, 28, 32)]
    [InlineData(0, 64, 64)]
    [InlineData(20, 122, 128)]
    public void CrtBrightnessChangesBeamEmissionAndPreservesBlack(int setting, int minimum, int maximum)
    {
        using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
        byte[] pixels = [0, 0, 0, 255, 64, 64, 64, 255];
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(pixels, 2, 1, 8,
            RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 2f, 1, TimeSpan.Zero);
        var settings = new RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.Crt
        };
        settings = RetroMediaLab.VideoPresentation.Functions.EmulationVideoProcessingConfigurationFunctions
            .WithImageAdjustments(settings, EmulationVideoScreenType.Crt, adjustments => adjustments with { Brightness = setting });
        var output = pipeline.Process(settings, frame, new(2, 1), new(2, 1)).Pixels.Span;
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, output[..4].ToArray());
        Assert.All(output[4..7].ToArray(), component => Assert.InRange((int)component, minimum, maximum));
    }

    [Fact]
    public void CrtCurvatureBendsThePictureWithoutChangingTheSimulatedMaskGrid()
    {
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = 60 };
        var flat = CrtSignal.From(new(), frame);
        var mild = CrtSignal.From(new() { Curvature = 10 }, frame);
        var curved = CrtSignal.From(new() { Curvature = 20 }, frame);
        var center = new Vector2(.5f);
        var nearCorner = new Vector2(.94f);
        var lineMiddle = new Vector2(.5f, .8f);
        var lineEnd = new Vector2(.8f, .8f);
        Assert.Equal(nearCorner, flat.ScreenUv(nearCorner));
        Assert.Equal(center, curved.ScreenUv(center));
        Assert.True(flat.ScreenUv(nearCorner).X < mild.ScreenUv(nearCorner).X);
        Assert.True(mild.ScreenUv(nearCorner).X < curved.ScreenUv(nearCorner).X);
        Assert.True(curved.ScreenUv(lineEnd).Y > curved.ScreenUv(lineMiddle).Y);
        Assert.False(CrtSignal.IsOnScreen(curved.ScreenUv(nearCorner)));
        Assert.Equal(flat.MaskRasterSize, curved.MaskRasterSize);
        var pixelSize = new Vector2(1f / 1440, 1f / 1080);
        Assert.Equal(pixelSize, curved.ScreenFootprint(center, pixelSize));
        Assert.True(curved.ScreenFootprint(lineEnd, pixelSize).X > pixelSize.X);
    }

    [Theory]
    [InlineData(EmulationCrtMask.None)]
    [InlineData(EmulationCrtMask.ApertureGrille)]
    public void CrtCurvatureMakesRoundedBlackCornersWithOrWithoutAPhosphorMask(EmulationCrtMask mask)
    {
        const int width = 160, height = 120;
        using var pipeline = new SoftwareEmulationVideoProcessingPipeline();
        var white = new byte[width * height * 4];
        Array.Fill(white, byte.MaxValue);
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(white, width, height, width * 4,
            RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 4f / 3f, 1, TimeSpan.Zero) { SignalFieldRate = 60 };
        var settings = new RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.Crt,
            Crt = new(Mask: mask)
        };
        var flat = pipeline.Process(settings, frame, new(width, height), new(width, height)).Pixels.ToArray();
        var curved = pipeline.Process(settings with { Crt = settings.Crt with { Curvature = 20 } },
            frame, new(width, height), new(width, height)).Pixels.Span;
        var upperLeft = (8 * width + 10) * 4;
        var topMiddle = (8 * width + width / 2) * 4;
        Assert.True(flat[upperLeft] + flat[upperLeft + 1] + flat[upperLeft + 2] > 32);
        Assert.True(curved[topMiddle] + curved[topMiddle + 1] + curved[topMiddle + 2] > 32);
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, curved.Slice(upperLeft, 4).ToArray());
    }

    [Fact]
    public void CrtBeamWidthChangesTheShoulderWhilePreservingThePeak()
    {
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = 50 };
        var wide = CrtSignal.From(new(BeamWidth: -20), frame);
        var normal = CrtSignal.From(new(), frame);
        var thin = CrtSignal.From(new(BeamWidth: 20), frame);
        float shoulder = .25f / 288f;
        Assert.True(wide.Beam(shoulder, 1080) > normal.Beam(shoulder, 1080));
        Assert.True(normal.Beam(shoulder, 1080) > thin.Beam(shoulder, 1080));
        Assert.Equal(.91f, normal.Beam(shoulder, 1080), 5);
        foreach (var signal in new[] { wide, normal, thin })
            Assert.Equal(1f, signal.Beam(.5f / 288f, 1080), 5);
    }

    [Fact]
    public void CrtMasksHaveDistinctPatternsAndMonochromeUsesOnlyLuminance()
    {
        var masks = Enum.GetValues<EmulationCrtMask>()
            .Where(mask => mask != EmulationCrtMask.Monochrome)
            .Select(mask => (Mask: mask, Pixels: Enumerable.Range(0, 128 * 14)
                .Select(pixel => mask == EmulationCrtMask.None ? Vector3.One
                    : CrtPhosphorMask.Coverage(new((pixel % 128 + .5f) / 8f, pixel / 128 + .5f),
                        new(.125f, 1f), mask))
                .ToArray())).ToArray();
        for (var i = 0; i < masks.Length; i++)
        for (var j = i + 1; j < masks.Length; j++)
            Assert.False(masks[i].Pixels.SequenceEqual(masks[j].Pixels), $"{masks[i].Mask} / {masks[j].Mask}");

        var trinitron = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.ApertureGrille));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 0, trinitron),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 8, trinitron));
        Assert.NotEqual(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 0, trinitron),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 2, 0, trinitron));
        var slots = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.SlotMask));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 1, slots),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 8, slots));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 1, slots),
            CrtPhosphorMask.ApplyPixel(Vector3.One, 0, 9, slots));

        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 320, 240, 1280, default, 4f / 3, 0, TimeSpan.Zero);
        var raster50 = CrtSignal.From(new(), frame with { SignalFieldRate = 50 });
        var raster60 = CrtSignal.From(new(), frame with { SignalFieldRate = 60 });
        Assert.Equal(576, raster50.ActiveLines);
        Assert.Equal(480, raster60.ActiveLines);
        Assert.Equal(new Vector2(768f, 576f), raster50.MaskRasterSize);
        Assert.Equal(new Vector2(640f, 480f), raster60.MaskRasterSize);
        var masterSystem = CrtSignal.From(new(), frame with { Width = 256, Height = 192, SignalFieldRate = 50 });
        Assert.Equal(raster50.MaskRasterSize, masterSystem.MaskRasterSize);
        var at1080p = CrtPhosphorMask.RasterCoordinate(new Vector2(360.5f / 1440f, 270.5f / 1080f), masterSystem.MaskRasterSize);
        var at2160p = CrtPhosphorMask.RasterCoordinate(new Vector2(720.5f / 2880f, 540.5f / 2160f), masterSystem.MaskRasterSize);
        Assert.Equal(at1080p, at2160p);

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

    [Theory]
    [InlineData(EmulationVideoAspectRatio.FiveFour, 600f)]
    [InlineData(EmulationVideoAspectRatio.FourThree, 640f)]
    [InlineData(EmulationVideoAspectRatio.FourteenNine, 746.6667f)]
    [InlineData(EmulationVideoAspectRatio.SixteenNine, 853.3333f)]
    [InlineData(EmulationVideoAspectRatio.TwentyOneNine, 1120f)]
    public void CrtFormatChangesTheNumberOfSlitsWithoutChangingTheirPitch(EmulationVideoAspectRatio aspect, float expectedWidth)
    {
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = 60 };
        var signal = CrtSignal.From(new(), frame, aspect);
        Assert.InRange(signal.MaskRasterSize.X, expectedWidth - .001f, expectedWidth + .001f);
        Assert.Equal(480f, signal.MaskRasterSize.Y);
        Assert.Equal(3f, CrtPhosphorMask.RasterCoordinate(new(1.25f / signal.MaskRasterSize.X, 0f), signal.MaskRasterSize).X);
    }

    [Fact]
    public void CrtStripedMaskPassesOnlyOneChannelAndBlocksTheSeparator()
    {
        var mask = CrtPhosphorMask.Parameters(new(Mask: EmulationCrtMask.SlotMask));
        var color = new Vector3(.2f, .6f, .8f);
        Assert.Equal(new Vector3(color.X, 0f, 0f), CrtPhosphorMask.ApplyPixel(color, 0, 0, mask));
        Assert.Equal(new Vector3(0f, color.Y, 0f), CrtPhosphorMask.ApplyPixel(color, 2, 0, mask));
        Assert.Equal(new Vector3(0f, 0f, color.Z), CrtPhosphorMask.ApplyPixel(color, 4, 0, mask));
        Assert.Equal(Vector3.Zero, CrtPhosphorMask.ApplyPixel(color, 5, 0, mask));
        Assert.Equal(CrtPhosphorMask.ApplyPixel(color, 0, 0, mask),
            CrtPhosphorMask.ApplyPixel(color, 6, 0, mask));
        Assert.Equal(new Vector3(1.75f / 6f),
            CrtPhosphorMask.Coverage(Vector2.Zero, new(6f, 1f), EmulationCrtMask.SlotMask));
    }

    [Fact]
    public void CrtTrinitronHasThreeEqualVerticalSeparatorsPerRepeat()
    {
        var mask = EmulationCrtMask.ApertureGrille;
        var channels = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
        foreach (var height in new[] { .5f, 3f, 8f, 480f })
        {
            for (var i = 0; i < channels.Length; ++i)
            {
                Assert.Equal(channels[i], CrtPhosphorMask.Coverage(new(2f * i + 1f, height), new(.125f, 1f), mask));
                Assert.Equal(Vector3.Zero, CrtPhosphorMask.Coverage(new(2f * i + 1.875f, height), new(.125f, 1f), mask));
            }
        }
    }

    [Fact]
    public void CrtTrinitronRendersContinuousColumnsOnAFlatSignal()
    {
        const int width = 384;
        const int height = 576;
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = 60 };

        float VerticalVariation(EmulationCrtMask mask)
        {
            var signal = CrtSignal.From(new(Mask: mask), frame);
            var pixels = new float[width * height * 3];
            Array.Fill(pixels, 1f);
            FilterCrt.Apply(pixels, width, height, frame.Width, frame.Height, signal, 1f);
            CrtPhosphorMask.Apply(pixels, width, height, signal, 1f);
            var column = Enumerable.Range(16, 32).Select(y => pixels[(y * width + 160) * 3]).ToArray();
            return column.Max() - column.Min();
        }

        Assert.InRange(VerticalVariation(EmulationCrtMask.ApertureGrille), 0f, .0001f);
        Assert.True(VerticalVariation(EmulationCrtMask.SlotMask) > .015f);
    }

    [Fact]
    public void CrtStructuredMasksKeepEqualChannelCoverageAcrossAFullRepeat()
    {
        var masks = new (EmulationCrtMask Mask, Vector2 Period)[]
        {
            (EmulationCrtMask.ShadowMask, new(8f, 12f)),
            (EmulationCrtMask.HitachiEdp, new(6f, 14f)),
            (EmulationCrtMask.SlotMask, new(6f, 10f)),
            (EmulationCrtMask.StaggeredSlotMask, new(12f, 10f))
        };
        foreach (var (mask, period) in masks)
        {
            var origin = CrtPhosphorMask.Coverage(new Vector2(.4f, .9f), period, mask);
            var shifted = CrtPhosphorMask.Coverage(new Vector2(3.8f, 6.2f), period, mask);
            Assert.InRange(origin.X, .2f, .33f);
            Assert.InRange(MathF.Abs(origin.X - origin.Y), 0f, .00001f);
            Assert.InRange(MathF.Abs(origin.X - origin.Z), 0f, .00001f);
            Assert.InRange(Vector3.Distance(origin, shifted), 0f, .00001f);
        }
    }

    [Fact]
    public void CrtStaggeredSlotsHaveLongerOpeningsWithOffsetBridges()
    {
        var staggered = EmulationCrtMask.StaggeredSlotMask;
        Assert.Equal(Vector3.UnitX, CrtPhosphorMask.Coverage(new(.8f, .5f), Vector2.Zero, staggered));
        Assert.Equal(Vector3.Zero, CrtPhosphorMask.Coverage(new(.8f, 0f), Vector2.Zero, staggered));
        Assert.Equal(Vector3.UnitX, CrtPhosphorMask.Coverage(new(6.8f, 0f), Vector2.Zero, staggered));
        Assert.Equal(Vector3.Zero, CrtPhosphorMask.Coverage(new(6.8f, 5f), Vector2.Zero, staggered));
    }

    [Fact]
    public void CrtMaskBlocksDirectLightAndOnlyReflectsNeutralLightOnBlack()
    {
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(default, 256, 192, 1024,
            default, 4f / 3f, 0, TimeSpan.Zero) { SignalFieldRate = 60 };
        var settings = new RetroMediaLab.VideoPresentation.Contracts.EmulationCrtVideoConfiguration(Mask: EmulationCrtMask.SlotMask);
        var narrow = CrtSignal.From(settings with { BeamWidth = 20 }, frame);
        var wide = CrtSignal.From(settings with { BeamWidth = -20 }, frame);
        Assert.Equal(Vector3.Zero, CrtPhosphorMask.ApplyPixel(Vector3.UnitX, 2, 0, narrow.Phosphor));
        const int width = 3840;
        var black = new float[width * 3];
        CrtPhosphorMask.Apply(black, width, 1, wide, 4f);
        Assert.True(black[0] > black[11 * 3]);
        for (var i = 0; i < black.Length; i += 3)
        {
            Assert.InRange(black[i], 0f, .001f);
            Assert.Equal(black[i], black[i + 1]);
            Assert.Equal(black[i], black[i + 2]);
        }

        float LightInAdjacentSlit(CrtSignal signal, float gain)
        {
            var colors = new float[width * 3];
            for (var i = 0; i < colors.Length; i += 3) colors[i] = 1f;
            CrtPhosphorMask.Apply(colors, width, 1, signal, gain);
            return colors[4 * 3];
        }
        Assert.True(LightInAdjacentSlit(wide, 1f) > LightInAdjacentSlit(narrow, 1f));
        Assert.True(LightInAdjacentSlit(narrow, 4f) > LightInAdjacentSlit(narrow, 1f));
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
            Connection = EmulationCrtConnection.Composite,
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

        var locked = RetroMediaLab.App.Rendering.Emulation.Processing.CrtSignal.From(
            new() { Connection = EmulationCrtConnection.Composite }, source);
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
        oldProfile = oldProfile with { Connection = EmulationCrtConnection.Composite };
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
