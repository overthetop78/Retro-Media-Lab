using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration)
    {
        var hardware = HardwareModelCatalog.Get(configuration.Model);
        return new EmulationConfigurationSummary(hardware.DisplayNameResourceKey, [$"Core {configuration.Core}"]);
    }
}
