using RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Enums;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Contracts;

public sealed record MiniVmacModelDefinition(MiniVmacModel Model, MachineModel MachineModel, long MainMemoryBytes);
