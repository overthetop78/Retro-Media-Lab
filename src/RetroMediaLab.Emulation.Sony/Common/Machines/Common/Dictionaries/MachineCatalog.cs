using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Sony.Common.Constants;

namespace RetroMediaLab.Emulation.Sony.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Select(model => new EmulationMachineDefinition(model.Id, MachineConfigurationConstants.ResourceKey(model.Id), $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{model.Id}.png"))
        .ToArray();
}
