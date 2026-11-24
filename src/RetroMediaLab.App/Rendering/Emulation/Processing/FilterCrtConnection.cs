using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterCrtConnection
{
    private static readonly Vector3 LumaWeights = new(.299f, .587f, .114f);
    private static readonly Vector3[] CgaPalette =
    [
        Rgb(0x000000), Rgb(0x0000AA), Rgb(0x00AA00), Rgb(0x00AAAA),
        Rgb(0xAA0000), Rgb(0xAA00AA), Rgb(0xAA5500), Rgb(0xAAAAAA),
        Rgb(0x555555), Rgb(0x5555FF), Rgb(0x55FF55), Rgb(0x55FFFF),
        Rgb(0xFF5555), Rgb(0xFF55FF), Rgb(0xFFFF55), Rgb(0xFFFFFF)
    ];

    internal static byte[]? Apply(ReadOnlySpan<byte> source, VideoFrame frame,
        EmulationVideoProcessingConfiguration configuration, ref byte[]? workspace)
    {
        var crt = configuration.Crt;
        return configuration.DisplayTechnology != EmulationVideoDisplayTechnology.Crt ? null
            : Apply(source, frame, crt.Connection, crt.ConnectionDegradation, crt.ColorStandards, crt.MonitorPalette, ref workspace);
    }

    internal static byte[]? Apply(ReadOnlySpan<byte> source, VideoFrame frame, EmulationCrtConnection connection,
        int connectionDegradation, EmulationCrtColorStandards colorStandards,
        EmulationCrtMonitorPalette palette, ref byte[]? workspace)
    {
        if (connection == EmulationCrtConnection.Digital) return null;

        var degradation = (float)connectionDegradation / EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum;
        if (degradation == 0f && connection is EmulationCrtConnection.Scart or EmulationCrtConnection.Component)
            return null;

        var stride = checked(frame.Width * 4);
        var length = checked(stride * frame.Height);
        if (workspace is null || workspace.Length < length) workspace = new byte[length];
        if (connection == EmulationCrtConnection.LegacyMonitor)
        {
            for (var offset = 0; offset < length; offset += 4)
                Write(workspace, offset, Quantize(Read(source, offset), palette));
        }
        else
        {
            EmulationVideoStandard? rfStandard = connection == EmulationCrtConnection.AnalogRf
                ? colorStandards switch
                {
                    EmulationCrtColorStandards.Pal => EmulationVideoStandard.Pal,
                    EmulationCrtColorStandards.Secam => EmulationVideoStandard.Secam,
                    EmulationCrtColorStandards.Ntsc => EmulationVideoStandard.Ntsc,
                    _ => CrtSignal.SourceStandard(frame)
                }
                : null;
            ApplyAnalog(source, workspace, frame.Width, frame.Height, frame.Sequence, connection, degradation, rfStandard);
        }
        return workspace;
    }

    private static void ApplyAnalog(ReadOnlySpan<byte> source, Span<byte> output, int width, int height, long sequence,
        EmulationCrtConnection connection, float degradation, EmulationVideoStandard? rfStandard)
    {
        var (lumaLoss, chromaLoss, chromaRadius, delay, interference) = connection switch
        {
            EmulationCrtConnection.Component => (.33f * degradation, .63f * degradation,
                1f + degradation, .55f * degradation, 0f),
            EmulationCrtConnection.SVideo => (.08f + .37f * degradation, .28f + .5f * degradation,
                1.15f + 1.5f * degradation, .12f + .48f * degradation, .008f + .027f * degradation),
            EmulationCrtConnection.Composite => (.19f + .55f * degradation, .58f + .38f * degradation,
                1.5f + 2f * degradation, .22f + .9f * degradation, .085f + .19f * degradation),
            EmulationCrtConnection.AnalogRf => (.29f + .62f * degradation, .64f + .34f * degradation,
                1.8f + 2.5f * degradation, .32f + degradation, rfStandard switch
                {
                    EmulationVideoStandard.Secam => .018f + .048f * degradation,
                    EmulationVideoStandard.Pal => .062f + .13f * degradation,
                    _ => .095f + .19f * degradation
                }),
            _ => (0f, 0f, 0f, 0f, 0f)
        };
        var stride = width * 4;
        var rf = rfStandard.HasValue;
        var palRf = rfStandard == EmulationVideoStandard.Pal;
        var secamRf = rfStandard == EmulationVideoStandard.Secam;
        var scart = connection == EmulationCrtConnection.Scart;
        var frame = unchecked((uint)sequence);
        var time = (frame & 4095) * .11f;
        for (var y = 0; y < height; y++)
        {
            var row = source.Slice(y * stride, stride);
            var lineNoise = rf ? Noise((uint)y, 0, frame) : 0f;
            var band = rf ? Noise((uint)y / 5, 1, frame / 3) : 0f;
            var burst = band > .72f ? (band - .72f) / .28f : 0f;
            var evenLine = (y & 1) == 0;
            // PAL reverses the phase error on adjacent lines. SECAM carries a
            // frequency-modulated color difference on each line and loses color in streaks.
            var hueError = !rf || secamRf ? 0f : palRf
                ? (.035f + .25f * degradation) * band * (evenLine ? 1f : -1f)
                : (.055f + .50f * degradation) * (.6f * MathF.Sin(time) + .4f * band);
            var (hueSin, hueCos) = MathF.SinCos(hueError);
            var secamLoss = secamRf ? Math.Clamp((MathF.Abs(lineNoise) - .55f) * (1f + 2f * degradation), 0f, 1f) : 0f;
            var offset = scart ? MathF.Sin(y * .048f + time) * .82f * degradation
                : rf ? MathF.Sin(y * .083f + time) * (.14f + 1.25f * degradation)
                    + lineNoise * .7f * degradation + burst * 2.4f * degradation
                : 0f;
            for (var x = 0; x < width; x++)
            {
                var position = x + offset;
                var center = Sample(row, width, position);
                var left = Sample(row, width, position - 1f);
                var right = Sample(row, width, position + 1f);
                Vector3 signal;
                if (scart)
                {
                    var tail = left * .58f + center * .24f + right * .18f;
                    signal = Vector3.Lerp(center, tail, .85f * degradation);
                }
                else
                {
                    var luma = Luma(center);
                    var averagedLuma = Luma(.5f * center + .25f * (left + right));
                    var outputLuma = luma + (averagedLuma - luma) * lumaLoss;
                    var chromaPosition = position - delay;
                    var originalChroma = Chroma(center);
                    var spread = Chroma(.5f * Sample(row, width, chromaPosition)
                        + .25f * (Sample(row, width, chromaPosition - chromaRadius)
                            + Sample(row, width, chromaPosition + chromaRadius)));
                    var outputChroma = Vector3.Lerp(originalChroma, spread, chromaLoss);
                    if (interference > 0f)
                    {
                        var phase = (x + y * 2 + (int)(frame & 3)) & 3;
                        var carrier = phase switch
                        {
                            0 => new Vector3(1f, -.509f, 0f),
                            1 => new Vector3(0f, -.194f, 1f),
                            2 => new Vector3(-1f, .509f, 0f),
                            _ => new Vector3(0f, .194f, -1f)
                        };
                        if (palRf && !evenLine && (phase & 1) == 0) carrier = -carrier;
                        outputChroma += carrier * ((luma - averagedLuma) * interference * 2.1f);
                        outputLuma += Vector3.Dot(Chroma(right - left), carrier) * interference * .48f;
                    }
                    if (connection == EmulationCrtConnection.SVideo)
                        outputChroma *= .96f - .27f * degradation;
                    if (rf)
                    {
                        var noise = Noise((uint)x, (uint)y, frame);
                        outputLuma += noise * (.015f + .34f * degradation)
                            + lineNoise * (.012f + .085f * degradation)
                            + burst * (.015f + .19f * degradation);
                        outputChroma *= .91f - .38f * degradation + lineNoise * .24f * degradation;
                        if (secamRf)
                        {
                            var disturbance = (lineNoise * .09f + noise * .045f) * degradation;
                            if (evenLine) outputChroma.X = outputChroma.X * (1f - secamLoss) + disturbance;
                            else outputChroma.Z = outputChroma.Z * (1f - secamLoss) + disturbance;
                        }
                        else
                        {
                            var red = outputChroma.X;
                            var blue = outputChroma.Z;
                            outputChroma.X = red * hueCos - blue * hueSin;
                            outputChroma.Z = red * hueSin + blue * hueCos;
                        }
                        outputChroma.Y = -(LumaWeights.X * outputChroma.X + LumaWeights.Z * outputChroma.Z) / LumaWeights.Y;
                    }
                    signal = new Vector3(outputLuma) + outputChroma;
                }
                Write(output, y * stride + x * 4, Vector3.Clamp(signal, Vector3.Zero, Vector3.One));
            }
        }
    }

    private static Vector3 Quantize(Vector3 color, EmulationCrtMonitorPalette palette)
    {
        if (palette == EmulationCrtMonitorPalette.Mda)
        {
            var luminance = Luma(color);
            return new Vector3(luminance < .27f ? 0f : luminance < .68f ? .57f : 1f);
        }
        if (palette == EmulationCrtMonitorPalette.Ega)
            return new Vector3(MathF.Round(color.X * 3f), MathF.Round(color.Y * 3f), MathF.Round(color.Z * 3f)) / 3f;

        var closest = Vector3.Zero;
        var best = float.MaxValue;
        foreach (var candidate in CgaPalette)
        {
            var distance = Vector3.DistanceSquared(color, candidate);
            if (distance >= best) continue;
            best = distance;
            closest = candidate;
        }
        return closest;
    }

    private static Vector3 Rgb(int value) => new Vector3((value >> 16) & 255, (value >> 8) & 255, value & 255) / 255f;
    private static float Luma(Vector3 color) => Vector3.Dot(color, LumaWeights);
    private static Vector3 Chroma(Vector3 color) => color - new Vector3(Luma(color));
    private static Vector3 Read(ReadOnlySpan<byte> pixels, int offset) =>
        new Vector3(pixels[offset + 2], pixels[offset + 1], pixels[offset]) * (1f / 255f);

    private static Vector3 Sample(ReadOnlySpan<byte> row, int width, float position)
    {
        var x = Math.Clamp(position, 0f, width - 1);
        var left = (int)x;
        return Vector3.Lerp(Read(row, left * 4), Read(row, Math.Min(left + 1, width - 1) * 4), x - left);
    }

    private static void Write(Span<byte> pixels, int offset, Vector3 color)
    {
        pixels[offset] = (byte)MathF.Round(color.Z * 255f);
        pixels[offset + 1] = (byte)MathF.Round(color.Y * 255f);
        pixels[offset + 2] = (byte)MathF.Round(color.X * 255f);
        pixels[offset + 3] = 255;
    }

    private static float Noise(uint x, uint y, uint frame)
    {
        var value = unchecked((x * 374761393u + y * 668265263u + frame * 1446648779u) ^ 2246822519u);
        value = unchecked((value ^ (value >> 13)) * 1274126177u);
        value ^= value >> 16;
        return (value & 65535u) * (2f / 65535f) - 1f;
    }
}
