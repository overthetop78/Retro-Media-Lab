using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public static IReadOnlyDictionary<EmulationVideoRenderer, string> RendererResourceKeys { get; } =
        new Dictionary<EmulationVideoRenderer, string>
        {
            [EmulationVideoRenderer.Direct3D11] = "Emulation.Video.Renderer.Direct3D11",
            [EmulationVideoRenderer.Vulkan] = "Emulation.Video.Renderer.Vulkan",
            [EmulationVideoRenderer.OpenGL] = "Emulation.Video.Renderer.OpenGL",
            [EmulationVideoRenderer.Wpf] = "Emulation.Video.Renderer.Wpf"
        };
    public const string Brightness = nameof(Brightness);
    public const string Contrast = nameof(Contrast);
    public const string Gamma = nameof(Gamma);
    public const string Saturation = nameof(Saturation);
    public const string Sharpness = nameof(Sharpness);
    public const string Dedithering = nameof(Dedithering);
    public const string Debanding = nameof(Debanding);
    public const string DetailRecovery = nameof(DetailRecovery);
    public const string Deinterlacing = nameof(Deinterlacing);
    public const string BlackFrameInsertion = nameof(BlackFrameInsertion);
    public const string SignalConnection = nameof(SignalConnection);
    public const string SignalConnectionIntensity = nameof(SignalConnectionIntensity);
    public const string SignalStandard = nameof(SignalStandard);
    public const string SignalStandardIntensity = nameof(SignalStandardIntensity);
    public const string Grain = nameof(Grain);
    public const string Vhs = nameof(Vhs);
    public const string ChromaticAberration = nameof(ChromaticAberration);
    public const string Bloom = nameof(Bloom);
    public const string Sepia = nameof(Sepia);
    public const string CrtColorStandards = nameof(CrtColorStandards);
    public const string CrtFieldRate = nameof(CrtFieldRate);
    public const string CrtMask = nameof(CrtMask);
    public const string CrtPhosphorColor = nameof(CrtPhosphorColor);
    public const string CrtBeamWidth = nameof(CrtBeamWidth);
    public const string CrtCurvature = nameof(CrtCurvature);
    public const string CrtConnection = nameof(CrtConnection);
    public const string CrtConnectionDegradation = nameof(CrtConnectionDegradation);
    public const string CrtMonitorPalette = nameof(CrtMonitorPalette);
    public const string FixedPixelTechnology = nameof(FixedPixelTechnology);
    public const string FixedPixelSubpixels = nameof(FixedPixelSubpixels);
    public const string FixedPixelMonochromeColor = nameof(FixedPixelMonochromeColor);
    public const string FixedPixelGridIntensity = nameof(FixedPixelGridIntensity);
    public const string FixedPixelPixelGap = nameof(FixedPixelPixelGap);
    public const string FixedPixelResponseTime = nameof(FixedPixelResponseTime);
    public const string FixedPixelPersistence = nameof(FixedPixelPersistence);
    public const string FixedPixelBacklight = nameof(FixedPixelBacklight);
    public const string FixedPixelBacklightBleed = nameof(FixedPixelBacklightBleed);
    public const string FixedPixelOledEmission = nameof(FixedPixelOledEmission);
    public const string FixedPixelBlackDepth = nameof(FixedPixelBlackDepth);
    public const string PlasmaCellStructure = nameof(PlasmaCellStructure);
    public const string VectorBeamWidth = nameof(VectorBeamWidth);
    public const string VectorPhosphorColor = nameof(VectorPhosphorColor);
    public const string VfdColor = nameof(VfdColor);
    public const string VfdDefinition = nameof(VfdDefinition);
    public const string VfdShape = nameof(VfdShape);
    public const string LedMatrixColor = nameof(LedMatrixColor);
    public const string LedMatrixDefinition = nameof(LedMatrixDefinition);
    public const string LedMatrixShape = nameof(LedMatrixShape);
    public const string DotMatrixPalette = nameof(DotMatrixPalette);
    public const string DotMatrixDefinition = nameof(DotMatrixDefinition);
    public const string DotMatrixShape = nameof(DotMatrixShape);
    public const string DotMatrixTechnology = nameof(DotMatrixTechnology);
    public const string SegmentDisplayTechnology = nameof(SegmentDisplayTechnology);
    public const string SegmentDisplayLayout = nameof(SegmentDisplayLayout);
    public const string SegmentDisplayColor = nameof(SegmentDisplayColor);
    public const string SegmentDisplayCellSize = nameof(SegmentDisplayCellSize);
    public const string SegmentDisplayHorizontalGap = nameof(SegmentDisplayHorizontalGap);
    public const string SegmentDisplayVerticalGap = nameof(SegmentDisplayVerticalGap);
    public const string SegmentDisplayThickness = nameof(SegmentDisplayThickness);
    public const string SegmentDisplaySegmentGap = nameof(SegmentDisplaySegmentGap);
    public const string SegmentDisplayEndShape = nameof(SegmentDisplayEndShape);
    public const string SegmentDisplayDecimalPoint = nameof(SegmentDisplayDecimalPoint);
    public const string SegmentDisplayColon = nameof(SegmentDisplayColon);
    public const string SegmentDisplayBrightness = nameof(SegmentDisplayBrightness);
    public const string SegmentDisplayActivationThreshold = nameof(SegmentDisplayActivationThreshold);
    public const string SegmentDisplayContrast = nameof(SegmentDisplayContrast);
    public const string SegmentDisplayOffSegmentVisibility = nameof(SegmentDisplayOffSegmentVisibility);
    public const string SegmentDisplayBlackDepth = nameof(SegmentDisplayBlackDepth);
    public const string SegmentDisplayGlow = nameof(SegmentDisplayGlow);
    public const string SegmentDisplayHaloRadius = nameof(SegmentDisplayHaloRadius);
    public const string SegmentDisplayResponseTime = nameof(SegmentDisplayResponseTime);
    public const string SegmentDisplayPersistence = nameof(SegmentDisplayPersistence);
    public const string EPaperColorMode = nameof(EPaperColorMode);
    public const string EPaperDefinition = nameof(EPaperDefinition);
    public const string ProjectionAmbientLight = nameof(ProjectionAmbientLight);

    public const string ExclusiveDisplayTechnology = nameof(ExclusiveDisplayTechnology);
    public const string UnsupportedBackend = nameof(UnsupportedBackend);

    private static readonly string[] ParameterIds =
    [
        Brightness, Contrast, Gamma, Saturation, Sharpness, Dedithering, Debanding,
        DetailRecovery,
        Deinterlacing, BlackFrameInsertion,
        SignalConnection, SignalConnectionIntensity, SignalStandard, SignalStandardIntensity,
        Grain,
        Vhs,
        ChromaticAberration,
        Bloom,
        Sepia,
        CrtColorStandards, CrtFieldRate, CrtMask, CrtPhosphorColor, CrtBeamWidth, CrtCurvature,
        CrtConnection, CrtConnectionDegradation, CrtMonitorPalette,
        FixedPixelTechnology, FixedPixelSubpixels, FixedPixelMonochromeColor,
        FixedPixelGridIntensity, FixedPixelPixelGap, FixedPixelResponseTime,
        FixedPixelPersistence, FixedPixelBacklight, FixedPixelBacklightBleed, FixedPixelOledEmission, FixedPixelBlackDepth,
        PlasmaCellStructure,
        VectorBeamWidth, VectorPhosphorColor,
        VfdColor, VfdDefinition, VfdShape,
        LedMatrixColor, LedMatrixDefinition, LedMatrixShape,
        DotMatrixPalette, DotMatrixDefinition, DotMatrixShape, DotMatrixTechnology,
        SegmentDisplayLayout, SegmentDisplayTechnology, SegmentDisplayColor,
        SegmentDisplayCellSize, SegmentDisplayHorizontalGap, SegmentDisplayVerticalGap,
        SegmentDisplayThickness, SegmentDisplaySegmentGap, SegmentDisplayEndShape,
        SegmentDisplayDecimalPoint, SegmentDisplayColon, SegmentDisplayBrightness,
        SegmentDisplayActivationThreshold, SegmentDisplayContrast,
        SegmentDisplayOffSegmentVisibility, SegmentDisplayBlackDepth, SegmentDisplayGlow,
        SegmentDisplayHaloRadius, SegmentDisplayResponseTime, SegmentDisplayPersistence,
        EPaperColorMode, EPaperDefinition, ProjectionAmbientLight
    ];

}
