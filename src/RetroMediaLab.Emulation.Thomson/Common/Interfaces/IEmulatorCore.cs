using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Thomson.Common.Contracts;
using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Thomson.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
}
