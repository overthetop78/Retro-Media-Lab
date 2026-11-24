using RetroMediaLab.VideoPresentation.Constants;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using static RetroMediaLab.VideoPresentation.Enums.EmulationImageAdjustmentControls;

namespace RetroMediaLab.VideoPresentation.Functions;

public static class EmulationVideoProcessingConfigurationFunctions
{
    private static readonly IReadOnlyList<EmulationVideoAspectRatio> OriginalAspectRatios =
        [EmulationVideoAspectRatio.Original];
    private static readonly IReadOnlyList<EmulationVideoAspectRatio> VectorAspectRatios =
        [EmulationVideoAspectRatio.Original, EmulationVideoAspectRatio.FourThree];
    private static readonly IReadOnlyList<EmulationVideoAspectRatio> MatrixAspectRatios =
        [EmulationVideoAspectRatio.Original, EmulationVideoAspectRatio.Square];
    private static readonly IReadOnlyList<EmulationVideoAspectRatio> PaperAspectRatios =
        [EmulationVideoAspectRatio.Original, EmulationVideoAspectRatio.FourThree, EmulationVideoAspectRatio.SixteenNine];
    private static readonly IReadOnlyList<EmulationVideoAspectRatio> StandardAspectRatios =
        [EmulationVideoAspectRatio.Original, EmulationVideoAspectRatio.FourThree, EmulationVideoAspectRatio.FiveFour,
         EmulationVideoAspectRatio.SixteenNine, EmulationVideoAspectRatio.FourteenNine, EmulationVideoAspectRatio.TwentyOneNine];

    public static IReadOnlyList<EmulationVideoAspectRatio> AspectRatioChoices(EmulationVideoScreenType screen) => screen switch
    {
        EmulationVideoScreenType.Vector => VectorAspectRatios,
        EmulationVideoScreenType.LedMatrix or EmulationVideoScreenType.DotMatrix => MatrixAspectRatios,
        EmulationVideoScreenType.Vfd or EmulationVideoScreenType.SegmentDisplay => OriginalAspectRatios,
        EmulationVideoScreenType.EPaper => PaperAspectRatios,
        _ => StandardAspectRatios
    };

    public static EmulationVideoAspectRatio EffectiveAspectRatio(EmulationVideoProcessingConfiguration configuration) =>
        AspectRatioChoices(ScreenType(configuration)).Contains(configuration.AspectRatio)
            ? configuration.AspectRatio : EmulationVideoAspectRatio.Original;

    public static (int Width, int Height) AspectRatioDimensions(EmulationVideoAspectRatio ratio) => ratio switch
    {
        EmulationVideoAspectRatio.Square => (1, 1),
        EmulationVideoAspectRatio.FourThree => (4, 3),
        EmulationVideoAspectRatio.FiveFour => (5, 4),
        EmulationVideoAspectRatio.SixteenNine => (16, 9),
        EmulationVideoAspectRatio.FourteenNine => (14, 9),
        EmulationVideoAspectRatio.TwentyOneNine => (21, 9),
        _ => (0, 0)
    };

    public static bool UsesHostDisplay(EmulationVideoProcessingConfiguration configuration) =>
        configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Normal;

    public static EmulationVideoSampling SamplingForDisplay(EmulationVideoProcessingConfiguration configuration) =>
        UsesHostDisplay(configuration) ? configuration.Sampling : EmulationVideoSampling.Nearest;

    public static bool SupportsDeinterlacing(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) is EmulationVideoScreenType.Host or EmulationVideoScreenType.Lcd
            or EmulationVideoScreenType.LedBacklitLcd or EmulationVideoScreenType.Oled
            or EmulationVideoScreenType.Plasma or EmulationVideoScreenType.Projection;

    public static EmulationDeinterlacingMode DeinterlacingForDisplay(EmulationVideoProcessingConfiguration configuration) =>
        SupportsDeinterlacing(configuration) ? configuration.Restoration.Deinterlacing : EmulationDeinterlacingMode.Off;

    public static bool SupportsDedithering(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) is EmulationVideoScreenType.Host or EmulationVideoScreenType.Crt
            or EmulationVideoScreenType.Lcd or EmulationVideoScreenType.LedBacklitLcd
            or EmulationVideoScreenType.Oled or EmulationVideoScreenType.Plasma or EmulationVideoScreenType.Projection;

    public static EmulationVideoProcessingConfiguration ForRendering(EmulationVideoProcessingConfiguration? configuration)
    {
        var normalized = Normalize(configuration);
        if (UsesHostDisplay(normalized)) return normalized;
        var screen = ScreenType(normalized);
        EmulationCrtConnection? connection = screen switch
        {
            EmulationVideoScreenType.Crt => normalized.Crt.Connection,
            EmulationVideoScreenType.Lcd or EmulationVideoScreenType.LedBacklitLcd => LcdPanel(normalized).Connection,
            EmulationVideoScreenType.Oled => normalized.Oled.Connection,
            _ => null
        };
        var mergesDithering = connection is EmulationCrtConnection.Composite or EmulationCrtConnection.AnalogRf;
        var controls = AdjustmentControls(normalized);
        var adjustments = ImageAdjustments(normalized, ScreenType(normalized));
        return normalized with
        {
            Sampling = SamplingForDisplay(normalized),
            SignalSimulation = connection.HasValue ? new() : normalized.SignalSimulation,
            Restoration = normalized.Restoration with
            {
                Dedithering = SupportsDedithering(normalized) && !mergesDithering ? normalized.Restoration.Dedithering : 0,
                Debanding = 0,
                DetailRecovery = 0
            },
            Adjustments = new(
                controls.HasFlag(Brightness) ? adjustments.Brightness : 0,
                controls.HasFlag(Contrast) ? adjustments.Contrast : 0,
                controls.HasFlag(Gamma) ? adjustments.Gamma : EmulationImageAdjustmentFunctions.OriginalGamma,
                controls.HasFlag(Saturation) ? adjustments.Saturation : 0,
                controls.HasFlag(Sharpness) ? adjustments.Sharpness : 0)
        };
    }

    public static EmulationImageAdjustmentControls AdjustmentControls(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) switch
        {
            EmulationVideoScreenType.Vector or EmulationVideoScreenType.Vfd => Brightness,
            EmulationVideoScreenType.LedMatrix => Brightness | Contrast | Gamma
                | (configuration.LedMatrix?.Color == EmulationLedMatrixColor.Rgb ? Saturation : None),
            EmulationVideoScreenType.DotMatrix => Brightness | Contrast | (configuration.DotMatrix?.Palette switch
            {
                EmulationDotMatrixPalette.Rgb => Gamma | Saturation,
                EmulationDotMatrixPalette.Gray => Gamma,
                _ => None
            }),
            EmulationVideoScreenType.SegmentDisplay => Brightness | Contrast,
            EmulationVideoScreenType.EPaper => Contrast,
            _ => All
        };

    public static EmulationImageAdjustments ImageAdjustments(EmulationVideoProcessingConfiguration configuration,
        EmulationVideoScreenType screen)
    {
        var adjustments = screen == EmulationVideoScreenType.Host
            ? configuration.Adjustments
            : configuration.ScreenAdjustments?.GetValueOrDefault(screen);
        return EmulationImageAdjustmentFunctions.Normalize(adjustments);
    }

    public static EmulationVideoProcessingConfiguration WithImageAdjustments(EmulationVideoProcessingConfiguration configuration,
        EmulationVideoScreenType screen, Func<EmulationImageAdjustments, EmulationImageAdjustments> update)
    {
        var current = ImageAdjustments(configuration, screen);
        var next = EmulationImageAdjustmentFunctions.Normalize(update(current));
        if (current == next) return configuration;
        if (screen == EmulationVideoScreenType.Host) return configuration with { Adjustments = next };

        var profiles = configuration.ScreenAdjustments is { } stored
            ? new Dictionary<EmulationVideoScreenType, EmulationImageAdjustments>(stored)
            : new Dictionary<EmulationVideoScreenType, EmulationImageAdjustments>();
        profiles[screen] = next;
        return configuration with { ScreenAdjustments = profiles };
    }

    public static EmulationVideoScreenType ScreenType(EmulationVideoProcessingConfiguration configuration) =>
        configuration.DisplayTechnology switch
        {
            EmulationVideoDisplayTechnology.Crt => EmulationVideoScreenType.Crt,
            EmulationVideoDisplayTechnology.FixedPixel => configuration.FixedPixel?.Technology switch
            {
                EmulationFixedPixelTechnology.LedBacklitLcd => EmulationVideoScreenType.LedBacklitLcd,
                EmulationFixedPixelTechnology.Oled => EmulationVideoScreenType.Oled,
                _ => EmulationVideoScreenType.Lcd
            },
            EmulationVideoDisplayTechnology.Plasma => EmulationVideoScreenType.Plasma,
            EmulationVideoDisplayTechnology.Projection => EmulationVideoScreenType.Projection,
            EmulationVideoDisplayTechnology.Vector => EmulationVideoScreenType.Vector,
            EmulationVideoDisplayTechnology.LedMatrix => EmulationVideoScreenType.LedMatrix,
            EmulationVideoDisplayTechnology.DotMatrix => EmulationVideoScreenType.DotMatrix,
            EmulationVideoDisplayTechnology.Vfd => EmulationVideoScreenType.Vfd,
            EmulationVideoDisplayTechnology.SegmentDisplay => EmulationVideoScreenType.SegmentDisplay,
            EmulationVideoDisplayTechnology.EPaper => EmulationVideoScreenType.EPaper,
            _ => EmulationVideoScreenType.Host
        };

    public static EmulationVideoProcessingConfiguration WithScreenType(EmulationVideoProcessingConfiguration configuration,
        EmulationVideoScreenType screen) => screen switch
    {
        EmulationVideoScreenType.Host => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Normal },
        EmulationVideoScreenType.Crt => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Crt },
        EmulationVideoScreenType.Lcd => WithFixedPixelType(configuration, EmulationFixedPixelTechnology.Lcd),
        EmulationVideoScreenType.LedBacklitLcd => WithFixedPixelType(configuration, EmulationFixedPixelTechnology.LedBacklitLcd),
        EmulationVideoScreenType.Oled => WithFixedPixelType(configuration, EmulationFixedPixelTechnology.Oled),
        EmulationVideoScreenType.Plasma => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Plasma },
        EmulationVideoScreenType.Projection => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Projection },
        EmulationVideoScreenType.Vector => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Vector },
        EmulationVideoScreenType.LedMatrix => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.LedMatrix },
        EmulationVideoScreenType.DotMatrix => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.DotMatrix },
        EmulationVideoScreenType.Vfd => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.Vfd },
        EmulationVideoScreenType.SegmentDisplay => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.SegmentDisplay },
        EmulationVideoScreenType.EPaper => configuration with { DisplayTechnology = EmulationVideoDisplayTechnology.EPaper },
        _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, null)
    };

    public static bool UsesLcdPanel(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) is EmulationVideoScreenType.Lcd or EmulationVideoScreenType.LedBacklitLcd;

    public static EmulationLcdVideoConfiguration LcdPanel(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) == EmulationVideoScreenType.LedBacklitLcd ? configuration.Led : configuration.Lcd;

    public static EmulationPanelSignalConfiguration PanelSignal(EmulationVideoProcessingConfiguration configuration) =>
        ScreenType(configuration) == EmulationVideoScreenType.Oled ? configuration.Oled : LcdPanel(configuration);

    private static T NormalizePanelSignal<T>(T panel) where T : EmulationPanelSignalConfiguration => panel with
    {
        ColorStandards = DefinedOrDefault(panel.ColorStandards, EmulationCrtColorStandards.PalSecamNtsc),
        FieldRate = DefinedOrDefault(panel.FieldRate, EmulationCrtFieldRate.Hz50And60),
        Definition = DefinedOrDefault(panel.Definition, EmulationLcdDefinition.Native),
        Connection = DefinedOrDefault(panel.Connection, EmulationCrtConnection.Digital),
        ConnectionDegradation = Math.Clamp(panel.ConnectionDegradation, 0, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum),
        MonitorPalette = DefinedOrDefault(panel.MonitorPalette, EmulationCrtMonitorPalette.Mda)
    };

    private static EmulationLcdVideoConfiguration NormalizeLcd(EmulationLcdVideoConfiguration lcd) => NormalizePanelSignal(lcd) with
    {
        PixelStructure = DefinedOrDefault(lcd.PixelStructure, EmulationLcdPixelStructure.None),
        Orientation = DefinedOrDefault(lcd.Orientation, EmulationLcdPixelOrientation.Vertical),
        PanelType = DefinedOrDefault(lcd.PanelType, EmulationLcdPanelType.Ips),
        BacklightIntensity = Intensity(lcd.BacklightIntensity),
        BacklightType = DefinedOrDefault(lcd.BacklightType, EmulationLedBacklightType.Edge)
    };

    private static EmulationOledVideoConfiguration NormalizeOled(EmulationOledVideoConfiguration oled) => NormalizePanelSignal(oled) with
    {
        PanelType = DefinedOrDefault(oled.PanelType, EmulationOledPanelType.RgbOled),
        PixelStructure = DefinedOrDefault(oled.PixelStructure, EmulationOledPixelStructure.RgbVertical),
        Connection = oled.Connection is EmulationCrtConnection.Scart or EmulationCrtConnection.SVideo
            ? EmulationCrtConnection.Digital : DefinedOrDefault(oled.Connection, EmulationCrtConnection.Digital)
    };

    private static EmulationVideoProcessingConfiguration WithFixedPixelType(EmulationVideoProcessingConfiguration configuration,
        EmulationFixedPixelTechnology technology)
    {
        var current = configuration.FixedPixel ?? new();
        if (current.Technology != technology)
        {
            configuration = current.Technology switch
            {
                EmulationFixedPixelTechnology.LedBacklitLcd => configuration with { LedBacklitLcdProfile = current },
                EmulationFixedPixelTechnology.Oled => configuration with { OledProfile = current },
                _ => configuration
            };
            current = technology switch
            {
                EmulationFixedPixelTechnology.LedBacklitLcd => configuration.LedBacklitLcdProfile,
                EmulationFixedPixelTechnology.Oled => configuration.OledProfile,
                _ => null
            } ?? new(Technology: technology);
        }

        return configuration with
        {
            DisplayTechnology = EmulationVideoDisplayTechnology.FixedPixel,
            FixedPixel = current with { Technology = technology }
        };
    }

    public static EmulationVideoProcessingConfiguration Normalize(EmulationVideoProcessingConfiguration? configuration)
    {
        configuration ??= new EmulationVideoProcessingConfiguration();
        var crt = configuration.Crt ?? new EmulationCrtVideoConfiguration();
        var fixedPixel = configuration.FixedPixel ?? new EmulationFixedPixelVideoConfiguration();
        var plasma = configuration.Plasma ?? new EmulationPlasmaVideoConfiguration();
        var vector = configuration.Vector ?? new EmulationVectorVideoConfiguration();
        var vfd = configuration.Vfd ?? new EmulationVfdVideoConfiguration();
        var ledMatrix = configuration.LedMatrix ?? new EmulationLedMatrixVideoConfiguration();
        var dotMatrix = configuration.DotMatrix ?? new EmulationDotMatrixVideoConfiguration();
        var segmentDisplay = configuration.SegmentDisplay ?? new EmulationSegmentDisplayVideoConfiguration();
        var ePaper = configuration.EPaper ?? new EmulationEPaperVideoConfiguration();
        var projection = configuration.Projection ?? new EmulationProjectionVideoConfiguration();
        var restoration = configuration.Restoration ?? new EmulationImageRestorationConfiguration();
        var signalSimulation = configuration.SignalSimulation ?? new EmulationSignalSimulationConfiguration();
        var stylistic = configuration.Stylistic ?? new EmulationStylisticVideoConfiguration();
        return configuration with
        {
            DisplayTechnology = DefinedOrDefault(configuration.DisplayTechnology, EmulationVideoDisplayTechnology.Normal),
            Sampling = DefinedOrDefault(configuration.Sampling, EmulationVideoSampling.Nearest),
            Adjustments = EmulationImageAdjustmentFunctions.Normalize(configuration.Adjustments),
            Restoration = restoration with
            {
                Dedithering = Intensity(restoration.Dedithering),
                Debanding = Intensity(restoration.Debanding),
                DetailRecovery = Intensity(restoration.DetailRecovery),
                Deinterlacing = restoration.Deinterlacing is EmulationDeinterlacingMode.BobEvenLines or EmulationDeinterlacingMode.BobOddLines
                    ? EmulationDeinterlacingMode.Bob : DefinedOrDefault(restoration.Deinterlacing, EmulationDeinterlacingMode.Off)
            },
            SignalSimulation = signalSimulation with
            {
                Connection = DefinedOrDefault(signalSimulation.Connection, EmulationSignalConnection.None),
                ConnectionIntensity = Intensity(signalSimulation.ConnectionIntensity),
                Standard = DefinedOrDefault(signalSimulation.Standard, EmulationSignalStandard.Automatic),
                StandardIntensity = Intensity(signalSimulation.StandardIntensity)
            },
            Stylistic = stylistic with
            {
                Grain = Intensity(stylistic.Grain),
                Vhs = Intensity(stylistic.Vhs),
                ChromaticAberration = Intensity(stylistic.ChromaticAberration),
                Bloom = Intensity(stylistic.Bloom),
                Sepia = stylistic.Sepia
            },
            Crt = crt with
            {
                Mask = DefinedOrDefault(crt.Mask, EmulationCrtMask.None),
                ColorStandards = DefinedOrDefault(crt.ColorStandards, EmulationCrtColorStandards.PalSecamNtsc),
                FieldRate = DefinedOrDefault(crt.FieldRate, EmulationCrtFieldRate.Hz50And60),
                PhosphorColor = DefinedOrDefault(crt.PhosphorColor, EmulationCrtPhosphorColor.Gray),
                BeamWidth = Math.Clamp(crt.BeamWidth, EmulationVideoProcessingLimits.AdjustmentMinimum,
                    EmulationVideoProcessingLimits.AdjustmentMaximum),
                Curvature = Math.Clamp(crt.Curvature, EmulationVideoProcessingLimits.CrtCurvatureMinimum,
                    EmulationVideoProcessingLimits.CrtCurvatureMaximum),
                Connection = DefinedOrDefault(crt.Connection, EmulationCrtConnection.Digital),
                ConnectionDegradation = Math.Clamp(crt.ConnectionDegradation, 0,
                    EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum),
                MonitorPalette = DefinedOrDefault(crt.MonitorPalette, EmulationCrtMonitorPalette.Mda)
            },
            Lcd = NormalizeLcd(configuration.Lcd ?? new()),
            Led = NormalizeLcd(configuration.Led ?? new() { BacklightIntensity = 80 }),
            Oled = NormalizeOled(configuration.Oled ?? new()),
            FixedPixel = fixedPixel with
            {
                Technology = DefinedOrDefault(fixedPixel.Technology, EmulationFixedPixelTechnology.Lcd),
                Subpixels = DefinedOrDefault(fixedPixel.Subpixels, EmulationSubpixelLayout.Rgb),
                GridIntensity = Intensity(fixedPixel.GridIntensity),
                PixelGap = Intensity(fixedPixel.PixelGap),
                MonochromePalette = DefinedOrDefault(fixedPixel.MonochromePalette, EmulationMonochromePalette.Green),
                ResponseTimeMilliseconds = Duration(fixedPixel.ResponseTimeMilliseconds),
                PersistenceIntensity = Intensity(fixedPixel.PersistenceIntensity),
                BacklightIntensity = OptionalIntensity(fixedPixel.BacklightIntensity),
                BlackDepth = OptionalIntensity(fixedPixel.BlackDepth),
                BacklightBleedIntensity = Intensity(fixedPixel.BacklightBleedIntensity),
                OledEmissionIntensity = Intensity(fixedPixel.OledEmissionIntensity)
            },
            Plasma = plasma with
            {
                CellStructure = Intensity(plasma.CellStructure),
                Diffusion = Intensity(plasma.Diffusion),
                TemporalDithering = Intensity(plasma.TemporalDithering),
                PersistenceIntensity = Intensity(plasma.PersistenceIntensity),
                BlackDepth = Intensity(plasma.BlackDepth),
                PhosphorIntensity = Intensity(plasma.PhosphorIntensity),
                GammaResponse = Intensity(plasma.GammaResponse),
                AutomaticBrightnessLimiter = Intensity(plasma.AutomaticBrightnessLimiter)
            },
            Vector = vector with
            {
                LineThreshold = Intensity(vector.LineThreshold),
                LineIntensity = Intensity(vector.LineIntensity),
                BeamWidth = Intensity(vector.BeamWidth),
                BeamFocus = Intensity(vector.BeamFocus),
                PhosphorColor = DefinedOrDefault(vector.PhosphorColor, EmulationCrtColorMode.Color),
                HaloIntensity = Intensity(vector.HaloIntensity),
                HaloRadius = Intensity(vector.HaloRadius),
                PersistenceIntensity = Intensity(vector.PersistenceIntensity)
            },
            Vfd = vfd with
            {
                Color = DefinedOrDefault(vfd.Color, EmulationVfdColor.Blue),
                PhosphorIntensity = Intensity(vfd.PhosphorIntensity),
                EmissionThreshold = Intensity(vfd.EmissionThreshold),
                GlassDarkening = Intensity(vfd.GlassDarkening),
                Structure = DefinedOrDefault(vfd.Structure, EmulationVfdStructure.Graphic),
                CellSize = Intensity(vfd.CellSize),
                CellGap = Intensity(vfd.CellGap),
                HaloIntensity = Intensity(vfd.HaloIntensity),
                HaloRadius = Intensity(vfd.HaloRadius),
                PersistenceMilliseconds = Duration(vfd.PersistenceMilliseconds)
            },
            LedMatrix = ledMatrix with
            {
                Color = DefinedOrDefault(ledMatrix.Color, EmulationLedMatrixColor.Rgb),
                CellSize = Intensity(ledMatrix.CellSize),
                CellGap = Intensity(ledMatrix.CellGap),
                Diffusion = Intensity(ledMatrix.Diffusion),
                Brightness = Intensity(ledMatrix.Brightness),
                Shape = DefinedOrDefault(ledMatrix.Shape, EmulationLedMatrixShape.Round),
                HaloRadius = Intensity(ledMatrix.HaloRadius),
                BlackDepth = Intensity(ledMatrix.BlackDepth)
            },
            DotMatrix = dotMatrix with
            {
                Palette = DefinedOrDefault(dotMatrix.Palette, EmulationDotMatrixPalette.Green),
                Shape = DefinedOrDefault(dotMatrix.Shape, EmulationDotMatrixShape.Round),
                DotSize = Intensity(dotMatrix.DotSize),
                CellSize = Intensity(dotMatrix.CellSize),
                CellGap = Intensity(dotMatrix.CellGap),
                Contrast = Intensity(dotMatrix.Contrast),
                Brightness = Intensity(dotMatrix.Brightness),
                HaloIntensity = Intensity(dotMatrix.HaloIntensity),
                ResponseTimeMilliseconds = Duration(dotMatrix.ResponseTimeMilliseconds),
                PersistenceMilliseconds = Duration(dotMatrix.PersistenceMilliseconds)
            },
            SegmentDisplay = segmentDisplay with
            {
                Layout = DefinedOrDefault(segmentDisplay.Layout, EmulationSegmentDisplayLayout.Seven),
                Color = DefinedOrDefault(segmentDisplay.Color, EmulationSegmentDisplayColor.Red),
                CellSize = Intensity(segmentDisplay.CellSize),
                HorizontalGap = Intensity(segmentDisplay.HorizontalGap),
                VerticalGap = Intensity(segmentDisplay.VerticalGap),
                Thickness = Intensity(segmentDisplay.Thickness),
                SegmentGap = Intensity(segmentDisplay.SegmentGap),
                EndShape = DefinedOrDefault(segmentDisplay.EndShape, EmulationSegmentEndShape.Beveled),
                Contrast = Intensity(segmentDisplay.Contrast),
                Brightness = Intensity(segmentDisplay.Brightness),
                ActivationThreshold = Intensity(segmentDisplay.ActivationThreshold),
                OffSegmentVisibility = Intensity(segmentDisplay.OffSegmentVisibility),
                BlackDepth = Intensity(segmentDisplay.BlackDepth),
                Glow = Intensity(segmentDisplay.Glow),
                HaloRadius = Intensity(segmentDisplay.HaloRadius),
                ResponseTimeMilliseconds = Duration(segmentDisplay.ResponseTimeMilliseconds),
                PersistenceMilliseconds = Duration(segmentDisplay.PersistenceMilliseconds)
            },
            EPaper = ePaper with
            {
                ColorMode = DefinedOrDefault(ePaper.ColorMode, EmulationEPaperColorMode.Monochrome),
                Contrast = Intensity(ePaper.Contrast),
                Dithering = Intensity(ePaper.Dithering),
                RefreshTimeMilliseconds = Duration(ePaper.RefreshTimeMilliseconds),
                Ghosting = Intensity(ePaper.Ghosting),
                InkDensity = Intensity(ePaper.InkDensity),
                PaperBrightness = Intensity(ePaper.PaperBrightness),
                PaperWarmth = Intensity(ePaper.PaperWarmth),
                ColorSaturation = Intensity(ePaper.ColorSaturation),
                SurfaceTexture = Intensity(ePaper.SurfaceTexture),
                EdgeSoftness = Intensity(ePaper.EdgeSoftness)
            },
            Projection = projection with
            {
                OpticalBlur = Intensity(projection.OpticalBlur),
                Diffusion = Intensity(projection.Diffusion),
                ScreenTexture = Intensity(projection.ScreenTexture),
                Convergence = Intensity(projection.Convergence),
                LightOutput = Intensity(projection.LightOutput),
                AmbientLight = Intensity(projection.AmbientLight),
                Vignette = Intensity(projection.Vignette)
            }
        };
    }


    private static int Intensity(int value) => Math.Clamp(value, EmulationVideoProcessingLimits.IntensityMinimum, EmulationVideoProcessingLimits.IntensityMaximum);

    private static int Duration(int value) => Math.Clamp(value, EmulationVideoProcessingLimits.DurationMinimumMilliseconds, EmulationVideoProcessingLimits.DurationMaximumMilliseconds);

    private static int? OptionalIntensity(int? value) => value is null ? null : Intensity(value.Value);

    private static TEnum DefinedOrDefault<TEnum>(TEnum value, TEnum fallback) where TEnum : struct, Enum => Enum.IsDefined(value) ? value : fallback;
}
