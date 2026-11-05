using System.IO;
using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Writing;
using RetroMediaLab.MediaEngine.Interfaces.Writing;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Publie les pistes MFM par la transaction commune, y compris vers leur fichier source.</summary>
public sealed class MfmEmuWriter : IMediaImageWriter
{
    public string Id => MediaImageWriterIds.MfmEmu;
    public IReadOnlySet<string> FormatIds { get; } = new[] { HardDiskImageFormatIds.MfmEmu }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<MediaRepresentationKind> RepresentationKinds { get; } = new[] { MediaRepresentationKind.Flux }.ToFrozenSet();
    public IReadOnlySet<string> ProducedFileExtensions { get; } = new[] { DiskImageFileExtensions.Emu }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public bool ProducesMultipleFiles => false;

    public bool CanWrite(MediaImageDocument document, string targetFormatId, string targetExtension) =>
        FormatIds.Contains(targetFormatId) && ProducedFileExtensions.Contains(targetExtension)
        && document.MediaKind == MediaKind.HardDisk && document.Representation is MfmEmuImageRepresentation;

    public Task<IReadOnlyList<string>> WriteAsync(MediaImageDocument document, string outputPath, string targetFormatId,
        CancellationToken cancellationToken = default) => WriteFilesAsync(document, outputPath, targetFormatId, true, false, cancellationToken);

    /// <summary>Publie une nouvelle image sans remplacer un fichier existant, avec réservation physique optionnelle.</summary>
    public Task<IReadOnlyList<string>> CreateAsync(MediaImageDocument document, string outputPath,
        CancellationToken cancellationToken = default, bool preallocate = false) =>
        WriteFilesAsync(document, outputPath, HardDiskImageFormatIds.MfmEmu, false, preallocate, cancellationToken);

    private async Task<IReadOnlyList<string>> WriteFilesAsync(MediaImageDocument document, string outputPath, string targetFormatId,
        bool replaceExisting, bool preallocate, CancellationToken cancellationToken)
    {
        if (!CanWrite(document, targetFormatId, Path.GetExtension(outputPath))) throw new InvalidDataException(MfmEmuFormat.InvalidOutput);
        var representation = (MfmEmuImageRepresentation)document.Representation;
        var path = Path.GetFullPath(outputPath);
        await AtomicMediaFileSetWriter.WriteAsync(new Dictionary<string, Func<Stream, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            [path] = (stream, token) => representation.Container.CopyToAsync(stream, token).AsTask()
        }, cancellationToken, replaceExisting, preallocate
            ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase) { [path] = representation.Container.Length }
            : null).ConfigureAwait(false);
        return [path];
    }
}
