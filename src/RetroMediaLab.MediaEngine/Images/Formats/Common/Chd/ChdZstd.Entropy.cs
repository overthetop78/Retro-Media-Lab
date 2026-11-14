using System.Buffers.Binary;
using System.IO;
using System.Numerics;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdZstd
{
    private ref struct ForwardBits
    {
        private readonly ReadOnlySpan<byte> _source;
        private int _position;
        internal ForwardBits(ReadOnlySpan<byte> source) { _source = source; }
        internal readonly int Bytes => (_position + 7) / 8;
        internal readonly uint Peek(int width)
        {
            int offset = _position >> 3;
            uint value = 0;
            for (int i = 0; i < 3 && offset + i < _source.Length; i++) value |= (uint)_source[offset + i] << (i * 8);
            return (value >> (_position & 7)) & ((1u << width) - 1);
        }
        internal uint Read(int width)
        {
            uint value = Peek(width);
            if ((long)_position + width > (long)_source.Length * 8) throw new InvalidDataException("Truncated CHD Zstandard entropy header.");
            _position += width; return value;
        }
    }
    private ref struct ReverseBits
    {
        private readonly ReadOnlySpan<byte> _source;
        private int _remaining;
        internal readonly int Remaining => _remaining;
        internal ReverseBits(ReadOnlySpan<byte> source)
        {
            _source = source;
            if (source.IsEmpty || source[^1] == 0) throw new InvalidDataException("Missing CHD Zstandard bitstream end marker.");
            _remaining = checked((source.Length - 1) * 8 + BitOperations.Log2(source[^1]));
        }
        internal readonly uint Peek(int width)
        {
            if (width == 0) return 0;
            if (width > 32 || _remaining < 0) throw new InvalidDataException("Invalid CHD Zstandard bit count.");
            int available = Math.Min(_remaining, width), start = _remaining - available, offset = start >> 3;
            ulong value;
            if (offset <= _source.Length - 8) value = BinaryPrimitives.ReadUInt64LittleEndian(_source[offset..]);
            else
            {
                value = 0;
                for (int i = 0; i < 5 && offset + i < _source.Length; i++) value |= (ulong)_source[offset + i] << (i * 8);
            }
            ulong mask = (1UL << available) - 1;
            return (uint)(((value >> (start & 7)) & mask) << (width - available));
        }
        internal void Remove(int width)
        {
            if (width > _remaining) throw new InvalidDataException("Truncated CHD Zstandard bitstream.");
            _remaining -= width;
        }
        internal uint Read(int width) { uint value = Peek(width); Remove(width); return value; }
        internal uint ReadTail(int width)
        {
            uint value = Peek(width); _remaining -= width; return value;
        }
    }
    private struct FseEntry { internal ushort Next; internal byte Symbol, Bits; }
    private sealed class FseTable
    {
        internal readonly FseEntry[] Entries;
        internal int Log;
        internal bool Valid;
        internal FseTable(int maxLog) { Entries = new FseEntry[1 << maxLog]; }
        internal void Rle(byte symbol)
        { Log = 0; Entries[0] = new() { Symbol = symbol }; Valid = true; }
        internal int Read(ReadOnlySpan<byte> source, int maxSymbol, int maxLog)
        {
            Span<short> normalized = stackalloc short[256];
            int size = ReadNormalized(source, normalized[..(maxSymbol + 1)], out int log);
            if (log > maxLog) throw new InvalidDataException("CHD Zstandard FSE table is too large.");
            Build(normalized[..(maxSymbol + 1)], log); return size;
        }
        internal void Build(ReadOnlySpan<short> normalized, int log)
        {
            int size = 1 << log;
            if (size > Entries.Length || log < 1) throw new InvalidDataException("Invalid CHD Zstandard FSE table size.");
            Span<int> next = stackalloc int[256]; next.Clear();
            int high = size - 1, total = 0;
            for (int symbol = 0; symbol < normalized.Length; symbol++)
            {
                int frequency = normalized[symbol];
                if (frequency < -1) throw new InvalidDataException("Invalid CHD Zstandard FSE frequency.");
                total += Math.Abs(frequency);
                if (frequency == -1)
                {
                    if (high < 0) throw new InvalidDataException("Excess CHD Zstandard low-frequency symbols.");
                    Entries[high--].Symbol = (byte)symbol; next[symbol] = 1;
                }
                else next[symbol] = frequency;
            }
            if (total != size) throw new InvalidDataException("Invalid CHD Zstandard FSE distribution.");
            int step = (size >> 1) + (size >> 3) + 3, mask = size - 1, position = 0;
            for (int symbol = 0; symbol < normalized.Length; symbol++)
            {
                for (int i = 0; i < normalized[symbol]; i++)
                {
                    Entries[position].Symbol = (byte)symbol;
                    position = (position + step) & mask;
                    while (position > high) position = (position + step) & mask;
                }
            }
            if (position != 0) throw new InvalidDataException("Invalid CHD Zstandard FSE spread.");
            for (int i = 0; i < size; i++)
            {
                int state = next[Entries[i].Symbol]++;
                int bits = log - BitOperations.Log2((uint)state);
                Entries[i].Bits = (byte)bits; Entries[i].Next = (ushort)((state << bits) - size);
            }
            Log = log; Valid = true;
        }
    }
    private static int ReadNormalized(ReadOnlySpan<byte> source, Span<short> normalized, out int log)
    {
        normalized.Clear(); ForwardBits bits = new(source);
        log = (int)bits.Read(4) + 5;
        if (log > 12) throw new InvalidDataException("Invalid CHD Zstandard FSE log.");
        int remaining = (1 << log) + 1, threshold = 1 << log, width = log + 1, symbol = 0;
        bool zero = false;
        while (remaining > 1 && symbol < normalized.Length)
        {
            if (zero)
            {
                int repeat;
                do { repeat = (int)bits.Read(2); symbol += repeat; } while (repeat == 3 && symbol < normalized.Length);
                if (symbol >= normalized.Length) throw new InvalidDataException("Invalid CHD Zstandard zero-frequency run.");
            }
            int maximum = (threshold * 2 - 1) - remaining, count;
            if (bits.Peek(width - 1) < maximum) count = (int)bits.Read(width - 1);
            else { count = (int)bits.Read(width); if (count >= threshold) count -= maximum; }
            count--;
            remaining -= Math.Abs(count); normalized[symbol++] = (short)count; zero = count == 0;
            if (remaining < threshold && remaining > 1)
            { width = BitOperations.Log2((uint)remaining) + 1; threshold = 1 << (width - 1); }
        }
        if (remaining != 1) throw new InvalidDataException("Invalid CHD Zstandard normalized counts.");
        return bits.Bytes;
    }
    private readonly FseTable _weightTable = new(6);
    private int DecodeWeights(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int header = _weightTable.Read(source, 255, 6);
        ReverseBits bits = new(source[header..]);
        int state1 = (int)bits.Read(_weightTable.Log), state2 = (int)bits.Read(_weightTable.Log), count = 0;
        while (true)
        {
            if (count > destination.Length - 2) throw new InvalidDataException("Excess CHD Zstandard Huffman weights.");
            FseEntry entry = _weightTable.Entries[state1]; destination[count++] = entry.Symbol;
            state1 = entry.Next + (int)bits.ReadTail(entry.Bits);
            if (bits.Remaining < 0) { destination[count++] = _weightTable.Entries[state2].Symbol; break; }
            entry = _weightTable.Entries[state2]; destination[count++] = entry.Symbol;
            state2 = entry.Next + (int)bits.ReadTail(entry.Bits);
            if (bits.Remaining < 0)
            {
                if (count == destination.Length) throw new InvalidDataException("Excess CHD Zstandard Huffman weights.");
                destination[count++] = _weightTable.Entries[state1].Symbol; break;
            }
        }
        return count;
    }
}
