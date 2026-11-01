using CommonCoreHostFunctions = RetroMediaLab.Emulation.Panasonic.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Panasonic.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Panasonic.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), configuration.Core == Emulator.None ? [] : [EmulatorCatalog.Get(configuration.Core).DisplayName]);
}
