using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct DotMatrixVideoShaderParameters(Vector4 Geometry, Vector4 Optics)
{
    internal static DotMatrixVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration, int width, int height)
    {
        var value = configuration.DotMatrix;
        var (columns, rows) = EmulationVideoProcessingCatalog.LedMatrixDefinitionDimensions(value.Definition, width, height);
        var tint = value.Palette switch
        {
            EmulationDotMatrixPalette.Gray => new Vector3(.55f),
            EmulationDotMatrixPalette.Green => new Vector3(.38f, .71f, .025f),
            EmulationDotMatrixPalette.Amber => new Vector3(1f, .45f, .032f),
            EmulationDotMatrixPalette.Red => new Vector3(1f, .025f, .015f),
            EmulationDotMatrixPalette.Blue => new Vector3(.035f, .22f, 1f),
            _ => Vector3.Zero
        };
        return new(
            new(columns, rows, value.Palette == EmulationDotMatrixPalette.Rgb ? 1f : 0f, (float)value.Shape),
            new(tint, (float)value.Technology));
    }
}
