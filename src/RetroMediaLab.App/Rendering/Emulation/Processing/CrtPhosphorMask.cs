using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class CrtPhosphorMask
{
    private static readonly Vector3 LuminanceWeights = new(.2126f, .7152f, .0722f);

    internal static Vector4 Parameters(EmulationCrtVideoConfiguration configuration)
    {
        var tint = configuration.PhosphorColor switch
        {
            EmulationCrtPhosphorColor.Green => new Vector3(.16f, 1f, .23f),
            EmulationCrtPhosphorColor.Amber => new Vector3(1f, .43f, .015f),
            _ => Vector3.One
        };
        return new Vector4(tint, (float)configuration.Mask);
    }

    internal static void Apply(float[] colors, int width, int height, CrtSignal signal)
    {
        if ((EmulationCrtMask)(int)signal.Phosphor.W == EmulationCrtMask.None) return;
        var phosphor = signal.Phosphor;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var pixel = (y * width + x) * 3;
            var coordinate = RasterCoordinate(new Vector2((x + .5f) / width, (y + .5f) / height), signal.ActiveLines);
            var color = ApplyPixel(new Vector3(colors[pixel], colors[pixel + 1], colors[pixel + 2]),
                (int)coordinate.X, (int)coordinate.Y, phosphor);
            colors[pixel] = color.X;
            colors[pixel + 1] = color.Y;
            colors[pixel + 2] = color.Z;
        }
    }

    // Work in phosphor coordinates, with two samples per active raster line
    // and a 4:3 reference aperture. Output pixels only sample this fixed grid.
    internal static Vector2 RasterCoordinate(Vector2 uv, int activeLines) =>
        new(MathF.Floor(uv.X * (activeLines * (8f / 3f))), MathF.Floor(uv.Y * (activeLines * 2f)));

    internal static Vector3 ApplyPixel(Vector3 color, int x, int y, Vector4 phosphor)
    {
        var mask = (EmulationCrtMask)(int)phosphor.W;
        if (mask == EmulationCrtMask.None) return color;
        if (mask == EmulationCrtMask.Monochrome)
            return Vector3.Dot(color, LuminanceWeights) * new Vector3(phosphor.X, phosphor.Y, phosphor.Z);

        var coverage = mask switch
        {
            EmulationCrtMask.ShadowMask => DotTriad(x, y, false),
            EmulationCrtMask.HitachiEdp => DotTriad(x, y, true),
            _ => Stripes(x, y, mask)
        };
        return color * (new Vector3(.48f) + .52f * coverage);
    }

    private static Vector3 DotTriad(int x, int y, bool enhanced)
    {
        // The EDP triad has a tighter horizontal pitch and taller dots.
        var width = enhanced ? 6 : 8;
        var height = enhanced ? 7 : 6;
        var cellX = (x + y / height % 2 * (width / 2)) % width + .5f;
        var cellY = y % height + .5f;
        var point = new Vector2(cellX, cellY);
        var radii = enhanced ? new Vector2(1.05f, 1.65f) : new Vector2(1.35f, 1.35f);
        return new Vector3(
            Dot(point, new Vector2(width * .25f, 2f), radii),
            Dot(point, new Vector2(width * .75f, 2f), radii),
            Dot(point, new Vector2(width * .50f, height - 1.5f), radii));
    }

    private static float Dot(Vector2 point, Vector2 center, Vector2 radii) =>
        Math.Clamp(1.5f - Vector2.DistanceSquared((point - center) / radii, Vector2.Zero), 0f, 1f);

    private static Vector3 Stripes(int x, int y, EmulationCrtMask mask)
    {
        var channel = x / 2 % 3;
        var level = 1f;
        if (mask is EmulationCrtMask.SlotMask or EmulationCrtMask.StaggeredSlotMask)
        {
            var offset = mask == EmulationCrtMask.StaggeredSlotMask ? x / 6 % 2 * 5 : 0;
            var cellY = (y + offset) % 10;
            level = Math.Clamp(4.5f - MathF.Abs(cellY - 4.5f), 0f, 1f);
        }
        var coverage = Vector3.Zero;
        coverage[channel] = level;
        return coverage;
    }

    internal const string Shader = """
        float crtPhosphorDot(vec2 point, vec2 center, vec2 radii)
        {
            vec2 delta = (point - center) / radii;
            return clamp(1.5 - dot(delta, delta), 0.0, 1.0);
        }
        vec3 crtPhosphorMask(vec3 color, vec2 uv, float activeLines, vec4 phosphor)
        {
            int mask = int(phosphor.w + .5);
            if (mask == 0) return color;
            if (mask == 5) return dot(color, vec3(.2126,.7152,.0722)) * phosphor.xyz;
            vec2 pixel = floor(uv * activeLines * vec2(8.0 / 3.0, 2.0));
            vec3 coverage = vec3(0.0);
            if (mask == 2 || mask == 6)
            {
                vec2 pitch = mask == 6 ? vec2(6.0,7.0) : vec2(8.0,6.0);
                vec2 radii = mask == 6 ? vec2(1.05,1.65) : vec2(1.35);
                float row = mod(floor(pixel.y / pitch.y), 2.0);
                vec2 point = mod(pixel + vec2(row * pitch.x * .5, 0.0), pitch) + vec2(.5);
                coverage = vec3(
                    crtPhosphorDot(point, vec2(pitch.x * .25, 2.0), radii),
                    crtPhosphorDot(point, vec2(pitch.x * .75, 2.0), radii),
                    crtPhosphorDot(point, vec2(pitch.x * .50, pitch.y - 1.5), radii));
            }
            else
            {
                int channel = int(mod(floor(pixel.x / 2.0), 3.0));
                float level = 1.0;
                if (mask == 3 || mask == 4)
                {
                    float offset = mask == 4 ? mod(floor(pixel.x / 6.0), 2.0) * 5.0 : 0.0;
                    level = clamp(4.5 - abs(mod(pixel.y + offset, 10.0) - 4.5), 0.0, 1.0);
                }
                coverage = vec3(channel == 0 ? level : 0.0,
                                channel == 1 ? level : 0.0,
                                channel == 2 ? level : 0.0);
            }
            return color * (.48 + .52 * coverage);
        }
        """;
}
