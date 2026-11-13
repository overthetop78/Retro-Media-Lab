using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class ChdImage
{
    private void ReadMap5(ulong offset)
    {
        _map = new byte[checked(_hunkCount * (_compression[0] == 0 ? 4 : 12))];
        if (_compression[0] == 0) { ReadAt(offset, _map); return; }
        Span<byte> header = stackalloc byte[16];
        ReadAt(offset, header);
        int length = checked((int)U32(header, 0));
        ulong currentOffset = U48(header[4..]);
        ushort crc = BinaryPrimitives.ReadUInt16BigEndian(header[10..]);
        int lengthBits = header[12], selfBits = header[13], parentBits = header[14];
        byte[] compressed = new byte[length]; ReadAt(offset + 16, compressed);
        ChdBitstream bits = new(compressed);
        ChdHuffman decoder = new(16, 8); decoder.ImportRle(ref bits);
        byte last = 0;
        int repeat = 0;
        for (int i = 0; i < _hunkCount; i++)
        {
            if ((i & 1023) == 0) _cancellation.ThrowIfCancellationRequested();
            byte type;
            if (repeat > 0) { type = last; repeat--; }
            else
            {
                if (bits.Overflow) throw new InvalidDataException("Truncated CHD map.");
                int value = decoder.Decode(ref bits);
                if (value == 7) { type = last; repeat = 2 + decoder.Decode(ref bits); }
                else if (value == 8) { type = last; repeat = 18 + (decoder.Decode(ref bits) << 4) + decoder.Decode(ref bits); }
                else type = last = (byte)value;
            }
            _map[i * 12] = type;
        }
        uint lastSelf = 0;
        ulong lastParent = 0;
        for (int i = 0; i < _hunkCount; i++)
        {
            Span<byte> entry = _map.AsSpan(i * 12, 12);
            ulong blockOffset = currentOffset;
            uint blockLength = 0;
            ushort blockCrc = 0;
            switch (entry[0])
            {
                case 0: case 1: case 2: case 3:
                    blockLength = bits.Read(lengthBits); currentOffset += blockLength;
                    blockCrc = (ushort)bits.Read(16); break;
                case 4:
                    blockLength = (uint)HunkBytes; currentOffset += blockLength;
                    blockCrc = (ushort)bits.Read(16); break;
                case 5: blockOffset = lastSelf = bits.Read(selfBits); break;
                case 6: blockOffset = lastParent = bits.Read(parentBits); break;
                case 10: lastSelf++; goto case 9;
                case 9: entry[0] = 5; blockOffset = lastSelf; break;
                case 11: entry[0] = 6; blockOffset = lastParent = (ulong)i * (uint)HunkBytes / (uint)_unitBytes; break;
                case 13: lastParent += (uint)(HunkBytes / _unitBytes); goto case 12;
                case 12: entry[0] = 6; blockOffset = lastParent; break;
                default: throw new InvalidDataException("Invalid CHD map compression type.");
            }
            Put24(entry[1..], blockLength); Put48(entry[4..], blockOffset);
            BinaryPrimitives.WriteUInt16BigEndian(entry[10..], blockCrc);
        }
        if (Crc16(_map) != crc) throw new InvalidDataException("CHD map CRC mismatch.");
    }
}
