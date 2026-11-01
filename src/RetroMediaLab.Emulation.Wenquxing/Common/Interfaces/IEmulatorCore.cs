using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Wenquxing.Common.Contracts;
using RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Wenquxing.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, EmulationPeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
}
