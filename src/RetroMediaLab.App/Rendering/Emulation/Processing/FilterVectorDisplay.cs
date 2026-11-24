using System.Numerics;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed class FilterVectorDisplay
{
    internal readonly record struct Phosphor(Vector3 Tint, float HalfLifeMs, float Diffusion, float Focus);

    private static readonly Vector3 Luminance = new(.2126f, .7152f, .0722f);
    private Vector3[] _emission = [];
    private int _width;
    private int _height;
    private long _sequence;
    private TimeSpan _timestamp;
    private EmulationVectorVideoConfiguration? _previous;
    private int _brightness;
    private bool _native;

    internal static Phosphor Profile(EmulationCrtColorMode color) =>
        color switch
        {
            EmulationCrtColorMode.White => new Phosphor(Vector3.One, 12f, .12f, 1f),
            EmulationCrtColorMode.Green => new Phosphor(new(.17f, 1f, .12f), 23f, .15f, .91f),
            EmulationCrtColorMode.Amber => new Phosphor(new(1f, .46f, .06f), 33f, .18f, 1.08f),
            _ => new Phosphor(Vector3.Zero, 10f, .10f, 1f)
        };
    internal static float BeamScale(int width, Phosphor phosphor) =>
        phosphor.Focus * MathF.Pow(2f, width / 13f);

    internal static float BeamRadius(float scale, int width, int height) =>
        MathF.Max(.30f, .62f * Math.Min(width, height) / 720f * scale);

    internal void Render(float[] output, float[]? raster, VideoFrame frame, int width, int height,
        EmulationVectorVideoConfiguration configuration, int brightness)
    {
        var phosphor = Profile(configuration.PhosphorColor);
        var native = frame.VectorSegments.HasValue;
        var compatible = _previous is not null && _width == width && _height == height
            && _previous.PhosphorColor == configuration.PhosphorColor && _previous.BeamWidth == configuration.BeamWidth
            && _brightness == brightness && _native == native && frame.Sequence >= _sequence && frame.Timestamp >= _timestamp;
        var length = checked(width * height);
        if (_emission.Length != length) _emission = new Vector3[length];

        if (!compatible || frame.Sequence != _sequence || frame.Timestamp != _timestamp)
        {
            if (compatible)
            {
                var elapsed = (frame.Timestamp - _timestamp).TotalMilliseconds;
                if (elapsed == 0d)
                    elapsed = (frame.Sequence - _sequence) * (1000d / (frame.SignalFieldRate is > 0 and < 1000 ? frame.SignalFieldRate : 60d));
                var decay = MathF.Pow(.5f, (float)elapsed / phosphor.HalfLifeMs);
                for (var index = 0; index < length; index++) _emission[index] *= decay;
            }
            else Array.Clear(_emission);

            var beam = BeamScale(configuration.BeamWidth, phosphor);
            var sigma = BeamRadius(beam, width, height);
            var gain = VideoBrightnessParameterFunctions.CrtBeamGain(brightness);
            if (frame.VectorSegments is { } vectors)
            {
                foreach (var vector in vectors.Span)
                    Draw(vector, width, height, sigma, beam, gain, phosphor);
            }
            else if (raster is not null)
                ExciteRaster(raster, width, height, sigma, beam, gain, phosphor);

            _width = width;
            _height = height;
            _sequence = frame.Sequence;
            _timestamp = frame.Timestamp;
            _previous = configuration;
            _brightness = brightness;
            _native = native;
        }

        var curvature = CrtSignal.CurvatureAmount(configuration.Curvature);
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var color = curvature == 0f ? _emission[index] : CurvedPixel(x, y, width, height, curvature);
                color.CopyTo(output, index * 3);
            }
        });
    }

    internal void Reset()
    {
        _previous = null;
        _emission = [];
    }

    private void Draw(VideoVectorSegment vector, int width, int height, float sigma, float beam, float gain, Phosphor phosphor)
    {
        if (!Finite(vector.Start) || !Finite(vector.End) || !float.IsFinite(vector.Intensity) || vector.Intensity <= 0f
            || !float.IsFinite(vector.Color.X) || !float.IsFinite(vector.Color.Y) || !float.IsFinite(vector.Color.Z))
            return;
        var scale = new Vector2(width, height);
        var start = Vector2.Clamp(vector.Start, Vector2.Zero, Vector2.One) * scale - new Vector2(.5f);
        var end = Vector2.Clamp(vector.End, Vector2.Zero, Vector2.One) * scale - new Vector2(.5f);
        var delta = end - start;
        var lengthSquared = delta.LengthSquared();
        var color = Tint(Vector3.Clamp(vector.Color, Vector3.Zero, Vector3.One), phosphor)
            * (gain * Math.Min(vector.Intensity, 1f));
        var light = Math.Clamp(Vector3.Dot(color, Luminance), 0f, 1f);
        var halo = phosphor.Diffusion * MathF.Sqrt(beam) * light;
        var spread = sigma * (2f + light);
        var radius = (halo > .005f ? spread : sigma) * 3f;
        var minY = Math.Max(0, (int)MathF.Ceiling(MathF.Min(start.Y, end.Y) - radius));
        var maxY = Math.Min(height - 1, (int)MathF.Floor(MathF.Max(start.Y, end.Y) + radius));
        for (var y = minY; y <= maxY; y++)
        {
            // Limit each row to the beam's neighborhood, including for a long diagonal.
            var t0 = delta.Y == 0f ? 0f : Math.Clamp((y - radius - start.Y) / delta.Y, 0f, 1f);
            var t1 = delta.Y == 0f ? 1f : Math.Clamp((y + radius - start.Y) / delta.Y, 0f, 1f);
            var x0 = start.X + delta.X * t0;
            var x1 = start.X + delta.X * t1;
            var minX = Math.Max(0, (int)MathF.Ceiling(MathF.Min(x0, x1) - radius));
            var maxX = Math.Min(width - 1, (int)MathF.Floor(MathF.Max(x0, x1) + radius));
            for (var x = minX; x <= maxX; x++)
            {
                var offset = new Vector2(x, y) - start;
                var t = lengthSquared == 0f ? 0f : Math.Clamp(Vector2.Dot(offset, delta) / lengthSquared, 0f, 1f);
                var distance = (offset - t * delta).LengthSquared();
                var emission = MathF.Exp(-.5f * distance / (sigma * sigma))
                    + halo * MathF.Exp(-.5f * distance / (spread * spread));
                _emission[y * width + x] += color * emission;
            }
        }
    }

    private void ExciteRaster(float[] raster, int width, int height, float sigma, float beam, float gain, Phosphor phosphor)
    {
        var radius = Math.Clamp((int)MathF.Ceiling(sigma * 2f), 1, Math.Max(width, height));
        var spread = Math.Min(.72f, .18f * beam);
        var thinness = Math.Max(0f, 1f - beam);
        Parallel.For(0, height, y =>
        {
            var top = Math.Max(0, y - radius) * width;
            var bottom = Math.Min(height - 1, y + radius) * width;
            var row = y * width;
            for (var x = 0; x < width; x++)
            {
                var center = Read(raster, row + x);
                var neighbors = (Read(raster, row + Math.Max(0, x - radius)) +
                    Read(raster, row + Math.Min(width - 1, x + radius)) +
                    Read(raster, top + x) + Read(raster, bottom + x)) * .25f;
                var core = Vector3.Lerp(center, neighbors, spread);
                var edgeAttenuation = 1f - thinness * (1f - Vector3.Dot(center, Luminance));
                var light = Math.Clamp(Vector3.Dot(neighbors, Luminance) * gain, 0f, 1f);
                var diffusion = phosphor.Diffusion * MathF.Sqrt(beam) * light;
                _emission[row + x] += Tint(core * edgeAttenuation + neighbors * diffusion, phosphor) * gain;
            }
        });
    }

    private Vector3 CurvedPixel(int x, int y, int width, int height, float curvature)
    {
        var uv = CrtSignal.ScreenUv(new Vector2((x + .5f) / width, (y + .5f) / height), curvature);
        if (!CrtSignal.IsOnScreen(uv)) return Vector3.Zero;
        var px = Math.Clamp(uv.X * width - .5f, 0f, width - 1f);
        var py = Math.Clamp(uv.Y * height - .5f, 0f, height - 1f);
        var left = (int)px;
        var top = (int)py;
        var right = Math.Min(width - 1, left + 1);
        var bottom = Math.Min(height - 1, top + 1);
        return Vector3.Lerp(Vector3.Lerp(_emission[top * width + left], _emission[top * width + right], px - left),
            Vector3.Lerp(_emission[bottom * width + left], _emission[bottom * width + right], px - left), py - top);
    }

    private static Vector3 Tint(Vector3 color, Phosphor phosphor) =>
        phosphor.Tint == Vector3.Zero ? color : Vector3.Dot(color, Luminance) * phosphor.Tint;

    private static Vector3 Read(float[] pixels, int index) => new(pixels[index * 3], pixels[index * 3 + 1], pixels[index * 3 + 2]);

    private static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
}
