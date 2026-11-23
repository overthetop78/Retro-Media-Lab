using System.Buffers;
using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class CrtPhosphorMask
{
    private const float StripePeriod = 6f;
    private const float StripeWidth = 1.75f;
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

    internal static void Apply(float[] colors, int width, int height, CrtSignal signal, float beamGain)
    {
        var mask = (EmulationCrtMask)(int)signal.Phosphor.W;
        if (mask == EmulationCrtMask.None) return;
        var phosphor = signal.Phosphor;
        var rasterSize = signal.MaskRasterSize;
        var footprint = rasterSize * 3f / new Vector2(width, height);
        var spacing = Vector2.One / (rasterSize * 3f);
        var across = new Vector2(spacing.X, 0f);
        var down = new Vector2(0f, spacing.Y);
        var spread = Math.Clamp(.72f * MathF.Sqrt(beamGain) / (1f + signal.BeamExponent * signal.BeamExponent), 0f, .78f);
        var source = ArrayPool<float>.Shared.Rent(colors.Length);
        try
        {
            colors.CopyTo(source, 0);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var pixel = (y * width + x) * 3;
                var uv = new Vector2((x + .5f) / width, (y + .5f) / height);
                var direct = Emit(new Vector3(source[pixel], source[pixel + 1], source[pixel + 2]), uv);
                var neighbors = (Sample(uv - across) + Sample(uv + across) + Sample(uv - down) + Sample(uv + down)) * .25f;
                var color = Vector3.Lerp(direct, neighbors, spread) + new Vector3(Glass(uv, rasterSize, mask, footprint));
                colors[pixel] = color.X;
                colors[pixel + 1] = color.Y;
                colors[pixel + 2] = color.Z;
            }
        }
        finally { ArrayPool<float>.Shared.Return(source); }

        Vector3 Emit(Vector3 color, Vector2 uv)
        {
            if (mask == EmulationCrtMask.Monochrome)
                return Vector3.Dot(color, LuminanceWeights) * new Vector3(phosphor.X, phosphor.Y, phosphor.Z);
            return color * Coverage(uv * rasterSize * 3f, footprint, mask);
        }

        Vector3 Sample(Vector2 uv)
        {
            if (uv.X < 0f || uv.X >= 1f || uv.Y < 0f || uv.Y >= 1f) return Vector3.Zero;
            var index = ((int)(uv.Y * height) * width + (int)(uv.X * width)) * 3;
            return Emit(new Vector3(source[index], source[index + 1], source[index + 2]), uv);
        }
    }

    internal static Vector2 RasterCoordinate(Vector2 uv, Vector2 rasterSize) =>
        new(MathF.Floor(uv.X * rasterSize.X * 3f), MathF.Floor(uv.Y * rasterSize.Y * 3f));

    internal static Vector3 ApplyPixel(Vector3 color, int x, int y, Vector4 phosphor)
    {
        var mask = (EmulationCrtMask)(int)phosphor.W;
        if (mask == EmulationCrtMask.None) return color;
        if (mask == EmulationCrtMask.Monochrome)
            return Vector3.Dot(color, LuminanceWeights) * new Vector3(phosphor.X, phosphor.Y, phosphor.Z);

        return color * Coverage(new Vector2(x + .5f, y + .5f), Vector2.Zero, mask);
    }

    private static float Glass(Vector2 uv, Vector2 rasterSize, EmulationCrtMask mask, Vector2 footprint)
    {
        if (mask == EmulationCrtMask.Monochrome) return 0f;
        var coverage = Coverage(uv * rasterSize * 3f, footprint, mask);
        return .00010f + .00075f * (coverage.X + coverage.Y + coverage.Z);
    }

    internal static Vector3 Coverage(Vector2 position, Vector2 footprint, EmulationCrtMask mask)
    {
        if (mask == EmulationCrtMask.ApertureGrille)
            return StripeCoverage(position.X, footprint.X, spacing: StripePeriod / 3f);
        if (mask == EmulationCrtMask.SlotMask)
            return StripeCoverage(position.X, footprint.X);
        if (mask == EmulationCrtMask.StaggeredSlotMask)
        {
            var first = Opening(position.Y, footprint.Y, 10f, 5f, 4.7f);
            var second = Opening(position.Y, footprint.Y, 10f, 0f, 4.7f);
            return first * StripeCoverage(position.X, footprint.X, 1.85f, 2f * StripePeriod, 1.85f)
                + second * StripeCoverage(position.X - StripePeriod, footprint.X, 1.85f, 2f * StripePeriod, 1.85f);
        }

        // Two staggered rows share the same dot profile for all three phosphors.
        var enhanced = mask == EmulationCrtMask.HitachiEdp;
        var pitch = enhanced ? new Vector2(6f, 7f) : new Vector2(8f, 6f);
        var radius = enhanced ? new Vector2(1.4f, 2.55f) : new Vector2(1.85f, 1.85f);
        return new Vector3(Dot(.25f, 2f), Dot(.75f, 2f), Dot(.5f, pitch.Y - 1.5f));

        float Dot(float x, float y) =>
            DotOpening(position.X, footprint.X, pitch.X, pitch.X * x, radius.X)
                * DotOpening(position.Y, footprint.Y, pitch.Y * 2f, y, radius.Y)
            + DotOpening(position.X, footprint.X, pitch.X, pitch.X * (x + .5f), radius.X)
                * DotOpening(position.Y, footprint.Y, pitch.Y * 2f, y + pitch.Y, radius.Y);
    }

    private static float DotOpening(float position, float footprint, float period, float center, float radius) =>
        .65f * Opening(position, footprint, period, center, radius)
        + .35f * Opening(position, footprint, period, center, radius * .7f);

    private static float Opening(float position, float footprint, float period, float center, float radius)
    {
        var start = position - center + radius;
        var length = 2f * radius;
        if (footprint <= 0f) return start - period * MathF.Floor(start / period) < length ? 1f : 0f;
        return (Integral(start + footprint * .5f) - Integral(start - footprint * .5f)) / footprint;

        float Integral(float coordinate)
        {
            var cycles = MathF.Floor(coordinate / period);
            return cycles * length + Math.Clamp(coordinate - cycles * period, 0f, length);
        }
    }

    private static Vector3 StripeCoverage(float x, float footprint,
        float width = StripeWidth, float period = StripePeriod, float spacing = StripeWidth)
    {
        var offsets = new Vector3(0f, spacing, 2f * spacing);
        if (footprint <= 0f)
        {
            var phase = x - period * MathF.Floor(x / period);
            return new Vector3(
                phase < width ? 1f : 0f,
                phase >= spacing && phase < spacing + width ? 1f : 0f,
                phase >= 2f * spacing && phase < 2f * spacing + width ? 1f : 0f);
        }

        return (Integral(x + footprint * .5f) - Integral(x - footprint * .5f)) / footprint;

        Vector3 Integral(float position)
        {
            var periods = MathF.Floor(position / period);
            var phase = position - periods * period;
            return new Vector3(periods * width) + Vector3.Clamp(
                new Vector3(phase) - offsets, Vector3.Zero, new Vector3(width));
        }
    }

    internal const string Shader = """
        float crtOpeningIntegral(float x, float period, float length)
        {
            float cycles = floor(x / period);
            return cycles * length + clamp(x - cycles * period, 0.0, length);
        }
        float crtOpening(float position, float footprint, float period, float center, float radius)
        {
            float start = position - center + radius;
            float length = 2.0 * radius;
            return (crtOpeningIntegral(start + footprint * .5, period, length)
                - crtOpeningIntegral(start - footprint * .5, period, length)) / footprint;
        }
        float crtDotOpening(float position, float footprint, float period, float center, float radius)
        {
            return .65 * crtOpening(position, footprint, period, center, radius)
                + .35 * crtOpening(position, footprint, period, center, radius * .7);
        }
        float crtDot(vec2 position, vec2 footprint, vec2 pitch, vec2 center, vec2 radius)
        {
            return crtDotOpening(position.x, footprint.x, pitch.x, center.x, radius.x)
                    * crtDotOpening(position.y, footprint.y, 2.0 * pitch.y, center.y, radius.y)
                + crtDotOpening(position.x, footprint.x, pitch.x, center.x + .5 * pitch.x, radius.x)
                    * crtDotOpening(position.y, footprint.y, 2.0 * pitch.y, center.y + pitch.y, radius.y);
        }
        vec3 crtStripeIntegral(float x, float width, float period, vec3 offsets)
        {
            float periods = floor(x / period);
            float phase = x - periods * period;
            return vec3(periods * width) + clamp(vec3(phase) - offsets, 0.0, width);
        }
        vec3 crtStripeCoverage(float x, float footprint, float width, float period, float spacing)
        {
            vec3 offsets = vec3(0.0, spacing, 2.0 * spacing);
            return (crtStripeIntegral(x + footprint * .5, width, period, offsets)
                - crtStripeIntegral(x - footprint * .5, width, period, offsets)) / footprint;
        }
        vec3 crtMaskCoverage(vec2 position, int mask, vec2 footprint)
        {
            if (mask == 1) return crtStripeCoverage(position.x, footprint.x, 1.75, 6.0, 2.0);
            if (mask == 3) return crtStripeCoverage(position.x, footprint.x, 1.75, 6.0, 1.75);
            if (mask == 2 || mask == 6)
            {
                vec2 pitch = mask == 6 ? vec2(6.0, 7.0) : vec2(8.0, 6.0);
                vec2 radius = mask == 6 ? vec2(1.4, 2.55) : vec2(1.85);
                return vec3(
                    crtDot(position, footprint, pitch, vec2(pitch.x * .25, 2.0), radius),
                    crtDot(position, footprint, pitch, vec2(pitch.x * .75, 2.0), radius),
                    crtDot(position, footprint, pitch, vec2(pitch.x * .50, pitch.y - 1.5), radius));
            }
            return crtOpening(position.y, footprint.y, 10.0, 5.0, 4.7)
                    * crtStripeCoverage(position.x, footprint.x, 1.85, 12.0, 1.85)
                + crtOpening(position.y, footprint.y, 10.0, 0.0, 4.7)
                    * crtStripeCoverage(position.x - 6.0, footprint.x, 1.85, 12.0, 1.85);
        }
        vec3 crtEmit(vec3 color, vec2 uv, vec2 grid, vec4 phosphor, vec2 footprint)
        {
            int mask = int(phosphor.w + .5);
            if (mask == 5) return dot(color, vec3(.2126,.7152,.0722)) * phosphor.xyz;
            return color * crtMaskCoverage(uv * grid, mask, footprint);
        }
        #if DISPLAY_TECHNOLOGY == 1
        vec3 crtImageAt(vec2 uv);
        vec3 crtNeighborEmission(vec2 uv, vec2 grid, vec4 phosphor, vec2 footprint)
        {
            if (any(lessThan(uv, vec2(0.0))) || any(greaterThanEqual(uv, vec2(1.0)))) return vec3(0.0);
            return crtEmit(crtImageAt(uv), uv, grid, phosphor, footprint);
        }
        vec3 crtCompose(vec3 color, vec2 uv, vec2 rasterSize, vec4 phosphor, float exponent, float gain, vec2 outputSize)
        {
            int mask = int(phosphor.w + .5);
            if (mask == 0) return color;
            vec2 grid = rasterSize * 3.0;
            vec2 footprint = grid / max(vec2(1.0), outputSize);
            vec2 across = vec2(1.0 / grid.x, 0.0);
            vec2 down = vec2(0.0, 1.0 / grid.y);
            vec3 direct = crtEmit(color, uv, grid, phosphor, footprint);
            vec3 adjacent = (crtNeighborEmission(uv - across, grid, phosphor, footprint)
                + crtNeighborEmission(uv + across, grid, phosphor, footprint)
                + crtNeighborEmission(uv - down, grid, phosphor, footprint)
                + crtNeighborEmission(uv + down, grid, phosphor, footprint)) * .25;
            float spread = clamp(.72 * sqrt(gain) / (1.0 + exponent * exponent), 0.0, .78);
            float glass = 0.0;
            if (mask != 5)
            {
                vec2 position = uv * grid;
                vec3 coverage = crtMaskCoverage(position, mask, footprint);
                glass = .00010 + .00075 * (coverage.r + coverage.g + coverage.b);
            }
            return mix(direct, adjacent, spread) + vec3(glass);
        }
        #endif
        """;
}
