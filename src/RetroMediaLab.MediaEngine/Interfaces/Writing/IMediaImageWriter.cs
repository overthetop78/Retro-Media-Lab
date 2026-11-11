using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Interfaces.Writing;

/// <summary>Writes compatible media representations to one declared target format.</summary>
public interface IMediaImageWriter
{
    string Id { get; }

    IReadOnlySet<string> FormatIds { get; }

    IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; }

    IReadOnlySet<string> ProducedFileExtensions { get; }

    bool ProducesMultipleFiles { get; }

    /// <summary>Vérifie le couple cible indépendamment de la représentation source à convertir.</summary>
    bool SupportsTarget(string targetFormatId, string targetExtension) => FormatIds.Contains(targetFormatId, StringComparer.OrdinalIgnoreCase)
        && ProducedFileExtensions.Contains(targetExtension, StringComparer.OrdinalIgnoreCase);

    bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension);

    Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId, CancellationToken cancellationToken = default);
}
