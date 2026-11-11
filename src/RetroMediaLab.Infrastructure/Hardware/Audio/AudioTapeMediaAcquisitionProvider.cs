using System.Collections.Frozen;
using System.IO;
using RetroMediaLab.Infrastructure.Constants;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.Infrastructure.Hardware.Audio;

/// <summary>Acquires a bounded, explicitly selected audio channel as neutral tape PCM.</summary>
public sealed class AudioTapeMediaAcquisitionProvider(Func<IAudioTapeDevice> deviceFactory) : IMediaAcquisitionProvider
{
    public string Id => AudioTapeMediaOptionKeys.ProviderId;
    public IReadOnlySet<MediaKind> SupportedMediaKinds { get; } = new[] { MediaKind.Tape }.ToFrozenSet();
    public IReadOnlySet<MediaRepresentationKind> OutputRepresentationKinds { get; } = new[] { MediaRepresentationKind.Sequential }.ToFrozenSet();
    public bool CanAcquire(MediaKind mediaKind, string deviceId) => mediaKind == MediaKind.Tape && !string.IsNullOrWhiteSpace(deviceId);

    public async Task<MediaAcquisitionResult> AcquireAsync(MediaKind mediaKind, string deviceId,
        IReadOnlyDictionary<string, string> options, IProgress<MediaAcquisitionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanAcquire(mediaKind, deviceId)) throw new NotSupportedException("An explicit tape audio device is required.");
        var channels = AudioTapeOptions.Read(options);
        var rate = AudioTapeOptions.ReadNumber(options, AudioTapeMediaOptionKeys.SampleRate);
        var count = AudioTapeOptions.ReadNumber(options, AudioTapeMediaOptionKeys.SampleCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        _ = checked(count * sizeof(short));
        cancellationToken.ThrowIfCancellationRequested();
        var device = deviceFactory();
        try
        {
            var signal = await device.CaptureAsync(deviceId, rate, count, channels.ChannelCount, channels.ChannelIndex, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (signal.SampleRate != rate || signal.Samples.Length != count)
                throw new InvalidDataException("The audio capture did not return the requested sample count and rate.");
            var unit = MediaTapePhysicalDataFunctions.CreateUnit(signal);
            progress?.Report(new(1, 1, 1, unit, unit.Metadata));
            return new(MediaKind.Tape, MediaRepresentationKind.Sequential, [unit],
                new Dictionary<string, string>(StringComparer.Ordinal) { [MediaPhysicalMetadataKeys.DeviceId] = deviceId });
        }
        finally { await device.DisposeAsync().ConfigureAwait(false); }
    }
}
