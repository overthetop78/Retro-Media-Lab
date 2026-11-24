using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class LedMatrixVideoShaderParameters
{
    internal static Vector4 From(EmulationVideoProcessingConfiguration configuration, int sourceWidth, int sourceHeight)
    {
        if (configuration.DisplayTechnology != EmulationVideoDisplayTechnology.LedMatrix) return default;
        var matrix = configuration.LedMatrix;
        var (columns, rows) = EmulationVideoProcessingCatalog.LedMatrixDefinitionDimensions(matrix.Definition, sourceWidth, sourceHeight);
        return new(columns, rows, (float)matrix.Color, (float)matrix.Shape);
    }
}
