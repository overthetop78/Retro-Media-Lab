using System.IO;
using RetroMediaLab.Infrastructure.Constants;
using RetroMediaLab.Infrastructure.Hardware.Audio;
using RetroMediaLab.Infrastructure.Hardware.Media;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.Tests.Hardware;

public sealed class AudioTapeMediaTests
{
    private static Dictionary<string, string> Options() => new()
    {
        [AudioTapeMediaOptionKeys.SampleRate] = "44100",
        [AudioTapeMediaOptionKeys.SampleCount] = "4",
        [AudioTapeMediaOptionKeys.ChannelCount] = "2",
        [AudioTapeMediaOptionKeys.ChannelIndex] = "1"
    };

    [Fact]
    public async Task CaptureAndWriteUseTheExistingRegistriesAndReleaseOwnedTransport()
    {
        var capture = new ControlledAudioDevice();
        var readers = new MediaAcquisitionProviderRegistry([new AudioTapeMediaAcquisitionProvider(() => capture)]);
        var acquired = await readers.AcquireAsync(MediaKind.Tape, "chosen-input", Options());
        Assert.True(capture.Disposed);
        Assert.Equal("chosen-input", capture.DeviceId);
        Assert.Equal((44100, 4, 2, 1), capture.CaptureSelection);
        var signal = MediaEngineComposition.CreateDefault().TapeMediaAcquisitionService.CreateSignal(acquired);
        Assert.Equal(new short[] { short.MinValue, 0, 123, short.MaxValue }, signal.Samples.ToArray());
        Assert.Equal(44100, signal.SampleRate);
        var playback = new ControlledAudioDevice();
        var writers = new MediaPhysicalWriterRegistry([new AudioTapeMediaPhysicalWriter(() => playback)]);
        var plan = MediaEngineComposition.CreateDefault().TapeMediaWritePlanningService.CreatePlan(signal);
        var result = await writers.WriteAsync(plan, "chosen-output", Options());
        Assert.True(result.IsSuccess);
        Assert.True(playback.Disposed);
        Assert.Equal("chosen-output", playback.DeviceId);
        Assert.Equal((2, 1), playback.PlaySelection);
        Assert.Equal(signal.Samples.ToArray(), playback.Signal!.Samples.ToArray());
    }

    [Theory]
    [InlineData("sampleRate", "0")]
    [InlineData("sampleCount", "0")]
    [InlineData("sampleCount", "invalid")]
    [InlineData("channelCount", "0")]
    [InlineData("channelIndex", "2")]
    [InlineData("channelIndex", "-1")]
    public async Task InvalidCaptureNeverCreatesADevice(string key, string value)
    {
        var options = Options(); options[key] = value;
        var calls = 0;
        var reader = new AudioTapeMediaAcquisitionProvider(() => { calls++; return new ControlledAudioDevice(); });
        await Assert.ThrowsAnyAsync<ArgumentException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", options));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task MissingDeviceOrChannelSelectionNeverCreatesATransport()
    {
        var reader = new AudioTapeMediaAcquisitionProvider(() => throw new InvalidOperationException("Unexpected device creation."));
        await Assert.ThrowsAsync<NotSupportedException>(() => reader.AcquireAsync(MediaKind.Tape, "", Options()));
        var options = Options(); options.Remove(AudioTapeMediaOptionKeys.ChannelIndex);
        await Assert.ThrowsAsync<ArgumentException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CaptureFailureOrCancellationStillAwaitsDisposal(bool cancel)
    {
        using var source = new CancellationTokenSource();
        var device = new ControlledAudioDevice
        {
            OnCapture = () => { if (cancel) { source.Cancel(); source.Token.ThrowIfCancellationRequested(); } throw new IOException("input disconnected"); }
        };
        var reader = new AudioTapeMediaAcquisitionProvider(() => device);
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", Options(), cancellationToken: source.Token));
        else await Assert.ThrowsAsync<IOException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", Options()));
        Assert.True(device.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlaybackFailureOrCancellationDoesNotReportSuccessAndReleasesDevice(bool cancel)
    {
        using var source = new CancellationTokenSource();
        var device = new ControlledAudioDevice
        {
            OnPlay = () => { if (cancel) { source.Cancel(); source.Token.ThrowIfCancellationRequested(); } throw new IOException("output disconnected"); }
        };
        var writer = new AudioTapeMediaPhysicalWriter(() => device);
        var plan = Plan();
        if (cancel)
        {
            var result = await writer.WriteAsync(plan, "chosen-output", Options(), cancellationToken: source.Token);
            Assert.True(result.Cancelled); Assert.False(result.IsSuccess); Assert.Equal(0, result.CompletedUnits);
        }
        else await Assert.ThrowsAsync<IOException>(() => writer.WriteAsync(plan, "chosen-output", Options()));
        Assert.True(device.Disposed);
    }

    [Fact]
    public async Task PrematureCaptureIsRejectedAndDeviceReleased()
    {
        var device = new ControlledAudioDevice { CaptureResult = new(new short[] { 1 }, 44100) };
        var reader = new AudioTapeMediaAcquisitionProvider(() => device);
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", Options()));
        Assert.True(device.Disposed);
    }

    [Fact]
    public async Task PartialOrReorderedTapePlanNeverOpensOutput()
    {
        var first = MediaTapePhysicalDataFunctions.CreateUnit(new(new short[] { 1 }, 44100));
        var second = MediaTapePhysicalDataFunctions.CreateUnit(new(new short[] { 2 }, 44100), 1);
        var writer = new AudioTapeMediaPhysicalWriter(() => throw new InvalidOperationException("Unexpected device creation."));
        foreach (var order in new int[][] { [0], [1, 0], [] })
        {
            var plan = new MediaWritePlan(MediaKind.Tape, MediaRepresentationKind.Sequential, [first, second], order, new Dictionary<string, string>());
            await Assert.ThrowsAsync<InvalidDataException>(() => writer.WriteAsync(plan, "chosen-output", Options()));
        }
    }

    [Fact]
    public async Task PrecancelledTransfersNeverOpenADevice()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        var reader = new AudioTapeMediaAcquisitionProvider(() => throw new InvalidOperationException("Unexpected device creation."));
        var writer = new AudioTapeMediaPhysicalWriter(() => throw new InvalidOperationException("Unexpected device creation."));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.AcquireAsync(MediaKind.Tape, "chosen-input", Options(), cancellationToken: source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(Plan(), "chosen-output", Options(), cancellationToken: source.Token));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PcmOutputPreservesSelectedChannelAndClearsOtherChannels(int selected)
    {
        var provider = new WasapiAudioTapeDevice.SelectedChannelWaveProvider(new(new short[] { -32768, 0, 32767 }, 44100), 2, selected);
        var bytes = Enumerable.Repeat((byte)0xAA, 24).ToArray();
        Assert.Equal(8, provider.Read(bytes, 4, 8));
        Assert.Equal(4, provider.Read(bytes, 12, 8));
        Assert.Equal(0, provider.Read(bytes, 16, 4));
        var expected = selected == 0
            ? new byte[] { 0, 128, 0, 0, 0, 0, 0, 0, 255, 127, 0, 0 }
            : new byte[] { 0, 0, 0, 128, 0, 0, 0, 0, 0, 0, 255, 127 };
        Assert.Equal(expected, bytes.AsSpan(4, 12).ToArray());
        Assert.All(bytes[..4], value => Assert.Equal(0xAA, value));
        Assert.All(bytes[16..], value => Assert.Equal(0xAA, value));
    }

    private static MediaWritePlan Plan() => MediaEngineComposition.CreateDefault().TapeMediaWritePlanningService.CreatePlan(new(new short[] { -1, 0, 1 }, 44100));

    private sealed class ControlledAudioDevice : IAudioTapeDevice
    {
        internal bool Disposed;
        internal string? DeviceId;
        internal (int, int, int, int) CaptureSelection;
        internal (int, int) PlaySelection;
        internal MediaTapeSignal? Signal;
        internal MediaTapeSignal CaptureResult = new(new short[] { short.MinValue, 0, 123, short.MaxValue }, 44100);
        internal Action? OnCapture;
        internal Action? OnPlay;
        public Task<MediaTapeSignal> CaptureAsync(string id, int rate, int count, int channels, int channel, CancellationToken token)
        {
            DeviceId = id; CaptureSelection = (rate, count, channels, channel); OnCapture?.Invoke();
            return Task.FromResult(CaptureResult);
        }
        public Task PlayAsync(string id, MediaTapeSignal signal, int channels, int channel, CancellationToken token)
        {
            DeviceId = id; Signal = signal; PlaySelection = (channels, channel); OnPlay?.Invoke();
            return Task.CompletedTask;
        }
        public async ValueTask DisposeAsync() { await Task.Yield(); Disposed = true; }
    }
}
