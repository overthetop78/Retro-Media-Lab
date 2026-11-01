using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Nec.Common.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineLt.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.LaserActive.Constants;

namespace RetroMediaLab.Emulation.Nec.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = ModelCatalog.All
        .Select(model => new EmulationMachineDefinition(model.Id,
            MachineConfigurationConstants.ResourcePrefix + model.Id,
            model.Id is PcEngineLtMachineConstants.Id or LaserActiveMachineConstants.Id ? EmulationModuleConstants.BrandAssetPath : $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{model.Id}.png"))
        .ToArray();

}
