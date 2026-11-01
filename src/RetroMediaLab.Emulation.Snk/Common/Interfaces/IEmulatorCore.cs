using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Snk.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
}
