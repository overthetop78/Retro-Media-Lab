using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed class FilterProjectionColorBreakup
{
    private byte[]? _previous;
    private byte[]? _output;
    private EmulationProjectionVideoConfiguration? _configuration;
    private int _width;
    private int _height;
    private long _sequence;
    private TimeSpan _timestamp;
    private bool _hasBreakup;

    internal byte[] Apply(byte[] pixels, VideoFrame frame, EmulationProjectionVideoConfiguration configuration)
    {
        var compatible = configuration == _configuration && frame.Width == _width && frame.Height == _height;
        if (compatible && frame.Sequence == _sequence && frame.Timestamp == _timestamp)
            return _hasBreakup ? _output! : pixels;

        var length = checked(frame.Width * frame.Height * 4);
        _hasBreakup = false;
        if (compatible && frame.Sequence > _sequence && frame.Sequence - _sequence <= 2
            && frame.Timestamp >= _timestamp && frame.Timestamp - _timestamp <= TimeSpan.FromMilliseconds(85))
        {
            if (_output is null || _output.Length < length) _output = new byte[length];
            pixels.AsSpan(0, length).CopyTo(_output);
            var stride = frame.Width * 4;
            for (var y = 0; y < frame.Height; y++)
            for (var x = 0; x < frame.Width; x++)
            {
                var index = y * stride + x * 4;
                var change = Luma(pixels, index) - Luma(_previous!, index);
                var motion = Math.Clamp((Math.Abs(change) - 24f) / 110f, 0f, 1f);
                if (motion <= 0f) continue;

                var left = x == 0 ? index : index - 4;
                var right = x + 1 == frame.Width ? index : index + 4;
                var top = y == 0 ? index : index - stride;
                var bottom = y + 1 == frame.Height ? index : index + stride;
                var horizontalEdge = Luma(pixels, right) - Luma(pixels, left);
                var verticalEdge = Luma(pixels, bottom) - Luma(pixels, top);
                var horizontal = Math.Abs(horizontalEdge) >= Math.Abs(verticalEdge);
                var edge = horizontal ? horizontalEdge : verticalEdge;
                var strength = .32f * motion * Math.Clamp((Math.Abs(edge) - 24f) / 110f, 0f, 1f);
                if (strength <= 0f) continue;

                // Separate the red and blue exposures along the detected edge motion.
                var rear = horizontal ? left : top;
                var front = horizontal ? right : bottom;
                if (change * edge > 0) (rear, front) = (front, rear);
                _output[index + 2] = Blend(pixels[index + 2], pixels[rear + 2], strength);
                _output[index] = Blend(pixels[index], pixels[front], strength);
                _hasBreakup = true;
            }
        }
        if (_previous is null || _previous.Length < length) _previous = new byte[length];
        pixels.AsSpan(0, length).CopyTo(_previous);
        _configuration = configuration;
        _width = frame.Width;
        _height = frame.Height;
        _sequence = frame.Sequence;
        _timestamp = frame.Timestamp;
        return _hasBreakup ? _output! : pixels;
    }

    internal void Reset() => _configuration = null;

    private static int Luma(byte[] pixels, int index) =>
        (54 * pixels[index + 2] + 183 * pixels[index + 1] + 19 * pixels[index]) >> 8;

    private static byte Blend(byte current, byte shifted, float weight) =>
        (byte)MathF.Round(float.Lerp(current, shifted, weight));
}
