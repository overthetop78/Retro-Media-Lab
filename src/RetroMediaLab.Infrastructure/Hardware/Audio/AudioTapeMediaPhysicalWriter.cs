using System.Collections.Frozen;
using System.IO;
using RetroMediaLab.Infrastructure.Constants;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.Infrastructure.Hardware.Audio;

/// <summary>Restitutes a complete tape PCM plan through an explicitly selected audio channel.</summary>
public sealed class AudioTapeMediaPhysicalWriter(Func<IAudioTapeDevice> deviceFactory) : IMediaPhysicalWriter
{
    public string Id => AudioTapeMediaOptionKeys.ProviderId;
    public IReadOnlySet<MediaKind> SupportedMediaKinds { get; } = new[] { MediaKind.Tape }.ToFrozenSet();

    public bool CanWrite(MediaWritePlan plan, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.MediaKind == MediaKind.Tape && plan.RepresentationKind == MediaRepresentationKind.Sequential
            && !string.IsNullOrWhiteSpace(deviceId) && plan.DataUnits.Count > 0
            && plan.DataUnits.All(unit => unit.Metadata.GetValueOrDefault(MediaPhysicalMetadataKeys.Encoding) == MediaPhysicalEncodingIds.TapePcm16Mono);
    }

    public async Task<MediaPhysicalWriteResult> WriteAsync(MediaWritePlan plan, string deviceId,
        IReadOnlyDictionary<string, string> options, IProgress<MediaPhysicalWriteProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!CanWrite(plan, deviceId)) throw new NotSupportedException("A tape PCM plan and explicit audio device are required.");
        var channels = AudioTapeOptions.Read(options);
        if (!plan.WriteOrder.SequenceEqual(Enumerable.Range(0, plan.DataUnits.Count)))
            throw new InvalidDataException("A tape write plan must contain every PCM fragment in sample order.");
        var signal = MediaTapePhysicalDataFunctions.ReadSignal(plan.DataUnits);
        cancellationToken.ThrowIfCancellationRequested();
        var device = deviceFactory();
        try
        {
            await device.PlayAsync(deviceId, signal, channels.ChannelCount, channels.ChannelIndex, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(plan.DataUnits.Count, plan.DataUnits.Count, signal.Samples.Length, false));
            return new(plan.DataUnits.Count, plan.DataUnits.Count, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A partially played signal is not a completed tape, even when some audio has left the host.
            return new(0, plan.DataUnits.Count, true);
        }
        finally { await device.DisposeAsync().ConfigureAwait(false); }
    }
}
