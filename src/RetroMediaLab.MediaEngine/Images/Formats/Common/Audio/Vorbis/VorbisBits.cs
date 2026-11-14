using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

// Contiguous equivalent of Tremor's chained oggpack_buffer. The packet reader
// has already joined its page fragments; look does not change the error state.
internal sealed class VorbisBits
{
    private ReadOnlyMemory<byte> _packet;
    private long _position;
    private bool _halted;
    public bool EndOfPacket => _halted;
    public long BitsConsumed => _position;
    public long BytesConsumed => (_position + 7) >> 3;
    public long BitsRemaining => Math.Max(0, (long)_packet.Length * 8 - _position);
    public void Reset(ReadOnlyMemory<byte> packet) { _packet = packet; _position = 0; _halted = false; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Look(int count)
    {
        if ((uint)count > 32) throw new ArgumentOutOfRangeException(nameof(count));
        if (_halted || _position + count > (long)_packet.Length * 8) return -1;
        if (count == 0) return 0;
        int offset = (int)(_position >> 3), shift = (int)(_position & 7);
        ReadOnlySpan<byte> source = _packet.Span;
        ulong value;
        if (offset <= source.Length - 8) value = BinaryPrimitives.ReadUInt64LittleEndian(source[offset..]);
        else
        {
            value = 0;
            int bytes = (count + shift + 7) >> 3;
            for (int i = 0; i < bytes; i++) value |= (ulong)source[offset + i] << (i * 8);
        }
        return unchecked((int)((value >> shift) & ((1UL << count) - 1)));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (_halted) return;
        _position += count;
        if (_position > (long)_packet.Length * 8) { _halted = true; _position = (long)_packet.Length * 8; }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(int count)
    {
        int result = Look(count);
        Advance(count);
        return result;
    }
}
