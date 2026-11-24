namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterProjection
{
    internal static void Apply(float[] colors, int sourceWidth, int sourceHeight, int width, int height,
        EmulationProjectionVideoConfiguration configuration)
    {
        var (panelWidth, panelHeight) = EmulationVideoProcessingCatalog.LcdDefinitionDimensions(
            configuration.Definition, sourceWidth, sourceHeight);
        var (optics, screen) = ProjectionVideoParameters.From(configuration);
        var source = optics.Y > 0f ? colors.ToArray() : colors;
        var stepX = Math.Max(1f, .55f * width / panelWidth);
        var stepY = Math.Max(1f, .55f * height / panelHeight);
        var convergence = optics.W * width / sourceWidth;
        var columns = new float[width];
        for (var x = 0; x < width; x++)
            columns[x] = Aperture(x, panelWidth, width, optics.Z);

        Parallel.For(0, height, y =>
        {
            var row = Aperture(y, panelHeight, height, optics.Z);
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 3;
                var area = columns[x] * row;
                var reflection = screen.X;
                if (screen.Z > 0f)
                {
                    var px = x + .5f;
                    var py = y + .5f;
                    reflection *= 1f - screen.Z * (.5f + .25f * MathF.Sin(px * .71f + py * .31f)
                        + .25f * MathF.Sin(px * .17f - py * .83f));
                }

                for (var channel = 0; channel < 3; channel++)
                {
                    var value = source[index + channel];
                    if (optics.Y > 0f)
                    {
                        var beamX = x + (channel - 1) * convergence;
                        var center = Sample(source, width, height, beamX, y, channel);
                        var surround = .25f * (Sample(source, width, height, beamX - stepX, y, channel)
                            + Sample(source, width, height, beamX + stepX, y, channel)
                            + Sample(source, width, height, beamX, y - stepY, channel)
                            + Sample(source, width, height, beamX, y + stepY, channel));
                        value = float.Lerp(center, surround, optics.Y);
                    }
                    var projection = float.Lerp(optics.X, 1f, value * area);
                    colors[index + channel] = float.Lerp(projection, 1f, screen.Y) * reflection;
                }
            }
        });
    }

    private static float Aperture(int position, int cells, int pixels, float border)
    {
        if (border <= 0f || pixels <= cells) return 1f;
        var span = cells / (float)pixels;
        var center = (position + .5f) * span;
        center -= MathF.Floor(center);
        var coverage = FilterLcdPixelStructure.Coverage(center, span, border, 1f - border);
        return float.Lerp(1f, coverage, FilterSampling.SmoothStep(1f, 2.5f, pixels / (float)cells));
    }

    private static float Sample(float[] source, int width, int height, float x, float y, int channel)
    {
        x = Math.Clamp(x, 0f, width - 1f);
        y = Math.Clamp(y, 0f, height - 1f);
        var left = (int)x;
        var top = (int)y;
        var right = Math.Min(width - 1, left + 1);
        var bottom = Math.Min(height - 1, top + 1);
        var topColor = float.Lerp(source[(top * width + left) * 3 + channel], source[(top * width + right) * 3 + channel], x - left);
        var bottomColor = float.Lerp(source[(bottom * width + left) * 3 + channel],
            source[(bottom * width + right) * 3 + channel], x - left);
        return float.Lerp(topColor, bottomColor, y - top);
    }
}
