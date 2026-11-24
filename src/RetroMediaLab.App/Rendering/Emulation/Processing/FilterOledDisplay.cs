using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterOledDisplay
{
    internal const string Shader = """
        vec3 filterOledDisplay(vec3 color,int panel,float frameLight)
        {
            float limiter=panel==1?.32:panel==2?.22:.10;
            if(panel==1)
            {
                float peak=max(color.r,max(color.g,color.b));
                float white=min(color.r,min(color.g,color.b));
                color=mix(color,vec3(dot(color,vec3(.2126,.7152,.0722))),
                    .25*smoothstep(.45,1.0,peak)*smoothstep(0.0,.50,white));
            }
            return clamp(color*(1.0-limiter*smoothstep(.18,.90,frameLight)),0.0,1.0);
        }
        """;

    // The panel input is opaque. The first alpha byte carries the frame's
    // linear light estimate to the GPU and to the software presenter.
    internal static void PrepareBrightness(Span<byte> pixels, int length)
    {
        long sum = 0;
        for (var offset = 0; offset < length; offset += 4)
        {
            var light = Math.Max(pixels[offset], Math.Max(pixels[offset + 1], pixels[offset + 2]));
            sum += light * light;
        }
        pixels[3] = (byte)Math.Round(sum / (255d * (length / 4)));
    }

    internal static void Apply(float[] colors, EmulationOledPanelType panel, float frameLight)
    {
        var limiter = panel switch
        {
            EmulationOledPanelType.Woled => .32f,
            EmulationOledPanelType.QdOled => .22f,
            _ => .10f
        };
        var output = 1f - limiter * SmoothStep((frameLight - .18f) / .72f);
        for (var index = 0; index < colors.Length; index += 3)
        {
            var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
            if (panel == EmulationOledPanelType.Woled)
            {
                var peak = Math.Max(color.X, Math.Max(color.Y, color.Z));
                var white = Math.Min(color.X, Math.Min(color.Y, color.Z));
                var desaturation = .25f * SmoothStep((peak - .45f) / .55f) * SmoothStep(white / .50f);
                color = Vector3.Lerp(color, new Vector3(Vector3.Dot(color, new(.2126f, .7152f, .0722f))), desaturation);
            }
            color = Vector3.Clamp(color * output, Vector3.Zero, Vector3.One);
            colors[index] = color.X;
            colors[index + 1] = color.Y;
            colors[index + 2] = color.Z;
        }
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
