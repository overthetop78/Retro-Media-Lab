using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Vue empruntée du bloc de défauts, figure 6-6 ; aucune copie ni allocation par entrée.</summary>
public readonly ref struct Acb4000FormatDefects
{
    private readonly ReadOnlySpan<byte> _entries;
    public int Count => _entries.Length / C.DefectEntryLength;

    private Acb4000FormatDefects(ReadOnlySpan<byte> entries) => _entries = entries;

    public (int Cylinder, byte Head, uint BytePosition) this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            var entry = _entries.Slice(index * C.DefectEntryLength, C.DefectEntryLength);
            return ((int)(BinaryPrimitives.ReadUInt32BigEndian(entry) >> C.CylinderHeadShift),
                entry[C.DefectHeadOffset], BinaryPrimitives.ReadUInt32BigEndian(entry[C.DefectBytePositionOffset..]));
        }
    }

    /// <summary>Détermine la réception requise après les quatre octets d'en-tête.</summary>
    public static bool TryReadLength(ReadOnlySpan<byte> header, out int totalLength)
    {
        totalLength = 0;
        if (header.Length < C.DefectHeaderLength || header[0] != 0 || header[1] != 0) return false;
        var length = BinaryPrimitives.ReadUInt16BigEndian(header[C.DefectLengthOffset..]);
        if (length >= C.DefectBufferLength || length % C.DefectEntryLength != 0) return false;
        totalLength = C.DefectHeaderLength + length;
        return totalLength <= C.DefectBufferLength;
    }

    public static bool TryDecode(ReadOnlySpan<byte> bytes, Acb4000DriveParameters parameters,
        out Acb4000FormatDefects defects)
    {
        defects = default;
        if (!parameters.IsValid || !TryReadLength(bytes, out var length) || length != bytes.Length) return false;
        var entries = bytes[C.DefectHeaderLength..];
        ulong previous = 0;
        for (var offset = 0; offset < entries.Length; offset += C.DefectEntryLength)
        {
            var entry = entries.Slice(offset, C.DefectEntryLength);
            var cylinder = BinaryPrimitives.ReadUInt32BigEndian(entry) >> C.CylinderHeadShift;
            var head = entry[C.DefectHeadOffset];
            var position = BinaryPrimitives.ReadUInt64BigEndian(entry);
            var bytePosition = BinaryPrimitives.ReadUInt32BigEndian(entry[C.DefectBytePositionOffset..]);
            if (cylinder >= parameters.Cylinders || head >= parameters.Heads || position < previous
                || cylinder == 0 && head == 0 || bytePosition >= C.TrackBytePositionLimit) return false;
            previous = position;
        }
        defects = new(entries);
        return true;
    }
}
