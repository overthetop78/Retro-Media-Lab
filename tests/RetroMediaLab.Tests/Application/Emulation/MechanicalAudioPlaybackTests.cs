using RetroMediaLab.App.Services.Audio;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Services;
using C = RetroMediaLab.MediaEngine.Constants.MechanicalSoundConstants;

namespace RetroMediaLab.Tests.Application.Emulation;

public sealed class MechanicalAudioPlaybackTests
{
    [Fact]
    public async Task HostOutputIsPrefilledAndRefilledFromActualConsumption()
    {
        var output = new MemoryOutput(buffered: true);
        var errors = new List<Exception>();
        var mechanics = MediaEngineComposition.RuntimePreparation.CreateDriveMechanics(C.Cassette, hostAudio: true);
        try
        {
            using var player = new MediaDriveAudioController(new MediaDriveService(), (error, _) => errors.Add(error), () => output);
            mechanics.SetTapeOperation(C.Play);
            await output.Filled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // A sink that consumes nothing must not receive further fragments.
            await Task.Delay(60);
            Assert.Equal(4, output.Writes);
            Assert.Equal(3528, output.BufferStatus!.Value.BufferedFrames);
            output.Consume(1764);
            await output.Refilled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(6, output.Writes);
            Assert.Equal(3528, output.BufferStatus!.Value.BufferedFrames);
        }
        finally { mechanics.Reset(); }
        Assert.True(output.Disposed);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RuntimeMotorPlaysWithoutEmulationFramesAndOnlyItsControllerStopsIt()
    {
        var output = new MemoryOutput();
        var errors = new List<Exception>();
        var mechanics = MediaEngineComposition.RuntimePreparation.CreateDriveMechanics(C.Floppy, hostAudio: true);
        try
        {
            using var player = new MediaDriveAudioController(new MediaDriveService(), (error, _) => errors.Add(error), () => output);
            mechanics.SetMotor(true);
            await output.Played.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(mechanics.State.MotorRunning);
            Assert.True(output.Nonzero);
            Assert.Equal(44100, output.SampleRate);
            mechanics.SetMotor(false);
        }
        finally { mechanics.Reset(); }
        Assert.True(output.Disposed);
        Assert.Empty(errors);
    }

    [Fact]
    public void TapeOperationAdvancesOnlyWithHostAudioRequestsAndHonorsVolumeAndStop()
    {
        var mechanics = new MediaDriveMechanics(C.Cassette);
        try
        {
            mechanics.SetTapeOperation(C.Play);
            var first = mechanics.RenderAudio(48000, 4800);
            mechanics.SetTapeOperation(C.Play); // An identical activity report must not restart the recording.
            var second = mechanics.RenderAudio(48000, 4800);
            var expected = MediaEngineComposition.RuntimePreparation.RenderMechanicalSound(C.Cassette, C.Play, 48000, 9600, 0);
            Assert.Equal(expected, first.Concat(second));
            mechanics.Configure(null, 0, true);
            Assert.All(mechanics.RenderAudio(48000, 480), sample => Assert.Equal(0, sample));
            mechanics.SetTapeOperation(C.Stop);
            mechanics.RenderAudio(48000, 48000);
            Assert.False(mechanics.HasPendingAudio);
            mechanics.Configure(null, 100, true);
            mechanics.SetTapeOperation(C.Play);
            mechanics.SetTapeOperation(C.Pause); // A resume click must retain the resumed continuous operation.
            mechanics.RenderAudio(48000, 48000);
            Assert.True(mechanics.HasPendingAudio);
            Assert.Contains(mechanics.RenderAudio(48000, 4800), sample => sample != 0);
            mechanics.Reset();
            Assert.False(mechanics.HasPendingAudio);
        }
        finally { mechanics.Reset(); }
    }

    private sealed class MemoryOutput(bool buffered = false) : IMultichannelAudioOutput
    {
        internal TaskCompletionSource Played { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Filled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Refilled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _bufferedFrames;
        private int _writes;
        internal int Writes => Volatile.Read(ref _writes);
        public AudioOutputBufferStatus? BufferStatus => buffered ? new(SampleRate, Volatile.Read(ref _bufferedFrames), 11025, true) : null;
        internal void Consume(int frames) => Interlocked.Add(ref _bufferedFrames, -frames);
        internal int SampleRate;
        internal bool Nonzero;
        internal bool Disposed;
        public void Start(int sampleRate) => SampleRate = sampleRate;
        public void Start(int sampleRate, EmulationAudioChannelLayout layout) => Start(sampleRate);
        public void Write(ReadOnlySpan<short> samples)
        {
            Interlocked.Add(ref _bufferedFrames, samples.Length);
            var writes = Interlocked.Increment(ref _writes);
            foreach (var sample in samples) Nonzero |= sample != 0;
            if (Nonzero) Played.TrySetResult();
            if (writes == 4) Filled.TrySetResult();
            if (writes == 6) Refilled.TrySetResult();
        }
        public void Flush() { }
        public void Stop() { }
        public void Dispose() => Disposed = true;
    }
}
