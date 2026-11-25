using System.Numerics;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Dictionaries;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct EPaperVideoShaderParameters(Vector4 InkAndColor, Vector4 PaperSurface, Vector4 Temporal)
{
    internal static EPaperVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration,
        int sourceWidth, int sourceHeight, EPaperRefresh refresh)
    {
        var panel = configuration.EPaper;
        var (width, height) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(panel.Definition, sourceWidth, sourceHeight);
        return new(new(width, height, (float)panel.ColorMode, 0f), FilterEPaper.Surface(panel.ColorMode),
            new(refresh.CurrentWeight, refresh.Inversion, 0f, refresh.HasPrevious ? 1f : 0f));
    }
}
