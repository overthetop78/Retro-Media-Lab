using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterLedBacklitLcdDisplay
{
    internal const string Shader = """
        vec3 filterLedBacklitLcdDisplay(vec3 color,vec2 uv,float backlight,int panel,int type,float zone)
        {
            float edge=exp(-9.0*min(uv.y,1.0-uv.y))*(.70+.30*sin(uv.x*11.0+uv.y*2.0));
            float light;
            if(type==0)light=.92+.08*edge;
            else if(type==1)light=.98+.02*(1.0-abs(2.0*uv.y-1.0));
            else light=zone;
            float black=panel==0?.005:panel==1?.0018:.0004;
            float dark=clamp(1.0-dot(color,vec3(.2126,.7152,.0722))/.22,0.0,1.0);
            float floorLevel=black*(type==0?1.0+1.25*edge*dark*dark:1.0);
            return clamp(pow(backlight,.8)*light*(vec3(floorLevel)+color*(1.0-floorLevel)),0.0,1.0);
        }
        """;

    // The decoded input is opaque. Its spare alpha byte transports the same
    // backlight field to the software presenter and to the source GPU texture.
    internal static void PrepareZones(Span<byte> pixels, int width, int height, EmulationLedBacklightType type)
    {
        if (type is not (EmulationLedBacklightType.FullArray or EmulationLedBacklightType.MiniLed)) return;
        var mini = type == EmulationLedBacklightType.MiniLed;
        var columns = Math.Min(width, mini ? 32 : 8);
        var rows = Math.Min(height, mini ? 18 : 5);
        Span<byte> peaks = stackalloc byte[columns * rows];
        peaks.Clear();

        for (var y = 0; y < height; y++)
        {
            var zoneRow = y * rows / height * columns;
            for (var x = 0; x < width; x++)
            {
                var pixel = (y * width + x) * 4;
                var zone = zoneRow + x * columns / width;
                var peak = Math.Max(pixels[pixel], Math.Max(pixels[pixel + 1], pixels[pixel + 2]));
                peaks[zone] = Math.Max(peaks[zone], peak);
            }
        }

        var minimum = mini ? .06f : .17f;
        var diffusion = mini ? .35f : .7f;
        for (var y = 0; y < height; y++)
        {
            var zoneRow = y * rows / height * columns;
            var (top, bottom, verticalBlend) = Axis(y, height, rows);
            for (var x = 0; x < width; x++)
            {
                var (left, right, horizontalBlend) = Axis(x, width, columns);
                var upper = float.Lerp(peaks[top * columns + left], peaks[top * columns + right], horizontalBlend);
                var lower = float.Lerp(peaks[bottom * columns + left], peaks[bottom * columns + right], horizontalBlend);
                var spread = float.Lerp(upper, lower, verticalBlend);
                var peak = peaks[zoneRow + x * columns / width];
                var level = Math.Max(peak, spread * diffusion) / 255f;
                pixels[(y * width + x) * 4 + 3] = (byte)MathF.Round(255f * float.Lerp(minimum, 1f, level));
            }
        }
    }

    internal static void Apply(float[] colors, int width, int height, ReadOnlySpan<byte> source,
        int sourceWidth, int sourceHeight, EmulationLcdVideoConfiguration panel)
    {
        var gain = MathF.Pow(panel.BacklightIntensity / 100f, .8f);
        var black = panel.PanelType switch
        {
            EmulationLcdPanelType.Tn => .005f,
            EmulationLcdPanelType.Ips => .0018f,
            _ => .0004f
        };
        var type = panel.BacklightType;
        var edgeLighting = type == EmulationLedBacklightType.Edge;
        var directLighting = type == EmulationLedBacklightType.Direct;
        var luminance = new Vector3(.2126f, .7152f, .0722f);
        for (var y = 0; y < height; y++)
        {
            var v = (y + .5f) / height;
            var sourceRow = Math.Min(sourceHeight - 1, (int)(v * sourceHeight)) * sourceWidth;
            var edgeFalloff = edgeLighting ? MathF.Exp(-9f * Math.Min(v, 1f - v)) : 0f;
            var directLight = .98f + .02f * (1f - MathF.Abs(2f * v - 1f));
            for (var x = 0; x < width; x++)
            {
                var u = (x + .5f) / width;
                var index = (y * width + x) * 3;
                var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
                var edge = edgeFalloff * (.70f + .30f * MathF.Sin(u * 11f + v * 2f));
                var light = edgeLighting ? .92f + .08f * edge : directLighting ? directLight
                    : source[(sourceRow + Math.Min(sourceWidth - 1, (int)(u * sourceWidth))) * 4 + 3] / 255f;
                var dark = Math.Clamp(1f - Vector3.Dot(color, luminance) / .22f, 0f, 1f);
                var floor = black * (1f + 1.25f * edge * dark * dark);
                var output = Vector3.Clamp(gain * light * (new Vector3(floor) + color * (1f - floor)),
                    Vector3.Zero, Vector3.One);
                colors[index] = output.X;
                colors[index + 1] = output.Y;
                colors[index + 2] = output.Z;
            }
        }
    }

    private static (int First, int Second, float Weight) Axis(int pixel, int length, int zones)
    {
        var position = Math.Clamp((pixel + .5f) * zones / length - .5f, 0f, zones - 1f);
        var first = (int)position;
        return (first, Math.Min(first + 1, zones - 1), position - first);
    }
}
