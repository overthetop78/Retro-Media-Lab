using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterLedMatrixCellStructure
{
    internal static float[] Render(float[] source, int width, int height,
        int outputWidth, int outputHeight, EmulationLedMatrixVideoConfiguration configuration)
    {
        var (columns, rows) = EmulationVideoProcessingCatalog.LedMatrixDefinitionDimensions(configuration.Definition, width, height);
        var cells = new Vector3[checked(columns * rows)];
        var offsetX = columns < width ? .25f : 0f;
        var offsetY = rows < height ? .25f : 0f;
        Parallel.For(0, rows, y =>
        {
            for (var x = 0; x < columns; x++)
            {
                var cx = x + .5f;
                var cy = y + .5f;
                var color = Sample(cx, cy);
                if (offsetX != 0f || offsetY != 0f)
                    color = (color * 4f + Sample(cx - offsetX, cy - offsetY) + Sample(cx + offsetX, cy - offsetY)
                        + Sample(cx - offsetX, cy + offsetY) + Sample(cx + offsetX, cy + offsetY)) * .125f;
                cells[y * columns + x] = FilterLedMatrixColor.Emission(color, configuration.Color);
            }
        });

        var output = new float[checked(outputWidth * outputHeight * 3)];
        var scale = new Vector2(outputWidth / (float)columns, outputHeight / (float)rows);
        var cellScale = MathF.Min(scale.X, scale.Y);
        var shapeScale = scale / cellScale;
        var detail = FilterSampling.SmoothStep(1f, 2.5f, cellScale);
        var edge = Math.Clamp(.5f / cellScale, .015f, .2f);
        Parallel.For(0, outputHeight, y =>
        {
            var matrixY = (y + .5f) / outputHeight * rows;
            var cellY = Math.Min(rows - 1, (int)matrixY);
            for (var x = 0; x < outputWidth; x++)
            {
                var matrixX = (x + .5f) / outputWidth * columns;
                var cellX = Math.Min(columns - 1, (int)matrixX);
                var light = cells[cellY * columns + cellX];
                var local = new Vector2(matrixX - cellX - .5f, matrixY - cellY - .5f) * shapeScale;
                var distance = Distance(local, configuration.Shape);
                Compose(light, distance, edge, detail).CopyTo(output, (y * outputWidth + x) * 3);
            }
        });
        return output;

        Vector3 Sample(float x, float y)
        {
            var sx = Math.Min(width - 1, (int)(x / columns * width));
            var sy = Math.Min(height - 1, (int)(y / rows * height));
            return new Vector3(source.AsSpan((sy * width + sx) * 3, 3));
        }
    }

    private static float Distance(Vector2 coordinate, EmulationLedMatrixShape shape)
    {
        if (shape == EmulationLedMatrixShape.Round) return coordinate.Length() - .36f;
        var corner = shape == EmulationLedMatrixShape.RoundedSquare ? .10f : 0f;
        var q = Vector2.Abs(coordinate) - new Vector2(.34f - corner);
        return Vector2.Max(q, Vector2.Zero).Length() + MathF.Min(MathF.Max(q.X, q.Y), 0f) - corner;
    }

    private static Vector3 Compose(Vector3 light, float distance, float edge, float detail)
    {
        var core = 1f - FilterSampling.SmoothStep(-edge, edge, distance);
        var energy = MathF.Max(light.X, MathF.Max(light.Y, light.Z));
        var halo = (1f - FilterSampling.SmoothStep(0f, .05f + .09f * energy, MathF.Max(distance, 0f)))
            * (1f - core) * (.25f * energy);
        var cell = Vector3.Clamp(new Vector3(.0045f + .010f * core) + light * (core + halo), Vector3.Zero, Vector3.One);
        return Vector3.Lerp(light, cell, detail);
    }

    internal static string CreateShader(bool veldrid)
    {
        var prefix = veldrid ? "Parameters." : "";
        var sample = veldrid ? "adjust(sourceColor(uv))" : "adjustColor(pointSample(uv).rgb)";
        return FilterLedMatrixColor.Shader + "\n" + $$"""
            vec3 ledMatrixSource(vec2 uv) { return {{sample}}; }

            float ledMatrixDistance(vec2 coordinate, float shape)
            {
                if (shape < .5) return length(coordinate) - .36;
                float corner = shape >= 1.5 ? .10 : 0.0;
                vec2 q = abs(coordinate) - vec2(.34 - corner);
                return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - corner;
            }

            vec3 ledMatrixPixel(vec2 uv)
            {
                vec4 panel = {{prefix}}LedMatrixStructure;
                vec2 cells = panel.xy;
                vec2 matrix = clamp(uv * cells, vec2(0.0), cells - .0001);
                vec2 index = floor(matrix);
                vec2 center = (index + .5) / cells;
                vec2 d = .25 * step(vec2(.5), {{prefix}}Processing.zw - cells) / cells;
                vec3 color = ledMatrixSource(center);
                if (any(greaterThan(d, vec2(0.0))))
                    color = (color * 4.0 + ledMatrixSource(center - d) + ledMatrixSource(center + d)
                        + ledMatrixSource(center + vec2(d.x, -d.y)) + ledMatrixSource(center + vec2(-d.x, d.y))) * .125;
                vec3 light = filterLedMatrixColor(color, panel.z);
                vec2 scale = {{prefix}}Output.xy / cells;
                float cellScale = min(scale.x, scale.y);
                float edge = clamp(.5 / cellScale, .015, .2);
                float distance = ledMatrixDistance((matrix - index - .5) * scale / cellScale, panel.w);
                float core = 1.0 - smoothstep(-edge, edge, distance);
                float energy = max(light.r, max(light.g, light.b));
                float halo = (1.0 - smoothstep(0.0, .05 + .09 * energy, max(distance, 0.0)))
                    * (1.0 - core) * (.25 * energy);
                vec3 cell = clamp(vec3(.0045 + .010 * core) + light * (core + halo), 0.0, 1.0);
                return mix(light, cell, smoothstep(1.0, 2.5, cellScale));
            }
            """;
    }
}
