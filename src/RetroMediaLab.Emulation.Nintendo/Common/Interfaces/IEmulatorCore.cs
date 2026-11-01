using RetroMediaLab.Emulation.Interfaces;
namespace RetroMediaLab.Emulation.Nintendo.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<string>
{
}
