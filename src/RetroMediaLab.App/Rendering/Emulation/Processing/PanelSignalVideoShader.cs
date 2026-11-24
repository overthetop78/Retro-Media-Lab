namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class PanelSignalVideoShader
{
    internal static CrtSignal? ForPresentation(EmulationVideoProcessingConfiguration config, VideoFrame frame)
    {
        var supported = EmulationVideoProcessingConfigurationFunctions.ScreenType(config) switch
        {
            EmulationVideoScreenType.Lcd => true,
            EmulationVideoScreenType.LedBacklitLcd =>
                config.Led.BacklightType is EmulationLedBacklightType.Edge or EmulationLedBacklightType.Direct,
            EmulationVideoScreenType.Projection =>
                config.Projection.Technology != EmulationProjectionTechnology.SingleChipDlp,
            _ => false
        };
        if (!supported) return null;

        var signal = CrtSignal.ForLcd(EmulationVideoProcessingConfigurationFunctions.PanelSignal(config), frame);
        // Unlocked sync needs to decode each temporal sample with its own raster phase.
        return signal.Tuning.X == 0f ? signal : null;
    }

    internal static string Create(bool veldrid)
    {
        var prefix = veldrid ? "Parameters." : "";
        var source = veldrid ? "texture(sampler2D(Source, PointSampler), uv)" : "texture2D(Source, uv)";
        var history = veldrid ? "texture(sampler2D(History, PointSampler), uv)" : "texture2D(History, uv)";
        return $$"""
            vec3 panelRawColor(vec2 uv, bool history)
            {
                return (history ? {{history}} : {{source}}).rgb;
            }

            vec3 panelDecodedColor(vec2 uv, bool history)
            {
                vec2 size = {{prefix}}Processing.zw;
                vec2 pixel = clamp(floor(uv * size), vec2(0.0), size - 1.0);
                vec2 centerUv = (pixel + .5) / size;
                vec3 color = panelRawColor(centerUv, history);
                int decoder = int({{prefix}}CrtTuning.y);
                vec2 neighborUv = max(crtChromaNeighbor(centerUv, size, decoder), .5 / size);
                vec3 neighbor = decoder == 0 ? color : panelRawColor(neighborUv, history);
                vec3 decoded = crtDecode(color, neighbor, decoder, pixel.y);
                return floor(decoded * 255.0 + .5) / 255.0;
            }
            """;
    }
}
