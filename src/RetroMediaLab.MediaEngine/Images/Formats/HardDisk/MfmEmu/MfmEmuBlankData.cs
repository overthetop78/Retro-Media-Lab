using System.Buffers.Binary;
using RetroMediaLab.MediaFileSystems.Interfaces;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Source virtuelle d'un EMU sans transitions ni champs de secteurs déjà formatés.</summary>
internal sealed class MfmEmuBlankData(MfmEmuHeader header) : IMediaRandomAccessData
{
    public long Length { get; } = checked(header.EndMarkerOffset + C.MinimumTrackHeaderLength);

    public ValueTask ReadExactlyAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset > Length || destination.Length > Length - offset) throw new ArgumentOutOfRangeException(nameof(destination));
        if (destination.IsEmpty) return ValueTask.CompletedTask;
        var span = destination.Span;
        span.Clear();
        CopyOverlap(header.Bytes.Span, 0, span, offset);
        var start = Math.Max(offset, header.HeaderLength);
        var end = Math.Min(offset + destination.Length, header.EndMarkerOffset);
        Span<byte> prefix = stackalloc byte[C.MinimumTrackHeaderLength];
        BinaryPrimitives.WriteUInt32LittleEndian(prefix, C.TrackMarker);
        if (end > start)
        {
            var first = (start - header.HeaderLength) / header.TrackRecordLength;
            var last = (end - 1 - header.HeaderLength) / header.TrackRecordLength;
            for (var track = first; track <= last; track++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(prefix[C.TrackCylinderOffset..], (int)(track / header.Heads));
                BinaryPrimitives.WriteInt32LittleEndian(prefix[C.TrackHeadOffset..], (int)(track % header.Heads));
                CopyOverlap(prefix, header.HeaderLength + track * header.TrackRecordLength, span, offset);
            }
        }
        BinaryPrimitives.WriteInt32LittleEndian(prefix[C.TrackCylinderOffset..], C.EndAddress);
        BinaryPrimitives.WriteInt32LittleEndian(prefix[C.TrackHeadOffset..], C.EndAddress);
        CopyOverlap(prefix, header.EndMarkerOffset, span, offset);
        return ValueTask.CompletedTask;
    }

    private static void CopyOverlap(ReadOnlySpan<byte> source, long sourceOffset, Span<byte> destination, long destinationOffset)
    {
        var start = Math.Max(sourceOffset, destinationOffset);
        var end = Math.Min(sourceOffset + source.Length, destinationOffset + destination.Length);
        if (end <= start) return;
        source.Slice((int)(start - sourceOffset), (int)(end - start)).CopyTo(destination[(int)(start - destinationOffset)..]);
    }
}
