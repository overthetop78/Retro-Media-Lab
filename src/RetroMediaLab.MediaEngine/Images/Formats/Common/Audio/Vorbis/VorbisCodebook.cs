using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

internal sealed partial class VorbisCodebook
{
    private readonly uint[] _codes, _first;
    private readonly int[] _entryIndex, _values;
    private readonly byte[] _lengths;
    private readonly int _firstBits, _maxLength;
    public int Dimension { get; }
    public int Entries { get; }
    public int UsedEntries => _codes.Length;
    public int BinaryPoint { get; }
    private VorbisCodebook(int dimension, byte[] lengths, int mapType, int min, int delta, int[] quantized, bool sequence)
    {
        Dimension = dimension; Entries = lengths.Length;
        int used = 0;
        foreach (int length in lengths) if (length > 0) used++;
        _codes = new uint[used]; _entryIndex = new int[used]; _lengths = new byte[used];
        Span<uint> marker = stackalloc uint[33]; marker.Clear();
        int index = 0;
        unchecked
        {
            for (int i = 0; i < lengths.Length; i++)
            {
                int length = lengths[i]; if (length == 0) continue;
                uint entry = marker[length];
                if (length < 32 && (entry >> length) != 0) throw new InvalidDataException("Oversubscribed Vorbis codebook.");
                _codes[index] = length == 32 ? entry : entry << (32 - length);
                _entryIndex[index++] = i;
                for (int j = length; j > 0; j--)
                {
                    if ((marker[j] & 1) != 0)
                    { if (j == 1) marker[1]++; else marker[j] = marker[j - 1] << 1; break; }
                    marker[j]++;
                }
                for (int j = length + 1; j < 33; j++)
                {
                    if ((marker[j] >> 1) != entry) break;
                    entry = marker[j]; marker[j] = marker[j - 1] << 1;
                }
            }
            Array.Sort(_codes, _entryIndex);
            for (int i = 0; i < used; i++) { _lengths[i] = lengths[_entryIndex[i]]; _maxLength = Math.Max(_maxLength, _lengths[i]); }
            int binaryPoint = 0;
            _values = mapType == 0 || used == 0 ? [] : Unquantize(dimension, lengths.Length, _entryIndex, mapType, min, delta, quantized, sequence, out binaryPoint);
            BinaryPoint = mapType == 0 || used == 0 ? 0 : binaryPoint;
            _firstBits = Math.Clamp(ILog((uint)used) - 4, 5, 8); _first = new uint[1 << _firstBits];
            for (int i = 0; i < used; i++)
            {
                int length = _lengths[i];
                if (length > _firstBits) continue;
                uint code = Reverse(_codes[i]);
                for (int j = 0; j < 1 << (_firstBits - length); j++) _first[code | ((uint)j << length)] = (uint)i + 1;
            }
            uint mask = 0xfffffffeu << (31 - _firstBits);
            int low = 0, high = 0;
            for (int i = 0; i < _first.Length; i++)
            {
                uint word = (uint)i << (32 - _firstBits), slot = Reverse(word);
                if (_first[slot] != 0) continue;
                while (low + 1 < used && _codes[low + 1] <= word) low++;
                while (high < used && word >= (_codes[high] & mask)) high++;
                _first[slot] = 0x80000000u | ((uint)Math.Min(low, 0x7fff) << 15) | (uint)Math.Min(used - high, 0x7fff);
            }
        }
    }
    internal static int ILog(uint value) => value == 0 ? 0 : BitOperations.Log2(value) + 1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Reverse(uint value)
    {
        value = (value >> 16) | (value << 16);
        value = ((value >> 8) & 0x00ff00ff) | ((value << 8) & 0xff00ff00);
        value = ((value >> 4) & 0x0f0f0f0f) | ((value << 4) & 0xf0f0f0f0);
        value = ((value >> 2) & 0x33333333) | ((value << 2) & 0xcccccccc);
        return ((value >> 1) & 0x55555555) | ((value << 1) & 0xaaaaaaaa);
    }
    public static VorbisCodebook Unpack(VorbisBits bits)
    {
        if (bits.Read(24) != 0x564342) throw new InvalidDataException("Invalid Vorbis codebook sync.");
        int dimension = bits.Read(16), entries = bits.Read(24);
        if (dimension <= 0 || entries < 0) throw new InvalidDataException("Invalid Vorbis codebook dimensions.");
        byte[] lengths = new byte[entries];
        int ordered = bits.Read(1);
        if (ordered == 0)
        {
            int sparse = bits.Read(1);
            if (sparse < 0) throw new InvalidDataException("Truncated Vorbis codebook.");
            for (int i = 0; i < entries; i++)
            {
                if (sparse == 0 || bits.Read(1) != 0)
                {
                    int length = bits.Read(5);
                    if (length < 0) throw new InvalidDataException("Truncated Vorbis codeword lengths.");
                    lengths[i] = (byte)(length + 1);
                }
            }
        }
        else if (ordered == 1)
        {
            int length = bits.Read(5) + 1;
            for (int i = 0; i < entries; length++)
            {
                int count = bits.Read(ILog((uint)(entries - i)));
                if (count < 0 || length is < 1 or > 32) throw new InvalidDataException("Invalid ordered Vorbis codebook.");
                count = Math.Min(count, entries - i); lengths.AsSpan(i, count).Fill((byte)length); i += count;
            }
        }
        else throw new InvalidDataException("Truncated Vorbis codebook ordering.");
        int mapType = bits.Read(4), min = 0, delta = 0;
        int[] quantized = [];
        bool sequence = false;
        if (mapType is 1 or 2)
        {
            min = bits.Read(32); delta = bits.Read(32);
            int quantBits = bits.Read(4) + 1; sequence = bits.Read(1) != 0;
            int values = mapType == 1 ? QuantValues(entries, dimension) : checked(entries * dimension);
            if (quantBits <= 0 || (long)values * quantBits > bits.BitsRemaining) throw new InvalidDataException("Truncated Vorbis quantization values.");
            quantized = new int[values];
            for (int i = 0; i < values; i++) quantized[i] = bits.Read(quantBits);
        }
        else if (mapType != 0) throw new InvalidDataException("Invalid Vorbis codebook mapping type.");
        if (bits.EndOfPacket) throw new InvalidDataException("Truncated Vorbis codebook setup.");
        return new(dimension, lengths, mapType, min, delta, quantized, sequence);
    }
    private static int QuantValues(int entries, int dimension)
    {
        if (entries == 0) return 0;
        int low = 1, high = entries;
        while (low < high)
        {
            int value = low + (high - low + 1) / 2, product = 1;
            bool exceeds = false;
            for (int i = 0; i < dimension; i++)
            {
                if (product > entries / value) { exceeds = true; break; }
                product *= value;
            }
            if (exceeds) high = value - 1; else low = value;
        }
        return low;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int DecodePacked(VorbisBits bits)
    {
        if (UsedEntries == 0) return -1;
        int low, high, read = _maxLength, look = bits.Look(_firstBits);
        if (look >= 0)
        {
            uint entry = _first[look];
            if ((entry & 0x80000000) == 0)
            { int index = (int)entry - 1; bits.Advance(_lengths[index]); return index; }
            low = (int)((entry >> 15) & 0x7fff); high = UsedEntries - (int)(entry & 0x7fff);
        }
        else { low = 0; high = UsedEntries; }
        look = bits.Look(read);
        while (look < 0 && read > 1) look = bits.Look(--read);
        if (look < 0) { bits.Advance(1); return -1; }
        uint word = Reverse((uint)look);
        while (high - low > 1)
        {
            int middle = low + ((high - low) >> 1);
            if (_codes[middle] > word) high = middle; else low = middle;
        }
        if (_lengths[low] <= read) { bits.Advance(_lengths[low]); return low; }
        bits.Advance(read + 1); return -1;
    }
    public int Decode(VorbisBits bits)
    { int packed = DecodePacked(bits); return packed < 0 ? -1 : _entryIndex[packed]; }
}
