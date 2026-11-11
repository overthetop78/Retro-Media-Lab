using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

/// <summary>Lit les conteneurs ADF Acorn et Amiga dont la géométrie est déterminée par la taille exacte.</summary>
public sealed class AdfReader : IMediaImageReader
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = new[]
    {
        DiskImageFormatIds.AcornAdfs800,
        DiskImageFormatIds.AcornDosPlus800,
        DiskImageFormatIds.AcornAdfs1600,
        DiskImageFormatIds.AcornAdfs160,
        DiskImageFormatIds.AcornAdfs320,
        DiskImageFormatIds.AcornAdfs640,
        DiskImageFormatIds.AmigaDos,
        DiskImageFormatIds.AmigaDosHighDensity
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> SupportedExtensions = new[]
    {
        DiskImageFileExtensions.Adf, DiskImageFileExtensions.Ads, DiskImageFileExtensions.Adm, DiskImageFileExtensions.Adl,
        DiskImageFileExtensions.AcornAdfs640, DiskImageFileExtensions.AcornDosPlus800
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<MediaKind> SupportedMediaKinds = new[]
    {
        MediaKind.Floppy
    }.ToFrozenSet();
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[]
    {
        MediaRepresentationKind.Sectors
    }.ToFrozenSet();
    private readonly Func<string, CancellationToken, Task<byte[]>> readBytes;

    public AdfReader() : this(File.ReadAllBytesAsync) { }

    internal AdfReader(Func<string, CancellationToken, Task<byte[]>> readBytes)
    {
        this.readBytes = readBytes ?? throw new ArgumentNullException(nameof(readBytes));
    }

    private static readonly int[] AcceptedSizes = AcornAdfGeometry.All.Select(geometry => geometry.Capacity).Concat([AcornAdfGeometry.PaddedCapacity, AmigaAdfGeometry.DoubleDensityCapacity, AmigaAdfGeometry.HighDensityCapacity]).ToArray();

    IReadOnlySet<string> IMediaImageReader.FormatIds => SupportedFormatIds;

    IReadOnlySet<string> IMediaImageReader.Extensions => SupportedExtensions;

    IReadOnlyList<ReadOnlyMemory<byte>> IMediaImageReader.Signatures => [];

    IReadOnlySet<string> IMediaImageReader.AssociatedFileExtensions => FrozenSet<string>.Empty;

    IReadOnlySet<MediaKind> IMediaImageReader.MediaKinds => SupportedMediaKinds;

    IReadOnlySet<MediaRepresentationKind> IMediaImageReader.RepresentationKinds => SupportedRepresentationKinds;

    bool IMediaImageReader.SupportsFormatId(string formatId) => SupportedFormatIds.Contains(formatId);

    /// <summary>Lit le fichier en conservant la numérotation de son profil, dont DOS Plus à base un.</summary>
    public async Task<SectorImage> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await readBytes(path, cancellationToken).ConfigureAwait(false);
        return Read(data, cancellationToken);
    }

    async ValueTask<bool> IMediaImageReader.CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestedFormatMatches = context.RequestedFormatId is null || SupportedFormatIds.Contains(context.RequestedFormatId);
        if (requestedFormatMatches && context.Extension.Equals(DiskImageFileExtensions.Adf, StringComparison.OrdinalIgnoreCase)
            && context.Length < AcornAdfGeometry.HighDensityCapacity && !AcceptedSizes.Contains((int)context.Length))
        {
            var header = await context.ReadHeaderAsync(AcornAdfPartialImage.ProbeLength, cancellationToken).ConfigureAwait(false);
            if (AcornAdfPartialImage.TryDescribe(header.Span, context.Length, out _, out _))
            {
                var bytes = await context.ReadBytesAsync(cancellationToken).ConfigureAwait(false);
                if (AcornAdfPartialImage.TryCreate(bytes.Span, out var partial)
                    && (context.RequestedFormatId is null || partial.FormatId.Equals(context.RequestedFormatId, StringComparison.OrdinalIgnoreCase))) return true;
            }
        }
        var lengthMatches = context.Length <= int.MaxValue && AcceptedSizes.Contains((int)context.Length);
        if (context.Extension.Equals(DiskImageFileExtensions.AcornAdfs640, StringComparison.OrdinalIgnoreCase))
            return context.Length == 655360 && (context.RequestedFormatId is null
                || context.RequestedFormatId.Equals(DiskImageFormatIds.AcornAdfs640, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(context.RequestedFormatId, DiskImageFormatIds.AcornDosPlus800, StringComparison.OrdinalIgnoreCase))
            return context.Length == AcornAdfGeometry.Capacity;
        if (AcornAdfGeometry.BbcGeometries.FirstOrDefault(geometry => geometry.Capacity == context.Length) is { } bbc)
            return requestedFormatMatches && (context.RequestedFormatId is null ? SupportedExtensions.Contains(context.Extension) : context.RequestedFormatId.Equals(bbc.FormatId, StringComparison.OrdinalIgnoreCase));
        return requestedFormatMatches && lengthMatches;
    }

    async Task<MediaImageDocument> IMediaImageReader.ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        var data = await context.ReadBytesAsync(cancellationToken).ConfigureAwait(false);
        var image = Read(data.ToArray(), cancellationToken, context.RequestedFormatId);
        if (data.Length < image.Capacity)
            return MediaImageDocument.CreateUnexplored(context.Source, image.FormatId, MediaKind.Floppy,
                new SectorMediaImageRepresentation(image), [AdfExceptions.InvalidSize(data.Length, [checked((int)image.Capacity)]).Message], new Dictionary<string, string>());
        return MediaImageDocumentFactory.CreateFloppySector(context.Source, image);
    }

    private static SectorImage Read(byte[] data, CancellationToken cancellationToken, string? requestedFormatId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Preserve the established Amiga DD/HD decisions before probing incomplete Acorn images.
        if (data.Length == AmigaAdfGeometry.DoubleDensity.Capacity) return RegularSectorImageBuilder.Create(data, AmigaAdfGeometry.DoubleDensity, cancellationToken);
        if (data.Length == AmigaAdfGeometry.HighDensity.Capacity) return RegularSectorImageBuilder.Create(data, AmigaAdfGeometry.HighDensity, cancellationToken);
        if (AcornAdfPartialImage.TryCreate(data, out var partial)
            && (requestedFormatId is null || partial.FormatId.Equals(requestedFormatId, StringComparison.OrdinalIgnoreCase))) return partial;
        if (AcornAdfGeometry.BbcGeometries.FirstOrDefault(geometry => geometry.Capacity == data.Length) is { } bbc) return AcornAdfLayout.CreateBbc(data, bbc, cancellationToken);
        if (data.Length == AcornAdfGeometry.Capacity)
        {
            var dosPlus = string.Equals(requestedFormatId, DiskImageFormatIds.AcornDosPlus800, StringComparison.OrdinalIgnoreCase)
                || requestedFormatId is null && AcornAdfGeometry.HasDosPlusHeader(data);
            return RegularSectorImageBuilder.Create(data, dosPlus ? AcornAdfGeometry.DosPlus800 : AcornAdfGeometry.Geometry, cancellationToken);
        }
        if (data.Length == AcornAdfGeometry.PaddedCapacity) return RegularSectorImageBuilder.Create(data, AcornAdfGeometry.Geometry, cancellationToken, AcornAdfGeometry.PaddedTrailingByteCount);
        if (data.Length == AcornAdfGeometry.HighDensityCapacity) return RegularSectorImageBuilder.Create(data, AcornAdfGeometry.HighDensityGeometry, cancellationToken);
        throw AdfExceptions.InvalidSize(data.Length, AcceptedSizes);
    }
}
