using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Sectors;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.BbcDfs;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;

/// <summary>Lit les images BBC DFS SSD et DSD dans leur ordre cylindre, face et secteur à base zéro.</summary>
public sealed class BbcDfsReader : IMediaImageReader
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = BbcDfsGeometry.Supported.Select(geometry => geometry.FormatId).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> SupportedExtensions = new[] { DiskImageFileExtensions.Ssd, DiskImageFileExtensions.Dsd, DiskImageFileExtensions.Adl, DiskImageFileExtensions.Ima, DiskImageFileExtensions.Img }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<MediaKind> SupportedMediaKinds = new[] { MediaKind.Floppy }.ToFrozenSet();
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[] { MediaRepresentationKind.Sectors }.ToFrozenSet();
    private readonly Func<string, CancellationToken, Task<byte[]>> readBytes;

    public BbcDfsReader() : this(File.ReadAllBytesAsync) { }

    internal BbcDfsReader(Func<string, CancellationToken, Task<byte[]>> readBytes)
    {
        this.readBytes = readBytes ?? throw new ArgumentNullException(nameof(readBytes));
    }

    IReadOnlySet<string> IMediaImageReader.FormatIds => SupportedFormatIds;

    IReadOnlySet<string> IMediaImageReader.Extensions => SupportedExtensions;

    IReadOnlyList<ReadOnlyMemory<byte>> IMediaImageReader.Signatures => [];

    IReadOnlySet<string> IMediaImageReader.AssociatedFileExtensions => FrozenSet<string>.Empty;

    IReadOnlySet<MediaKind> IMediaImageReader.MediaKinds => SupportedMediaKinds;

    IReadOnlySet<MediaRepresentationKind> IMediaImageReader.RepresentationKinds => SupportedRepresentationKinds;

    bool IMediaImageReader.SupportsFormatId(string formatId) => SupportedFormatIds.Contains(formatId);

    /// <summary>Charge SSD ou DSD selon l'extension et la taille déclarée par les catalogues, sans supprimer les octets d'une image tronquée.</summary>
    public async Task<SectorImage> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path);
        var data = await readBytes(path, cancellationToken).ConfigureAwait(false);
        return Read(data, extension, cancellationToken);
    }

    async ValueTask<bool> IMediaImageReader.CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.RequestedFormatId is not null && !SupportedFormatIds.Contains(context.RequestedFormatId)) return false;
        if (IsRawAlias(context.Extension))
        {
            var header = await context.ReadHeaderAsync(BbcDfsGeometry.TrackSize + 2 * BbcDfsGeometry.SectorSize, cancellationToken).ConfigureAwait(false);
            var geometry = FindRawGeometry(header.Span, context.Length);
            return geometry is not null && (context.RequestedFormatId is null || geometry.FormatId.Equals(context.RequestedFormatId, StringComparison.OrdinalIgnoreCase));
        }
        if (context.Extension == DiskImageFileExtensions.Adl)
        {
            if (context.Length != BbcDfsGeometry.Dsd80.Capacity
                || context.RequestedFormatId is not null && context.RequestedFormatId != BbcDfsGeometry.Dsd80.FormatId) return false;
            var header = await context.ReadHeaderAsync(BbcDfsGeometry.TrackSize + 2 * BbcDfsGeometry.SectorSize, cancellationToken).ConfigureAwait(false);
            return HasAdlDfsCatalogues(header.Span);
        }
        var kind = GetContainerKind(context.Extension);
        if (kind is null) return false;
        var heads = kind == BbcDfsContainerKind.Ssd ? DiskGeometryConstants.SingleSidedHeadCount : DiskGeometryConstants.DoubleSidedHeadCount;
        var maximumCapacity = BbcDfsGeometry.Supported.Where(geometry => geometry.Heads == heads).Max(geometry => geometry.Capacity);
        var matches = context.Length >= BbcDfsGeometry.SectorSize && context.Length <= maximumCapacity;
        return matches;
    }

    async Task<MediaImageDocument> IMediaImageReader.ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        var data = await context.ReadBytesAsync(cancellationToken).ConfigureAwait(false);
        var image = Read(data.ToArray(), context.Extension, cancellationToken);
        return MediaImageDocumentFactory.CreateFloppySector(context.Source, image);
    }

    private static SectorImage Read(byte[] data, string extension, CancellationToken cancellationToken)
    {
        if (IsRawAlias(extension))
            return BbcDfsLayout.CreateImage(data, FindRawGeometry(data, data.Length) ?? throw BbcDfsExceptions.UnknownExtension(extension), cancellationToken);
        var kind = extension.Equals(DiskImageFileExtensions.Adl, StringComparison.OrdinalIgnoreCase)
            && data.Length == BbcDfsGeometry.Dsd80.Capacity && HasAdlDfsCatalogues(data)
            ? BbcDfsContainerKind.Dsd : GetContainerKind(extension) ?? throw BbcDfsExceptions.UnknownExtension(extension);
        var heads = kind == BbcDfsContainerKind.Ssd ? DiskGeometryConstants.SingleSidedHeadCount : DiskGeometryConstants.DoubleSidedHeadCount;
        var completeLength = data.Length / BbcDfsGeometry.SectorSize * BbcDfsGeometry.SectorSize;
        if (completeLength == 0)
            throw BbcDfsExceptions.IncompleteTrack(data.Length, heads, BbcDfsGeometry.SectorSize);
        var requiredLength = data.Length;
        for (var head = 0; head < heads; head++)
        {
            var offset = head * BbcDfsGeometry.TrackSize + BbcDfsGeometry.SectorSize;
            if (data.Length < offset + BbcDfsGeometry.SectorSize) continue;
            var metadata = data.AsSpan(offset, BbcDfsGeometry.SectorSize);
            var entries = metadata[BbcDfsFileSystemLayout.EntryCountOffset];
            if (entries % BbcDfsFileSystemLayout.EntryPartSize != 0
                || entries > BbcDfsFileSystemLayout.MaximumEntryCount * BbcDfsFileSystemLayout.EntryPartSize) continue;
            var declaredSectors = ((metadata[BbcDfsFileSystemLayout.TotalSectorsHighOffset] & BbcDfsFileSystemLayout.TotalSectorsHighMask) << 8)
                | metadata[BbcDfsFileSystemLayout.TotalSectorsLowOffset];
            if (declaredSectors is 400 or 800)
                requiredLength = Math.Max(requiredLength, declaredSectors * heads * BbcDfsGeometry.SectorSize);
        }
        var geometry = BbcDfsGeometry.Supported.Where(candidate => candidate.Heads == heads && candidate.Capacity >= requiredLength).OrderBy(candidate => candidate.Capacity).FirstOrDefault()
            ?? throw BbcDfsExceptions.UnsupportedCylinderCount(data.Length, (int)Math.Ceiling(data.Length / (double)(BbcDfsGeometry.TrackSize * heads)), heads);
        return BbcDfsLayout.CreateImage(data, geometry, cancellationToken);
    }

    private static BbcDfsContainerKind? GetContainerKind(string extension)
        => extension.Equals(DiskImageFileExtensions.Ssd, StringComparison.OrdinalIgnoreCase) ? BbcDfsContainerKind.Ssd : extension.Equals(DiskImageFileExtensions.Dsd, StringComparison.OrdinalIgnoreCase) ? BbcDfsContainerKind.Dsd : null;

    private static bool IsRawAlias(string extension) => extension.Equals(DiskImageFileExtensions.Ima, StringComparison.OrdinalIgnoreCase)
        || extension.Equals(DiskImageFileExtensions.Img, StringComparison.OrdinalIgnoreCase);

    private static BbcDfsGeometry? FindRawGeometry(ReadOnlySpan<byte> header, long length)
    {
        foreach (var geometry in BbcDfsGeometry.Supported)
            if (geometry.Capacity == length && HasDfsCatalogues(header, geometry)) return geometry;
        return null;
    }

    // Some BBC collections label a DSD image ADL. Require both actual DFS catalogues;
    // the filename and capacity alone must never reinterpret an ADFS image as DFS.
    internal static bool HasAdlDfsCatalogues(ReadOnlySpan<byte> header)
        => HasDfsCatalogues(header, BbcDfsGeometry.Dsd80);

    internal static bool HasDfsCatalogues(ReadOnlySpan<byte> header, BbcDfsGeometry geometry)
    {
        if (header.Length < (geometry.Heads - 1) * BbcDfsGeometry.TrackSize + 2 * BbcDfsGeometry.SectorSize) return false;
        for (var head = 0; head < geometry.Heads; head++)
        {
            var names = header.Slice(head * BbcDfsGeometry.TrackSize, BbcDfsGeometry.SectorSize);
            var metadata = header.Slice(head * BbcDfsGeometry.TrackSize + BbcDfsGeometry.SectorSize, BbcDfsGeometry.SectorSize);
            var total = ((metadata[BbcDfsFileSystemLayout.TotalSectorsHighOffset] & BbcDfsFileSystemLayout.TotalSectorsHighMask) << 8)
                | metadata[BbcDfsFileSystemLayout.TotalSectorsLowOffset];
            var count = metadata[BbcDfsFileSystemLayout.EntryCountOffset];
            if (total != geometry.Cylinders * BbcDfsGeometry.SectorsPerTrack
                || count % BbcDfsFileSystemLayout.EntryPartSize != 0
                || count > BbcDfsFileSystemLayout.MaximumEntryCount * BbcDfsFileSystemLayout.EntryPartSize) return false;
            for (var offset = BbcDfsFileSystemLayout.FirstEntryOffset; offset < count + BbcDfsFileSystemLayout.FirstEntryOffset; offset += BbcDfsFileSystemLayout.EntryPartSize)
            {
                for (var character = 0; character < BbcDfsFileSystemLayout.EntryPartSize; character++)
                    if ((names[offset + character] & BbcDfsFileSystemLayout.CharacterMask) is < 32 or > 126) return false;
                var start = BbcDfsEntryDecoder.StartSector(metadata, offset);
                var length = BbcDfsEntryDecoder.Length(metadata, offset);
                if (start < 2 || start + (length + BbcDfsGeometry.SectorSize - 1) / BbcDfsGeometry.SectorSize > total) return false;
            }
        }
        return true;
    }
}
