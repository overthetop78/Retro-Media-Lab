using System.Text.Json;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using BbcAudio = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Audio;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuAudioStateTests
{
    [Fact]
    public void PartialSamplesAndPsgTransferResumeWithoutChangingThePcmStream()
    {
        foreach (var instant in new[] { 0, 1, 7, 15, 44, 45, 46, 127, 511, 1333 })
        {
            var audio = new BbcAudio();
            audio.Write(0x85);
            audio.Write(0x12);
            audio.SetBus(0x92, true);
            for (var tick = 0; tick < instant; tick++) Advance(audio, tick);
            var state = JsonSerializer.Deserialize<AudioState>(JsonSerializer.SerializeToUtf8Bytes(audio.CaptureState()))!;
            var restored = new BbcAudio();
            restored.RestoreState(state);
            for (var tick = instant; tick < instant + 20_000; tick++)
            {
                Advance(audio, tick);
                Advance(restored, tick);
                if (tick % 1333 == 0) Assert.Equal(audio.DrainSamples(), restored.DrainSamples());
            }
            Assert.Equal(audio.DrainSamples(), restored.DrainSamples());
            Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(audio.CaptureState()), JsonSerializer.SerializeToUtf8Bytes(restored.CaptureState()));
        }
    }

    private static void Advance(BbcAudio audio, int tick)
    {
        if (tick == 37) audio.SetBus(0, false);
        if (tick == 91) audio.Write(0xe4);
        if (tick == 501) audio.Write(0xf2);
        if (tick == 1001) audio.SetBus(0x95, true);
        if (tick == 1060) audio.SetBus(0, false);
        audio.Tick();
    }
}
