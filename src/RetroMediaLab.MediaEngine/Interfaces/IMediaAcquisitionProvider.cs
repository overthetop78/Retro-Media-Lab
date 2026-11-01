using MediaAcquisitionProgress = RetroMediaLab.MediaEngine.Contracts.MediaAcquisitionProgress;
using MediaAcquisitionResult = RetroMediaLab.MediaEngine.Contracts.MediaAcquisitionResult;
using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaEngine.Interfaces;

/// <summary>Acquires neutral data from a physical media device without exposing its implementation type.</summary>
public interface IMediaAcquisitionProvider
{
    string Id { get; }

    IReadOnlySet<MediaKind> SupportedMediaKinds { get; }

    IReadOnlySet<MediaRepresentationKind> OutputRepresentationKinds { get; }

    bool CanAcquire(MediaKind mediaKind, string deviceId);

    Task<MediaAcquisitionResult> AcquireAsync(MediaKind mediaKind, string deviceId, IReadOnlyDictionary<string, string> options, IProgress<MediaAcquisitionProgress>? progress = null, CancellationToken cancellationToken = default);
}
