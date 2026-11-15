using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Functions;

internal static class CpuConfigurationFunctions
{
    internal static Cpu68000Configuration Main { get; } = new(ClockMultiplier: MachineClockRatios.MainCpu,
        TrackInstructionAddress: false, WriteTasMemory: false,
        RefreshPeriod: 896, RefreshStall: 14, RefreshSkipAdvance: 896, ResetRefreshAdvance: 896);
    internal static Cpu68000Configuration Secondary { get; } = new(ClockMultiplier: MachineClockRatios.SecondaryCpu,
        Prefetch: false, RefreshSkipAdvance: 512, ResetRefreshAdvance: 512, PrefetchResetAddress: null);

    internal static Cpu68000<MegaDriveCpuBus> CreateMain(MegaDriveCpuBus bus) => new(bus, Main);
    internal static Cpu68000<MegaCdCpuBus> CreateSecondary(MegaCdCpuBus bus) => new(bus, Secondary);
    internal static CpuZ80<TBus> CreateZ80<TBus>(TBus bus, Func<int, int> interrupt) where TBus : class, ICpuZ80Bus =>
        new(bus, interrupt, clockMultiplier: MachineClockRatios.Z80);
}
