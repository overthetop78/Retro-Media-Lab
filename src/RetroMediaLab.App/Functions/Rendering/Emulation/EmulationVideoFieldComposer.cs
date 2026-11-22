using System.Buffers.Binary;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Rendering.Emulation;

/// <summary>Suit tous les champs natifs avant la file de rendu, y compris quand une présentation est sautée.</summary>
internal sealed class EmulationVideoFieldComposer
{
    private VideoFrame? _latest;
    private VideoFrame? _previous;
    private VideoFrame? _sameParity;
    private VideoFrame? _previousComplement;
    private VideoFrame? _composed;
    private EmulationDeinterlacingMode _composedMode;
    private byte[] _output = [];
    private byte[] _blendSource = [];
    private float[] _motion = [];
    private long? _motionSequence;

    internal VideoFrame? Latest => _latest;

    internal void Reset()
    {
        _latest = _previous = _sameParity = _previousComplement = _composed = null;
        _motionSequence = null;
    }

    internal void Receive(VideoFrame frame, double fieldsPerSecond = 0)
    {
        if (ReferenceEquals(frame, _latest)) return;

        var maximumGap = fieldsPerSecond > 0 && double.IsFinite(fieldsPerSecond)
            ? Math.Clamp(1.75 / fieldsPerSecond, .001, .15)
            : .1;
        if (_latest is not { } previous || !IsConsecutive(previous, frame, maximumGap))
            Reset();

        _previousComplement = _sameParity;
        _sameParity = _previous;
        _previous = _latest;
        _latest = frame;
        _composed = null;
    }

    private static bool IsConsecutive(VideoFrame previous, VideoFrame current, double maximumGap) =>
        current.ScanMode != EmulationVideoScanMode.Progressive
        && previous.ScanMode != EmulationVideoScanMode.Progressive && current.ScanMode != previous.ScanMode
        && current.Sequence - previous.Sequence == 1 && current.Timestamp > previous.Timestamp
        && (current.Timestamp - previous.Timestamp).TotalSeconds <= maximumGap
        && current.Width == previous.Width && current.Height == previous.Height
        && current.Pitch == previous.Pitch && current.AspectRatio.Equals(previous.AspectRatio)
        && current.PixelFormat == previous.PixelFormat;

    // The presenter serializes render calls. A capture takes its own copy before leaving that gate.
    internal VideoFrame? Compose(EmulationDeinterlacingMode mode)
    {
        if (_composedMode != mode)
        {
            _composed = null;
            _motionSequence = null;
            _composedMode = mode;
        }
        if (_latest is not { } field || field.ScanMode == EmulationVideoScanMode.Progressive || mode == EmulationDeinterlacingMode.Off)
            return _latest;
        if (_composed is not null) return _composed;

        var size = checked(field.Pitch * field.ImageHeight);
        if (_output.Length < size) _output = new byte[size];
        var output = _output.AsSpan(0, size);
        var ownParity = field.ScanMode == EmulationVideoScanMode.InterlacedEvenField ? 0 : 1;
        int bytesPerPixel = field.PixelFormat == EmulationPixelFormat.Xrgb8888 ? 4 : 2;
        int tilesAcross = (field.Width + 3) / 4;
        if (mode == EmulationDeinterlacingMode.Adaptive && _previous is not null)
            UpdateMotion(field, bytesPerPixel, tilesAcross);

        for (var y = 0; y < field.ImageHeight; y++)
        {
            var row = y / 2;
            var destination = output.Slice(y * field.Pitch, field.Pitch);
            if ((y & 1) == ownParity)
            {
                field.Pixels.Span.Slice(row * field.Pitch, field.Pitch).CopyTo(destination);
                continue;
            }

            var complement = mode != EmulationDeinterlacingMode.Bob ? _previous : null;
            if (complement is not null)
            {
                complement.Pixels.Span.Slice(row * field.Pitch, field.Pitch).CopyTo(destination);
                if (mode != EmulationDeinterlacingMode.Adaptive) continue;
            }
            else
                field.Pixels.Span.Slice(row * field.Pitch, field.Pitch).CopyTo(destination);

            for (var x = 0; x < field.Width; x++)
            {
                var movement = complement is null ? 1f : _motion[row / 2 * tilesAcross + x / 4];
                if (movement <= 0f) continue;
                var bob = Bob(field, x, row, ownParity, bytesPerPixel);
                if (complement is not null && movement < 1f)
                    bob = Color.Mix(Read(complement, x, row, bytesPerPixel), bob, movement);
                Write(destination, x, field.PixelFormat, bytesPerPixel, bob);
            }
        }

        if (mode == EmulationDeinterlacingMode.Blend)
            Blend(field, output, bytesPerPixel);

        return _composed = field with
        {
            Pixels = _output.AsMemory(0, size), Height = field.ImageHeight, ScanMode = EmulationVideoScanMode.Progressive
        };
    }

    private static Color Bob(VideoFrame field, int x, int row, int parity, int bytesPerPixel)
    {
        var upper = row - parity;
        var lower = upper + 1;
        if (upper < 0 || lower >= field.Height)
            return Read(field, x, Math.Clamp(upper, 0, field.Height - 1), bytesPerPixel);

        int bestOffset = 0, bestScore = Score(0);
        for (var offset = -2; offset <= 2; offset++)
        {
            if (offset == 0 || x - Math.Abs(offset) < 0 || x + Math.Abs(offset) >= field.Width) continue;
            var score = Score(offset) + Math.Abs(offset) * 7;
            if (score >= bestScore) continue;
            bestScore = score;
            bestOffset = offset;
        }
        return Color.Mix(Read(field, x + bestOffset, upper, bytesPerPixel),
            Read(field, x - bestOffset, lower, bytesPerPixel), .5f);

        int Score(int offset)
        {
            var score = Color.Distance(Read(field, x + offset, upper, bytesPerPixel),
                Read(field, x - offset, lower, bytesPerPixel)) * 2;
            for (var dx = -1; dx <= 1; dx += 2)
            {
                var a = Math.Clamp(x + offset + dx, 0, field.Width - 1);
                var b = Math.Clamp(x - offset + dx, 0, field.Width - 1);
                score += Color.Distance(Read(field, a, upper, bytesPerPixel), Read(field, b, lower, bytesPerPixel));
            }
            return score;
        }
    }

    private void Blend(VideoFrame field, Span<byte> output, int bytesPerPixel)
    {
        if (_blendSource.Length < output.Length) _blendSource = new byte[output.Length];
        output.CopyTo(_blendSource);
        for (int y = 1; y < field.ImageHeight - 1; y++)
        {
            var above = _blendSource.AsSpan((y - 1) * field.Pitch, field.Pitch);
            var center = _blendSource.AsSpan(y * field.Pitch, field.Pitch);
            var below = _blendSource.AsSpan((y + 1) * field.Pitch, field.Pitch);
            var destination = output.Slice(y * field.Pitch, field.Pitch);
            for (var x = 0; x < field.Width; x++)
            {
                var a = Read(above, x, field.PixelFormat, bytesPerPixel);
                var c = Read(center, x, field.PixelFormat, bytesPerPixel);
                var b = Read(below, x, field.PixelFormat, bytesPerPixel);
                var verticalEdge = Color.Distance(a, b);
                var alternating = Math.Min(Color.Distance(a, c), Color.Distance(c, b));
                var weight = Ramp(alternating - verticalEdge, 4, 32) * (1f - Ramp(verticalEdge, 12, 52));
                if (weight > 0f)
                    Write(destination, x, field.PixelFormat, bytesPerPixel, Color.Mix(c, Color.Mix(a, b, .5f), .5f * weight));
            }
        }
    }

    private void UpdateMotion(VideoFrame field, int bytesPerPixel, int tilesAcross)
    {
        var count = checked(tilesAcross * ((field.Height + 1) / 2));
        if (_motion.Length < count) _motion = new float[count];
        if (_motionSequence == field.Sequence) return;
        bool smooth = _motionSequence == field.Sequence - 1;
        for (var y = 0; y < field.Height; y += 2)
        for (var x = 0; x < field.Width; x += 4)
        {
            var distanceSum = 0;
            var maximum = 0;
            var samples = 0;
            for (var dy = 0; dy < 2 && y + dy < field.Height; dy++)
            for (var dx = 0; dx < 4 && x + dx < field.Width; dx++)
            {
                var cx = x + dx;
                var cy = y + dy;
                int change = _sameParity is { } earlier
                    ? Color.Distance(Read(field, cx, cy, bytesPerPixel), Read(earlier, cx, cy, bytesPerPixel))
                    : Color.Distance(Read(_previous!, cx, cy, bytesPerPixel),
                        Bob(field, cx, cy, field.ScanMode == EmulationVideoScanMode.InterlacedEvenField ? 0 : 1, bytesPerPixel));
                if (_previousComplement is { } earlierComplement)
                    change = Math.Max(change, Color.Distance(Read(_previous!, cx, cy, bytesPerPixel),
                        Read(earlierComplement, cx, cy, bytesPerPixel)));
                distanceSum += change;
                maximum = Math.Max(maximum, change);
                samples++;
            }
            var detected = Ramp(.75f * distanceSum / samples + .25f * maximum, 5, 25);
            var index = y / 2 * tilesAcross + x / 4;
            var old = _motion[index];
            _motion[index] = smooth ? old + (detected - old) * (detected < old ? .36f : 1f) : detected;
        }
        _motionSequence = field.Sequence;
    }

    private static float Ramp(float value, float minimum, float maximum) => Math.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);

    private static Color Read(VideoFrame field, int x, int y, int bytesPerPixel) =>
        Read(field.Pixels.Span.Slice(y * field.Pitch, field.Pitch), x, field.PixelFormat, bytesPerPixel);

    private static Color Read(ReadOnlySpan<byte> row, int x, EmulationPixelFormat format, int bytesPerPixel)
    {
        var offset = x * bytesPerPixel;
        if (bytesPerPixel == 4) return new Color(row[offset + 2], row[offset + 1], row[offset]);
        var packed = BinaryPrimitives.ReadUInt16LittleEndian(row[offset..]);
        var rgb565 = format == EmulationPixelFormat.Rgb565;
        var greenMaximum = rgb565 ? 63 : 31;
        return new Color((packed >> (rgb565 ? 11 : 10) & 31) * 255 / 31,
            (packed >> 5 & greenMaximum) * 255 / greenMaximum, (packed & 31) * 255 / 31);
    }

    private static void Write(Span<byte> row, int x, EmulationPixelFormat format, int bytesPerPixel, Color color)
    {
        var offset = x * bytesPerPixel;
        if (bytesPerPixel == 4)
        {
            row[offset] = (byte)color.Blue;
            row[offset + 1] = (byte)color.Green;
            row[offset + 2] = (byte)color.Red;
            row[offset + 3] = 255;
            return;
        }
        var rgb565 = format == EmulationPixelFormat.Rgb565;
        var packed = ((color.Red * 31 + 127) / 255 << (rgb565 ? 11 : 10))
            | ((color.Green * (rgb565 ? 63 : 31) + 127) / 255 << 5) | (color.Blue * 31 + 127) / 255;
        BinaryPrimitives.WriteUInt16LittleEndian(row[offset..], (ushort)packed);
    }

    private readonly record struct Color(int Red, int Green, int Blue)
    {
        private int Luma => (54 * Red + 183 * Green + 19 * Blue) >> 8;

        internal static int Distance(Color first, Color second)
        {
            var channel = Math.Max(Math.Abs(first.Red - second.Red),
                Math.Max(Math.Abs(first.Green - second.Green), Math.Abs(first.Blue - second.Blue)));
            return (Math.Abs(first.Luma - second.Luma) + channel) / 2;
        }

        internal static Color Mix(Color first, Color second, float amount) => new(
            (int)MathF.Round(first.Red + (second.Red - first.Red) * amount),
            (int)MathF.Round(first.Green + (second.Green - first.Green) * amount),
            (int)MathF.Round(first.Blue + (second.Blue - first.Blue) * amount));
    }
}
