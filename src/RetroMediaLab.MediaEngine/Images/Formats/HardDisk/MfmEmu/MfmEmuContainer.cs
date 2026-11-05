using System.IO;
using System.Buffers;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaFileSystems.Interfaces;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Source empruntée : accès bornés aux mots de piste, sans expansion du disque entier en bits.</summary>
internal sealed class MfmEmuContainer
{
    private readonly IMediaRandomAccessData source;
    private readonly IReadOnlyDictionary<long, ReadOnlyMemory<byte>> changes;

    private MfmEmuContainer(IMediaRandomAccessData source, MfmEmuHeader header,
        IReadOnlyDictionary<long, ReadOnlyMemory<byte>>? changes = null)
    {
        this.source = source;
        Header = header;
        this.changes = changes ?? new Dictionary<long, ReadOnlyMemory<byte>>();
    }

    public MfmEmuHeader Header { get; }
    internal long Length => source.Length;

    internal static MfmEmuContainer CreateBlank(MfmEmuHeader header) => new(new MfmEmuBlankData(header), header);
    internal static MfmEmuContainer CreateFormatted(MfmEmuHeader header, HardDiskGeometry geometry,
        ReadOnlyMemory<byte> logicalPrefix) => new(new MfmEmuFormattedData(header, geometry, logicalPrefix), header);

    public MfmEmuContainer WithTrackBytes(int cylinder, int head, ReadOnlyMemory<byte> bytes)
    {
        var offset = Header.TrackOffset(cylinder, head);
        if (bytes.Length != Header.TrackDataLength) throw new ArgumentException(nameof(bytes));
        var updated = new Dictionary<long, ReadOnlyMemory<byte>>(changes)
        {
            [offset + Header.TrackHeaderLength] = bytes.ToArray()
        };
        return new(source, Header, updated);
    }

    public static async ValueTask<MfmEmuContainer> OpenAsync(IMediaRandomAccessData source, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        token.ThrowIfCancellationRequested();
        if (source.Length < C.FixedHeaderLength) throw new InvalidDataException(C.InvalidHeader);
        var prefix = new byte[C.FixedHeaderLength];
        await source.ReadExactlyAsync(0, prefix, token).ConfigureAwait(false);
        if (!prefix.AsSpan(0, C.Signature.Length).SequenceEqual(C.Signature)) throw new InvalidDataException(C.InvalidHeader);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(prefix.AsSpan(C.HeaderLengthOffset));
        if (length < C.FixedHeaderLength || length > Array.MaxLength || length > source.Length)
            throw new InvalidDataException(C.InvalidHeader);
        var bytes = new byte[(int)length];
        prefix.CopyTo(bytes, 0);
        await source.ReadExactlyAsync(C.FixedHeaderLength, bytes.AsMemory(C.FixedHeaderLength), token).ConfigureAwait(false);
        if (!MfmEmuHeader.TryDecode(bytes, out var header)) throw new InvalidDataException(C.InvalidHeader);
        if (source.Length < C.MinimumTrackHeaderLength || header.EndMarkerOffset != source.Length - C.MinimumTrackHeaderLength)
            throw new InvalidDataException(C.InvalidLength);
        var end = new byte[C.MinimumTrackHeaderLength];
        await source.ReadExactlyAsync(header.EndMarkerOffset, end, token).ConfigureAwait(false);
        if (!header.TryReadTrackAddress(end, out _, out _, out var eof) || !eof)
            throw new InvalidDataException(C.InvalidEndMarker);
        return new(source, header);
    }

    public async ValueTask ReadTrackBytesAsync(int cylinder, int head, int byteOffset, Memory<byte> destination,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var offset = Header.TrackOffset(cylinder, head);
        ArgumentOutOfRangeException.ThrowIfNegative(byteOffset);
        if (byteOffset > Header.TrackDataLength || destination.Length > Header.TrackDataLength - byteOffset)
            throw new ArgumentOutOfRangeException(nameof(destination));
        await ValidateTrackAsync(offset, cylinder, head, new byte[C.MinimumTrackHeaderLength], token).ConfigureAwait(false);
        if (changes.TryGetValue(offset + Header.TrackHeaderLength, out var changed))
        {
            changed.Slice(byteOffset, destination.Length).CopyTo(destination);
            return;
        }
        await source.ReadExactlyAsync(offset + Header.TrackHeaderLength + byteOffset, destination, token).ConfigureAwait(false);
    }

    /// <summary>Valide toutes les adresses avant de recopier, sans fermer ni vider le flux reçu.</summary>
    public async ValueTask CopyToAsync(Stream destination, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException(nameof(destination));
        token.ThrowIfCancellationRequested();
        var prefix = new byte[C.MinimumTrackHeaderLength];
        for (var cylinder = 0; cylinder < Header.Cylinders; cylinder++)
            for (var head = 0; head < Header.Heads; head++)
                await ValidateTrackAsync(Header.TrackOffset(cylinder, head), cylinder, head, prefix, token).ConfigureAwait(false);
        var buffer = ArrayPool<byte>.Shared.Rent(C.CopyBufferLength);
        try
        {
            long offset = 0;
            foreach (var change in changes.OrderBy(change => change.Key))
            {
                await CopyRangeAsync(offset, change.Key - offset, destination, buffer, token).ConfigureAwait(false);
                await destination.WriteAsync(change.Value, token).ConfigureAwait(false);
                offset = change.Key + change.Value.Length;
            }
            await CopyRangeAsync(offset, source.Length - offset, destination, buffer, token).ConfigureAwait(false);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private async ValueTask CopyRangeAsync(long offset, long length, Stream destination, byte[] buffer, CancellationToken token)
    {
        while (length > 0)
        {
            token.ThrowIfCancellationRequested();
            var count = (int)Math.Min(C.CopyBufferLength, length);
            await source.ReadExactlyAsync(offset, buffer.AsMemory(0, count), token).ConfigureAwait(false);
            await destination.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
            offset += count;
            length -= count;
        }
    }

    private async ValueTask ValidateTrackAsync(long offset, int cylinder, int head, Memory<byte> prefix, CancellationToken token)
    {
        await source.ReadExactlyAsync(offset, prefix, token).ConfigureAwait(false);
        if (BinaryPrimitives.ReadUInt32LittleEndian(prefix.Span) != C.TrackMarker
            || BinaryPrimitives.ReadInt32LittleEndian(prefix.Span[C.TrackCylinderOffset..]) != cylinder
            || BinaryPrimitives.ReadInt32LittleEndian(prefix.Span[C.TrackHeadOffset..]) != head)
            throw new InvalidDataException(C.InvalidTrack);
    }
}
