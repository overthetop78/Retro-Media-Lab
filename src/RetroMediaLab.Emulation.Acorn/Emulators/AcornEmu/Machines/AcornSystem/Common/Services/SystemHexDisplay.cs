using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Intègre les durées d'activation des LED entre changements des broches du décodeur et des segments.
/// La présentation ne cadence pas les circuits et ne transforme pas une impulsion brève en segment permanent.</summary>
internal sealed class SystemHexDisplay
{
    private readonly byte[] _pixels = new byte[C.DisplayWidth * C.DisplayHeight * C.DisplayPixelBytes];
    private readonly long[] _litTicks = new long[C.DisplayDigits * C.DisplaySegments];
    private readonly long[] _previousLitTicks = new long[C.DisplayDigits * C.DisplaySegments];
    private long _previousDuration;
    private long _lastTick;
    private long _frameStart;
    private int _digit = -1;
    private byte _segments;

    internal void SelectDigit(byte pins, bool driven, long tick)
    {
        var digit = driven ? pins & (C.DisplayDigits - 1) : -1;
        if (_digit == digit) return;
        Accumulate(tick);
        _digit = digit;
    }

    internal void SetSegments(byte segments, long tick)
    {
        if (_segments == segments) return;
        Accumulate(tick);
        _segments = segments;
    }

    internal void Reset(long tick)
    {
        Array.Clear(_litTicks);
        Array.Clear(_previousLitTicks);
        _previousDuration = 0;
        Array.Clear(_pixels);
        _lastTick = _frameStart = tick;
        _digit = -1;
        _segments = 0;
    }

    private void Accumulate(long tick)
    {
        var elapsed = tick - _lastTick;
        _lastTick = tick;
        if (elapsed <= 0 || _digit < 0 || _segments == 0) return;
        var offset = _digit * C.DisplaySegments;
        for (var bit = 0; bit < C.DisplaySegments; bit++)
            if ((_segments & (1 << bit)) != 0) _litTicks[offset + bit] += elapsed;
    }

    internal byte[] Render(long tick)
    {
        Accumulate(tick);
        var duration = tick - _frameStart;
        // The monitor's eight-digit scan is longer than a 20 ms presentation.
        // Integrate two consecutive presentation intervals so a boundary cannot discard an entire digit.
        // This is optical presentation only; every driven interval keeps its actual board-clock duration.
        var exposure = duration + _previousDuration;
        Array.Clear(_pixels);
        for (var digit = 0; digit < C.DisplayDigits; digit++)
        {
            var x = C.DisplayStartX + digit * C.DisplayDigitPitch;
            var y = C.DisplayStartY;
            var length = C.DisplaySegmentLength;
            var width = C.DisplaySegmentWidth;
            var height = C.DisplayDigitHeight;
            Segment(x + width, y, length, width, digit, 0, exposure);
            Segment(x + length + width, y + width, width, length, digit, 1, exposure);
            Segment(x + length + width, y + height / 2 + width, width, length, digit, 2, exposure);
            Segment(x + width, y + height, length, width, digit, 3, exposure);
            Segment(x, y + height / 2 + width, width, length, digit, 4, exposure);
            Segment(x, y + width, width, length, digit, 5, exposure);
            Segment(x + width, y + height / 2, length, width, digit, 6, exposure);
            Segment(x + C.DisplayPointX, y + C.DisplayPointY, C.DisplayPointSize, C.DisplayPointSize, digit, 7, exposure);
        }
        _litTicks.CopyTo(_previousLitTicks, 0);
        _previousDuration = duration;
        Array.Clear(_litTicks);
        _frameStart = tick;
        return _pixels;
    }

    private void Segment(int x, int y, int width, int height, int digit, int bit, long duration)
    {
        // One digit's normal multiplex duty is 1/8. This is a display gain, not a change to the pin timings.
        var index = digit * C.DisplaySegments + bit;
        var active = duration > 0 ? Math.Min(duration, (_litTicks[index] + _previousLitTicks[index]) * C.DisplayDigits) : 0;
        var red = duration > 0 ? (byte)(C.DisplayDimRed + (C.DisplayRed - C.DisplayDimRed) * active / duration) : C.DisplayDimRed;
        for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                _pixels[(row * C.DisplayWidth + column) * C.DisplayPixelBytes + 2] = red;
    }
}
