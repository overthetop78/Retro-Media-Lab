using System.Buffers.Binary;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
/// <summary>Lit les images sectorielles Dave Dunfield ImageDisk.</summary>
public sealed class ImdReader : IMediaImageReader
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = new[]
    {
        DiskImageFormatIds.Imd,
        DiskImageFormatIds.EpsonQx10_320,
        DiskImageFormatIds.EpsonQx10_396,
        DiskImageFormatIds.EpsonQx10_399,
        DiskImageFormatIds.EpsonQx10_400,
        DiskImageFormatIds.EpsonQx10Booter,
        DiskImageFormatIds.EpsonQx10Logo
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> SupportedExtensions = new[]
    {
        DiskImageFileExtensions.Imd
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyList<ReadOnlyMemory<byte>> SupportedSignatures = [ImdFormat.Signature.ToArray()];
    private static readonly IReadOnlySet<MediaKind> SupportedMediaKinds = new[]
    {
        MediaKind.Floppy
    }.ToFrozenSet();
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[]
    {
        MediaRepresentationKind.Sectors
    }.ToFrozenSet();
    IReadOnlySet<string> IMediaImageReader.FormatIds => SupportedFormatIds;

    IReadOnlySet<string> IMediaImageReader.Extensions => SupportedExtensions;

    IReadOnlyList<ReadOnlyMemory<byte>> IMediaImageReader.Signatures => SupportedSignatures;

    IReadOnlySet<string> IMediaImageReader.AssociatedFileExtensions => FrozenSet<string>.Empty;

    IReadOnlySet<MediaKind> IMediaImageReader.MediaKinds => SupportedMediaKinds;

    IReadOnlySet<MediaRepresentationKind> IMediaImageReader.RepresentationKinds => SupportedRepresentationKinds;

    bool IMediaImageReader.SupportsFormatId(string formatId) => SupportedFormatIds.Contains(formatId) || formatId.StartsWith(DiskImageFormatIds.EpsonQx10Prefix, StringComparison.OrdinalIgnoreCase);
    /// <summary>Lit un fichier ImageDisk et construit son image sectorielle.</summary>
    public async Task<SectorImage> ReadAsync(string path, CancellationToken cancellationToken = default) => (await ReadDetailedAsync(path, cancellationToken).ConfigureAwait(false)).SectorImage;
    /// <summary>Lit un fichier ImageDisk en conservant ses pistes, modes, cartes et états.</summary>
    public async Task<ImdImage> ReadDetailedAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return ReadDetailed(data, cancellationToken);
    }

    /// <summary>Analyse une séquence ImageDisk et construit son image sectorielle.</summary>
    internal static SectorImage Read(ReadOnlySpan<byte> data, CancellationToken cancellationToken = default) => ReadDetailed(data, cancellationToken).SectorImage;
    /// <summary>Analyse une séquence ImageDisk en conservant les informations réinscriptibles.</summary>
    public static ImdImage ReadDetailed(ReadOnlySpan<byte> data, CancellationToken cancellationToken = default)
    {
        var offset = FindTrackDataOffset(data, out var comment);
        var tracks = new List<ImdTrack>();
        while (offset < data.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var header = ReadTrackHeader(data, ref offset);
            var numbers = ReadByteMap(data, ref offset, header.SectorCount, ImdSection.SectorNumberMap);
            var cylinders = header.HeadFlags.HasFlag(ImdHeadFlags.HasCylinderMap) ? ReadByteMap(data, ref offset, header.SectorCount, ImdSection.CylinderMap) : null;
            var heads = header.HeadFlags.HasFlag(ImdHeadFlags.HasHeadMap) ? ReadByteMap(data, ref offset, header.SectorCount, ImdSection.HeadMap) : null;
            var sizes = ReadSectorSizes(data, ref offset, header.SectorCount, header.SectorSizeCode);
            var sectors = new List<ImdSector>(header.SectorCount);
            for (var index = 0; index < header.SectorCount; index++)
            {
                EnsureAvailable(data, offset, ImdLayout.MapEntrySize, ImdSection.SectorRecord);
                var recordType = (ImdSectorRecordType)data[offset++];
                if (!Enum.IsDefined(recordType))
                    throw ImdExceptions.InvalidRecordType((byte)recordType);
                var bytes = ReadSectorRecord(data, ref offset, recordType, sizes[index]);
                sectors.Add(new(cylinders?[index] ?? header.Cylinder, heads?[index] ?? checked((byte)header.Head), numbers[index], sizes[index], recordType, bytes));
            }

            tracks.Add(new(header.Mode, header.Cylinder, checked((byte)header.Head), sectors));
        }

        return new(comment, tracks, BuildImage(tracks));
    }

    async ValueTask<bool> IMediaImageReader.CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        if (context.RequestedFormatId is not null && !((IMediaImageReader)this).SupportsFormatId(context.RequestedFormatId))
            return false;
        if (context.Length < ImdFormat.SignatureLength)
            return false;
        var signature = await context.ReadHeaderAsync(ImdFormat.SignatureLength, cancellationToken).ConfigureAwait(false);
        return signature.Span.SequenceEqual(ImdFormat.Signature);
    }

    async Task<MediaImageDocument> IMediaImageReader.ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        var data = await context.ReadBytesAsync(cancellationToken).ConfigureAwait(false);
        var image = ReadDetailed(data.Span, cancellationToken);
        return MediaImageDocumentFactory.CreateFloppySector(context.Source, image.SectorImage, ImdMetadataFunctions.Create(image));
    }

    private static int FindTrackDataOffset(ReadOnlySpan<byte> data, out string comment)
    {
        var commentEnd = data.IndexOf(ImdFormat.CommentTerminator);
        if (commentEnd < ImdFormat.SignatureLength || !data[..ImdFormat.SignatureLength].SequenceEqual(ImdFormat.Signature))
            throw ImdExceptions.MissingSignature(commentEnd, data.Length);
        comment = System.Text.Encoding.ASCII.GetString(data[..commentEnd]);
        return commentEnd + ImdLayout.MapEntrySize;
    }

    private static ImdTrackHeader ReadTrackHeader(ReadOnlySpan<byte> data, ref int offset)
    {
        EnsureAvailable(data, offset, ImdLayout.TrackHeaderSize, ImdSection.TrackHeader);
        var header = data.Slice(offset, ImdLayout.TrackHeaderSize);
        offset += ImdLayout.TrackHeaderSize;
        var mode = (ImdMode)header[ImdLayout.ModeOffset];
        var sectorCount = header[ImdLayout.SectorCountOffset];
        if (!Enum.IsDefined(mode) || sectorCount == 0)
            throw ImdExceptions.InvalidTrackHeader(mode, sectorCount);
        var flags = (ImdHeadFlags)header[ImdLayout.HeadFlagsOffset];
        return new(mode, header[ImdLayout.CylinderOffset], flags, (int)(flags & ImdHeadFlags.HeadMask), sectorCount, header[ImdLayout.SectorSizeCodeOffset]);
    }

    private static byte[] ReadByteMap(ReadOnlySpan<byte> data, ref int offset, int count, ImdSection section)
    {
        EnsureAvailable(data, offset, count, section);
        var map = data.Slice(offset, count).ToArray();
        offset += count;
        return map;
    }

    private static int[] ReadSectorSizes(ReadOnlySpan<byte> data, ref int offset, int count, byte sizeCode)
    {
        if (sizeCode != ImdLayout.ExplicitSectorSizeCode)
        {
            if (sizeCode > ImdLayout.MaximumExponentialSizeCode)
                throw ImdExceptions.InvalidSizeCode(sizeCode);
            return Enumerable.Repeat(ImdLayout.BaseSectorSize << sizeCode, count).ToArray();
        }

        var length = count * ImdLayout.SectorSizeMapEntrySize;
        EnsureAvailable(data, offset, length, ImdSection.SectorSizeMap);
        var sizes = new int[count];
        for (var index = 0; index < count; index++)
            sizes[index] = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + index * ImdLayout.SectorSizeMapEntrySize, ImdLayout.SectorSizeMapEntrySize));
        offset += length;
        return sizes;
    }

    private static byte[] ReadSectorRecord(ReadOnlySpan<byte> data, ref int offset, ImdSectorRecordType type, int size)
    {
        if (!type.HasData())
            return new byte[size];
        if (type.IsCompressed())
        {
            EnsureAvailable(data, offset, 1, ImdSection.CompressedValue);
            return Enumerable.Repeat(data[offset++], size).ToArray();
        }

        EnsureAvailable(data, offset, size, ImdSection.SectorData);
        var bytes = data.Slice(offset, size).ToArray();
        offset += size;
        return bytes;
    }

    internal static SectorImage BuildImage(IReadOnlyList<ImdTrack> tracks)
    {
        var ordered = tracks.SelectMany(track => track.Sectors.Select((sector, order) => (Track: track, Sector: sector, Order: order)))
            .OrderBy(item => item.Track.Cylinder).ThenBy(item => item.Track.Head).ThenBy(item => item.Sector.Number).ToArray();
        var sectors = ordered.Select(item => item.Sector).ToArray();
        if (sectors.Length == 0)
            throw ImdExceptions.NoSectors();
        var blockSize = sectors.GroupBy(sector => sector.Size).OrderByDescending(group => group.Count()).First().Key;
        var cylinders = tracks.Max(track => track.Cylinder) + 1;
        var heads = tracks.Max(track => track.Head) + 1;
        var sectorsPerTrack = tracks.Max(track => track.Sectors.Count);
        var blocks = ordered.Select((item, logical) => (item.Track, item.Sector, item.Order, Logical: logical))
            .Where(item => item.Sector.RecordType.HasData()).Select(item => new SectorBlock(item.Logical,
            new(item.Track.Cylinder, item.Track.Head, item.Sector.Number),
            item.Sector.Data,
            item.Sector.RecordType.IsIntegrityValid(),
            DiagnosticCode: (byte)item.Sector.RecordType)
            {
                RecordedAddress = new(item.Sector.Cylinder, item.Sector.Head, item.Sector.Number),
                RecordedTrackOrder = item.Order,
                DataIntegrityValid = item.Sector.RecordType.IsIntegrityValid(),
                DeletedDataMark = item.Sector.RecordType.IsDeleted()
            }).ToArray();
        var descriptors = sectors.Select(sector => new EpsonQx10SectorDescriptor(sector.Cylinder, sector.Head, sector.Number, sector.Size)).ToArray();
        var addressesMatch = ordered.All(item => item.Sector.Cylinder == item.Track.Cylinder && item.Sector.Head == item.Track.Head);
        var formatId = addressesMatch && EpsonQx10FormatDetector.TryDetect(descriptors, out var detected) ? detected : DiskImageFormatIds.Imd;
        return new(formatId, blockSize, cylinders, heads, sectorsPerTrack, blocks, sectors.Any(sector => sector.Size != blockSize), sectors.Sum(sector => (long)sector.Size), ordered.Length);
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> data, int offset, int count, ImdSection section)
    {
        if (offset < 0 || count < 0 || offset > data.Length - count)
            throw ImdExceptions.TruncatedSection(section, offset, count, Math.Max(0, data.Length - offset));
    }

    private readonly record struct ImdTrackHeader(ImdMode Mode, byte Cylinder, ImdHeadFlags HeadFlags, int Head, int SectorCount, byte SectorSizeCode);
}
