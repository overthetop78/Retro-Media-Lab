using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Contracts.Explorer;

namespace RetroMediaLab.MediaAudit;

internal static class TemporaryNamedMediaFormatIdentification
{
    private const string AppleInformXzipId = "apple-inform-xzip";

    public static void ThrowIfIdentificationIsRequired(MediaImageDocument document, ExploredMediaImage explored)
    {
        var provisionalRule = explored.Volumes.Any(volume => string.Equals(volume.FileSystem?.FileSystemId, AppleInformXzipId, StringComparison.Ordinal)) ? "AppleInformXzipFileSystemReader" : null;
        if (provisionalRule is null)
            return;

        var internalFiles = explored.Volumes.Where(volume => volume.FileSystem is not null).SelectMany(volume => RetroMediaLab.MediaAudit.MediaAuditTree.Flatten(volume.FileSystem!.Entries, entry => entry.Children)).Where(entry => entry.Kind == FileSystemEntryKind.File).Select(entry => entry.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        throw new InvalidDataException(
            $"Temporary media-format identification required: media='{document.Source.PrimaryPath}', " +
            $"format='{document.FormatId}', provisionalRule='{provisionalRule}', " +
            $"internalFiles=[{string.Join(", ", internalFiles)}].");
    }
}
