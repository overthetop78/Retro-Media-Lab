using RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Constants;
using RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Contracts;
using RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Enums;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Dictionaries;

public static class MiniVmacModelCatalog
{
    public static IReadOnlyList<MiniVmacModelDefinition> All { get; } =
    [new(MiniVmacModel.MacintoshII, MachineModel.MacintoshII, EmulatorConstants.MainMemoryBytes)];
}
