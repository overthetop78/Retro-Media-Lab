using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

internal sealed class MegaDriveCpuBus(CartridgeMemoryBank[] banks, Vdp video, Func<uint, bool>? acknowledge = null) : MappedCpuBus(banks)
{
    public override void AcknowledgeInterrupt(uint level)
    {
        if (acknowledge?.Invoke(level) != true) video.AcknowledgeInterrupt((int)level);
    }
}
