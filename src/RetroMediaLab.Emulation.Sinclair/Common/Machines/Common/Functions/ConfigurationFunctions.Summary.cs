using CommonCoreHostFunctions = RetroMediaLab.Emulation.Sinclair.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Sinclair.Common.Machines.Common.Functions;

internal static class ConfigurationSummaryFunctions
{
    internal static EmulationConfigurationSummary Create(MachineConfiguration configuration) => new(CommonCoreHostFunctions.Adapters.DisplayResourceKey(ModelCatalog.All, configuration), configuration.Core == Emulator.None ? [] : [new Engine().Adapter(configuration).Definition.DisplayName]);
}
