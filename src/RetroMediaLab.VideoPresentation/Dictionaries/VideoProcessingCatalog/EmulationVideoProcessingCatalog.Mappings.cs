using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public static IReadOnlyDictionary<EmulationVideoDisplayTechnology, string> DisplayTechnologyResourceKeys { get; }
        = ResourceKeys("Technology", Enum.GetValues<EmulationVideoDisplayTechnology>());

    public static IReadOnlyDictionary<EmulationVideoScreenType, string> ScreenTypeResourceKeys { get; }
        = Enum.GetValues<EmulationVideoScreenType>().ToDictionary(screen => screen, screen => screen switch
        {
            EmulationVideoScreenType.Host => "Emulation.Video.Screen.Host",
            EmulationVideoScreenType.Crt => "Emulation.Video.Screen.Crt",
            EmulationVideoScreenType.Lcd => "Emulation.Video.FixedPixel.Technology.Lcd",
            EmulationVideoScreenType.LedBacklitLcd => "Emulation.Video.Screen.LedBacklitLcd",
            EmulationVideoScreenType.Oled => "Emulation.Video.FixedPixel.Technology.Oled",
            EmulationVideoScreenType.Vector => "Emulation.Video.Screen.Vector",
            EmulationVideoScreenType.Vfd => "Emulation.Video.Screen.Vfd",
            EmulationVideoScreenType.SegmentDisplay => "Emulation.Video.Screen.SegmentDisplay",
            EmulationVideoScreenType.EPaper => "Emulation.Video.Screen.EPaper",
            _ => $"Emulation.Video.Technology.{screen}"
        });

    public static IReadOnlyDictionary<EmulationVideoSampling, string> SamplingResourceKeys { get; }
        = ResourceKeys("Sampling",
        [
            EmulationVideoSampling.Nearest,
            EmulationVideoSampling.Bilinear,
            EmulationVideoSampling.SharpBilinear,
            EmulationVideoSampling.Bicubic,
            EmulationVideoSampling.Jinc2,
            EmulationVideoSampling.Lanczos,
            EmulationVideoSampling.Xbr,
            EmulationVideoSampling.Xbrz,
            EmulationVideoSampling.Hq2x,
            EmulationVideoSampling.Hq3x,
            EmulationVideoSampling.Hq4x,
            EmulationVideoSampling.TwoXSai,
            EmulationVideoSampling.SuperTwoXSai,
            EmulationVideoSampling.SuperEagle,
            EmulationVideoSampling.EpxScale2x,
            EmulationVideoSampling.ScaleFx,
            EmulationVideoSampling.ScaleNx,
            EmulationVideoSampling.Sabr
        ]);

    public static IReadOnlyDictionary<EmulationDeinterlacingMode, string> DeinterlacingResourceKeys { get; }
        = ResourceKeys("Deinterlacing", new[] { EmulationDeinterlacingMode.Off, EmulationDeinterlacingMode.Bob,
            EmulationDeinterlacingMode.Blend, EmulationDeinterlacingMode.Weave, EmulationDeinterlacingMode.Adaptive });

    public static IReadOnlyDictionary<EmulationCrtColorMode, string> CrtColorModeResourceKeys { get; }
        = ResourceKeys("Crt.Color", Enum.GetValues<EmulationCrtColorMode>());

    public static IReadOnlyDictionary<EmulationCrtPhosphorColor, string> CrtPhosphorColorResourceKeys { get; }
        = ResourceKeys("Crt.Color", Enum.GetValues<EmulationCrtPhosphorColor>());

    public static IReadOnlyDictionary<EmulationCrtConnection, string> CrtConnectionResourceKeys { get; }
        = ResourceKeys("Crt.Connection", Enum.GetValues<EmulationCrtConnection>());

    public static IReadOnlyDictionary<EmulationCrtMonitorPalette, string> CrtMonitorPaletteResourceKeys { get; }
        = ResourceKeys("Crt.MonitorPalette", Enum.GetValues<EmulationCrtMonitorPalette>());

    public static IReadOnlyDictionary<EmulationCrtMask, string> CrtMaskResourceKeys { get; }
        = ResourceKeys("Crt.Mask", new[] { EmulationCrtMask.None, EmulationCrtMask.ShadowMask,
            EmulationCrtMask.ApertureGrille, EmulationCrtMask.SlotMask, EmulationCrtMask.StaggeredSlotMask,
            EmulationCrtMask.Monochrome, EmulationCrtMask.HitachiEdp });

    public static IReadOnlyDictionary<EmulationCrtColorStandards, string> CrtColorStandardsResourceKeys { get; }
        = ResourceKeys("Crt.ColorStandards", new[] { EmulationCrtColorStandards.Pal, EmulationCrtColorStandards.Secam,
            EmulationCrtColorStandards.Ntsc, EmulationCrtColorStandards.PalSecam, EmulationCrtColorStandards.PalNtsc,
            EmulationCrtColorStandards.PalSecamNtsc });

    public static IReadOnlyDictionary<EmulationCrtFieldRate, string> CrtFieldRateResourceKeys { get; }
        = ResourceKeys("Crt.FieldRate", Enum.GetValues<EmulationCrtFieldRate>());

    public static IReadOnlyDictionary<EmulationSubpixelLayout, string> SubpixelLayoutResourceKeys { get; }
        = ResourceKeys("Subpixels", Enum.GetValues<EmulationSubpixelLayout>());

    public static IReadOnlyDictionary<EmulationFixedPixelTechnology, string> FixedPixelTechnologyResourceKeys { get; }
        = ResourceKeys("FixedPixel.Technology", Enum.GetValues<EmulationFixedPixelTechnology>());

    public static IReadOnlyDictionary<EmulationMonochromePalette, string> MonochromePaletteResourceKeys { get; }
        = new Dictionary<EmulationMonochromePalette, string>
        {
            [EmulationMonochromePalette.Green] = "Emulation.Video.DotMatrix.Palette.Green",
            [EmulationMonochromePalette.Gray] = "Emulation.Video.DotMatrix.Palette.Gray",
            [EmulationMonochromePalette.Amber] = "Emulation.Video.DotMatrix.Palette.Amber",
            [EmulationMonochromePalette.Blue] = "Emulation.Video.DotMatrix.Palette.Blue",
            [EmulationMonochromePalette.White] = "Emulation.Video.SegmentDisplay.Color.White"
        };

    public static IReadOnlyDictionary<EmulationVideoPreset, string> PresetResourceKeys { get; }
        = Enum.GetValues<EmulationVideoPreset>().ToDictionary(
            preset => preset,
            preset => preset switch
            {
                EmulationVideoPreset.Oled => "Emulation.Video.FixedPixel.Technology.Oled",
                EmulationVideoPreset.Plasma => "Emulation.Video.Technology.Plasma",
                _ => $"Emulation.Video.Preset.{preset}"
            });

    public static IReadOnlyDictionary<EmulationSignalConnection, string> SignalConnectionResourceKeys { get; }
        = ResourceKeys("Signal.Connection", Enum.GetValues<EmulationSignalConnection>());

    public static IReadOnlyDictionary<EmulationSignalStandard, string> SignalStandardResourceKeys { get; }
        = ResourceKeys("Signal.Standard", Enum.GetValues<EmulationSignalStandard>());

    public static IReadOnlyDictionary<EmulationVfdColor, string> VfdColorResourceKeys { get; }
        = new Dictionary<EmulationVfdColor, string>
        {
            [EmulationVfdColor.BlueGreen] = "Emulation.Video.Vfd.Color.BlueGreen",
            [EmulationVfdColor.Green] = "Emulation.Video.Vfd.Color.Green",
            [EmulationVfdColor.Blue] = "Emulation.Video.Vfd.Color.Blue",
            [EmulationVfdColor.White] = "Emulation.Video.SegmentDisplay.Color.White",
            [EmulationVfdColor.Amber] = "Emulation.Video.Vfd.Color.Amber",
            [EmulationVfdColor.Red] = "Emulation.Video.Vfd.Color.Red"
        };

    public static IReadOnlyDictionary<EmulationLedMatrixColor, string> LedMatrixColorResourceKeys { get; }
        = ResourceKeys("LedMatrix.Color", Enum.GetValues<EmulationLedMatrixColor>().OrderBy(value => value == EmulationLedMatrixColor.Rgb));

    public static IReadOnlyDictionary<EmulationLedMatrixShape, string> LedMatrixShapeResourceKeys { get; }
        = ResourceKeys("LedMatrix.Shape", Enum.GetValues<EmulationLedMatrixShape>());

    public static IReadOnlyDictionary<EmulationDotMatrixPalette, string> DotMatrixPaletteResourceKeys { get; }
        = ResourceKeys("DotMatrix.Color", new[]
        {
            EmulationDotMatrixPalette.Black, EmulationDotMatrixPalette.Gray, EmulationDotMatrixPalette.Green,
            EmulationDotMatrixPalette.Amber, EmulationDotMatrixPalette.Red, EmulationDotMatrixPalette.Blue,
            EmulationDotMatrixPalette.Rgb
        });

    public static IReadOnlyDictionary<EmulationDotMatrixDisplayTechnology, string> DotMatrixTechnologyResourceKeys { get; }
        = ResourceKeys("DotMatrix.Display", Enum.GetValues<EmulationDotMatrixDisplayTechnology>());

    public static IReadOnlyDictionary<EmulationDotMatrixShape, string> DotMatrixShapeResourceKeys { get; }
        = ResourceKeys("DotMatrix.Shape", Enum.GetValues<EmulationDotMatrixShape>());

    public static IReadOnlyDictionary<EmulationSegmentDisplayLayout, string> SegmentDisplayLayoutResourceKeys { get; }
        = ResourceKeys("SegmentDisplay.Layout", Enum.GetValues<EmulationSegmentDisplayLayout>());

    public static IReadOnlyDictionary<EmulationSegmentDisplayTechnology, string> SegmentDisplayTechnologyResourceKeys { get; }
        = ResourceKeys("SegmentDisplay.Technology", new[] { EmulationSegmentDisplayTechnology.Lcd,
            EmulationSegmentDisplayTechnology.Led, EmulationSegmentDisplayTechnology.Vfd });

    private static readonly IReadOnlyDictionary<EmulationSegmentDisplayColor, string> LcdSegmentColors =
        ResourceKeys("SegmentDisplay.Color", new[] { EmulationSegmentDisplayColor.Black, EmulationSegmentDisplayColor.Gray });

    private static readonly IReadOnlyDictionary<EmulationSegmentDisplayColor, string> LedSegmentColors =
        ResourceKeys("SegmentDisplay.Color", new[] { EmulationSegmentDisplayColor.Red, EmulationSegmentDisplayColor.Green,
            EmulationSegmentDisplayColor.Amber, EmulationSegmentDisplayColor.Blue, EmulationSegmentDisplayColor.White });

    private static readonly IReadOnlyDictionary<EmulationSegmentDisplayColor, string> VfdSegmentColors =
        ResourceKeys("SegmentDisplay.Color", new[] { EmulationSegmentDisplayColor.BlueGreen, EmulationSegmentDisplayColor.Green,
            EmulationSegmentDisplayColor.Blue, EmulationSegmentDisplayColor.White, EmulationSegmentDisplayColor.Amber });

    public static IReadOnlyDictionary<EmulationSegmentDisplayColor, string> SegmentColors(EmulationSegmentDisplayTechnology technology) =>
        technology switch
        {
            EmulationSegmentDisplayTechnology.Lcd => LcdSegmentColors,
            EmulationSegmentDisplayTechnology.Vfd => VfdSegmentColors,
            _ => LedSegmentColors
        };

    public static IReadOnlyDictionary<EmulationSegmentDisplayColor, string> SegmentDisplayColorResourceKeys { get; }
        = ResourceKeys("SegmentDisplay.Color", Enum.GetValues<EmulationSegmentDisplayColor>());

    public static IReadOnlyDictionary<EmulationSegmentEndShape, string> SegmentDisplayEndShapeResourceKeys { get; }
        = ResourceKeys("SegmentDisplay.EndShape", Enum.GetValues<EmulationSegmentEndShape>());

    public static IReadOnlyDictionary<EmulationEPaperColorMode, string> EPaperColorModeResourceKeys { get; }
        = ResourceKeys("EPaper.ColorMode", Enum.GetValues<EmulationEPaperColorMode>());

    public static IReadOnlyDictionary<string, string> ParameterResourceKeys { get; } =
        ParameterIds.ToDictionary(id => id, id => id switch
        {
            Gamma => "Emulation.Video.Gamma",
            LedMatrixDefinition or DotMatrixDefinition or VfdDefinition => LcdDefinition,
            CrtPhosphorColor => $"Emulation.Video.Parameter.{VectorPhosphorColor}",
            CrtBeamWidth => $"Emulation.Video.Parameter.{VectorBeamWidth}",
            _ => $"Emulation.Video.Parameter.{id}"
        }, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, object?> NeutralValues { get; } =
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [Brightness] = 0, [Contrast] = 0, [Gamma] = 0, [Saturation] = 0, [Sharpness] = 0,
            [Dedithering] = 0, [Debanding] = 0, [DetailRecovery] = 0,
            [Deinterlacing] = EmulationDeinterlacingMode.Off,
            [BlackFrameInsertion] = false,
            [SignalConnection] = EmulationSignalConnection.None,
            [SignalConnectionIntensity] = 0,
            [SignalStandard] = EmulationSignalStandard.Automatic,
            [SignalStandardIntensity] = 0,
            [Grain] = 0,
            [Vhs] = 0,
            [ChromaticAberration] = 0,
            [Bloom] = 0,
            [Sepia] = false,
            [CrtColorStandards] = EmulationCrtColorStandards.PalSecamNtsc,
            [CrtFieldRate] = EmulationCrtFieldRate.Hz50And60,
            [CrtMask] = EmulationCrtMask.None,
            [CrtPhosphorColor] = EmulationCrtPhosphorColor.Gray,
            [CrtBeamWidth] = 0,
            [CrtCurvature] = 0,
            [CrtConnection] = EmulationCrtConnection.Digital,
            [CrtConnectionDegradation] = 0,
            [CrtMonitorPalette] = EmulationCrtMonitorPalette.Mda,
            [FixedPixelTechnology] = EmulationFixedPixelTechnology.Lcd,
            [FixedPixelSubpixels] = EmulationSubpixelLayout.Rgb, [FixedPixelMonochromeColor] = EmulationMonochromePalette.Green,
            [FixedPixelGridIntensity] = 0, [FixedPixelPixelGap] = 0, [FixedPixelResponseTime] = 0,
            [FixedPixelPersistence] = 0, [FixedPixelBacklight] = null, [FixedPixelBacklightBleed] = 25, [FixedPixelOledEmission] = 100, [FixedPixelBlackDepth] = null,
            [PlasmaCellStructure] = EmulationPlasmaCellStructure.Stripe,
            [VectorBeamWidth] = 0, [VectorPhosphorColor] = EmulationCrtColorMode.Color,
            [VfdColor] = EmulationVfdColor.BlueGreen,
            [VfdDefinition] = EmulationLedMatrixDefinition.Native,
            [VfdShape] = EmulationDotMatrixShape.Round,
            [LedMatrixColor] = EmulationLedMatrixColor.Rgb, [LedMatrixDefinition] = EmulationLedMatrixDefinition.Native,
            [LedMatrixShape] = EmulationLedMatrixShape.Round,
            [DotMatrixPalette] = EmulationDotMatrixPalette.Green,
            [DotMatrixDefinition] = EmulationLedMatrixDefinition.Native,
            [DotMatrixShape] = EmulationDotMatrixShape.Round,
            [DotMatrixTechnology] = EmulationDotMatrixDisplayTechnology.Reflective,
            [SegmentDisplayTechnology] = EmulationSegmentDisplayTechnology.Led,
            [SegmentDisplayLayout] = EmulationSegmentDisplayLayout.Seven,
            [SegmentDisplayColor] = EmulationSegmentDisplayColor.Red,
            [SegmentDisplayCellSize] = 45, [SegmentDisplayHorizontalGap] = 15,
            [SegmentDisplayVerticalGap] = 20, [SegmentDisplayThickness] = 55,
            [SegmentDisplaySegmentGap] = 12,
            [SegmentDisplayEndShape] = EmulationSegmentEndShape.Beveled,
            [SegmentDisplayDecimalPoint] = false, [SegmentDisplayColon] = false,
            [SegmentDisplayBrightness] = 85, [SegmentDisplayActivationThreshold] = 45,
            [SegmentDisplayContrast] = 80, [SegmentDisplayOffSegmentVisibility] = 8,
            [SegmentDisplayBlackDepth] = 100, [SegmentDisplayGlow] = 20,
            [SegmentDisplayHaloRadius] = 25, [SegmentDisplayResponseTime] = 30,
            [SegmentDisplayPersistence] = 60,
            [EPaperColorMode] = EmulationEPaperColorMode.Monochrome, [EPaperContrast] = 70,
            [EPaperDithering] = 35, [EPaperRefreshTime] = 500, [EPaperGhosting] = 20,
            [EPaperInkDensity] = 90, [EPaperPaperBrightness] = 90,
            [EPaperPaperWarmth] = 35, [EPaperColorSaturation] = 55,
            [EPaperSurfaceTexture] = 10, [EPaperEdgeSoftness] = 10,
            [ProjectionAmbientLight] = 0
        };

    public static IReadOnlyDictionary<string, EmulationVideoDisplayTechnology> RequiredTechnologies { get; } =
        TechnologyRequirements();

    public static IReadOnlyDictionary<string, string> RequiredParameters { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CrtPhosphorColor] = CrtMask,
            [FixedPixelMonochromeColor] = FixedPixelSubpixels
        };

    public static IReadOnlyDictionary<string, object> RequiredParameterValues { get; } =
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [CrtPhosphorColor] = EmulationCrtMask.Monochrome,
            [FixedPixelMonochromeColor] = EmulationSubpixelLayout.Monochrome
        };

    public static IReadOnlyDictionary<EmulationVideoDisplayTechnology,
        IReadOnlySet<EmulationVideoDisplayTechnology>> IncompatibleTechnologies { get; }
        = Enum.GetValues<EmulationVideoDisplayTechnology>().ToDictionary(
            technology => technology,
            technology => (IReadOnlySet<EmulationVideoDisplayTechnology>)Enum.GetValues<EmulationVideoDisplayTechnology>().Where(other => other != technology).ToHashSet());

    public static IReadOnlyDictionary<string, string> CompatibilityResourceKeys { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ExclusiveDisplayTechnology] = "Emulation.Video.Compatibility.ExclusiveDisplayTechnology",
            [UnsupportedBackend] = "Emulation.Video.Limitation.UnsupportedBackend"
        };

    public static IReadOnlyDictionary<EmulationVideoPreset, EmulationVideoProcessingConfiguration>
        PresetConfigurations { get; } = CreatePresets();

    private static IReadOnlyDictionary<TEnum, string> ResourceKeys<TEnum>(string group, IEnumerable<TEnum> values) where TEnum : struct, Enum => values.ToDictionary(value => value, value => $"Emulation.Video.{group}.{value}");

    private static IReadOnlyDictionary<string, EmulationVideoDisplayTechnology> TechnologyRequirements()
    {
        var result = new Dictionary<string, EmulationVideoDisplayTechnology>(StringComparer.Ordinal);
        Add(CrtColorStandards, CrtMonitorPalette, EmulationVideoDisplayTechnology.Crt);
        Add(FixedPixelTechnology, FixedPixelBlackDepth, EmulationVideoDisplayTechnology.FixedPixel);
        Add(PlasmaCellStructure, PlasmaCellStructure, EmulationVideoDisplayTechnology.Plasma);
        Add(VectorBeamWidth, VectorPhosphorColor, EmulationVideoDisplayTechnology.Vector);
        Add(VfdColor, VfdShape, EmulationVideoDisplayTechnology.Vfd);
        Add(LedMatrixColor, LedMatrixShape, EmulationVideoDisplayTechnology.LedMatrix);
        Add(DotMatrixPalette, DotMatrixTechnology, EmulationVideoDisplayTechnology.DotMatrix);
        Add(SegmentDisplayLayout, SegmentDisplayPersistence, EmulationVideoDisplayTechnology.SegmentDisplay);
        Add(EPaperColorMode, EPaperEdgeSoftness, EmulationVideoDisplayTechnology.EPaper);
        Add(ProjectionAmbientLight, ProjectionAmbientLight, EmulationVideoDisplayTechnology.Projection);
        return result;

        void Add(string first, string last, EmulationVideoDisplayTechnology technology)
        {
            var start = Array.IndexOf(ParameterIds, first);
            var end = Array.IndexOf(ParameterIds, last);
            for (var index = start; index <= end; index++) result[ParameterIds[index]] = technology;
        }
    }

}
