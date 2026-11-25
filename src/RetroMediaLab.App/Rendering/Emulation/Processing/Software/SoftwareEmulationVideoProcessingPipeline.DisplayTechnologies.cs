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
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.SegmentDisplay)
        {
            FilterSegmentDisplay.Apply(colors, sourceWidth, sourceHeight, outputWidth, outputHeight, configuration.SegmentDisplay, frame);
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
