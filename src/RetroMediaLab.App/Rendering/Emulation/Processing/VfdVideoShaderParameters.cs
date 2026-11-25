using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct VfdVideoShaderParameters(Vector4 Display, Vector4 Structure)
{
    internal static VfdVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration, int width, int height)
    {
        var vfd = configuration.Vfd;
        var (columns, rows) = EmulationVideoProcessingCatalog.LedMatrixDefinitionDimensions(vfd.Definition, width, height);
        var phosphor = vfd.Color switch
        {
            EmulationVfdColor.Green => new Vector3(.055f, 1f, .12f),
            EmulationVfdColor.Blue => new Vector3(.055f, .29f, 1f),
            EmulationVfdColor.White => Vector3.One,
            EmulationVfdColor.Amber => new Vector3(1f, .42f, .028f),
            EmulationVfdColor.Red => new Vector3(1f, .045f, .025f),
            _ => new Vector3(.085f, 1f, .73f)
        };
        return new(new(phosphor, 0f), new(columns, rows, (float)vfd.Shape, 0f));
    }
}
