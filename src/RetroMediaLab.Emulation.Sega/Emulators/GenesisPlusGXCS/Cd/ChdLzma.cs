using System.IO;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

// libchdr reconstructs the SDK's level-9 properties: lc=3, lp=0, pb=2.
// An entire independently compressed hunk is its own dictionary.
internal sealed class ChdLzma
{
    private const int IsMatch = 0, IsRep = 192, RepG0 = 204, RepG1 = 216, RepG2 = 228;
    private const int Rep0Long = 240, PosSlot = 432, SpecPos = 688, Align = 802;
    private const int Len = 818, RepLen = 1332, Literal = 1846;
    private readonly ushort[] _probabilities = new ushort[Literal + (0x300 << 3)];

    private ref struct RangeDecoder
    {
        private readonly ReadOnlySpan<byte> _source;
        private readonly ushort[] _probabilities;
        private uint _range, _code;
        private int _offset;
        internal readonly int Offset => _offset;
        internal readonly uint Code => _code;
        internal RangeDecoder(ReadOnlySpan<byte> source, ushort[] probabilities)
        {
            _source = source; _probabilities = probabilities;
            _range = uint.MaxValue; _code = 0; _offset = 0;
            if (Next() != 0) throw new InvalidDataException("Invalid CHD LZMA range header.");
            for (int i = 0; i < 4; i++) _code = (_code << 8) | Next();
            if (_code == uint.MaxValue) throw new InvalidDataException("Invalid CHD LZMA range value.");
        }
        private byte Next()
        {
            if (_offset == _source.Length) throw new InvalidDataException("Truncated CHD LZMA stream.");
            return _source[_offset++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Normalize()
        {
            if (_range < 0x1000000) { _range <<= 8; _code = (_code << 8) | Next(); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Bit(int index)
        {
            Normalize();
            uint probability = _probabilities[index], bound = (_range >> 11) * probability;
            if (_code < bound)
            {
                _range = bound; _probabilities[index] = (ushort)(probability + ((2048 - probability) >> 5)); return 0;
            }
            _range -= bound; _code -= bound; _probabilities[index] = (ushort)(probability - (probability >> 5)); return 1;
        }
        internal uint Direct(int count)
        {
            uint value = 0;
            for (int i = 0; i < count; i++)
            {
                Normalize(); _range >>= 1;
                int bit = _code >= _range ? 1 : 0;
                if (bit != 0) _code -= _range;
                value = (value << 1) | (uint)bit;
            }
            return value;
        }
        internal int Tree(int start, int width)
        {
            int symbol = 1;
            for (int i = 0; i < width; i++) symbol = (symbol << 1) | Bit(start + symbol);
            return symbol - (1 << width);
        }
        internal uint ReverseTree(int start, int width)
        {
            int symbol = 1; uint value = 0;
            for (int i = 0; i < width; i++)
            {
                int bit = Bit(start + symbol); symbol = (symbol << 1) | bit; value |= (uint)bit << i;
            }
            return value;
        }
        internal int Length(int start, int posState)
        {
            if (Bit(start) == 0) return Tree(start + 2 + (posState << 3), 3) + 2;
            if (Bit(start + 1) == 0) return Tree(start + 130 + (posState << 3), 3) + 10;
            return Tree(start + 258, 8) + 18;
        }
        internal uint Distance(int length)
        {
            uint slot = (uint)Tree(PosSlot + (Math.Min(length - 2, 3) << 6), 6);
            if (slot < 4) return slot;
            int direct = (int)(slot >> 1) - 1;
            uint distance = (2u | (slot & 1)) << direct;
            if (slot < 14) return distance + ReverseTree(SpecPos + (int)distance - (int)slot - 1, direct);
            return unchecked(distance + (Direct(direct - 4) << 4) + ReverseTree(Align, 4));
        }
    }

    public void Decompress(ReadOnlySpan<byte> source, Span<byte> destination, CancellationToken cancellation)
    {
        _probabilities.AsSpan().Fill(1024);
        RangeDecoder decoder = new(source, _probabilities);
        int state = 0, position = 0;
        uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;
        int dictionarySize = Math.Max(4096, Math.Min(1 << 26, destination.Length));
        // SDK property serialization rounds smaller dictionaries to 2/3 * 2^n.
        if (dictionarySize < 1 << 21)
        {
            int rounded = 4096;
            for (int bits = 11; rounded < dictionarySize; bits++)
            {
                rounded = 2 << bits;
                if (rounded < dictionarySize) rounded = 3 << bits;
            }
            dictionarySize = rounded;
        }
        else dictionarySize = (dictionarySize + ((1 << 20) - 1)) & ~((1 << 20) - 1);

        while (position < destination.Length)
        {
            if ((position & 16383) == 0) cancellation.ThrowIfCancellationRequested();
            int posState = position & 3;
            if (decoder.Bit(IsMatch + (state << 4) + posState) == 0)
            {
                int previous = position == 0 ? 0 : destination[position - 1];
                int start = Literal + (previous >> 5) * 0x300, symbol = 1;
                if (state >= 7)
                {
                    if (rep0 >= position) throw new InvalidDataException("Invalid CHD LZMA literal reference.");
                    int match = destination[position - (int)rep0 - 1];
                    do
                    {
                        int matchBit = (match >> 7) & 1; match <<= 1;
                        int bit = decoder.Bit(start + ((matchBit + 1) << 8) + symbol);
                        symbol = (symbol << 1) | bit;
                        if (matchBit != bit) break;
                    } while (symbol < 0x100);
                }
                while (symbol < 0x100) symbol = (symbol << 1) | decoder.Bit(start + symbol);
                destination[position++] = (byte)symbol;
                state = state < 4 ? 0 : state < 10 ? state - 3 : state - 6;
                continue;
            }
            int length;
            if (decoder.Bit(IsRep + state) != 0)
            {
                if (decoder.Bit(RepG0 + state) == 0)
                {
                    if (decoder.Bit(Rep0Long + (state << 4) + posState) == 0)
                    {
                        if (rep0 >= position) throw new InvalidDataException("Invalid CHD LZMA short repetition.");
                        destination[position] = destination[position - (int)rep0 - 1]; position++;
                        state = state < 7 ? 9 : 11; continue;
                    }
                }
                else
                {
                    uint distance;
                    if (decoder.Bit(RepG1 + state) == 0) distance = rep1;
                    else
                    {
                        if (decoder.Bit(RepG2 + state) == 0) distance = rep2;
                        else { distance = rep3; rep3 = rep2; }
                        rep2 = rep1;
                    }
                    rep1 = rep0; rep0 = distance;
                }
                length = decoder.Length(RepLen, posState); state = state < 7 ? 8 : 11;
            }
            else
            {
                rep3 = rep2; rep2 = rep1; rep1 = rep0;
                length = decoder.Length(Len, posState);
                rep0 = decoder.Distance(length); state = state < 7 ? 7 : 10;
                if (rep0 == uint.MaxValue) throw new InvalidDataException("Premature CHD LZMA end marker.");
            }
            if (rep0 >= position || rep0 >= dictionarySize || length > destination.Length - position)
                throw new InvalidDataException("Invalid CHD LZMA match.");
            int from = position - (int)rep0 - 1;
            for (int i = 0; i < length; i++) destination[position++] = destination[from++];
        }
        decoder.Normalize();
        if (decoder.Code != 0)
        {
            if (decoder.Bit(IsMatch + (state << 4) + (position & 3)) == 0 || decoder.Bit(IsRep + state) != 0)
                throw new InvalidDataException("Excess CHD LZMA output.");
            int length = decoder.Length(Len, position & 3);
            if (decoder.Distance(length) != uint.MaxValue) throw new InvalidDataException("Missing CHD LZMA end marker.");
            decoder.Normalize();
        }
        if (decoder.Code != 0 || decoder.Offset != source.Length) throw new InvalidDataException("CHD LZMA final range mismatch.");
    }
}
