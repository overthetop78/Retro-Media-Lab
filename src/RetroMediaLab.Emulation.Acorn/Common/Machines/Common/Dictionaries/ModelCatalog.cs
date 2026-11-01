using CommonCoreHostFunctions = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

public static class ModelCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } =
        HardwareModelCatalog.All.Select(definition => new EmulationMachineDefinition(definition.Model.ToString(), definition.DisplayNameResourceKey)).ToArray();

    public static MachineModel Parse(string id) => CommonCoreHostFunctions.Adapters.Parse<MachineModel>(id);
}
