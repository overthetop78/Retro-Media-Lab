using RetroMediaLab.App.Constants.Rendering.Emulation;
using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing.OpenGl;

internal sealed class OpenGlVideoProgramUniforms
{
    private readonly OpenGlNativeApi _api;
    private readonly int _adjustments;
    private readonly int _processing;
    private readonly int _output;
    private readonly int _crtRaster;
    private readonly int _crtTuning;
    private readonly int _crtPhosphor;
    private readonly int _crtGeometry;
    private readonly int _fixedDisplay;
    private readonly int _fixedSpatial;
    private readonly int _fixedTechnology;
    private readonly int _fixedTemporal;
    private readonly int _plasmaEffect;
    private readonly int _plasmaTemporal;
    private readonly int _plasmaDisplay;
    private readonly int _vectorEffect;
    private readonly int _vectorTemporal;
    private readonly int _vectorDisplay;
    private readonly int _segmentGeometry;
    private readonly int _segmentShape;
    private readonly int _segmentEmission;
    private readonly int _segmentOptical;
    private readonly int _segmentTemporal;
    private readonly int _general;
    private readonly int _restoration;
    private readonly int _signal;
    private readonly int _stylistic;
    private readonly int _stylistic2;
    private readonly int _vfdDisplay;
    private readonly int _vfdStructure;
    private readonly int _vfdOptical;
    private readonly int _ledMatrixEmission;
    private readonly int _ledMatrixStructure;
    private readonly int _dotMatrixGeometry;
    private readonly int _dotMatrixEmission;
    private readonly int _dotMatrixTemporal;
    private readonly int _ePaperInkAndColor;
    private readonly int _ePaperSurface;
    private readonly int _ePaperTemporal;
    private readonly int _projection;
    private readonly int _projectionScreen;

    internal OpenGlVideoProgramUniforms(OpenGlNativeApi api, uint program)
    {
        _api = api;
        _adjustments = Location(OpenGlVideoUniformNames.Adjustments);
        _processing = Location(OpenGlVideoUniformNames.Processing);
        _output = Location(OpenGlVideoUniformNames.Output);
        _crtRaster = Location(OpenGlVideoUniformNames.CrtRaster);
        _crtTuning = Location(OpenGlVideoUniformNames.CrtTuning);
        _crtPhosphor = Location(OpenGlVideoUniformNames.CrtPhosphor);
        _crtGeometry = Location(OpenGlVideoUniformNames.CrtGeometry);
        _fixedDisplay = Location(OpenGlVideoUniformNames.FixedDisplay);
        _fixedSpatial = Location(OpenGlVideoUniformNames.FixedSpatial);
        _fixedTechnology = Location(OpenGlVideoUniformNames.FixedTechnology);
        _fixedTemporal = Location(OpenGlVideoUniformNames.FixedTemporal);
        _plasmaEffect = Location(OpenGlVideoUniformNames.PlasmaEffect);
        _plasmaTemporal = Location(OpenGlVideoUniformNames.PlasmaTemporal);
        _plasmaDisplay = Location(OpenGlVideoUniformNames.PlasmaDisplay);
        _vectorEffect = Location(OpenGlVideoUniformNames.VectorEffect);
        _vectorTemporal = Location(OpenGlVideoUniformNames.VectorTemporal);
        _vectorDisplay = Location(OpenGlVideoUniformNames.VectorDisplay);
        _segmentGeometry = Location(OpenGlVideoUniformNames.SegmentGeometry);
        _segmentShape = Location(OpenGlVideoUniformNames.SegmentShape);
        _segmentEmission = Location(OpenGlVideoUniformNames.SegmentEmission);
        _segmentOptical = Location(OpenGlVideoUniformNames.SegmentOptical);
        _segmentTemporal = Location(OpenGlVideoUniformNames.SegmentTemporal);
        _general = Location(OpenGlVideoUniformNames.General);
        _restoration = Location(OpenGlVideoUniformNames.Restoration);
        _signal = Location(OpenGlVideoUniformNames.Signal);
        _stylistic = Location(OpenGlVideoUniformNames.Stylistic);
        _stylistic2 = Location(OpenGlVideoUniformNames.Stylistic2);
        _vfdDisplay = Location(OpenGlVideoUniformNames.VfdDisplay);
        _vfdStructure = Location(OpenGlVideoUniformNames.VfdStructure);
        _vfdOptical = Location(OpenGlVideoUniformNames.VfdOptical);
        _ledMatrixEmission = Location(OpenGlVideoUniformNames.LedMatrixEmission);
        _ledMatrixStructure = Location(OpenGlVideoUniformNames.LedMatrixStructure);
        _dotMatrixGeometry = Location(OpenGlVideoUniformNames.DotMatrixGeometry);
        _dotMatrixEmission = Location(OpenGlVideoUniformNames.DotMatrixEmission);
        _dotMatrixTemporal = Location(OpenGlVideoUniformNames.DotMatrixTemporal);
        _ePaperInkAndColor = Location(OpenGlVideoUniformNames.EPaperInkAndColor);
        _ePaperSurface = Location(OpenGlVideoUniformNames.EPaperSurface);
        _ePaperTemporal = Location(OpenGlVideoUniformNames.EPaperTemporal);
        _projection = Location(OpenGlVideoUniformNames.Projection);
        _projectionScreen = Location(OpenGlVideoUniformNames.ProjectionScreen);

        int Location(string name) => api.GetUniformLocation(program, name);
    }

    internal void Apply(EmulationVideoProcessingConfiguration configuration, int sourceWidth, int sourceHeight, int outputWidth, int outputHeight, bool hasHistory, double elapsedMilliseconds, long sequence, CrtSignal crtSignal, float averageLuminance)
    {
        var adjustments = configuration.Adjustments;
        _api.SetUniform(_adjustments,
            configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Crt
                ? VideoBrightnessParameterFunctions.CrtBeamGain(adjustments.Brightness)
                : EmulationImageAdjustmentFunctions.BrightnessAmount(adjustments.Brightness),
            EmulationImageAdjustmentFunctions.ContrastGain(adjustments.Contrast),
            EmulationImageAdjustmentFunctions.GammaParameter(adjustments.Gamma),
            EmulationImageAdjustmentFunctions.SaturationGain(adjustments.Saturation));
        _api.SetUniform(_processing, EmulationImageAdjustmentFunctions.SharpnessAmount(adjustments.Sharpness), (float)configuration.Sampling, sourceWidth, sourceHeight);
        _api.SetUniform(_output, outputWidth, outputHeight, 0f, crtSignal.BeamExponent);

        Set(_crtRaster, crtSignal.Raster);
        Set(_crtTuning, crtSignal.Tuning);
        Set(_crtPhosphor, crtSignal.Phosphor);
        _api.SetUniform(_crtGeometry, crtSignal.Curvature, 0f, 0f, 0f);

        var fixedPixel = FixedPixelVideoShaderParameters.From(configuration, sourceWidth, sourceHeight, hasHistory, elapsedMilliseconds);
        Set(_fixedDisplay, fixedPixel.Display);
        Set(_fixedSpatial, fixedPixel.Spatial);
        Set(_fixedTechnology, fixedPixel.Technology);
        Set(_fixedTemporal, fixedPixel.Temporal);

        var plasma = PlasmaVideoShaderParameters.From(configuration, hasHistory, sequence, averageLuminance);
        Set(_plasmaEffect, plasma.Effect);
        Set(_plasmaTemporal, plasma.Temporal);
        Set(_plasmaDisplay, plasma.Display);

        var vector = VectorVideoShaderParameters.From(configuration, hasHistory);
        Set(_vectorEffect, vector.Effect);
        Set(_vectorTemporal, vector.Temporal);
        Set(_vectorDisplay, vector.Display);

        var segmentDisplay = SegmentDisplayVideoShaderParameters.From(configuration, hasHistory, elapsedMilliseconds);
        Set(_segmentGeometry, segmentDisplay.Geometry);
        Set(_segmentShape, segmentDisplay.Shape);
        Set(_segmentEmission, segmentDisplay.Emission);
        Set(_segmentOptical, segmentDisplay.Optical);
        Set(_segmentTemporal, segmentDisplay.Temporal);

        Set(_general, new((float)configuration.DisplayTechnology, hasHistory ? 1f : 0f, sequence % OpenGlVideoConstants.ShaderSequenceCycle, (float)elapsedMilliseconds));
        Set(_restoration, new(
            0f,
            0f,
            configuration.Restoration.Debanding / OpenGlVideoConstants.PercentageDivisor,
            0f));
        Set(_signal, new(
            (float)configuration.SignalSimulation.Connection,
            configuration.SignalSimulation.ConnectionIntensity
                / OpenGlVideoConstants.PercentageDivisor,
            (float)configuration.SignalSimulation.Standard,
            configuration.SignalSimulation.StandardIntensity
                / OpenGlVideoConstants.PercentageDivisor));
        Set(_stylistic, new(
            configuration.Stylistic.Grain / OpenGlVideoConstants.PercentageDivisor,
            configuration.Stylistic.Vhs / OpenGlVideoConstants.PercentageDivisor,
            configuration.Stylistic.ChromaticAberration
                / OpenGlVideoConstants.PercentageDivisor,
            configuration.Stylistic.Bloom / OpenGlVideoConstants.PercentageDivisor));
        Set(_stylistic2, new(configuration.Stylistic.Sepia ? 1f : 0f, 0f, configuration.Restoration.DetailRecovery / OpenGlVideoConstants.PercentageDivisor, 0f));

        var vfd = VfdVideoShaderParameters.From(configuration, hasHistory, elapsedMilliseconds);
        Set(_vfdDisplay, vfd.Display);
        Set(_vfdStructure, vfd.Structure);
        Set(_vfdOptical, vfd.Optical);

        var ledMatrix = LedMatrixVideoShaderParameters.From(configuration);
        Set(_ledMatrixEmission, ledMatrix.Emission);
        Set(_ledMatrixStructure, ledMatrix.Structure);

        var dotMatrix = DotMatrixVideoShaderParameters.From(configuration, hasHistory, elapsedMilliseconds);
        Set(_dotMatrixGeometry, dotMatrix.Geometry);
        Set(_dotMatrixEmission, dotMatrix.Emission);
        Set(_dotMatrixTemporal, dotMatrix.Temporal);

        var ePaper = EPaperVideoShaderParameters.From(configuration, hasHistory, elapsedMilliseconds);
        Set(_ePaperInkAndColor, ePaper.InkAndColor);
        Set(_ePaperSurface, ePaper.PaperSurface);
        Set(_ePaperTemporal, ePaper.Temporal);

        Set(_projection, new(
            configuration.Projection.OpticalBlur / OpenGlVideoConstants.PercentageDivisor,
            configuration.Projection.Diffusion / OpenGlVideoConstants.PercentageDivisor,
            configuration.Projection.ScreenTexture / OpenGlVideoConstants.PercentageDivisor,
            configuration.Projection.Convergence / OpenGlVideoConstants.PercentageDivisor));
        Set(_projectionScreen, new(
            configuration.Projection.LightOutput / OpenGlVideoConstants.PercentageDivisor,
            configuration.Projection.AmbientLight / OpenGlVideoConstants.PercentageDivisor,
            configuration.Projection.Vignette / OpenGlVideoConstants.PercentageDivisor, 0f));
    }

    private void Set(int location, Vector4 value) => _api.SetUniform(location, value);
}
