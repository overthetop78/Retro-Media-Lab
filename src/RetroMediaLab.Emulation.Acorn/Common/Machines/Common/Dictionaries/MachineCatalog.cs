using RetroMediaLab.Emulation;
using C = RetroMediaLab.Emulation.Acorn.Common.Constants.EmulationModuleConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    private static readonly IReadOnlyDictionary<MachineModel, string> ImagePaths = new Dictionary<MachineModel, string>
    {
        [MachineModel.BbcMicroModelA] = C.BbcMicroImagePath,
        [MachineModel.BbcMicroModelB] = C.BbcMicroImagePath,
        [MachineModel.BbcMicroModelBPlus] = C.BbcMicroImagePath,
        [MachineModel.BbcMicroModelBPlus128] = C.BbcMicroImagePath,
        [MachineModel.BbcMaster128] = C.Master128ImagePath,
        [MachineModel.BbcMasterET] = C.Master128ImagePath,
        [MachineModel.BbcMasterCompact] = C.Master128ImagePath,
        [MachineModel.AcornSystem1] = C.System1ImagePath,
        [MachineModel.AcornSystem2] = C.System2ImagePath,
        [MachineModel.AcornSystem3] = C.System3ImagePath,
        [MachineModel.AcornSystem4] = C.System4ImagePath,
        [MachineModel.AcornSystem5] = C.System5ImagePath,
        [MachineModel.AcornAtom] = C.AtomImagePath,
        [MachineModel.AcornElectron] = C.ElectronImagePath,
        [MachineModel.AcornArchimedes] = C.ArchimedesImagePath,
        [MachineModel.AcornA3000] = C.A3000ImagePath
    };

    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Where(model => model.Id != BbcMicroModelConstants.ModelBPlus128Id)
        .Select(model => new EmulationMachineDefinition(model.Id, model.DisplayResourceKey,
            ImagePaths[ModelCatalog.Parse(model.Id)])).ToArray();
}
