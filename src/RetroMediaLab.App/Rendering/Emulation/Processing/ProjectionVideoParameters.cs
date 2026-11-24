using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct ProjectionVideoParameters(Vector4 Optics, Vector4 Screen)
{
    internal static ProjectionVideoParameters From(EmulationProjectionVideoConfiguration configuration)
    {
        // Black level, blur weight, half-width of a cell border, and CRT convergence in source pixels.
        var optics = configuration.Technology switch
        {
            EmulationProjectionTechnology.Crt => new Vector4(.0007f, .48f, 0f, .20f),
            EmulationProjectionTechnology.ThreeLcd => new Vector4(.009f, 0f, .065f, 0f),
            EmulationProjectionTechnology.SingleChipDlp => new Vector4(.004f, 0f, .020f, 0f),
            EmulationProjectionTechnology.ThreeChipDlp => new Vector4(.003f, 0f, .020f, 0f),
            _ => new Vector4(.0013f, 0f, .008f, 0f)
        };
        var reflection = configuration.Surface == EmulationProjectionSurface.GrayScreen ? .62f : 1f;
        var ambient = .25f * configuration.AmbientLight / EmulationProjectionVideoConfiguration.MaximumAmbientLight;
        var texture = configuration.Surface == EmulationProjectionSurface.WhiteWall ? .045f : 0f;
        return new ProjectionVideoParameters(optics, new Vector4(reflection, ambient, texture, 0f));
    }
}
