using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Sega.Common.Constants;

namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Select(model => new EmulationMachineDefinition(model.Id, MachineConfigurationConstants.ResourcePrefix + model.Id, $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{model.Id}.png"))
        .ToArray();
}
