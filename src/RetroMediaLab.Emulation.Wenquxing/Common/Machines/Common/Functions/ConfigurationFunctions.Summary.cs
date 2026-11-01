using CommonCoreHostFunctions = RetroMediaLab.Emulation.Wenquxing.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Wenquxing.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), configuration.Core == Emulator.None ? [] : [EmulatorCatalog.Get(configuration.Core).DisplayName]);
}
