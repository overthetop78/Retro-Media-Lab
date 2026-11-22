namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoSharpnessParameterFunctions
{
    internal static void Apply(float[] colors, int width, int height, int setting)
    {
        if (setting == 0) return;
        var source = colors.ToArray();
        var strength = MathF.Abs(EmulationImageAdjustmentFunctions.SharpnessAmount(setting));
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var channel = 0; channel < 3; channel++)
        {
            var average = NeighborhoodAverage(source, width, height, x, y, channel, setting < 0);
            var index = (y * width + x) * 3 + channel;
            colors[index] = Math.Clamp(setting > 0 ? source[index] + (source[index] - average) * strength : source[index] + (average - source[index]) * strength, 0f, 1f);
        }
    }

    private static float NeighborhoodAverage(float[] colors, int width, int height, int x, int y, int channel,
        bool blur)
    {
        var sum = 0f;
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        for (var offsetX = -1; offsetX <= 1; offsetX++)
        {
            sum += Sample(offsetX, offsetY);
        }
        if (!blur) return sum / 9f;

        return (sum + (Sample(-2, 0) + Sample(2, 0) + Sample(0, -2) + Sample(0, 2)) * .5f) / 11f;

        float Sample(int offsetX, int offsetY)
        {
            var sampleX = Math.Clamp(x + offsetX, 0, width - 1);
            var sampleY = Math.Clamp(y + offsetY, 0, height - 1);
            return colors[(sampleY * width + sampleX) * 3 + channel];
        }
    }

    internal const string Shader = """
        vec3 videoSharpnessParameter(vec3 center,vec3 average,float setting)
        { float strength=abs(setting); return clamp(setting>=0.0?center+(center-average)*strength:mix(center,average,strength),0.0,1.0); }
        """;}
