using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal readonly record struct OggPacketInfo(ReadOnlyMemory<byte> Data, int Serial, long Number, long Granule,
    long PageOffset, bool Beginning, bool End, bool HoleBefore);

// Borrowed stream. Page and packet buffers remain valid until the next read.
// Lacing, continuation loss and granule attribution follow Tremor framing.c.
internal sealed class OggPacketReader
{
    private readonly Stream _stream;
    private readonly CancellationToken _cancellation;
    private readonly byte[] _page = new byte[27 + 255 + 65025];
    private byte[] _packet = new byte[4096];
    private int _segments, _segment, _body, _packetLength, _lastComplete, _serial;
    private uint _expectedSequence;
    private bool _sequenceKnown, _partial, _hole, _skipContinued, _beginning;
    private long _granule, _pageOffset, _packetNumber;
    private byte _flags;
    public int? SerialFilter { get; set; }
    public long Position => _stream.Position;
    public OggPacketReader(Stream stream, CancellationToken cancellation)
    { _stream = stream; _cancellation = cancellation; }
    public void Seek(long offset, int? serial = null)
    {
        _cancellation.ThrowIfCancellationRequested(); _stream.Seek(offset, SeekOrigin.Begin);
        _segments = _segment = _body = _packetLength = 0;
        _sequenceKnown = _partial = _hole = _skipContinued = _beginning = false;
        _packetNumber = 0; SerialFilter = serial;
    }
    private bool ReadExactly(Span<byte> destination)
    {
        while (!destination.IsEmpty)
        {
            _cancellation.ThrowIfCancellationRequested();
            int count = _stream.Read(destination);
            if (count == 0) return false;
            destination = destination[count..];
        }
        return true;
    }
    private static readonly uint[] CrcTable = CreateCrcTable();
    private static uint[] CreateCrcTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint value = i << 24;
            for (int bit = 0; bit < 8; bit++) value = (value << 1) ^ ((value & 0x80000000) != 0 ? 0x04c11db7u : 0);
            table[i] = value;
        }
        return table;
    }
    private bool ReadPage()
    {
        while (true)
        {
            _cancellation.ThrowIfCancellationRequested();
            if (!FindPageStart()) return false;
            long start = _stream.Position;
            if (!ReadExactly(_page.AsSpan(0, 27))) return false;
            if (!_page.AsSpan(0, 4).SequenceEqual("OggS"u8) || _page[4] != 0)
            { _stream.Seek(start + 1, SeekOrigin.Begin); _hole = true; continue; }
            int segments = _page[26];
            if (!ReadExactly(_page.AsSpan(27, segments))) return false;
            int bodyLength = 0, lastComplete = -1;
            for (int i = 0; i < segments; i++)
            { bodyLength += _page[27 + i]; if (_page[27 + i] < 255) lastComplete = i; }
            int headerLength = 27 + segments, length = headerLength + bodyLength;
            if (!ReadExactly(_page.AsSpan(headerLength, bodyLength))) return false;
            uint expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(_page.AsSpan(22));
            uint crc = 0;
            for (int i = 0; i < length; i++)
            {
                byte value = i is >= 22 and < 26 ? (byte)0 : _page[i];
                crc = (crc << 8) ^ CrcTable[(crc >> 24) ^ value];
            }
            if (crc != expectedCrc)
            { _stream.Seek(start + 1, SeekOrigin.Begin); _hole = true; continue; }
            int serial = BinaryPrimitives.ReadInt32LittleEndian(_page.AsSpan(14));
            if (SerialFilter.HasValue && SerialFilter.Value != serial) continue;
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(_page.AsSpan(18));
            byte flags = _page[5];
            bool same = _sequenceKnown && _serial == serial;
            bool gap = same && sequence != _expectedSequence;
            if (!same)
            {
                _partial = false; _packetLength = 0; _packetNumber = 0;
                _beginning = (flags & 2) != 0;
            }
            if (gap || _hole)
            {
                _partial = false; _packetLength = 0;
                if (same) { _hole = true; _packetNumber++; }
            }
            if (_partial && (flags & 1) == 0)
            { _partial = false; _packetLength = 0; _hole = true; _packetNumber++; }
            _skipContinued = !_partial && (flags & 1) != 0;
            _serial = serial; _expectedSequence = unchecked(sequence + 1); _sequenceKnown = true;
            _flags = flags; _segments = segments; _segment = 0; _body = headerLength;
            _lastComplete = lastComplete; _pageOffset = start;
            _granule = BinaryPrimitives.ReadInt64LittleEndian(_page.AsSpan(6));
            return true;
        }
    }
    private bool FindPageStart()
    {
        while (true)
        {
            _cancellation.ThrowIfCancellationRequested();
            long offset = _stream.Position;
            int read = _stream.Read(_page, 0, 4096);
            if (read < 4) return false;
            int found = _page.AsSpan(0, read).IndexOf("OggS"u8);
            if (found >= 0)
            {
                if (found != 0) _hole = true;
                _stream.Seek(offset + found, SeekOrigin.Begin); return true;
            }
            if (_stream.Position == _stream.Length) return false;
            _stream.Seek(offset + read - 3, SeekOrigin.Begin); _hole = true;
        }
    }
    public bool TryRead(out OggPacketInfo packet)
    {
        if (!_partial) _packetLength = 0;
        while (true)
        {
            if (_segment == _segments && !ReadPage()) { packet = default; return false; }
            if (_segment == _segments) continue;
            int segment = _segment++, count = _page[27 + segment];
            if (_skipContinued)
            {
                _body += count;
                if (count < 255) { _skipContinued = false; _partial = false; _packetLength = 0; }
                continue;
            }
            int length = checked(_packetLength + count);
            if (length > _packet.Length) Array.Resize(ref _packet, Math.Max(length, checked(_packet.Length * 2)));
            _page.AsSpan(_body, count).CopyTo(_packet.AsSpan(_packetLength)); _body += count; _packetLength = length;
            _partial = count == 255;
            if (_partial) continue;
            bool last = segment == _lastComplete;
            packet = new(_packet.AsMemory(0, _packetLength), _serial, _packetNumber++, last ? _granule : -1,
                _pageOffset, _beginning, segment == _segments - 1 && (_flags & 4) != 0, _hole);
            _beginning = _hole = false;
            return true;
        }
    }
}
