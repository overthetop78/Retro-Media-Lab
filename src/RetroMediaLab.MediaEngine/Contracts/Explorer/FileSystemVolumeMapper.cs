using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.MediaEngine.Contracts.Explorer;

namespace RetroMediaLab.MediaEngine.Contracts.Explorer;

/// <summary>Convertit les volumes entre la lecture interne et les données échangées par MediaEngine.</summary>
public static class FileSystemVolumeMapper
{
    public static FileSystemVolume ConvertVolume(RetroMediaLab.MediaFileSystems.FileSystemVolume volume, string formatId) => ConvertVolume(volume, RetroMediaLab.MediaFileSystems.Exploration.FileSystemEntryAnalyzer.AnalyzeEntries(formatId, volume));

    public static FileSystemVolume ConvertVolume(RetroMediaLab.MediaFileSystems.FileSystemVolume volume, IReadOnlyList<RetroMediaLab.MediaFileSystems.FileSystemEntry>? analyzedEntries = null)
    {
        ArgumentNullException.ThrowIfNull(volume);
        return new FileSystemVolume(
            volume.Name,
            volume.FileSystemId,
            volume.Capacity,
            volume.FreeBytes,
            volume.Created,
            volume.Modified,
            (analyzedEntries ?? volume.Entries).Select(ConvertEntry),
            volume.Warnings,
            volume.FreeSpaceKnown,
            volume.Attributes,
            volume.Bootable,
            volume.DiskNumber,
            volume.DiskCount,
            volume.DiskNumberOrigin,
            volume.FileSystemDisplayName);
    }

    public static RetroMediaLab.MediaFileSystems.FileSystemVolume ToFileSystemsVolume(FileSystemVolume volume)
    {
        ArgumentNullException.ThrowIfNull(volume);
        return new RetroMediaLab.MediaFileSystems.FileSystemVolume(
            volume.Name, volume.FileSystemId, volume.Capacity, volume.FreeBytes,
            volume.Created, volume.Modified, volume.Entries.Select(ToFileSystemsEntry),
            volume.Warnings, volume.FreeSpaceKnown, volume.Attributes, volume.Bootable,
            volume.DiskNumber, volume.DiskCount, volume.DiskNumberOrigin, volume.FileSystemDisplayName);
    }

    private static RetroMediaLab.MediaFileSystems.FileSystemEntry ToFileSystemsEntry(FileSystemEntry entry) =>
        new(
            entry.Name,
            entry.Kind switch
            {
                FileSystemEntryKind.Directory => RetroMediaLab.MediaFileSystems.FileSystemEntryKind.Directory,
                FileSystemEntryKind.File => RetroMediaLab.MediaFileSystems.FileSystemEntryKind.File,
                FileSystemEntryKind.Link => RetroMediaLab.MediaFileSystems.FileSystemEntryKind.Link,
                _ => RetroMediaLab.MediaFileSystems.FileSystemEntryKind.Unknown
            },
            entry.Size, entry.Modified, entry.Comment, entry.RawAttributes,
            entry.StorageReference, entry.MetadataValid, entry.Children.Select(ToFileSystemsEntry),
            entry.Content, entry.NativeTypeId, entry.OccupiedSize, entry.Created, entry.Accessed,
            entry.Attributes, entry.DataValid, entry.SyntheticName, entry.LinkTarget,
            entry.Diagnostics, entry.Metadata);

    private static FileSystemEntry ConvertEntry(RetroMediaLab.MediaFileSystems.FileSystemEntry entry) =>
        new(
            entry.Name,
            entry.Kind switch
            {
                RetroMediaLab.MediaFileSystems.FileSystemEntryKind.Directory => FileSystemEntryKind.Directory,
                RetroMediaLab.MediaFileSystems.FileSystemEntryKind.File => FileSystemEntryKind.File,
                RetroMediaLab.MediaFileSystems.FileSystemEntryKind.Link => FileSystemEntryKind.Link,
                _ => FileSystemEntryKind.Unknown
            },
            entry.Size,
            entry.Modified,
            entry.Comment,
            entry.RawAttributes,
            entry.StorageReference,
            entry.MetadataValid,
            entry.Children.Select(ConvertEntry),
            entry.Content,
            entry.NativeTypeId,
            entry.OccupiedSize,
            entry.Created,
            entry.Accessed,
            entry.Attributes,
            entry.DataValid,
            entry.SyntheticName,
            entry.LinkTarget,
            entry.Diagnostics,
            entry.Metadata)
        {
            Analysis = entry.Analysis is { } analysis ? new FileSystemEntryAnalysis(analysis.CategoryId, analysis.IconId, analysis.TypeResourceKey, analysis.Extension, analysis.ExecutionKindId, analysis.ContentFormatId, analysis.TextEncodingId, analysis.PreviewKindId) : null
        };
}
