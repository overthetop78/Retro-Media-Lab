using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Nintendo.Common.Constants;

namespace RetroMediaLab.Emulation.Nintendo.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Select(model => new EmulationMachineDefinition(model.Id, MachineConfigurationConstants.ResourcePrefix + model.Id, $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{model.Id}.png"))
        .ToArray();
}
