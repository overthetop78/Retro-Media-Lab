using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdContainer : IDisposable
{
    private Stream? _file;
    private readonly bool _leaveOpen;
    private readonly CancellationToken _cancellation;
    private int _version, _hunkCount, _unitBytes;
    private ulong _metadataOffset;
    private readonly uint[] _compression = new uint[4];
    private byte[] _map = [];
    private byte[] _compressed = [];
    private MapEntry[] _legacyMap = [];
    private struct MapEntry { internal ulong Offset; internal int Length; internal byte Type; }
    public int HunkBytes { get; private set; }
    public int Version => _version;
    public int UnitBytes => _unitBytes;
    public long LogicalBytes { get; private set; }
    public long MetadataOffset => checked((long)_metadataOffset);
    public string Compression => string.Join(",",_compression.Where(codec=>codec!=0).Select(codec=>_version<5 ? "zlib" :
        new string([(char)(codec>>24),(char)(codec>>16&255),(char)(codec>>8&255),(char)(codec&255)])));

    private ChdContainer(Stream file, bool leaveOpen, CancellationToken cancellation)
    { _file = file; _leaveOpen = leaveOpen; _cancellation = cancellation; }

    public static ChdContainer Open(Stream file, bool leaveOpen, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(file);
        ChdContainer image = new(file, leaveOpen, cancellation);
        bool opened = false;
        try { image.ReadHeader(); opened = true; return image; }
        finally { if (!opened) image.Dispose(); }
    }

    private void ReadAt(ulong offset, Span<byte> output)
    {
        Stream file = _file ?? throw new ObjectDisposedException(nameof(ChdContainer));
        if (offset > (ulong)file.Length || (ulong)output.Length > (ulong)file.Length - offset)
            throw new InvalidDataException("CHD data extends beyond the image.");
        file.Seek((long)offset, SeekOrigin.Begin);
        while (!output.IsEmpty)
        {
            _cancellation.ThrowIfCancellationRequested();
            int read = file.Read(output);
            if (read == 0) throw new InvalidDataException("Truncated CHD image.");
            output = output[read..];
        }
    }
    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
    private static ulong U64(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt64BigEndian(data[offset..]);
    private static int U24(ReadOnlySpan<byte> data) => (data[0] << 16) | (data[1] << 8) | data[2];
    private static ulong U48(ReadOnlySpan<byte> data) => ((ulong)U24(data) << 24) | (uint)U24(data[3..]);
    private static void Put24(Span<byte> data, uint value)
    { data[0] = (byte)(value >> 16); data[1] = (byte)(value >> 8); data[2] = (byte)value; }
    private static void Put48(Span<byte> data, ulong value)
    { Put24(data, (uint)(value >> 24)); Put24(data[3..], (uint)value); }

    private void ReadHeader()
    {
        Span<byte> header = stackalloc byte[124];
        ReadAt(0, header);
        if (!header[..8].SequenceEqual("MComprHD"u8)) throw new InvalidDataException("Invalid CHD signature.");
        _version = (int)U32(header, 12);
        int size = _version switch { 1 => 76, 2 => 80, 3 => 120, 4 => 108, 5 => 124, _ => 0 };
        if (size == 0 || U32(header, 8) != size) throw new InvalidDataException("Invalid CHD header version or size.");
        ulong logicalBytes, mapOffset;
        if (_version == 5)
        {
            for (int i = 0; i < 4; i++) _compression[i] = U32(header, 16 + i * 4);
            logicalBytes = U64(header, 32); mapOffset = U64(header, 40); _metadataOffset = U64(header, 48);
            HunkBytes = checked((int)U32(header, 56)); _unitBytes = checked((int)U32(header, 60));
            if (HunkBytes <= 0 || _unitBytes <= 0) throw new InvalidDataException("Invalid CHD hunk or unit size.");
            _hunkCount = checked((int)((logicalBytes + (uint)HunkBytes - 1) / (uint)HunkBytes));
            if (header.Slice(104, 20).IndexOfAnyExcept((byte)0) >= 0) throw new InvalidDataException("This CHD requires a parent image.");
        }
        else
        {
            _compression[0] = U32(header, 20);
            if (_compression[0] > 2) throw new InvalidDataException("Unsupported legacy CHD codec.");
            if ((U32(header, 16) & 1) != 0) throw new InvalidDataException("This CHD requires a parent image.");
            _hunkCount = checked((int)U32(header, _version < 3 ? 28 : 24)); mapOffset = (uint)size;
            if (_version < 3)
            {
                uint sectorBytes = _version == 1 ? 512 : U32(header, 76);
                HunkBytes = checked((int)(sectorBytes * U32(header, 24)));
                logicalBytes = (ulong)U32(header, 32) * U32(header, 36) * U32(header, 40) * sectorBytes;
            }
            else
            {
                logicalBytes = U64(header, 28); _metadataOffset = U64(header, 36);
                HunkBytes = checked((int)U32(header, _version == 3 ? 76 : 44));
            }
            _unitBytes = HunkBytes;
        }
        if (logicalBytes > 10UL * 1024 * 1024 * 1024 || HunkBytes is <= 0 or > 128 * 1024 * 1024 || _hunkCount <= 0)
            throw new InvalidDataException("Invalid CHD image dimensions.");
        LogicalBytes = checked((long)logicalBytes);
        _compressed = new byte[HunkBytes];
        if (_version == 5) ReadMap5(mapOffset); else ReadLegacyMap(mapOffset);
        InitializeCodecs();
    }

    private void ReadLegacyMap(ulong offset)
    {
        int entrySize = _version < 3 ? 8 : 16;
        _legacyMap = new MapEntry[_hunkCount];
        Span<byte> bytes = stackalloc byte[16];
        for (int i = 0; i < _hunkCount; i++, offset += (uint)entrySize)
        {
            ReadAt(offset, bytes[..entrySize]);
            ulong block = U64(bytes, 0);
            MapEntry entry = _version < 3
                ? new() { Offset = block & 0x00000fffffffffffUL, Length = (int)(block >> 44), Type = (byte)((block >> 44) == (uint)HunkBytes ? 2 : 1) }
                : new() { Offset = block, Length = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]) | bytes[14] << 16, Type = (byte)(bytes[15] & 15) };
            if (entry.Type is 1 or 2 && (entry.Offset > (ulong)_file!.Length || (ulong)entry.Length > (ulong)_file.Length - entry.Offset))
                throw new InvalidDataException("Invalid CHD hunk map entry.");
            _legacyMap[i] = entry;
        }
        ReadAt(offset, bytes[..entrySize]);
        if (!bytes[..entrySize].SequenceEqual("EndOfListCookie\0"u8[..entrySize])) throw new InvalidDataException("Invalid CHD map cookie.");
    }

    public string? GetMetadata(string tag, int index)
    {
        if (tag.Length != 4 || index < 0) throw new ArgumentOutOfRangeException();
        uint expected = ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | tag[3];
        ulong offset = _metadataOffset;
        Span<byte> header = stackalloc byte[16];
        // Links can run backwards. Bound their count without allocating a visited set.
        long remaining = _file!.Length / 16 + 1;
        while (offset != 0 && remaining-- > 0)
        {
            ReadAt(offset, header);
            if (U32(header, 0) == expected && index-- == 0)
            {
                int length = (int)(U32(header, 4) & 0xffffff);
                byte[] data = new byte[length]; ReadAt(offset + 16, data);
                int end = data.AsSpan().IndexOf((byte)0);
                return Encoding.ASCII.GetString(data.AsSpan(0, end < 0 ? data.Length : end));
            }
            offset = U64(header, 8);
        }
        if (offset != 0) throw new InvalidDataException("Cyclic CHD metadata chain.");
        return null;
    }

    public void ReadHunk(int number, Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_file is null, this);
        if (destination.Length < HunkBytes) throw new ArgumentException("CHD hunk buffer is too short.", nameof(destination));
        destination = destination[..HunkBytes];
        int remaining = _hunkCount;
        while (true)
        {
            _cancellation.ThrowIfCancellationRequested();
            if ((uint)number >= (uint)_hunkCount || remaining-- == 0) throw new InvalidDataException("Invalid or cyclic CHD hunk reference.");
            if (_version < 5)
            {
                MapEntry entry = _legacyMap[number];
                switch (entry.Type)
                {
                    case 1:
                        Decompress(_compression[0], ReadCompressed(entry.Offset, entry.Length), destination); return;
                    case 2: ReadAt(entry.Offset, destination); return;
                    case 3:
                        Span<byte> pattern = stackalloc byte[8]; BinaryPrimitives.WriteUInt64BigEndian(pattern, entry.Offset);
                        for (int i = 0; i < destination.Length; i++) destination[i] = pattern[i & 7];
                        return;
                    case 4: number = checked((int)entry.Offset); continue;
                    default: throw new InvalidDataException("Invalid CHD hunk type or missing parent.");
                }
            }
            if (_compression[0] == 0)
            {
                ulong offset = (ulong)U32(_map, number * 4) * (uint)HunkBytes;
                if (offset == 0) destination.Clear(); else ReadAt(offset, destination);
                return;
            }
            ReadOnlySpan<byte> map = _map.AsSpan(number * 12, 12);
            ulong blockOffset = U48(map[4..]);
            int type = map[0];
            if (type == 5) { number = checked((int)blockOffset); continue; }
            if (type <= 3) Decompress(_compression[type], ReadCompressed(blockOffset, U24(map[1..])), destination);
            else if (type == 4) ReadAt(blockOffset, destination);
            else throw new InvalidDataException("Invalid CHD hunk type or missing parent.");
            if (Crc16(destination) != BinaryPrimitives.ReadUInt16BigEndian(map[10..])) throw new InvalidDataException("CHD hunk CRC mismatch.");
            return;
        }
    }
    private ReadOnlySpan<byte> ReadCompressed(ulong offset, int length)
    {
        if ((uint)length > _compressed.Length) throw new InvalidDataException("Invalid CHD compressed length.");
        ReadAt(offset, _compressed.AsSpan(0, length)); return _compressed.AsSpan(0, length);
    }
    private static readonly ushort[] Crc16Table = MakeCrc16Table();
    private static ushort[] MakeCrc16Table()
    {
        ushort[] result = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            int value = i << 8;
            for (int bit = 0; bit < 8; bit++) value = (value << 1) ^ ((value & 0x8000) != 0 ? 0x1021 : 0);
            result[i] = (ushort)value;
        }
        return result;
    }
    private static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xffff;
        foreach (byte value in data) crc = (ushort)((crc << 8) ^ Crc16Table[(crc >> 8) ^ value]);
        return crc;
    }
    public void Dispose()
    {
        Stream? file = _file;
        try { DisposeCodecs(); }
        finally
        {
            try { if (!_leaveOpen) file?.Dispose(); }
            finally { _file = null; _map = []; _legacyMap = []; _compressed = []; }
        }
    }
}
