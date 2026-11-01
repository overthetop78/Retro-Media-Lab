using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Apple.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<Emulator>
{
}
