using System.IO;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

// Raw (-MAX_WBITS) DEFLATE blocks used by libchdr. Tables and output history
// are reused; no stream, native inflater or intermediate hunk is allocated.
internal sealed class ChdDeflate
{
    private readonly uint[] _literal = new uint[32768], _distance = new uint[32768], _codes = new uint[128];
    private readonly byte[] _lengths = new byte[320];
    private static ReadOnlySpan<byte> CodeOrder => [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];
    private static ReadOnlySpan<ushort> LengthBase => [3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258];
    private static ReadOnlySpan<byte> LengthExtra => [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0];
    private static ReadOnlySpan<ushort> DistanceBase => [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577];

    private ref struct Bits
    {
        private readonly ReadOnlySpan<byte> _source;
        private uint _value;
        private int _count, _offset;
        internal Bits(ReadOnlySpan<byte> source) { _source = source; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint Peek(int count)
        {
            while (_count < count && _offset < _source.Length) { _value |= (uint)_source[_offset++] << _count; _count += 8; }
            return _value & ((1u << count) - 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(int count)
        {
            if (count > _count) throw new InvalidDataException("Truncated CHD DEFLATE stream.");
            _value >>= count; _count -= count;
        }
        internal int Read(int count) { uint value = Peek(count); Remove(count); return (int)value; }
        internal void Align() { int count = _count & 7; Remove(count); }
    }

    private static void Build(ReadOnlySpan<byte> lengths, Span<uint> table, int width, bool codeTree = false)
    {
        Span<int> counts = stackalloc int[16], next = stackalloc int[16];
        counts.Clear(); next.Clear(); table.Clear();
        int max = 0;
        foreach (int length in lengths)
        {
            if (length > width) throw new InvalidDataException("Invalid CHD DEFLATE code length.");
            counts[length]++; max = Math.Max(max, length);
        }
        if (max == 0) return;
        int left = 1, code = 0;
        for (int i = 1; i <= 15; i++)
        {
            left = (left << 1) - counts[i];
            if (left < 0) throw new InvalidDataException("Oversubscribed CHD DEFLATE tree.");
            code = (code + (i == 1 ? 0 : counts[i - 1])) << 1;
            next[i] = code;
        }
        if (left > 0 && (codeTree || max != 1)) throw new InvalidDataException("Incomplete CHD DEFLATE tree.");
        for (int symbol = 0; symbol < lengths.Length; symbol++)
        {
            int length = lengths[symbol];
            if (length == 0) continue;
            int value = next[length]++, reversed = 0;
            for (int i = 0; i < length; i++) { reversed = (reversed << 1) | (value & 1); value >>= 1; }
            uint entry = ((uint)symbol << 5) | (uint)length;
            for (int i = reversed; i < table.Length; i += 1 << length) table[i] = entry;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Decode(ref Bits bits, uint[] table, int width = 15)
    {
        uint entry = table[bits.Peek(width)];
        int count = (int)(entry & 31);
        if (count == 0) throw new InvalidDataException("Invalid CHD DEFLATE code.");
        bits.Remove(count); return (int)(entry >> 5);
    }

    public void Decompress(ReadOnlySpan<byte> source, Span<byte> destination, CancellationToken cancellation)
    {
        Bits bits = new(source);
        int position = 0;
        bool final;
        do
        {
            cancellation.ThrowIfCancellationRequested();
            final = bits.Read(1) != 0;
            int type = bits.Read(2);
            if (type == 0)
            {
                bits.Align();
                int length = bits.Read(16), complement = bits.Read(16);
                if ((length ^ complement) != 65535 || length > destination.Length - position) throw new InvalidDataException("Invalid CHD DEFLATE stored block.");
                for (int i = 0; i < length; i++) destination[position++] = (byte)bits.Read(8);
                continue;
            }
            if (type == 1)
            {
                _lengths.AsSpan(0, 144).Fill(8); _lengths.AsSpan(144, 112).Fill(9);
                _lengths.AsSpan(256, 24).Fill(7); _lengths.AsSpan(280, 8).Fill(8);
                Build(_lengths.AsSpan(0, 288), _literal, 15);
                _lengths.AsSpan(0, 32).Fill(5); Build(_lengths.AsSpan(0, 32), _distance, 15);
            }
            else if (type == 2)
            {
                int literals = bits.Read(5) + 257, distances = bits.Read(5) + 1, codes = bits.Read(4) + 4;
                if (literals > 286 || distances > 30) throw new InvalidDataException("Invalid CHD DEFLATE alphabet.");
                _lengths.AsSpan(0, 19).Clear();
                for (int i = 0; i < codes; i++) _lengths[CodeOrder[i]] = (byte)bits.Read(3);
                Build(_lengths.AsSpan(0, 19), _codes, 7, codeTree: true);
                int count = literals + distances;
                for (int index = 0; index < count;)
                {
                    int value = Decode(ref bits, _codes, 7);
                    if (value < 16) _lengths[index++] = (byte)value;
                    else
                    {
                        int repeat, length = 0;
                        if (value == 16)
                        {
                            if (index == 0) throw new InvalidDataException("Invalid CHD DEFLATE repeat.");
                            length = _lengths[index - 1]; repeat = bits.Read(2) + 3;
                        }
                        else repeat = value == 17 ? bits.Read(3) + 3 : bits.Read(7) + 11;
                        if (repeat > count - index) throw new InvalidDataException("Invalid CHD DEFLATE repeat length.");
                        _lengths.AsSpan(index, repeat).Fill((byte)length); index += repeat;
                    }
                }
                if (_lengths[256] == 0) throw new InvalidDataException("Missing CHD DEFLATE end code.");
                Build(_lengths.AsSpan(0, literals), _literal, 15);
                Build(_lengths.AsSpan(literals, distances), _distance, 15);
            }
            else throw new InvalidDataException("Reserved CHD DEFLATE block type.");

            while (true)
            {
                int symbol = Decode(ref bits, _literal);
                if (symbol < 256)
                {
                    if (position == destination.Length) throw new InvalidDataException("Excess CHD DEFLATE output.");
                    destination[position++] = (byte)symbol;
                }
                else if (symbol == 256) break;
                else
                {
                    if (symbol > 285) throw new InvalidDataException("Invalid CHD DEFLATE length code.");
                    int length = LengthBase[symbol - 257] + bits.Read(LengthExtra[symbol - 257]);
                    int distanceCode = Decode(ref bits, _distance);
                    if (distanceCode > 29) throw new InvalidDataException("Invalid CHD DEFLATE distance code.");
                    int distance = DistanceBase[distanceCode] + bits.Read(distanceCode < 4 ? 0 : (distanceCode >> 1) - 1);
                    if (distance > position || length > destination.Length - position) throw new InvalidDataException("Invalid CHD DEFLATE match.");
                    int from = position - distance;
                    for (int i = 0; i < length; i++) destination[position++] = destination[from++];
                }
                if ((position & 16383) == 0) cancellation.ThrowIfCancellationRequested();
            }
        } while (!final);
        if (position != destination.Length) throw new InvalidDataException("CHD DEFLATE output length mismatch.");
    }
}
