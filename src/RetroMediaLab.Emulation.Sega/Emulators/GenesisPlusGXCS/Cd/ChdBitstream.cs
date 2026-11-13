using System.IO;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

// MSB-first reader used by libchdr's map and Huffman codecs. Read-ahead may
// pass the input end; only consumed bytes count towards Overflow.
internal ref struct ChdBitstream
{
    private readonly ReadOnlySpan<byte> _source;
    private uint _buffer;
    private int _bits, _offset;
    public ChdBitstream(ReadOnlySpan<byte> source) { _source = source; }
    public readonly bool Overflow => _offset - _bits / 8 > _source.Length;
    public readonly int Offset => _offset - _bits / 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Peek(int count)
    {
        if ((uint)count > 32) throw new InvalidDataException("Invalid CHD bit width.");
        if (count == 0) return 0;
        if (count > _bits)
        {
            while (_bits <= 24)
            {
                if (_offset < _source.Length) _buffer |= (uint)_source[_offset] << (24 - _bits);
                _offset++; _bits += 8;
            }
        }
        return _buffer >> (32 - count);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int count) { _buffer = count == 32 ? 0 : _buffer << count; _bits -= count; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Read(int count) { uint value = Peek(count); Remove(count); return value; }
    public int Flush()
    {
        while (_bits >= 8) { _offset--; _bits -= 8; }
        _bits = 0; _buffer = 0;
        return _offset;
    }
}
