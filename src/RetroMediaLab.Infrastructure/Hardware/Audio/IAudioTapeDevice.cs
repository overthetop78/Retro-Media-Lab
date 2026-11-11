using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.Infrastructure.Hardware.Audio;

/// <summary>Owned audio transfer: completion includes stopping the device and releasing its native resources.</summary>
public interface IAudioTapeDevice : IAsyncDisposable
{
    Task<MediaTapeSignal> CaptureAsync(string deviceId, int sampleRate, int sampleCount,
        int channelCount, int channelIndex, CancellationToken cancellationToken);

    Task PlayAsync(string deviceId, MediaTapeSignal signal, int channelCount, int channelIndex,
        CancellationToken cancellationToken);
}
