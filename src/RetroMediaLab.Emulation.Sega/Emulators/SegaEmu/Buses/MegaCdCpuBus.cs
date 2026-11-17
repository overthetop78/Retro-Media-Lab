using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

internal sealed class MegaCdCpuBus(CartridgeMemoryBank[] banks, IScdHost host, Action<uint> acknowledge, Scd? controller = null)
    : MappedCpuBus(banks), ICpu68000ProtectedMemoryBus
{
    private long _lastMappingVersion = long.MinValue, _projectionVersion;
    private int _lastProtection = -1;
    public override long DirectMemoryVersion
    {
        get
        {
            if (controller is null) return base.DirectMemoryVersion;
            long mapping = base.DirectMemoryVersion;
            int protection = controller.NativeWriteProtection;
            if (mapping != _lastMappingVersion || protection != _lastProtection)
            {
                _lastMappingVersion = mapping; _lastProtection = protection;
                _projectionVersion = unchecked(_projectionVersion + 1);
            }
            return _projectionVersion;
        }
    }
    public override void GetDirectMemory(int page, out ReadOnlyMemory<byte> read, out Memory<byte> write)
    {
        if (controller is null) { base.GetDirectMemory(page, out read, out write); return; }
        controller.GetNativeMemory(page, out read, out write);
    }
    public void GetProtectedWriteMemory(int page, out Memory<byte> memory, out int minimumOffset)
    {
        if (controller is null) { memory = default; minimumOffset = 0; return; }
        controller.GetNativeProtectedMemory(page, out memory, out minimumOffset);
    }
    public override void AcknowledgeInterrupt(uint level) => acknowledge(level);
    public override bool IsRefreshWindowElapsed(int cycles, int deadline) => host.MainState.cycles >= host.MainState.refresh_cycles;
    public override Cpu68000NativeRefreshMode NativeRefreshMode => Cpu68000NativeRefreshMode.StableDuringNativeExecution;
}
