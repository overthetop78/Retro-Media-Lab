using CommonCoreHostFunctions = RetroMediaLab.Emulation.Philips.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Philips.Common.Machines.Common.Dictionaries;

public static class ModelCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = Array.AsReadOnly(Enum.GetValues<MachineModel>().Select(model => new EmulationMachineDefinition(model.ToString(), $"Emulation.Philips.Model.{model}")).ToArray());

    public static MachineModel Parse(string id) => CommonCoreHostFunctions.Adapters.Parse<MachineModel>(id);
}
