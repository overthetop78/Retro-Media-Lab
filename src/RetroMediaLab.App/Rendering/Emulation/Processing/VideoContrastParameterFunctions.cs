namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoContrastParameterFunctions
{
    private const float MiddleGray = .21586f; // sRGB 128 in linear light.

    internal static void Apply(float[] colors, int setting)
    {
        if (setting == 0) return;
        var factor = EmulationImageAdjustmentFunctions.ContrastGain(setting);
        for (var index = 0; index < colors.Length; index++)
            colors[index] = Math.Clamp(MiddleGray + (colors[index] - MiddleGray) * factor, 0f, 1f);
    }

    internal const string Shader = """
        vec3 videoContrastParameter(vec3 color,float factor)
        {
            if(factor==1.0) return color;
            const float middleGray=.21586;
            return clamp(vec3(middleGray)+(color-vec3(middleGray))*factor,0.0,1.0);
        }
        """;}
