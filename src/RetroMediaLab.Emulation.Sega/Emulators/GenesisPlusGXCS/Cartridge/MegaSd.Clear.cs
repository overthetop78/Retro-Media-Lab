using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class MegaSd
{
    private void ClearState()
    {
        megasd_hw.unlock = 0;
        megasd_hw.bank0 = 0;
        megasd_hw.special = 0;
        megasd_hw.writeEnable = 0;
        megasd_hw.overlayEnable = 0;
        megasd_hw.playbackLoop = 0;
        megasd_hw.playbackLoopTrack = 0;
        megasd_hw.playbackEndTrack = 0;
        megasd_hw.result = 0;
        megasd_hw.fadeoutStartVolume = 0;
        megasd_hw.fadeoutSamplesTotal = 0;
        megasd_hw.fadeoutSamplesCount = 0;
        megasd_hw.playbackSamplesCount = 0;
        megasd_hw.playbackLoopSector = 0;
        megasd_hw.playbackEndSector = 0;
        megasd_hw.buffer.AsSpan().Clear();
    }
}
