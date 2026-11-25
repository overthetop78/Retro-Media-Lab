namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterVfdCellStructure
{
    internal static string CreateShader(bool veldrid)
    {
        var prefix = veldrid ? "Parameters." : "";
        var sample = veldrid ? "adjust(sourceColor(uv))" : "adjustColor(pointSample(uv).rgb)";
        return $$"""
            float vfdLuminance(vec2 uv)
            {
                return dot({{sample}}, vec3(.2126, .7152, .0722));
            }

            vec3 vfdPixel(vec2 uv)
            {
                vec4 panel = {{prefix}}VfdStructure;
                vec2 cells = panel.xy;
                vec2 position = clamp(uv * cells, vec2(0.0), cells - .0001);
                vec2 center = (floor(position) + .5) / cells;
                vec2 offset = .25 * step(vec2(.5), {{prefix}}Processing.zw - cells) / cells;
                float level = vfdLuminance(center);
                if (any(greaterThan(offset, vec2(0.0))))
                    level = (level * 4.0 + vfdLuminance(center - offset) + vfdLuminance(center + offset)
                        + vfdLuminance(center + vec2(offset.x, -offset.y))
                        + vfdLuminance(center + vec2(-offset.x, offset.y))) * .125;

                vec2 local = abs(fract(position) - .5);
                float shape = panel.z;
                float distance = shape < .5 ? length(local) - .38
                    : max(local.x - (shape < 1.5 ? .38 : .28), local.y - .38);
                float scale = min({{prefix}}Output.x / cells.x, {{prefix}}Output.y / cells.y);
                float edge = clamp(.5 / max(scale, 1.0), .025, .20);
                float core = 1.0 - smoothstep(-edge, edge, distance);
                float meanArea = shape < .5 ? .454 : (shape < 1.5 ? .578 : .426);
                float detail = smoothstep(1.0, 2.5, scale);
                float coverage = mix(meanArea, core, detail);
                float glowRadius = mix(.035, .115, level);
                float glow = (1.0 - smoothstep(0.0, glowRadius, max(distance, 0.0)))
                    * (1.0 - core) * (.24 * level * level) * detail;

                vec3 phosphor = {{prefix}}VfdDisplay.rgb;
                vec3 glass = vec3(.002, .0045, .0055);
                vec3 darkCell = vec3(.0018) + phosphor * .004;
                return min(glass + darkCell * coverage + phosphor * (.94 * level * coverage + glow), vec3(1.0));
            }
            """;
    }
}
