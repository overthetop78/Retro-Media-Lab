namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterPlasmaBlackDepth
{
    internal const string Shader = """
        vec3 filterPlasmaBlackDepth(vec3 color)
        {
            return mix(vec3(.0025),vec3(1.0),color);
        }
        """;

    internal static void Apply(float[] colors)
    {
        const float floor = .0025f;
        for (var index = 0; index < colors.Length; index++)
            colors[index] = float.Lerp(floor, 1f, colors[index]);
    }
}
