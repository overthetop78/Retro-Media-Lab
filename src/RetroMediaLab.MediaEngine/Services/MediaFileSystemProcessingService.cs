using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaFileSystems.Functions;
using FileSystemsMediaExplorer = RetroMediaLab.MediaFileSystems.Exploration.MediaExplorer;

namespace RetroMediaLab.MediaEngine.Services;

/// <summary>Relaie les opérations existantes sans exposer MediaFileSystems à l'application.</summary>
internal sealed class MediaFileSystemProcessingService : IMediaFileSystemProcessing
{
    /// <inheritdoc/>
    public int ReadIntOrDefault(IReadOnlyDictionary<string, string> metadata, string key) => MediaMetadataFunctions.ReadIntOrDefault(metadata, key);

    /// <inheritdoc/>
    public IEnumerable<TEntry> EnumerateFiles<TEntry>(TEntry entry, Func<TEntry, bool> isFile, Func<TEntry, IReadOnlyList<TEntry>> children) => FileSystemsMediaExplorer.EnumerateFiles(entry, isFile, children);
}
