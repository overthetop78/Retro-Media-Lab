using System.Buffers.Binary;
using System.IO;
using System.Numerics;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdZstd
{
    private readonly ushort[] _huffman = new ushort[4096];
    private readonly byte[] _weights = new byte[256], _literals = new byte[128 * 1024];
    private int _huffmanLog, _literalCount;

    private int ReadHuffman(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty) throw new InvalidDataException("Truncated CHD Zstandard Huffman header.");
        int size = source[0], symbols;
        if (size >= 128)
        {
            symbols = size - 127; size = (symbols + 1) / 2;
            if (size + 1 > source.Length) throw new InvalidDataException("Truncated CHD Zstandard Huffman weights.");
            for (int i = 0; i < symbols; i++) _weights[i] = (byte)((source[1 + i / 2] >> ((i & 1) == 0 ? 4 : 0)) & 15);
        }
        else
        {
            if (size + 1 > source.Length) throw new InvalidDataException("Truncated CHD Zstandard compressed weights.");
            symbols = DecodeWeights(source.Slice(1, size), _weights.AsSpan(0, 255));
        }
        Span<int> counts = stackalloc int[13], starts = stackalloc int[13];
        counts.Clear(); starts.Clear();
        uint total = 0;
        for (int i = 0; i < symbols; i++)
        {
            int weight = _weights[i];
            if (weight > 12) throw new InvalidDataException("Invalid CHD Zstandard Huffman weight.");
            counts[weight]++; total += (1u << weight) >> 1;
        }
        if (total == 0) throw new InvalidDataException("Empty CHD Zstandard Huffman tree.");
        int log = BitOperations.Log2(total) + 1;
        if (log > 12) throw new InvalidDataException("Excess CHD Zstandard Huffman depth.");
        uint rest = (1u << log) - total;
        if (!BitOperations.IsPow2(rest)) throw new InvalidDataException("Invalid CHD Zstandard implied weight.");
        int lastWeight = BitOperations.Log2(rest) + 1;
        _weights[symbols++] = (byte)lastWeight; counts[lastWeight]++;
        if (counts[1] < 2 || (counts[1] & 1) != 0) throw new InvalidDataException("Invalid CHD Zstandard weight distribution.");
        int position = 0;
        for (int weight = 1; weight <= log; weight++)
        { starts[weight] = position; position += counts[weight] << (weight - 1); }
        for (int i = 0; i < symbols; i++)
        {
            int weight = _weights[i]; if (weight == 0) continue;
            int length = 1 << (weight - 1);
            _huffman.AsSpan(starts[weight], length).Fill((ushort)((i << 4) | (log + 1 - weight)));
            starts[weight] += length;
        }
        _huffmanLog = log; return size + 1;
    }
    private void DecodeLiteralsStream(ReadOnlySpan<byte> source, Span<byte> output)
    {
        ReverseBits bits = new(source);
        for (int i = 0; i < output.Length; i++)
        {
            ushort entry = _huffman[bits.Peek(_huffmanLog)];
            int count = entry & 15;
            if (count == 0) throw new InvalidDataException("Invalid CHD Zstandard Huffman code.");
            bits.Remove(count); output[i] = (byte)(entry >> 4);
        }
        if (bits.Remaining != 0) throw new InvalidDataException("CHD Zstandard Huffman stream length mismatch.");
    }
    private int ReadLiterals(ReadOnlySpan<byte> source)
    {
        if (source.Length < 2) throw new InvalidDataException("Truncated CHD Zstandard literal block.");
        int type = source[0] & 3, format = (source[0] >> 2) & 3, header, size;
        if (type <= 1)
        {
            header = format switch { 1 => 2, 3 => 3, _ => 1 };
            if (source.Length < header) throw new InvalidDataException("Truncated CHD Zstandard literal header.");
            size = header == 1 ? source[0] >> 3 : header == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(source) >> 4 : Read24(source) >> 4;
            if ((uint)size > _literals.Length) throw new InvalidDataException("Excess CHD Zstandard literal size.");
            int stored = type == 0 ? size : 1;
            if (source.Length - header < stored) throw new InvalidDataException("Truncated CHD Zstandard literals.");
            if (type == 0) source.Slice(header, size).CopyTo(_literals);
            else _literals.AsSpan(0, size).Fill(source[header]);
            _literalCount = size; return header + stored;
        }
        header = format < 2 ? 3 : format == 2 ? 4 : 5;
        if (source.Length < header) throw new InvalidDataException("Truncated CHD Zstandard compressed literal header.");
        ulong descriptor = 0;
        for (int i = 0; i < header; i++) descriptor |= (ulong)source[i] << (i * 8);
        int width = format < 2 ? 10 : format == 2 ? 14 : 18;
        int mask = (1 << width) - 1;
        size = (int)(descriptor >> 4) & mask;
        int compressed = (int)(descriptor >> (4 + width)) & mask;
        if (size > _literals.Length || compressed > source.Length - header) throw new InvalidDataException("Invalid CHD Zstandard compressed literal sizes.");
        ReadOnlySpan<byte> data = source.Slice(header, compressed);
        if (type == 2) { int tree = ReadHuffman(data); data = data[tree..]; }
        else if (_huffmanLog == 0) throw new InvalidDataException("Missing CHD Zstandard repeated Huffman table.");
        if (format == 0) DecodeLiteralsStream(data, _literals.AsSpan(0, size));
        else
        {
            if (data.Length < 6 || size < 6) throw new InvalidDataException("Invalid CHD Zstandard four-stream literals.");
            int a = BinaryPrimitives.ReadUInt16LittleEndian(data), b = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]), c = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
            int segment = (size + 3) / 4;
            if (a + b + c > data.Length - 6 || segment * 3 > size) throw new InvalidDataException("Invalid CHD Zstandard Huffman jump table.");
            DecodeLiteralsStream(data.Slice(6, a), _literals.AsSpan(0, segment));
            DecodeLiteralsStream(data.Slice(6 + a, b), _literals.AsSpan(segment, segment));
            DecodeLiteralsStream(data.Slice(6 + a + b, c), _literals.AsSpan(segment * 2, segment));
            DecodeLiteralsStream(data[(6 + a + b + c)..], _literals.AsSpan(segment * 3, size - segment * 3));
        }
        _literalCount = size; return header + compressed;
    }
}
