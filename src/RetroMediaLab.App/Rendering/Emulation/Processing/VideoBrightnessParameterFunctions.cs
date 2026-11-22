namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoBrightnessParameterFunctions
{
    internal static void Apply(float[] colors, int setting)
    {
        if (setting == 0) return;
        var adjustment = EmulationImageAdjustmentFunctions.BrightnessAmount(setting);
        var target = adjustment < 0f ? 0f : 1f;
        var strength = MathF.Abs(adjustment);
        for (var index = 0; index < colors.Length; index++)
            colors[index] += (target - colors[index]) * strength;
    }

    internal const string Shader = """
        vec3 videoBrightnessParameter(vec3 color,float adjustment)
        {
            return mix(color,vec3(adjustment<0.0?0.0:1.0),abs(adjustment));
        }
        """;}
