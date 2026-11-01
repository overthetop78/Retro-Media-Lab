using RetroMediaLab.Emulation.Interfaces;
using System.IO;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Commodore.Common.Interfaces;

internal interface IEmulatorCore : IEmulationCoreRuntime<MachineConfiguration, CoreOption>, IEmulationCoreCommands<MediaConfiguration, PeripheralCategory>, IEmulationCoreIdentity<string>
{
}
