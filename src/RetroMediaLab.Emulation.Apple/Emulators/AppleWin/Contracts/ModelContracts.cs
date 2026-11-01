using RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Enums;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Apple.Emulators.AppleWin.Contracts;

public sealed record AppleWinModelDefinition(AppleWinModel Model, MachineModel MachineModel, string NativeValue);
