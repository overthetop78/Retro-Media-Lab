using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

internal sealed class MegaDriveCpuBus(CartridgeMemoryBank[] banks, Vdp video) : MappedCpuBus(banks)
{
    public override void AcknowledgeInterrupt(uint level) => video.AcknowledgeInterrupt((int)level);
}
