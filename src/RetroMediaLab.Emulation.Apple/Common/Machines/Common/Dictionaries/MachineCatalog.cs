using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Apple.Common.Constants;

namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = Array.AsReadOnly(
        ModelCatalog.All.Select(model => new EmulationMachineDefinition(model.Id, model.DisplayResourceKey, $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{model.Id}.png")).ToArray());
}
