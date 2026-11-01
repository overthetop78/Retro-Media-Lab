namespace RetroMediaLab.Tests.Emulation.Audio;
public class AudioTests
{
    [Theory]
    [InlineData(RetroMediaLab.Emulation.Enums.EmulationAudioChannelLayout.Stereo)]
    [InlineData(RetroMediaLab.Emulation.Enums.EmulationAudioChannelLayout.Surround51)]
    [InlineData(RetroMediaLab.Emulation.Enums.EmulationAudioChannelLayout.Surround71)]
    public void SpeakerPositionsAndSamplesSurviveLatencyChanges(RetroMediaLab.Emulation.Enums.EmulationAudioChannelLayout layout) => AudioBufferScenarios.Multichannel(layout);
    [Fact] public void VolumeMutePauseAndSampleRateChangesUseOnlySimulatedOutput() => AudioBufferScenarios.ControlledAudio();
    [Theory] [InlineData(false)] [InlineData(true)] public void FailedOutputIsDisposedAndCanBeReplaced(bool available) => AudioBufferScenarios.OutputRecovery(available);
    [Fact] public void UnavailableOutputIsNotRetriedForEveryChunk() => AudioBufferScenarios.UnavailableOutputIsNotRetriedForEveryChunk();
    [Fact] public void SamplesReachInjectedOutputAndFlushProducesSilence()=>AudioBufferScenarios.Samples();
    [Fact] public void LifecycleRejectsInvalidUseAndDisposesOutput()=>AudioBufferScenarios.Lifecycle();
}
