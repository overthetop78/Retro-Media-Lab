using System.IO;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdContainer
{
    private const uint Zlib = 0x7a6c6962, Lzma = 0x6c7a6d61, Huffman = 0x68756666;
    private const uint Flac = 0x666c6163, Zstd = 0x7a737464;
    private const uint CdZlib = 0x63647a6c, CdLzma = 0x63646c7a, CdFlac = 0x6364666c, CdZstd = 0x63647a73;
    private ChdDeflate? _deflate;
    private ChdLzma? _lzma;
    private ChdHuffman? _huffman;
    private ChdFlac? _flac;
    private ChdZstd? _zstd;
    private byte[] _cdBuffer = [];

    private void InitializeCodecs()
    {
        foreach (uint original in _compression)
        {
            uint codec = _version < 5 && original is 1 or 2 ? Zlib : original;
            switch (codec)
            {
                case 0: break;
                case Zlib: _deflate ??= new(); break;
                case Lzma: _lzma ??= new(); break;
                case Huffman: _huffman ??= new(256, 16); break;
                case Flac:
                    if (HunkBytes % 4 != 0) throw new InvalidDataException("Invalid FLAC CHD hunk size.");
                    _flac ??= new(); break;
                case Zstd: _zstd ??= new(); break;
                case CdZlib: case CdLzma: case CdFlac: case CdZstd:
                    if (HunkBytes % 2448 != 0) throw new InvalidDataException("Invalid CD CHD hunk size.");
                    if (_cdBuffer.Length == 0) _cdBuffer = new byte[HunkBytes];
                    if (codec == CdLzma) _lzma ??= new();
                    if (codec == CdFlac) _flac ??= new();
                    if (codec == CdZstd) _zstd ??= new();
                    else _deflate ??= new();
                    break;
                default: throw new InvalidDataException("Unknown CHD compression codec.");
            }
        }
    }

    private void Decompress(uint codec, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (_version < 5 && codec is 1 or 2) codec = Zlib;
        switch (codec)
        {
            case Zlib: _deflate!.Decompress(source, destination, _cancellation); return;
            case Lzma: _lzma!.Decompress(source, destination, _cancellation); return;
            case Huffman: _huffman!.Decompress(source, destination); return;
            case Flac:
                if (source.IsEmpty || (source[0] != (byte)'L' && source[0] != (byte)'B')) throw new InvalidDataException("Invalid CHD FLAC byte order.");
                _flac!.DecodeFrames(source[1..], destination, source[0] == 'B', _cancellation); return;
            case Zstd: _zstd!.Decompress(source, destination, _cancellation); return;
            case CdFlac:
            {
                int baseLength = destination.Length / 2448 * 2352;
                int offset = _flac!.DecodeFrames(source, _cdBuffer.AsSpan(0, baseLength), bigEndian: true, _cancellation);
                _deflate!.Decompress(source[offset..], _cdBuffer.AsSpan(baseLength), _cancellation);
                ReassembleCd(destination, default); return;
            }
            case CdZlib: case CdLzma: case CdZstd:
            {
                int frames = destination.Length / 2448, baseLength = frames * 2352;
                int eccBytes = (frames + 7) / 8, lengthBytes = destination.Length < 65536 ? 2 : 3;
                int headerLength = eccBytes + lengthBytes;
                if (source.Length < headerLength) throw new InvalidDataException("Truncated CHD CD compression header.");
                int compressedLength = (source[eccBytes] << 8) | source[eccBytes + 1];
                if (lengthBytes == 3) compressedLength = (compressedLength << 8) | source[eccBytes + 2];
                if (compressedLength > source.Length - headerLength) throw new InvalidDataException("Truncated CHD CD base data.");
                ReadOnlySpan<byte> main = source.Slice(headerLength, compressedLength), subcode = source[(headerLength + compressedLength)..];
                Span<byte> mainOut = _cdBuffer.AsSpan(0, baseLength), subcodeOut = _cdBuffer.AsSpan(baseLength);
                if (codec == CdZstd)
                { _zstd!.Decompress(main, mainOut, _cancellation); _zstd.Decompress(subcode, subcodeOut, _cancellation); }
                else
                {
                    if (codec == CdLzma) _lzma!.Decompress(main, mainOut, _cancellation);
                    else _deflate!.Decompress(main, mainOut, _cancellation);
                    _deflate!.Decompress(subcode, subcodeOut, _cancellation);
                }
                ReassembleCd(destination, source[..eccBytes]); return;
            }
            default: throw new InvalidDataException("Invalid CHD hunk compression codec.");
        }
    }
    private void ReassembleCd(Span<byte> destination, ReadOnlySpan<byte> ecc)
    {
        int frames = destination.Length / 2448;
        for (int frame = 0; frame < frames; frame++)
        {
            Span<byte> sector = destination.Slice(frame * 2448, 2448);
            _cdBuffer.AsSpan(frame * 2352, 2352).CopyTo(sector);
            _cdBuffer.AsSpan(frames * 2352 + frame * 96, 96).CopyTo(sector[2352..]);
            if (!ecc.IsEmpty && (ecc[frame >> 3] & (1 << (frame & 7))) != 0)
            {
                ReadOnlySpan<byte> sync = [0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0];
                sync.CopyTo(sector); GenerateEcc(sector);
            }
        }
    }
    private static void ComputeEcc(ReadOnlySpan<byte> sector, ReadOnlySpan<ushort> row, out byte first, out byte second)
    {
        first = second = 0;
        foreach (int offset in row)
        {
            byte value = sector[15] == 2 && offset < 4 ? (byte)0 : sector[12 + offset];
            first ^= value; second ^= value; first = ecclow[first];
        }
        first = ecchigh[ecclow[first] ^ second]; second ^= first;
    }
    private static void GenerateEcc(Span<byte> sector)
    {
        for (int i = 0; i < 86; i++)
        {
            ComputeEcc(sector, poffsets.AsSpan(i * 24, 24), out byte first, out byte second);
            sector[0x81c + i] = first; sector[0x81c + 86 + i] = second;
        }
        for (int i = 0; i < 52; i++)
        {
            ComputeEcc(sector, qoffsets.AsSpan(i * 43, 43), out byte first, out byte second);
            sector[0x8c8 + i] = first; sector[0x8c8 + 52 + i] = second;
        }
    }
    private void DisposeCodecs()
    { _deflate = null; _lzma = null; _huffman = null; _flac = null; _zstd = null; _cdBuffer = []; }
}
