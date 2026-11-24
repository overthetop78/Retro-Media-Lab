using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VectorRasterVideoShader
{
    internal static (Vector4 Phosphor, Vector4 Beam) Parameters(EmulationVideoProcessingConfiguration config,
        int width, int height, bool hasHistory, double elapsedMilliseconds)
    {
        if (config.DisplayTechnology != EmulationVideoDisplayTechnology.Vector) return default;
        var settings = config.Vector;
        var phosphor = FilterVectorDisplay.Profile(settings.PhosphorColor);
        var scale = FilterVectorDisplay.BeamScale(settings.BeamWidth, phosphor);
        var radius = MathF.Ceiling(FilterVectorDisplay.BeamRadius(scale, width, height) * 2f);
        var decay = hasHistory && elapsedMilliseconds > 0
            ? MathF.Pow(.5f, (float)elapsedMilliseconds / phosphor.HalfLifeMs) : 0f;
        return (new(phosphor.Tint, phosphor.Diffusion),
            new(radius, scale, CrtSignal.CurvatureAmount(settings.Curvature), decay));
    }

    internal static string Create(bool veldrid)
    {
        var parameters = veldrid ? "Parameters." : "";
        var source = veldrid ? "texture(sampler2D(Source, LinearSampler), uv)" : "texture2D(Source, uv)";
        var history = veldrid ? "texture(sampler2D(History, LinearSampler), uv)" : "texture2D(History, uv)";
        return $$"""
            vec3 vectorSample(vec2 uv, bool history)
            {
                vec2 edge = .5 / {{parameters}}Processing.zw;
                uv = clamp(uv, edge, 1.0 - edge);
                return (history ? {{history}} : {{source}}).rgb;
            }

            vec3 vectorEmission(vec2 uv, bool history)
            {
                const vec3 luma = vec3(.2126, .7152, .0722);
                float scale = {{parameters}}VectorBeam.y;
                float gain = {{parameters}}Adjustments.x;
                vec2 d = {{parameters}}VectorBeam.x / {{parameters}}Output.xy;
                vec3 center = vectorSample(uv, history);
                vec3 neighbors = (vectorSample(uv - vec2(d.x, 0), history)
                    + vectorSample(uv + vec2(d.x, 0), history)
                    + vectorSample(uv - vec2(0, d.y), history)
                    + vectorSample(uv + vec2(0, d.y), history)) * .25;
                float thinness = max(0.0, 1.0 - scale);
                float attenuation = 1.0 - thinness * (1.0 - dot(center, luma));
                float light = clamp(dot(neighbors, luma) * gain, 0.0, 1.0);
                float diffusion = {{parameters}}VectorPhosphor.w * sqrt(scale) * light;
                vec3 color = mix(center, neighbors, min(.72, .18 * scale)) * attenuation + neighbors * diffusion;
                vec3 tint = {{parameters}}VectorPhosphor.xyz;
                if (any(greaterThan(tint, vec3(0.0)))) color = dot(color, luma) * tint;
                return color * gain;
            }

            vec3 vectorAdjustedPixel(vec2 uv)
            {
                vec3 color = vectorEmission(uv, false);
                float decay = {{parameters}}VectorBeam.w;
                if (decay > .001) color += vectorEmission(uv, true) * decay;
                return adjustLinear(color);
            }

            vec3 vectorPixel(vec2 uv)
            {
                vec3 color = vectorAdjustedPixel(uv);
                float sharpness = {{parameters}}Processing.x;
                if (abs(sharpness) <= .0001) return color;

                vec2 d = 1.0 / {{parameters}}Output.xy;
                vec3 sum = color;
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                        if (x != 0 || y != 0) sum += vectorAdjustedPixel(uv + vec2(x, y) * d);
                float weight = 9.0;
                if (sharpness < 0.0)
                {
                    vec2 dx = vec2(d.x * 2.0, 0), dy = vec2(0, d.y * 2.0);
                    sum += (vectorAdjustedPixel(uv - dx) + vectorAdjustedPixel(uv + dx)
                        + vectorAdjustedPixel(uv - dy) + vectorAdjustedPixel(uv + dy)) * .5;
                    weight = 11.0;
                }
                return videoSharpnessParameter(color, sum / weight, sharpness);
            }
            """;
    }
}
