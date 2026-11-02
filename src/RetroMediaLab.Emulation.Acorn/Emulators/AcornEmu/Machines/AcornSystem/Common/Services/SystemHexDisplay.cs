using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Rendu en pixels des segments réellement commandés par le moniteur System 1.</summary>
internal sealed class SystemHexDisplay
{
    private readonly byte[] _pixels = new byte[C.DisplayWidth * C.DisplayHeight * C.DisplayPixelBytes];

    internal byte[] Render(ReadOnlySpan<byte> digits)
    {
        Array.Clear(_pixels);
        for (var digit = 0; digit < digits.Length; digit++)
        {
            var x = C.DisplayStartX + digit * C.DisplayDigitPitch;
            var y = C.DisplayStartY;
            var length = C.DisplaySegmentLength;
            var width = C.DisplaySegmentWidth;
            var height = C.DisplayDigitHeight;
            Segment(x + width, y, length, width, digits[digit], 0);
            Segment(x + length + width, y + width, width, length, digits[digit], 1);
            Segment(x + length + width, y + height / 2 + width, width, length, digits[digit], 2);
            Segment(x + width, y + height, length, width, digits[digit], 3);
            Segment(x, y + height / 2 + width, width, length, digits[digit], 4);
            Segment(x, y + width, width, length, digits[digit], 5);
            Segment(x + width, y + height / 2, length, width, digits[digit], 6);
            Segment(x + C.DisplayPointX, y + C.DisplayPointY, C.DisplayPointSize, C.DisplayPointSize, digits[digit], 7);
        }
        return _pixels;
    }

    private void Segment(int x, int y, int width, int height, byte value, int bit)
    {
        var red = (value & (1 << bit)) != 0 ? C.DisplayRed : C.DisplayDimRed;
        for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                _pixels[(row * C.DisplayWidth + column) * C.DisplayPixelBytes + 2] = red;
    }
}
