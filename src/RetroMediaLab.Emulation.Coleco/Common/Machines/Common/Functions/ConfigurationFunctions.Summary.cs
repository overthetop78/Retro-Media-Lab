using CommonCoreHostFunctions = RetroMediaLab.Emulation.Coleco.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Coleco.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), [EmulatorCatalog.Get(configuration.Core).DisplayName]);
}

