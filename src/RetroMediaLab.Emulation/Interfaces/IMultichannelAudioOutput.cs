using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Emulation.Interfaces;

public interface IMultichannelAudioOutput : IAudioOutput
{
    void Start(int sampleRate, EmulationAudioChannelLayout channelLayout);
}
