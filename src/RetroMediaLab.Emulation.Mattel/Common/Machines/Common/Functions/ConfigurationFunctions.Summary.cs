using CommonCoreHostFunctions = RetroMediaLab.Emulation.Mattel.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Mattel.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), [new Engine().Adapter(configuration).Definition.DisplayName]);
}
