using System.Collections.ObjectModel;
using MediaVolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Interfaces;
using IMediaImageDocument = RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageDocument;

namespace RetroMediaLab.MediaEngine.Contracts;

/// <summary>Contains a recognized media image, its representation, volumes, diagnostics, and source-provided metadata.</summary>
public sealed class MediaImageDocument : IMediaImageDocument
{
    public static MediaImageDocument CreateUnexplored(
        MediaSourceDescriptor source,
        string formatId,
        MediaKind mediaKind,
        IMediaImageRepresentation representation,
        IReadOnlyList<string> diagnostics,
        IReadOnlyDictionary<string, string> metadata)
        => new(source, formatId, mediaKind, representation, [], diagnostics, metadata);

    public MediaImageDocument(
        MediaSourceDescriptor source,
        string formatId,
        MediaKind mediaKind,
        IMediaImageRepresentation representation,
        IReadOnlyList<MediaVolumeDescriptor> volumes,
        IReadOnlyList<string> diagnostics,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(formatId)) throw new ArgumentException("A recognized format identifier is required.", nameof(formatId));
        ArgumentNullException.ThrowIfNull(representation);
        ArgumentNullException.ThrowIfNull(volumes);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(metadata);

        Source = source;
        FormatId = formatId;
        MediaKind = mediaKind;
        Representation = source.TrackDriveAccesses ? RetroMediaLab.MediaEngine.Functions.MediaDriveBindingFunctions.Bind(source.Drive, representation, source.DriveWriting) : representation;
        FileSystemVolumes = new ReadOnlyCollection<MediaVolumeDescriptor>(volumes.ToArray());
        Volumes = new ReadOnlyCollection<MediaVolumeInfo>(volumes.Select(volume => new MediaVolumeInfo(volume)).ToArray());
        Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToArray());
        Metadata = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(metadata, StringComparer.Ordinal));
    }

    public MediaSourceDescriptor Source { get; }

    public string FormatId { get; }

    public MediaKind MediaKind { get; }

    public IMediaImageRepresentation Representation { get; }

    public IReadOnlyList<MediaVolumeInfo> Volumes { get; }

    internal IReadOnlyList<MediaVolumeDescriptor> FileSystemVolumes { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    RetroMediaLab.MediaFileSystems.Interfaces.IMediaSourceDescriptor IMediaImageDocument.Source => Source;

    IReadOnlyList<MediaVolumeDescriptor> IMediaImageDocument.Volumes => FileSystemVolumes;

    bool IMediaImageDocument.IsTape => MediaKind == MediaKind.Tape;

    bool IMediaImageDocument.IsHardDisk => MediaKind == MediaKind.HardDisk;

    RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageRepresentation IMediaImageDocument.Representation => Representation;
}
