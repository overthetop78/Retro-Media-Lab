using System.Runtime.InteropServices;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    // Un index nul représente un pixel non écrit. Noir et blanc sont communs aux
    // deux palettes ; les six niveaux gris occupent les derniers index.
    private static readonly uint[] ColourScanoutLookup = CreateScanoutLookup(false);
    private static readonly uint[] MonochromeScanoutLookup = CreateScanoutLookup(true);
    private static readonly uint[] ScanoutPixels = CreateScanoutPixels();
    private static readonly ulong[] ScanoutPixelPairs = CreateScanoutPixelPairs();
    private uint[] _scanoutLookup = ColourScanoutLookup;

    private static uint[] CreateScanoutLookup(bool monochrome)
    {
        var lookup = new uint[C.ScanoutHalfMask + 1];
        for (var pattern = 0; pattern < lookup.Length; pattern++)
        {
            uint packed = 0;
            for (var pixel = 0; pixel < C.ScanoutHalfPixels; pixel++)
            {
                var colour = (pattern >> (pixel * C.ColourBits)) & C.ColourMask;
                var index = monochrome && colour != 0 && colour != C.ColourMask
                    ? colour + C.ScanoutMonochromeOffset : colour + 1;
                packed |= (uint)index << (pixel * C.ScanoutPixelBits);
            }
            lookup[pattern] = packed;
        }
        return lookup;
    }

    private static uint[] CreateScanoutPixels()
    {
        var pixels = new uint[C.ScanoutPixelMask + 1];
        for (var colour = 0; colour <= C.ColourMask; colour++) pixels[colour + 1] = C.PackedColours[colour];
        for (var colour = 1; colour < C.ColourMask; colour++) pixels[colour + C.ScanoutMonochromeOffset] = C.PackedMonochrome[colour];
        return pixels;
    }

    private static ulong[] CreateScanoutPixelPairs()
    {
        var pairs = new ulong[C.ScanoutPairMask + 1];
        for (var pattern = 0; pattern < pairs.Length; pattern++)
        {
            var first = ScanoutPixels[pattern & C.ScanoutPixelMask];
            var second = ScanoutPixels[pattern >> C.ScanoutPixelBits];
            pairs[pattern] = BitConverter.IsLittleEndian
                ? first | ((ulong)second << C.PackedPixelBits)
                : ((ulong)first << C.PackedPixelBits) | second;
        }
        return pairs;
    }

    private void WriteScanout(int left, int line, uint output)
    {
        var first = left < 0 ? -left : 0;
        var last = C.Width - left;
        if (last > C.CpuMasterDots) last = C.CpuMasterDots;
        if (first >= last) return;
        var packed = _scanoutLookup[output & C.ScanoutHalfMask]
            | (_scanoutLookup[(output >> C.ScanoutHalfBits) & C.ScanoutHalfMask] << C.ScanoutPackedHalfBits);
        var x = left + first;
        var count = last - first;
        packed >>= first * C.ScanoutPixelBits;
        var destination = line * C.ScanoutWordsPerLine + x / C.CpuMasterDots;
        var offset = x % C.CpuMasterDots;
        if (offset == 0 && count == C.CpuMasterDots)
        {
            _drawing[destination] = packed;
            return;
        }
        if (count == C.CpuMasterDots)
        {
            var shift = offset * C.ScanoutPixelBits;
            var remaining = C.ScanoutWordBits - shift;
            var preserved = (1u << shift) - 1;
            _drawing[destination] = (_drawing[destination] & preserved) | (packed << shift);
            _drawing[destination + 1] = (_drawing[destination + 1] & ~preserved) | (packed >> remaining);
            return;
        }
        while (count > 0)
        {
            var available = C.CpuMasterDots - offset;
            var take = count < available ? count : available;
            var bits = take * C.ScanoutPixelBits;
            // take est au plus sept ici : le mot complet est traité au-dessus.
            var mask = (1u << bits) - 1;
            var shift = offset * C.ScanoutPixelBits;
            _drawing[destination] = (_drawing[destination] & ~(mask << shift)) | ((packed & mask) << shift);
            packed >>= bits;
            count -= take;
            offset = 0;
            destination++;
        }
    }

    private byte[] ExpandScanout(uint[] source)
    {
        var result = new byte[C.Width * Height * C.PackedPixelBytes];
        var pixels = MemoryMarshal.Cast<byte, ulong>(result.AsSpan());
        for (var word = 0; word < source.Length; word++)
        {
            var packed = source[word];
            if (packed == 0) continue;
            var destination = word * C.ScanoutPairsPerWord;
            for (var pair = 0; pair < C.ScanoutPairsPerWord; pair++)
            {
                pixels[destination + pair] = ScanoutPixelPairs[packed & C.ScanoutPairMask];
                packed >>= C.ScanoutPairBits;
            }
        }
        return result;
    }

    private byte[] ExpandInterlacedScanout()
    {
        var result = new byte[C.Width * OutputHeight * C.PackedPixelBytes];
        var pixels = MemoryMarshal.Cast<byte, ulong>(result.AsSpan());
        var pairsPerLine = C.Width / C.FieldsPerInterlacedImage;
        for (var line = 0; line < Height; line++)
        {
            var sourceStart = line * C.ScanoutWordsPerLine;
            for (var parity = 0; parity < C.FieldsPerInterlacedImage; parity++)
            {
                if (parity != _completedParity && !_previousFieldValid) continue;
                var source = parity == _completedParity ? _completed : _previousField;
                var destination = (line * C.FieldsPerInterlacedImage + parity) * pairsPerLine;
                for (var word = 0; word < C.ScanoutWordsPerLine; word++)
                {
                    var packed = source[sourceStart + word];
                    if (packed == 0) continue;
                    var target = destination + word * C.ScanoutPairsPerWord;
                    for (var pair = 0; pair < C.ScanoutPairsPerWord; pair++)
                    {
                        pixels[target + pair] = ScanoutPixelPairs[packed & C.ScanoutPairMask];
                        packed >>= C.ScanoutPairBits;
                    }
                }
            }
        }
        return result;
    }
}
