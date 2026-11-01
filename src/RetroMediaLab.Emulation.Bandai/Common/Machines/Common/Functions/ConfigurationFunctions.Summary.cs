using CommonCoreHostFunctions = RetroMediaLab.Emulation.Bandai.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), [EmulatorCatalog.Get(configuration.Core).DisplayName]);
}
