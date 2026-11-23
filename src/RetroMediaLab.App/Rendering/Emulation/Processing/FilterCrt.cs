using System.Buffers;
using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterCrt
{
    internal static void Apply(float[] colors, int width, int height, int sourceWidth, int sourceHeight,
        CrtSignal signal, float beamGain)
    {
        var source = ArrayPool<float>.Shared.Rent(colors.Length);
        try
        {
            colors.CopyTo(source, 0);
            var decoder = (int)signal.Tuning.Y;
            var neighborOffset = decoder == 1 ? new Vector2(1f / sourceWidth, 0) : new Vector2(0, 1f / sourceHeight);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var uv = signal.SourceUv(new Vector2((x + .5f) / width, (y + .5f) / height));
                var index = (y * width + x) * 3;
                if (uv.X < 0 || uv.X > 1 || uv.Y < 0 || uv.Y > 1)
                {
                    colors.AsSpan(index, 3).Clear();
                    continue;
                }

                var color = SampleColor(source, width, height, uv);
                var neighbor = decoder == 0 ? color : SampleColor(source, width, height, uv - neighborOffset);
                color = CrtSignal.Decode(color, neighbor, decoder, (int)(uv.Y * sourceHeight));
                color = Vector3.Min(Vector3.One, color * (beamGain * signal.Beam((y + .5f) / height, height)));
                colors[index] = color.X;
                colors[index + 1] = color.Y;
                colors[index + 2] = color.Z;
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(source);
        }
    }

    private static Vector3 SampleColor(float[] source, int width, int height, Vector2 uv)
    {
        var x = uv.X * width - .5f;
        var y = uv.Y * height - .5f;
        var left = (int)MathF.Floor(x);
        var top = (int)MathF.Floor(y);
        var a = Vector3.Lerp(Pixel(source, width, height, left, top), Pixel(source, width, height, left + 1, top), x - left);
        var b = Vector3.Lerp(Pixel(source, width, height, left, top + 1), Pixel(source, width, height, left + 1, top + 1), x - left);
        return Vector3.Lerp(a, b, y - top);
    }

    private static Vector3 Pixel(float[] colors, int width, int height, int x, int y)
    {
        var i = (Math.Clamp(y, 0, height - 1) * width + Math.Clamp(x, 0, width - 1)) * 3;
        return new Vector3(colors[i], colors[i + 1], colors[i + 2]);
    }
}
