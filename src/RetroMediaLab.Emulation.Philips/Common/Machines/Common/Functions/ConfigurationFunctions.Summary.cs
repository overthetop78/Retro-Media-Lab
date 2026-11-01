using CommonCoreHostFunctions = RetroMediaLab.Emulation.Philips.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Philips.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), [new Engine().Adapter(configuration).Definition.DisplayName]);
}
