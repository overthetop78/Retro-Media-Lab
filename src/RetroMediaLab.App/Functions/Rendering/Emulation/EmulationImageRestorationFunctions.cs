using System.Numerics;
using System.Runtime.InteropServices;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Functions.Rendering.Emulation;

internal static class EmulationImageRestorationFunctions
{
    private const int PatternColorTolerance = 3;
    private const int PatternMinimumContrast = 10;
    private static readonly float[] LinearByteValues = Enumerable.Range(0, 256)
        .Select(value => SoftwareEmulationVideoProcessingPipeline.SrgbToLinear(value / 255f)).ToArray();

    // Checkerboard, alternating columns and rows, then the two phases of paired columns and rows.
    private static readonly uint[] PatternMasks = Enumerable.Range(0, 7).Select(CreatePatternMask).ToArray();

    internal static byte[]? ApplyDedithering(ReadOnlySpan<byte> pixels, int width, int height, int intensity,
        ref byte[]? workspace)
    {
        if (intensity <= 0 || width < 5 || height < 5) return null;

        var source = MemoryMarshal.Cast<byte, uint>(pixels);
        byte[]? output = null;
        var weight = Math.Clamp(intensity, 0, 100) * .005f;
        for (var y = 2; y < height - 2; y++)
        for (var x = 2; x < width - 2; x++)
        {
            var index = y * width + x;
            var center = source[index];
            var left = source[index - 1];
            var right = source[index + 1];
            var up = source[index - width];
            var down = source[index + width];
            if ((((center ^ left) | (center ^ right) | (center ^ up) | (center ^ down)) & 0x00ffffffu) == 0)
                continue;
            var alternate = left;
            var pattern = -1;

            if (Similar(left, right))
            {
                if (Similar(left, up) && Similar(up, down))
                    pattern = 0;
                else if (Similar(center, up) && Similar(center, down))
                    pattern = 1;
            }

            if (pattern < 0 && Similar(center, left) && Similar(center, right) && Similar(up, down))
            {
                alternate = up;
                pattern = 2;
            }
            if (pattern < 0 && Similar(center, up) && Similar(center, down))
            {
                if (Similar(center, left) && !Similar(center, right))
                {
                    alternate = right;
                    pattern = 3;
                }
                else if (Similar(center, right) && !Similar(center, left))
                {
                    alternate = left;
                    pattern = 4;
                }
            }
            if (pattern < 0 && Similar(center, left) && Similar(center, right))
            {
                if (Similar(center, up) && !Similar(center, down))
                {
                    alternate = down;
                    pattern = 5;
                }
                else if (Similar(center, down) && !Similar(center, up))
                {
                    alternate = up;
                    pattern = 6;
                }
            }
            if (pattern < 0 || ColorDistance(center, alternate) < PatternMinimumContrast
                || !MatchesPattern(source, index, width, center, alternate, PatternMasks[pattern])) continue;

            if (output is null)
            {
                if (workspace is null || workspace.Length < pixels.Length)
                    workspace = new byte[pixels.Length];
                pixels.CopyTo(workspace);
                output = workspace;
            }

            var byteOffset = index * 4;
            for (var channel = 0; channel < 3; channel++)
            {
                var shift = channel * 8;
                var original = LinearByteValues[(byte)(center >> shift)];
                var other = LinearByteValues[(byte)(alternate >> shift)];
                var mixed = SoftwareEmulationVideoProcessingPipeline.LinearToSrgb(original + (other - original) * weight);
                output[byteOffset + channel] = (byte)MathF.Round(mixed * 255f);
            }
        }
        return output;
    }

    private static uint CreatePatternMask(int pattern)
    {
        uint mask = 0;
        for (var y = -2; y <= 2; y++)
        for (var x = -2; x <= 2; x++)
        {
            var alternate = pattern switch
            {
                0 => ((x + y) & 1) != 0,
                1 => (x & 1) != 0,
                2 => (y & 1) != 0,
                3 => ((x + 1) & 2) != 0,
                4 => (x & 2) != 0,
                5 => ((y + 1) & 2) != 0,
                _ => (y & 2) != 0
            };
            if (alternate) mask |= 1u << ((y + 2) * 5 + x + 2);
        }
        return mask;
    }

    private static bool MatchesPattern(ReadOnlySpan<uint> source, int index, int width,
        uint center, uint alternate, uint mask)
    {
        for (var y = -2; y <= 2; y++)
        {
            var start = index + y * width - 2;
            for (var x = 0; x < 5; x++, mask >>= 1)
                if (!Similar(source[start + x], (mask & 1) == 0 ? center : alternate))
                    return false;
        }
        return true;
    }

    private static bool Similar(uint first, uint second) => ColorDistance(first, second) <= PatternColorTolerance;

    private static int ColorDistance(uint first, uint second) => Math.Max(
        Math.Abs((byte)first - (byte)second), Math.Max(
        Math.Abs((byte)(first >> 8) - (byte)(second >> 8)),
        Math.Abs((byte)(first >> 16) - (byte)(second >> 16))));

    private static readonly int[] BandRadii = [2, 4, 8, 16, 32];
    private const float BandEdgeLimit = 0.075f;
    private const float FlatThreshold = 0.0015f;

    internal static void ApplyDebanding(float[] colors, int width, int height, int intensity)
    {
        if (intensity <= 0 || width < 5 || height < 5) return;
        var source = colors.ToArray();
        var strength = Math.Clamp(intensity / 100f, 0f, 1f);
        Parallel.For(1, height - 1, y =>
        {
            for (var x = 1; x < width - 1; x++)
            {
                var center = Read(source, width, x, y);
                if (MaxChannel(Read(source, width, x - 1, y) - center) > BandEdgeLimit
                    || MaxChannel(Read(source, width, x + 1, y) - center) > BandEdgeLimit
                    || MaxChannel(Read(source, width, x, y - 1) - center) > BandEdgeLimit
                    || MaxChannel(Read(source, width, x, y + 1) - center) > BandEdgeLimit)
                    continue;

                if (!TryBandCorrection(x, y, 1, 0, center, out var correction)
                    && !TryBandCorrection(x, y, 0, 1, center, out correction))
                    continue;

                // Spatially stable noise at less than one 8-bit output step.
                var noise = Fract(52.9829189f * Fract(0.06711056f * x + 0.00583715f * y)) - 0.5f;
                var quantizationStep = (0.08f + 2f * MathF.Sqrt(Math.Clamp(Vector3.Dot(center,
                    new Vector3(0.2126f, 0.7152f, 0.0722f)), 0f, 1f))) / 255f;
                Write(colors, width, x, y, Vector3.Clamp(center + (correction + new Vector3(noise * quantizationStep)) * strength,
                    Vector3.Zero, Vector3.One));
            }
        });

        bool TryBandCorrection(int x, int y, int axisX, int axisY, Vector3 center, out Vector3 correction)
        {
            correction = default;
            var available = axisX == 0 ? Math.Min(y, height - 1 - y) : Math.Min(x, width - 1 - x);
            foreach (var radius in BandRadii)
            {
                if (radius > available) break;
                var before = Sample(-radius) - center;
                var after = Sample(radius) - center;
                var beforeMagnitude = MaxChannel(before);
                var afterMagnitude = MaxChannel(after);
                var span = MathF.Max(beforeMagnitude, afterMagnitude);
                if (span > BandEdgeLimit) return false;
                if (MathF.Min(beforeMagnitude, afterMagnitude) < MathF.Max(FlatThreshold, span * .25f))
                    continue;
                if (Vector3.Dot(before, after) >= -.85f * before.Length() * after.Length()) return false;
                if (!IsBetween(Sample(-radius / 2) - center, before)
                    || !IsBetween(Sample(radius / 2) - center, after)) return false;

                var leftBoundary = FirstStep(-1, radius, before);
                var rightBoundary = FirstStep(1, radius, after);
                var position = (leftBoundary - .5f) / (leftBoundary + rightBoundary - 1f);
                correction = Vector3.Lerp(before, after, position) * .5f;
                return true;
            }
            return false;

            Vector3 Sample(int distance) => Read(source, width, x + axisX * distance, y + axisY * distance);

            int FirstStep(int sign, int far, Vector3 delta)
            {
                var near = 0;
                var threshold = Vector3.Dot(delta, delta) * .5f;
                for (var i = 0; i < 5 && far - near > 1; i++)
                {
                    var middle = (near + far) / 2;
                    if (Vector3.Dot(Sample(sign * middle) - center, delta) >= threshold)
                        far = middle;
                    else
                        near = middle;
                }
                return far;
            }
        }
    }

    internal static void ApplyDetailRecovery(float[] reference, int sourceWidth, int sourceHeight,
        float[] output, int width, int height, int intensity)
    {
        if (intensity <= 0 || ReferenceEquals(reference, output) || sourceWidth < 3 || sourceHeight < 3) return;
        var strength = Math.Clamp(intensity / 100f, 0f, 1f);
        Parallel.For(0, height, y =>
        {
            var sourceY = (y + .5f) / height * sourceHeight;
            var iy = (int)sourceY;
            if (iy <= 0 || iy >= sourceHeight - 1) return;
            for (var x = 0; x < width; x++)
            {
                var sourceX = (x + .5f) / width * sourceWidth;
                var ix = (int)sourceX;
                if (ix <= 0 || ix >= sourceWidth - 1) continue;
                var center = Read(reference, sourceWidth, ix, iy);
                var filtered = Read(output, width, x, y);
                var horizontal = Direction(1, 0, Math.Min(ix, sourceWidth - 1 - ix));
                var vertical = Direction(0, 1, Math.Min(iy, sourceHeight - 1 - iy));
                var lost = horizontal.LengthSquared() >= vertical.LengthSquared() ? horizontal : vertical;
                var distance = MathF.Max(MathF.Abs(sourceX - ix - .5f), MathF.Abs(sourceY - iy - .5f));
                var weight = Math.Clamp((.5f - distance) * 4f, 0f, 1f) * strength;
                Write(output, width, x, y, Vector3.Clamp(filtered + lost * weight, Vector3.Zero, Vector3.One));

                Vector3 Direction(int stepX, int stepY, int available)
                {
                    var before = Read(reference, sourceWidth, ix - stepX, iy - stepY);
                    var after = Read(reference, sourceWidth, ix + stepX, iy + stepY);
                    if (available >= 2)
                    {
                        if (MaxChannel(center - before) <= FlatThreshold && MaxChannel(center - after) >= .018f)
                            before = Read(reference, sourceWidth, ix - 2 * stepX, iy - 2 * stepY);
                        if (MaxChannel(center - after) <= FlatThreshold && MaxChannel(center - before) >= .018f)
                            after = Read(reference, sourceWidth, ix + 2 * stepX, iy + 2 * stepY);
                    }
                    return LostDetail(center, filtered, before, after);
                }
            }
        });
    }

    private static Vector3 LostDetail(Vector3 center, Vector3 filtered, Vector3 before, Vector3 after)
    {
        var detail = center - (before + after) * .5f;
        var amplitude = MaxChannel(detail);
        if (amplitude < .018f || MaxChannel(before - after) > amplitude * .45f) return Vector3.Zero;
        var energy = detail.LengthSquared();
        var missing = Math.Clamp(Vector3.Dot(center - filtered, detail) / energy, 0f, 1f);
        var bounded = Vector3.Clamp(filtered + detail * missing, Vector3.Min(center, filtered), Vector3.Max(center, filtered));
        return bounded - filtered;
    }

    private static bool IsBetween(Vector3 sample, Vector3 endpoint)
    {
        var fraction = Vector3.Dot(sample, endpoint) / endpoint.LengthSquared();
        return fraction >= -.08f && fraction <= 1.08f && MaxChannel(sample - endpoint * fraction) <= FlatThreshold * 2f;
    }

    private static float MaxChannel(Vector3 value)
    {
        value = Vector3.Abs(value);
        return MathF.Max(value.X, MathF.Max(value.Y, value.Z));
    }

    private static float Fract(float value) => value - MathF.Floor(value);

    private static Vector3 Read(float[] colors, int width, int x, int y)
    {
        var offset = (y * width + x) * 3;
        return new Vector3(colors[offset], colors[offset + 1], colors[offset + 2]);
    }

    private static void Write(float[] colors, int width, int x, int y, Vector3 color)
    {
        var offset = (y * width + x) * 3;
        colors[offset] = color.X;
        colors[offset + 1] = color.Y;
        colors[offset + 2] = color.Z;
    }
}
