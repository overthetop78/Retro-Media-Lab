namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoGammaParameterFunctions
{
    // BT.1886 with 0.1 cd/m² black and 100 cd/m² white, normalized to white.
    private static readonly float Bt1886BlackRoot = MathF.Pow(.001f, 1f / 2.4f);

    internal static void Apply(float[] colors, int setting)
    {
        var curve = EmulationImageAdjustmentFunctions.GammaParameter(setting);
        if (curve == 0f) return;
        for (var index = 0; index < colors.Length; index += 3)
        {
            var luminance = VideoSaturationParameterFunctions.Luminance(colors[index], colors[index + 1], colors[index + 2]);
            var signal = SoftwareEmulationVideoProcessingPipeline.LinearToSrgb(luminance);
            var corrected = curve < 0f
                ? MathF.Pow(Bt1886BlackRoot + (1f - Bt1886BlackRoot) * signal, 2.4f)
                : MathF.Pow(signal, curve);
            if (corrected == luminance) continue;
            var target = corrected > luminance ? 1f : 0f;
            var amount = (corrected - luminance) / (target - luminance);
            for (var channel = 0; channel < 3; channel++)
                colors[index + channel] += (target - colors[index + channel]) * amount;
        }
    }

    internal const string Shader = """
        float linearToSrgb(float value);
        vec3 videoGammaParameter(vec3 color,float curve)
        {
            if(curve==0.0) return color;
            float luminance=dot(color,vec3(0.2126,0.7152,0.0722));
            float signal=linearToSrgb(luminance);
            const float blackRoot=pow(.001,1.0/2.4);
            float corrected=curve<0.0?pow(mix(blackRoot,1.0,signal),2.4):pow(signal,curve);
            if(corrected==luminance) return color;
            float target=corrected>luminance?1.0:0.0;
            float amount=(corrected-luminance)/(target-luminance);
            return mix(color,vec3(target),amount);
        }
        """;}
