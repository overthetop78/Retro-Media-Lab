using MediaSourceDescriptor = RetroMediaLab.MediaEngine.Contracts.MediaSourceDescriptor;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Inspection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Constants;
using MediaExplorer = RetroMediaLab.MediaEngine.Images.Reading.MediaExplorer;
using System.IO;

namespace RetroMediaLab.MediaEngine.Images.Reading;

/// <summary>Loads one media source once and derives every exploration result from the loaded document.</summary>
public sealed class MediaOpeningAnalysisService(MediaImageReadingService reader, DiskImageExplorer diskExplorer, MediaExplorer mediaExplorer)
{
    public async Task<MediaOpeningAnalysisResult> AnalyzeAsync(
        string path,
        string? requestedFormatId = null,
        IProgress<ScpExplorationProgress>? scpProgress = null,
        Action<MediaExplorationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        bool includeFileSystems = true,
        Action<ScpImage>? scpImageLoaded = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        progress?.Invoke(new(MediaExplorationProgressStage.ReadingMedia, Path.GetFileName(path), 12));

        var source = new MediaSourceDescriptor(path, [], RequestedFormatId: requestedFormatId);
        MediaImageDocument document;
        try
        {
            document = await reader.ReadAsync(source, cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException) when (requestedFormatId is not null)
        {
            document = await reader.ReadAsync(source with { RequestedFormatId = null }, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        document = await RetroMediaLab.MediaEngine.Functions.CassetteBlockFunctions.IndexAsync(document, cancellationToken).ConfigureAwait(false);
        if (!includeFileSystems)
            return new(document, null, null);

        progress?.Invoke(new(MediaExplorationProgressStage.ReadingFileSystem, document.FormatId, 45, document.MediaKind));

        var diskTask = diskExplorer.ExploreAsync(document, requestedFormatId, scpProgress, cancellationToken, scpImageLoaded);
        var mediaTask = document.Representation is FluxMediaImageRepresentation ? ExploreDecodedFluxAsync(document, diskTask, cancellationToken) : mediaExplorer.ExploreAsync(document, cancellationToken).AsTask();
        await Task.WhenAll(diskTask, mediaTask).ConfigureAwait(false);

        progress?.Invoke(new(MediaExplorationProgressStage.RecognizingFiles, document.FormatId, 94, document.MediaKind));
        return new(document, await diskTask.ConfigureAwait(false), await mediaTask.ConfigureAwait(false));
    }

    private async Task<ExploredMediaImage> ExploreDecodedFluxAsync(MediaImageDocument source, Task<ExploredDiskImage> diskTask, CancellationToken cancellationToken)
    {
        var decoded = await diskTask.ConfigureAwait(false);
        var document = decoded.Image.FormatId.Equals(DiskImageFormatIds.Unknown, StringComparison.OrdinalIgnoreCase) ? source : new MediaImageDocument(source.Source,
            decoded.Image.FormatId, source.MediaKind, new SectorMediaImageRepresentation(decoded.Image), [], source.Diagnostics, source.Metadata);
        return await mediaExplorer.ExploreAsync(document, cancellationToken).ConfigureAwait(false);
    }
}
