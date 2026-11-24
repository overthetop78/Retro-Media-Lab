namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterDotMatrix
{
    internal static string CreateShader(bool veldrid)
    {
        var prefix = veldrid ? "Parameters." : "";
        var sample = veldrid ? "adjust(sourceColor(uv))" : "adjustColor(pointSample(uv).rgb)";
        return $$"""
            vec3 dotMatrixSource(vec2 uv) { return {{sample}}; }

            vec3 dotMatrixPixel(vec2 uv)
            {
                vec4 grid = {{prefix}}DotMatrixGeometry;
                vec4 optics = {{prefix}}DotMatrixOptics;
                vec2 cells = grid.xy;
                vec2 position = clamp(uv * cells, vec2(0.0), cells - .0001);
                vec2 center = (floor(position) + .5) / cells;
                vec2 offset = .25 * step(vec2(.5), {{prefix}}Processing.zw - cells) / cells;
                vec3 source = dotMatrixSource(center);
                if (any(greaterThan(offset, vec2(0.0))))
                    source = (source * 4.0 + dotMatrixSource(center - offset)
                        + dotMatrixSource(center + offset)
                        + dotMatrixSource(center + vec2(offset.x, -offset.y))
                        + dotMatrixSource(center + vec2(-offset.x, offset.y))) * .125;

                bool rgb = grid.z > .5;
                float level = rgb ? max(source.r, max(source.g, source.b))
                    : dot(source, vec3(.2126, .7152, .0722));
                vec3 tint = rgb ? source / max(level, .0001) : optics.rgb;

                vec2 local = abs(fract(position) - .5);
                float distance = grid.w < .5 ? length(local) - .40
                    : max(local.x - (grid.w < 1.5 ? .40 : .30), local.y - .40);
                float scale = min({{prefix}}Output.x / cells.x, {{prefix}}Output.y / cells.y);
                float edge = min(.5 / max(scale, 1.0), .20);
                float openArea = grid.w < .5 ? .503 : (grid.w < 1.5 ? .64 : .48);
                float coverage = mix(openArea, 1.0 - smoothstep(-edge, edge, distance),
                    smoothstep(1.0, 2.5, scale));

                if (optics.w > 1.5)
                    return mix(vec3(.004), vec3(.009) + .97 * tint * level, coverage);

                bool backlit = optics.w > .5;
                vec3 background = backlit ? vec3(.70, .72, .69) : vec3(.462, .533, .258);
                vec3 inactive = background * (backlit ? .94 : .96);
                float transmission = backlit ? .085 : .042;
                if (rgb)
                {
                    // A chromatic filter transmits its selected channels while absorbing the others.
                    // Neutral points retain the original dark state of the reflective or backlit panel.
                    float chroma = (level - min(source.r, min(source.g, source.b))) / max(level, .0001);
                    transmission = mix(transmission, backlit ? .60 : .42, chroma);
                }
                vec3 activatedDot = vec3(.014) + tint * transmission;
                return mix(background, mix(inactive, activatedDot, level), coverage);
            }
            """;
    }
}
