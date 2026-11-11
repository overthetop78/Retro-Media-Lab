using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.Infrastructure.Hardware.Audio;

/// <summary>Finite audio transfers on an explicitly selected Windows endpoint.</summary>
/// <remarks>Shared-mode PCM conversion belongs to Windows. This is not a bit-exact analog capture guarantee.
/// Native objects are scoped to each awaited operation; no microphone or default device is opened on construction.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WasapiAudioTapeDevice : IAudioTapeDevice
{
    private bool disposed;

    public Task<MediaTapeSignal> CaptureAsync(string deviceId, int sampleRate, int sampleCount,
        int channelCount, int channelIndex, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(deviceId, sampleRate, channelCount, channelIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleCount);
        _ = checked(sampleCount * sizeof(short));
        // NAudio captures SynchronizationContext at construction. A worker keeps its completion event
        // independent of a UI dispatcher, so cleanup can always await the native thread's termination.
        return Task.Run(() => CaptureCoreAsync(deviceId, sampleRate, sampleCount, channelCount, channelIndex, cancellationToken));
    }

    private static async Task<MediaTapeSignal> CaptureCoreAsync(string deviceId, int rate, int count,
        int channels, int channel, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var enumerator = new MMDeviceEnumerator();
        using var endpoint = enumerator.GetDevice(deviceId);
        if (endpoint.DataFlow != DataFlow.Capture) throw new ArgumentException("A capture endpoint is required.", nameof(deviceId));
        // NAudio 2.3.0 does not dispose its event-sync handle. The non-event mode creates no such handle.
        using var capture = new WasapiCapture(endpoint, false) { WaveFormat = new WaveFormat(rate, 16, channels) };
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var samples = new short[count];
        var position = 0;
        Thread? captureThread = null;
        void Data(object? sender, WaveInEventArgs args)
        {
            try
            {
                var align = checked(channels * sizeof(short));
                if (args.BytesRecorded % align != 0) throw new InvalidDataException("Capture returned an incomplete PCM frame.");
                var frames = Math.Min(args.BytesRecorded / align, count - position);
                for (var frame = 0; frame < frames; frame++)
                    samples[position++] = BinaryPrimitives.ReadInt16LittleEndian(args.Buffer.AsSpan(frame * align + channel * sizeof(short)));
                if (position == count) complete.TrySetResult();
            }
            catch (Exception error) { complete.TrySetException(error); }
        }
        void Stopped(object? sender, StoppedEventArgs args)
        {
            captureThread = Thread.CurrentThread;
            if (args.Exception is { } error) complete.TrySetException(error);
            else if (position != count) complete.TrySetException(new IOException("The audio input stopped before the selected capture length."));
            stopped.TrySetResult();
        }
        var started = false;
        capture.DataAvailable += Data;
        capture.RecordingStopped += Stopped;
        try
        {
            token.ThrowIfCancellationRequested();
            capture.StartRecording();
            started = true;
            await complete.Task.WaitAsync(token).ConfigureAwait(false);
            return new(samples, rate);
        }
        finally
        {
            try
            {
                if (started)
                {
                    capture.StopRecording();
                    await stopped.Task.ConfigureAwait(false);
                    captureThread?.Join();
                }
            }
            finally
            {
                capture.DataAvailable -= Data;
                capture.RecordingStopped -= Stopped;
            }
        }
    }

    public Task PlayAsync(string deviceId, MediaTapeSignal signal, int channelCount, int channelIndex,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(signal);
        Validate(deviceId, signal.SampleRate, channelCount, channelIndex);
        if (signal.Samples.IsEmpty) throw new ArgumentException("The tape signal is empty.", nameof(signal));
        return Task.Run(() => PlayCoreAsync(deviceId, signal, channelCount, channelIndex, cancellationToken));
    }

    private static async Task PlayCoreAsync(string deviceId, MediaTapeSignal signal, int channels, int channel, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var enumerator = new MMDeviceEnumerator();
        using var endpoint = enumerator.GetDevice(deviceId);
        if (endpoint.DataFlow != DataFlow.Render) throw new ArgumentException("A render endpoint is required.", nameof(deviceId));
        using var player = new WasapiOut(endpoint, AudioClientShareMode.Shared, false, 100);
        var stopped = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread? playbackThread = null;
        void Stopped(object? sender, StoppedEventArgs args)
        {
            playbackThread = Thread.CurrentThread;
            stopped.TrySetResult(args.Exception);
        }
        player.PlaybackStopped += Stopped;
        var started = false;
        try
        {
            player.Init(new SelectedChannelWaveProvider(signal, channels, channel));
            token.ThrowIfCancellationRequested();
            player.Play();
            started = true;
            var error = await stopped.Task.WaitAsync(token).ConfigureAwait(false);
            if (error is not null) throw new IOException("Audio tape output failed.", error);
        }
        finally
        {
            try
            {
                player.Stop();
                if (started)
                {
                    await stopped.Task.ConfigureAwait(false);
                    playbackThread?.Join();
                }
            }
            finally { player.PlaybackStopped -= Stopped; }
        }
    }

    private static void Validate(string deviceId, int rate, int channels, int channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate);
        if (channels is <= 0 or > short.MaxValue || channel < 0 || channel >= channels)
            throw new ArgumentOutOfRangeException(nameof(channel));
        _ = checked(rate * channels * sizeof(short));
    }

    // Providers await their operation before disposing this resource-free factory wrapper.
    public ValueTask DisposeAsync() { disposed = true; return ValueTask.CompletedTask; }

    internal sealed class SelectedChannelWaveProvider(MediaTapeSignal signal, int channels, int channel) : IWaveProvider
    {
        private int position;
        public WaveFormat WaveFormat { get; } = new(signal.SampleRate, 16, channels);

        public int Read(byte[] buffer, int offset, int count)
        {
            var align = checked(channels * sizeof(short));
            var frames = Math.Min(count / align, signal.Samples.Length - position);
            var bytes = checked(frames * align);
            buffer.AsSpan(offset, bytes).Clear();
            for (var frame = 0; frame < frames; frame++)
                BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset + frame * align + channel * sizeof(short)), signal.Samples.Span[position++]);
            return bytes;
        }
    }
}
