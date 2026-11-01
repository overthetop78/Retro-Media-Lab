using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Conversion;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Interfaces.Conversion;

/// <summary>Transforms an in-memory media representation without writing an output file.</summary>
public interface IMediaRepresentationConverter
{
    string Id { get; }

    IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; }

    IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds { get; }

    bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind);

    Task<MediaRepresentationConversionResult> ConvertAsync(
        MediaImageDocument source,
        string targetFormatId,
        MediaRepresentationKind targetRepresentationKind,
        IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
