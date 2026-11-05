using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>En-tête possédé ; conserve les octets supplémentaires des révisions mineures futures.</summary>
internal sealed class MfmEmuHeader
{
    private MfmEmuHeader() { }
    public uint Version { get; private init; }
    public int HeaderLength { get; private init; }
    public int TrackDataLength { get; private init; }
    public int TrackHeaderLength { get; private init; }
    public int Cylinders { get; private init; }
    public int Heads { get; private init; }
    public uint SampleRate { get; private init; }
    public uint StartNanoseconds { get; private init; }
    public ReadOnlyMemory<byte> Bytes { get; private init; }
    private int CommandOffset { get; init; }
    private int CommandLength { get; init; }
    private int NoteOffset { get; init; }
    private int NoteLength { get; init; }
    public ReadOnlyMemory<byte> CommandLine => Bytes.Slice(CommandOffset, CommandLength);
    public ReadOnlyMemory<byte> Note => Bytes.Slice(NoteOffset, NoteLength);
    public long TrackRecordLength => (long)TrackHeaderLength + TrackDataLength;
    public long EndMarkerOffset => HeaderLength + (long)Cylinders * Heads * TrackRecordLength;

    public long TrackOffset(int cylinder, int head)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cylinder);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cylinder, Cylinders);
        ArgumentOutOfRangeException.ThrowIfNegative(head);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(head, Heads);
        return HeaderLength + ((long)cylinder * Heads + head) * TrackRecordLength;
    }

    public bool TryReadTrackAddress(ReadOnlySpan<byte> bytes, out int cylinder, out int head, out bool end)
    {
        cylinder = head = 0;
        end = false;
        if (bytes.Length < C.MinimumTrackHeaderLength || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != C.TrackMarker)
            return false;
        var c = BinaryPrimitives.ReadInt32LittleEndian(bytes[C.TrackCylinderOffset..]);
        var h = BinaryPrimitives.ReadInt32LittleEndian(bytes[C.TrackHeadOffset..]);
        if (c == C.EndAddress && h == C.EndAddress)
        {
            cylinder = head = C.EndAddress;
            end = true;
            return true;
        }
        if (bytes.Length < TrackHeaderLength || c < 0 || c >= Cylinders || h < 0 || h >= Heads) return false;
        cylinder = c;
        head = h;
        return true;
    }

    public static bool TryDecode(ReadOnlySpan<byte> source, [NotNullWhen(true)] out MfmEmuHeader? header)
    {
        header = null;
        if (source.Length < C.FixedHeaderLength || !source[..C.Signature.Length].SequenceEqual(C.Signature)) return false;
        var version = Read(source, C.VersionOffset);
        var major = version & C.MajorMask;
        if ((version & C.TypeMask) != C.FileType || major < C.FirstMajor || major > C.LatestMajor
            || (version & C.ReservedVersionMask) != 0) return false;
        var headerLength = Read(source, C.HeaderLengthOffset);
        var dataLength = Read(source, C.TrackDataLengthOffset);
        var trackHeaderLength = Read(source, C.TrackHeaderLengthOffset);
        var cylinders = Read(source, C.CylinderCountOffset);
        var heads = Read(source, C.HeadCountOffset);
        var rate = Read(source, C.SampleRateOffset);
        if (headerLength < C.FixedHeaderLength || headerLength > source.Length || dataLength == 0 || dataLength > int.MaxValue
            || dataLength % C.WordLength != 0 || trackHeaderLength < C.MinimumTrackHeaderLength || trackHeaderLength > int.MaxValue
            || cylinders == 0 || cylinders > int.MaxValue || heads == 0 || heads > int.MaxValue || rate == 0) return false;
        try { _ = checked((long)headerLength + checked((long)cylinders * heads * ((long)dataLength + trackHeaderLength)) + C.MinimumTrackHeaderLength); }
        catch (OverflowException) { return false; }
        var bytes = source[..(int)headerLength];
        var cursor = C.FixedHeaderLength;
        var commandOffset = 0;
        var commandLength = 0;
        var noteOffset = 0;
        var noteLength = 0;
        if ((version & (C.TypeMask | C.MajorMask)) >= C.StringVersion
            && (!ReadString(bytes, ref cursor, out commandOffset, out commandLength)
                || !ReadString(bytes, ref cursor, out noteOffset, out noteLength))) return false;
        uint start = 0;
        if (version >= C.StartTimeVersion)
        {
            if (bytes.Length - cursor < C.WordLength) return false;
            start = Read(bytes, cursor);
        }
        header = new()
        {
            Version = version,
            HeaderLength = (int)headerLength,
            TrackDataLength = (int)dataLength,
            TrackHeaderLength = (int)trackHeaderLength,
            Cylinders = (int)cylinders,
            Heads = (int)heads,
            SampleRate = rate,
            StartNanoseconds = start,
            Bytes = bytes.ToArray(),
            CommandOffset = commandOffset,
            CommandLength = commandLength,
            NoteOffset = noteOffset,
            NoteLength = noteLength
        };
        return true;
    }

    public static MfmEmuHeader Create(int cylinders, int heads, int trackDataLength, uint sampleRate,
        uint startNanoseconds = 0, string commandLine = "", string note = "")
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(note);
        if (commandLine.Contains('\0') || note.Contains('\0')) throw new ArgumentException();
        var commandLength = checked(Encoding.UTF8.GetByteCount(commandLine) + C.StringTerminatorLength);
        var noteLength = checked(Encoding.UTF8.GetByteCount(note) + C.StringTerminatorLength);
        var bytes = new byte[checked(C.FixedHeaderLength + C.WordLength * 3 + commandLength + noteLength)];
        C.Signature.CopyTo(bytes);
        Write(bytes, C.VersionOffset, C.CurrentVersion);
        Write(bytes, C.HeaderLengthOffset, (uint)bytes.Length);
        Write(bytes, C.TrackDataLengthOffset, (uint)trackDataLength);
        Write(bytes, C.TrackHeaderLengthOffset, C.MinimumTrackHeaderLength);
        Write(bytes, C.CylinderCountOffset, (uint)cylinders);
        Write(bytes, C.HeadCountOffset, (uint)heads);
        Write(bytes, C.SampleRateOffset, sampleRate);
        var cursor = C.FixedHeaderLength;
        Write(bytes, cursor, (uint)commandLength);
        cursor += C.WordLength;
        Encoding.UTF8.GetBytes(commandLine, bytes.AsSpan(cursor));
        cursor += commandLength;
        Write(bytes, cursor, (uint)noteLength);
        cursor += C.WordLength;
        Encoding.UTF8.GetBytes(note, bytes.AsSpan(cursor));
        cursor += noteLength;
        Write(bytes, cursor, startNanoseconds);
        if (!TryDecode(bytes, out var header)) throw new ArgumentOutOfRangeException(nameof(trackDataLength));
        return header;
    }

    private static uint Read(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void Write(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);

    private static bool ReadString(ReadOnlySpan<byte> bytes, ref int cursor, out int offset, out int length)
    {
        offset = length = 0;
        if (bytes.Length - cursor < C.WordLength) return false;
        var count = Read(bytes, cursor);
        cursor += C.WordLength;
        if (count == 0 || count > bytes.Length - cursor || bytes[cursor + (int)count - C.StringTerminatorLength] != 0) return false;
        offset = cursor;
        length = (int)count - C.StringTerminatorLength;
        cursor += (int)count;
        return true;
    }
}
