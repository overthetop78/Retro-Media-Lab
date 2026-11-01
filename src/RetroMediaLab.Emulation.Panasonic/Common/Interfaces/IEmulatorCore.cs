using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Panasonic.Common.Contracts;
using RetroMediaLab.Emulation.Panasonic.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Panasonic.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Panasonic.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
}
