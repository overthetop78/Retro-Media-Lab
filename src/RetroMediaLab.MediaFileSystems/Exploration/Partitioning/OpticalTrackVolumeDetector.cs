using MediaVolumeOrigins = RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins;
using MediaVolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;
using MediaVolumeDetectionResult = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDetectionResult;
using IMediaImageDocument = RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageDocument;
using IMediaOpticalRepresentation = RetroMediaLab.MediaFileSystems.Interfaces.IMediaOpticalRepresentation;
using RetroMediaLab.MediaFileSystems.Interfaces.Exploration;




namespace RetroMediaLab.MediaFileSystems.Exploration.Partitioning;

/// <summary>Exposes each optical data track as a distinct file-system candidate volume.</summary>
public sealed class OpticalTrackVolumeDetector : IMediaVolumeDetector
{
    public ValueTask<MediaVolumeDetectionResult?> DetectAsync(IMediaImageDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        if (document.Representation is not IMediaOpticalRepresentation optical)
            return new ValueTask<MediaVolumeDetectionResult?>((MediaVolumeDetectionResult?)null);
        var dataTracks = optical.Tracks?.Where(track => !track.IsAudio).ToArray() ?? [];
        if (dataTracks.Length == 0)
            return new ValueTask<MediaVolumeDetectionResult?>(new MediaVolumeDetectionResult([], ["The optical image contains no addressable data track."]));
        var volumes = dataTracks.Select(track => new MediaVolumeDescriptor(
            checked(track.FirstSector * track.UserDataLength),
            checked(track.SectorCount * track.UserDataLength),
            MediaVolumeOrigins.OpticalTrack,
            sessionNumber: track.SessionNumber,
            trackNumber: track.TrackNumber)).ToArray();
        return new ValueTask<MediaVolumeDetectionResult?>(new MediaVolumeDetectionResult(volumes, []));
    }
}
