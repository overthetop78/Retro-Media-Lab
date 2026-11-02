using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Where(model => model.Id != BbcMicroModelConstants.ModelBPlus128Id)
        .Select(model => new EmulationMachineDefinition(model.Id, model.DisplayResourceKey,
            SystemModelCatalog.IsSystem(ModelCatalog.Parse(model.Id)) || ModelCatalog.Parse(model.Id) == MachineModel.AcornAtom ? null
                : !AcornMasterModel.IsMaster(ModelCatalog.Parse(model.Id)) ? EmulationModuleConstants.BbcMicroImagePath : $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{BbcMicroModelConstants.Master128Id}.png")).ToArray();
}
