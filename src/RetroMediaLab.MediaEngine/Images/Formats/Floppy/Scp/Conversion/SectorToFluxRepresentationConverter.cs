using RetroMediaLab.MediaEngine.Images.Conversion;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Conversion;

/// <summary>Reconstructs an SCP flux document from an encodable sector representation.</summary>
internal sealed class SectorToFluxRepresentationConverter : IMediaRepresentationConverter
{
    private static readonly IReadOnlySet<MediaRepresentationKind> SourceKinds = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Sectors };
    private static readonly IReadOnlySet<MediaRepresentationKind> TargetKinds = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Flux };
    private readonly SectorImageScpConversionService conversion;

    public SectorToFluxRepresentationConverter(SectorImageScpConversionService conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);
        this.conversion = conversion;
    }

    public string Id => "sectors-to-scp-flux";

    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds => SourceKinds;

    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds => TargetKinds;

    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind)
        => source.Representation is SectorMediaImageRepresentation sectors &&
           (targetFormatId.Equals(DiskImageFormatIds.RawScp, StringComparison.OrdinalIgnoreCase) ||
            targetFormatId.Equals(DiskImageFormatIds.RawHfe, StringComparison.OrdinalIgnoreCase) ||
            targetFormatId.Equals(DiskImageFormatIds.RawHxcStream, StringComparison.OrdinalIgnoreCase)) &&
           targetRepresentationKind == MediaRepresentationKind.Flux &&
           conversion.CanCreate(sectors.Image);

    public Task<MediaRepresentationConversionResult> ConvertAsync(
        MediaImageDocument source,
        string targetFormatId,
        MediaRepresentationKind targetRepresentationKind,
        IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        if (!CanConvert(source, targetFormatId, targetRepresentationKind) ||
            source.Representation is not SectorMediaImageRepresentation sectors)
            throw new NotSupportedException($"Sector format '{source.FormatId}' cannot be reconstructed as flux for '{targetFormatId}'.");

        cancellationToken.ThrowIfCancellationRequested();
        var image = conversion.Create(sectors.Image, cancellationToken);
        var document = MediaImageDocumentFactory.CreateFloppyFlux(source.Source, DiskImageFormatIds.RawScp, ScpProtectedTrackImageAdapter.Create(image), ScpMetadataFunctions.Create(image));
        return Task.FromResult(new MediaRepresentationConversionResult(document, [], ["The generated flux is reconstructed from sectors and does not contain the original physical capture."]));
    }
}
