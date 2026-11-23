using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct CrtSignal(Vector4 Raster, Vector4 Tuning, Vector4 Phosphor,
    float BeamExponent, float Curvature)
{
    internal int ActiveLines => (int)(Raster.Y * 2);
    internal Vector2 MaskRasterSize => new(Tuning.W, ActiveLines);

    internal static CrtSignal From(EmulationCrtVideoConfiguration crt, VideoFrame frame,
        EmulationVideoAspectRatio aspectRatio = EmulationVideoAspectRatio.Original)
    {
        var rate = frame.SignalFieldRate;
        var sourceStandard = frame.SignalStandard ?? (rate is > 0 and < 55
            ? EmulationVideoStandard.Pal : EmulationVideoStandard.Ntsc);
        var source50 = rate > 0 ? rate < 55 : sourceStandard != EmulationVideoStandard.Ntsc;
        if (!(rate > 0) || !double.IsFinite(rate)) rate = source50 ? 50 : 60;
        var receiver50 = crt.FieldRate switch
        {
            EmulationCrtFieldRate.Hz50 => true,
            EmulationCrtFieldRate.Hz60 => false,
            _ => source50
        };
        var color = sourceStandard switch
        {
            EmulationVideoStandard.Pal => EmulationCrtColorStandards.Pal,
            EmulationVideoStandard.Secam => EmulationCrtColorStandards.Secam,
            _ => EmulationCrtColorStandards.Ntsc
        };
        var activeLines = receiver50 ? 576 : 480;
        var totalLines = receiver50 ? 625 : 525;
        var sourceLines = frame.SignalFieldHeight > 0 ? frame.SignalFieldHeight : frame.Height;
        // Low-resolution progressive video repeats one field raster. A full-height
        // progressive input is mapped against the complete active raster instead.
        var activeRows = sourceLines <= 312 ? activeLines / 2f : activeLines;
        var unlocked = source50 != receiver50;
        var receiverRate = receiver50 ? 50 : 60;
        var ratio = unlocked ? rate / receiverRate : 0;
        var elapsed = frame.Sequence / rate;
        var drift = unlocked ? elapsed * (rate - receiverRate) : 0;
        var phase = (float)(drift - Math.Floor(drift));
        var sourceTotalLines = source50 ? 625 : 525;
        var lineSlip = unlocked ? (float)((sourceTotalLines * rate) / (totalLines * receiverRate) - 1) : 0;
        var decoder = (crt.ColorStandards & color) == color ? (int)sourceStandard + 1 : 0;
        var (aspectWidth, aspectHeight) = EmulationVideoProcessingConfigurationFunctions.AspectRatioDimensions(aspectRatio);
        var screenAspect = aspectHeight > 0 ? (float)aspectWidth / aspectHeight
            : frame.AspectRatio > 0 && float.IsFinite(frame.AspectRatio) ? frame.AspectRatio
            : (float)Math.Max(1, frame.Width) / Math.Max(1, frame.ImageHeight);
        return new CrtSignal(new(activeRows / Math.Max(1, sourceLines), activeLines / 2f,
            activeLines / (float)totalLines, phase),
            new((float)ratio, decoder, lineSlip, activeLines * screenAspect), CrtPhosphorMask.Parameters(crt),
            MathF.Pow(2f, crt.BeamWidth / 10f),
            .16f * Math.Clamp(crt.Curvature, 0, EmulationVideoProcessingLimits.CrtCurvatureMaximum)
                / EmulationVideoProcessingLimits.CrtCurvatureMaximum);
    }

    internal Vector2 ScreenUv(Vector2 uv)
    {
        if (Curvature == 0f) return uv;
        var offset = uv - new Vector2(.5f);
        return uv + offset * (4f * Curvature * offset.LengthSquared());
    }

    internal Vector2 ScreenFootprint(Vector2 uv, Vector2 pixelSize)
    {
        if (Curvature == 0f) return pixelSize;
        var offset = uv - new Vector2(.5f);
        var radialScale = 1f + 4f * Curvature * offset.LengthSquared();
        var diagonal = new Vector2(radialScale) + 8f * Curvature * offset * offset;
        var cross = 8f * Curvature * MathF.Abs(offset.X * offset.Y);
        return diagonal * pixelSize + cross * new Vector2(pixelSize.Y, pixelSize.X);
    }

    internal static bool IsOnScreen(Vector2 uv) => uv.X is >= 0f and <= 1f && uv.Y is >= 0f and <= 1f;

    internal Vector2 SourceUv(Vector2 uv)
    {
        if (Tuning.X > 0)
        {
            var receiverTime = .5f + (uv.Y - .5f) * Raster.Z;
            var sourceTime = Fraction((receiverTime - .5f) * Tuning.X + .5f + Raster.W);
            var sourceAperture = Tuning.X > 1 ? 480f / 525f : 576f / 625f;
            if (MathF.Abs(sourceTime - .5f) > sourceAperture * .5f) return new Vector2(-1f);
            var sourcePhase = (sourceTime - .5f) / sourceAperture;
            var linesFromRetrace = sourceTime * Raster.Y / Raster.Z;
            uv.X -= .20f * MathF.Sin(MathF.Tau * (linesFromRetrace * Tuning.Z + Raster.W));
            uv.Y = sourcePhase * Raster.X + .5f;
            return uv;
        }
        return uv;
    }

    internal static Vector3 Decode(Vector3 center, Vector3 neighbor, int decoder, int line)
    {
        var luminance = Vector3.Dot(center, LumaWeights);
        if (decoder == 0) return new Vector3(luminance);
        var neighborLuma = Vector3.Dot(neighbor, LumaWeights);
        var chroma = center - new Vector3(luminance);
        var neighborChroma = neighbor - new Vector3(neighborLuma);
        switch (decoder)
        {
            case 1: // NTSC uses horizontal chroma bandwidth limiting.
                chroma = Vector3.Lerp(chroma, neighborChroma, .45f);
                break;
            case 2: // PAL delay-line decoder combines adjacent line chroma.
                chroma = Vector3.Lerp(chroma, neighborChroma, .25f);
                break;
            case 3: // SECAM transmits alternating red and blue difference signals.
                if ((line & 1) == 0) chroma.Z = neighborChroma.Z;
                else chroma.X = neighborChroma.X;
                chroma.Y = -(LumaWeights.X * chroma.X + LumaWeights.Z * chroma.Z) / LumaWeights.Y;
                break;
        }
        return Vector3.Clamp(new Vector3(luminance) + chroma, Vector3.Zero, Vector3.One);
    }

    private static readonly Vector3 LumaWeights = new(.2126f, .7152f, .0722f);

    internal float Beam(float y, int outputHeight)
    {
        if ((EmulationCrtMask)(int)Phosphor.W == EmulationCrtMask.ApertureGrille) return 1f;
        var strength = .18f * Math.Clamp(outputHeight / Raster.Y - 1, 0, 1);
        var wave = .5f + .5f * MathF.Cos(MathF.Tau * (y * Raster.Y - .5f));
        return 1 - strength * (1 - MathF.Pow(Math.Clamp(wave, 0f, 1f), BeamExponent));
    }

    private static float Fraction(float value) => value - MathF.Floor(value);

    internal const string Shader = """
        vec2 crtScreenUv(vec2 uv, float curvature)
        {
            vec2 offset = uv - vec2(.5);
            return uv + offset * (4.0 * curvature * dot(offset, offset));
        }
        vec2 crtScreenFootprint(vec2 uv, vec2 pixelSize, float curvature)
        {
            vec2 offset = uv - vec2(.5);
            float radialScale = 1.0 + 4.0 * curvature * dot(offset, offset);
            vec2 diagonal = vec2(radialScale) + 8.0 * curvature * offset * offset;
            float crossTerm = 8.0 * curvature * abs(offset.x * offset.y);
            return diagonal * pixelSize + crossTerm * pixelSize.yx;
        }
        bool crtOnScreen(vec2 uv)
        {
            return all(greaterThanEqual(uv, vec2(0.0))) && all(lessThanEqual(uv, vec2(1.0)));
        }
        vec2 crtSignalUv(vec2 uv, vec4 raster, vec4 tuning)
        {
            if (tuning.x > 0.0)
            {
                float receiverTime = .5 + (uv.y - .5) * raster.z;
                float sourceTime = fract((receiverTime - .5) * tuning.x + .5 + raster.w);
                float sourceAperture = tuning.x > 1.0 ? 480.0 / 525.0 : 576.0 / 625.0;
                if (abs(sourceTime - .5) > sourceAperture * .5) return vec2(-1.0);
                float sourcePhase = (sourceTime - .5) / sourceAperture;
                float linesFromRetrace = sourceTime * raster.y / raster.z;
                uv.x -= .20 * sin(6.2831853 * (linesFromRetrace * tuning.z + raster.w));
                uv.y = sourcePhase * raster.x + .5;
                return uv;
            }
            return uv;
        }
        vec2 crtChromaNeighbor(vec2 uv, vec2 sourceSize, int decoder)
        {
            return decoder == 1 ? uv - vec2(1.0 / sourceSize.x, 0.0)
                : uv - vec2(0.0, 1.0 / sourceSize.y);
        }
        vec3 crtDecode(vec3 center, vec3 neighbor, int decoder, float line)
        {
            const vec3 weights = vec3(.2126,.7152,.0722);
            float luma = dot(center, weights);
            if (decoder == 0) return vec3(luma);
            vec3 chroma = center - vec3(luma);
            vec3 neighborChroma = neighbor - vec3(dot(neighbor, weights));
            if (decoder == 1) chroma = mix(chroma, neighborChroma, .45);
            else if (decoder == 2) chroma = mix(chroma, neighborChroma, .25);
            else
            {
                if (mod(line, 2.0) < .5) chroma.b = neighborChroma.b;
                else chroma.r = neighborChroma.r;
                chroma.g = -(weights.r * chroma.r + weights.b * chroma.b) / weights.g;
            }
            return clamp(vec3(luma) + chroma, 0.0, 1.0);
        }
        float crtBeam(float y, float height, vec4 raster, float exponent, float mask)
        {
            if (int(mask + .5) == 1) return 1.0;
            float strength = .18 * clamp(height / raster.y - 1.0, 0.0, 1.0);
            float wave = .5 + .5 * cos(6.2831853 * (y * raster.y - .5));
            return 1.0 - strength * (1.0 - pow(clamp(wave, 0.0, 1.0), exponent));
        }
        """;
}
