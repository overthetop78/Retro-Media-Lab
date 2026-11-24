using RetroMediaLab.App.Constants.Rendering.Emulation;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed partial class SoftwareEmulationVideoProcessingPipeline : IEmulationVideoProcessingPipeline
{
    private static void ApplyDisplayTechnology(float[] colors, int sourceWidth, int sourceHeight, int outputWidth, int outputHeight, VideoFrame frame, EmulationVideoProcessingConfiguration configuration, CrtSignal crt, float plasmaLoad)
    {
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.FixedPixel)
        {
            FilterFixedPixel.Apply(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, configuration);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Plasma)
        {
            var plasma = configuration.Plasma;
            var (panelWidth, panelHeight) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(plasma.Definition, sourceWidth, sourceHeight);
            FilterPlasmaPhosphorIntensity.Apply(colors, PlasmaVideoShaderParameters.PhosphorIntensity);
            FilterPlasmaAutomaticBrightnessLimiter.Apply(colors, plasmaLoad, PlasmaVideoShaderParameters.BrightnessLimiter);
            FilterPlasmaTemporalDithering.Apply(colors, outputWidth, outputHeight, panelWidth, panelHeight,
                frame.Sequence, PlasmaVideoShaderParameters.Dithering);
            FilterPlasmaBlackDepth.Apply(colors);
            FilterPlasmaCellStructure.Apply(colors, panelWidth, panelHeight, outputWidth, outputHeight, plasma.CellStructure);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Vector)
        {
            var vector = configuration.Vector;
            if (vector.LineIntensity > 0)
            {
                var emission = FilterVectorLineDetection.Detect(colors, outputWidth, outputHeight, vector.LineThreshold);
                emission = FilterVectorBeamWidth.Apply(emission, outputWidth, outputHeight, vector.BeamWidth);
                emission = FilterVectorBeamFocus.Apply(emission, outputWidth, outputHeight, vector.BeamFocus);
                FilterVectorLineIntensity.Apply(colors, emission, vector.LineIntensity);
                FilterVectorHalo.Apply(colors, emission, outputWidth, outputHeight, vector.LineIntensity, vector.HaloIntensity, vector.HaloRadius);
                FilterVectorPhosphorColor.Apply(colors, vector.PhosphorColor);
            }
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Vfd)
        {
            var vfd = configuration.Vfd;
            var emission = FilterVfdEmissionThreshold.Extract(colors, vfd.EmissionThreshold);
            FilterVfdCellStructure.Apply(emission, outputWidth, outputHeight, sourceWidth, sourceHeight, vfd.Structure, vfd.CellSize, vfd.CellGap);
            FilterVfdPhosphorIntensity.Apply(emission, vfd.PhosphorIntensity);
            var halo = FilterVfdHalo.Create(emission, outputWidth, outputHeight, sourceWidth, sourceHeight, vfd.HaloRadius, vfd.HaloIntensity);
            FilterVfdGlass.Apply(colors, vfd.GlassDarkening);
            FilterVfdPhosphorColor.Apply(colors, emission, halo, vfd.Color);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.LedMatrix)
        {
            var ledMatrix = configuration.LedMatrix;
            var cells = FilterLedMatrixCellStructure.Create(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, ledMatrix.CellSize, ledMatrix.CellGap, ledMatrix.Shape, ledMatrix.HaloRadius);
            FilterLedMatrixColor.Apply(cells.Emission, ledMatrix.Color);
            FilterLedMatrixBrightness.Apply(cells.Emission, ledMatrix.Brightness);
            FilterLedMatrixBlackDepth.Compose(colors, cells, ledMatrix.Diffusion, ledMatrix.BlackDepth);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.DotMatrix)
        {
            FilterDotMatrix.Apply(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, configuration.DotMatrix);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.SegmentDisplay)
        {
            FilterSegmentDisplay.Apply(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, configuration.SegmentDisplay);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.EPaper)
        {
            FilterEPaper.Apply(colors, outputWidth, outputHeight, configuration.EPaper);
            return;
        }
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Projection)
        {
            FilterProjection.Apply(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, configuration.Projection);
            return;
        }
        if (configuration.DisplayTechnology != EmulationVideoDisplayTechnology.Crt) return;

        FilterCrt.Apply(colors, outputWidth, outputHeight, sourceWidth, sourceHeight, crt,
            VideoBrightnessParameterFunctions.CrtBeamGain(configuration.Adjustments.Brightness));
    }
}
