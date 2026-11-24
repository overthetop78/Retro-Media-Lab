using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterLedMatrixColor
{
    private static readonly Vector3 Luminance = new(.2126f, .7152f, .0722f);

    internal static Vector3 Emission(Vector3 source, EmulationLedMatrixColor color)
    {
        source = Vector3.Clamp(source, Vector3.Zero, Vector3.One);
        if (color == EmulationLedMatrixColor.Rgb) return source;
        if (color == EmulationLedMatrixColor.RedGreen) return new(source.X, source.Y * .5f, 0f);

        var tint = color switch
        {
            EmulationLedMatrixColor.Red => new Vector3(1f, .03f, .01f),
            EmulationLedMatrixColor.Green => new Vector3(.03f, 1f, .08f),
            EmulationLedMatrixColor.Amber => new Vector3(1f, .42f, .02f),
            EmulationLedMatrixColor.Blue => new Vector3(.03f, .25f, 1f),
            _ => Vector3.One
        };
        return Vector3.Dot(source, Luminance) * tint;
    }

    internal const string Shader = """
        vec3 filterLedMatrixColor(vec3 source, float color)
        {
            source = clamp(source, 0.0, 1.0);
            if (color < .5) return source;
            if (color > 5.5) return vec3(source.r, source.g * .5, 0.0);
            float luminance = dot(source, vec3(.2126, .7152, .0722));
            if (color < 1.5) return luminance * vec3(1.0, .03, .01);
            if (color < 2.5) return luminance * vec3(.03, 1.0, .08);
            if (color < 3.5) return luminance * vec3(1.0, .42, .02);
            if (color < 4.5) return luminance * vec3(.03, .25, 1.0);
            return vec3(luminance);
        }
        """;
}
