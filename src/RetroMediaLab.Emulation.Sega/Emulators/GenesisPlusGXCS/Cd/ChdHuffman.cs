using System.IO;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed class ChdHuffman
{
    private readonly int _maxBits;
    private readonly byte[] _lengths;
    private readonly uint[] _lookup;
    public ChdHuffman(int symbols, int maxBits)
    {
        if (symbols <= 0 || maxBits is < 1 or > 24) throw new ArgumentOutOfRangeException();
        _maxBits = maxBits; _lengths = new byte[symbols]; _lookup = new uint[1 << maxBits];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Decode(ref ChdBitstream bits)
    {
        uint entry = _lookup[bits.Peek(_maxBits)];
        int length = (int)(entry & 31);
        if (length == 0) throw new InvalidDataException("Invalid CHD Huffman code.");
        bits.Remove(length);
        return (int)(entry >> 5);
    }
    public void ImportRle(ref ChdBitstream bits)
    {
        int width = _maxBits >= 16 ? 5 : _maxBits >= 8 ? 4 : 3;
        for (int node = 0; node < _lengths.Length;)
        {
            int length = (int)bits.Read(width), repeat = 1;
            if (length == 1)
            {
                length = (int)bits.Read(width);
                if (length != 1) repeat = (int)bits.Read(width) + 3;
            }
            if (node + repeat > _lengths.Length) throw new InvalidDataException("Invalid CHD Huffman run.");
            _lengths.AsSpan(node, repeat).Fill((byte)length);
            node += repeat;
        }
        Build();
        if (bits.Overflow) throw new InvalidDataException("Truncated CHD Huffman tree.");
    }
    public void ImportHuffman(ref ChdBitstream bits)
    {
        ChdHuffman small = new(24, 6);
        small._lengths[0] = (byte)bits.Read(3);
        int start = (int)bits.Read(3) + 1, count = 0;
        for (int i = 1; i < 24; i++)
        {
            if (i < start || count == 7) small._lengths[i] = 0;
            else
            {
                count = (int)bits.Read(3);
                small._lengths[i] = (byte)(count == 7 ? 0 : count);
            }
        }
        small.Build();
        int runBits = 0;
        for (int n = _lengths.Length - 9; n > 0; n >>= 1) runBits++;
        int last = 0;
        for (int node = 0; node < _lengths.Length;)
        {
            int value = small.Decode(ref bits);
            if (value != 0) _lengths[node++] = (byte)(last = value - 1);
            else
            {
                count = (int)bits.Read(3) + 2;
                if (count == 9) count += (int)bits.Read(runBits);
                count = Math.Min(count, _lengths.Length - node);
                _lengths.AsSpan(node, count).Fill((byte)last); node += count;
            }
        }
        Build();
        if (bits.Overflow) throw new InvalidDataException("Truncated CHD Huffman tree.");
    }
    private void Build()
    {
        Span<uint> histogram = stackalloc uint[33];
        histogram.Clear();
        foreach (int length in _lengths)
        {
            if (length > _maxBits) throw new InvalidDataException("Invalid CHD Huffman length.");
            histogram[length]++;
        }
        uint current = 0;
        for (int length = 32; length > 0; length--)
        {
            uint next = (current + histogram[length]) >> 1;
            if (length != 1 && next * 2 != current + histogram[length]) throw new InvalidDataException("Invalid CHD Huffman tree.");
            histogram[length] = current; current = next;
        }
        Array.Clear(_lookup);
        for (int symbol = 0; symbol < _lengths.Length; symbol++)
        {
            int length = _lengths[symbol];
            if (length == 0) continue;
            uint code = histogram[length]++;
            int shift = _maxBits - length;
            uint first = code << shift, end = (code + 1) << shift;
            if (end > _lookup.Length || first >= end) throw new InvalidDataException("Invalid CHD Huffman lookup.");
            _lookup.AsSpan((int)first, (int)(end - first)).Fill(((uint)symbol << 5) | (uint)length);
        }
    }
    public void Decompress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        ChdBitstream bits = new(source);
        ImportHuffman(ref bits);
        for (int i = 0; i < destination.Length; i++) destination[i] = (byte)Decode(ref bits);
        bits.Flush();
        if (bits.Overflow) throw new InvalidDataException("Truncated CHD Huffman data.");
    }
}
