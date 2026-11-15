using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

internal sealed class MegaCdCpuBus(CartridgeMemoryBank[] banks, IScdHost host, Action<uint> acknowledge) : MappedCpuBus(banks)
{
    public override void AcknowledgeInterrupt(uint level) => acknowledge(level);
    public override bool IsRefreshWindowElapsed(int cycles, int deadline) => host.MainState.cycles >= host.MainState.refresh_cycles;
}
