using System.Runtime.InteropServices;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    private static readonly uint[] EmptyRenderLine = new uint[C.ScanoutWordsPerLine];
    private uint[] _renderedWords = [];
    private byte[] _renderedPixels = [];

    private byte[] RenderCachedScanout()
    {
        var height = OutputHeight;
        if (_renderedPixels.Length != C.Width * height * C.PackedPixelBytes)
        {
            _renderedPixels = new byte[C.Width * height * C.PackedPixelBytes];
            _renderedWords = new uint[C.ScanoutWordsPerLine * height];
        }
        var pixels = MemoryMarshal.Cast<byte, ulong>(_renderedPixels.AsSpan());
        for (var line = 0; line < Height; line++)
        {
            var sourceStart = line * C.ScanoutWordsPerLine;
            if (!_completedInterlaced)
            {
                UpdateRenderLine(_completed.AsSpan(sourceStart, C.ScanoutWordsPerLine), line, pixels);
                continue;
            }
            for (var parity = 0; parity < C.FieldsPerInterlacedImage; parity++)
            {
                var source = parity == _completedParity ? _completed : _previousField;
                var row = parity != _completedParity && !_previousFieldValid
                    ? EmptyRenderLine.AsSpan() : source.AsSpan(sourceStart, C.ScanoutWordsPerLine);
                UpdateRenderLine(row, line * C.FieldsPerInterlacedImage + parity, pixels);
            }
        }
        // Le cache reste privé. Chaque destinataire possède un instantané indépendant.
        return _renderedPixels.ToArray();
    }

    private void UpdateRenderLine(ReadOnlySpan<uint> source, int line, Span<ulong> pixels)
    {
        var retained = _renderedWords.AsSpan(line * C.ScanoutWordsPerLine, C.ScanoutWordsPerLine);
        if (source.SequenceEqual(retained)) return;
        source.CopyTo(retained);
        var destination = line * C.ScanoutWordsPerLine * C.ScanoutPairsPerWord;
        for (var word = 0; word < C.ScanoutWordsPerLine; word++)
        {
            var packed = source[word];
            var target = destination + word * C.ScanoutPairsPerWord;
            if (packed == 0)
            {
                pixels.Slice(target, C.ScanoutPairsPerWord).Clear();
                continue;
            }
            for (var pair = 0; pair < C.ScanoutPairsPerWord; pair++)
            {
                pixels[target + pair] = ScanoutPixelPairs[packed & C.ScanoutPairMask];
                packed >>= C.ScanoutPairBits;
            }
        }
    }
}
