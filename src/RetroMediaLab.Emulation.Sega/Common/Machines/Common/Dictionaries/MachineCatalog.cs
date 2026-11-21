using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Sega.Common.Constants;

namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Dictionaries;

public static class MachineCatalog
{
    private static readonly string[] PreferredOrder =
    [
        ModelConstants.Sg1000, ModelConstants.Sc3000, ModelConstants.Sf7000, ModelConstants.MarkIII,
        ModelConstants.MasterSystem, ModelConstants.MasterSystemII, ModelConstants.GameGear,
        ModelConstants.MegaDrive, ModelConstants.MegaDriveII, ModelConstants.MegaCd, ModelConstants.MegaCdSecond,
        ModelConstants.Saturn, ModelConstants.Dreamcast, ModelConstants.DreamcastVmu, ModelConstants.Pico
    ];

    public static IReadOnlyList<EmulationMachineDefinition> All { get; } = PreferredOrder
        .Concat(ModelCatalog.All.Select(model => model.Id).Where(id => !PreferredOrder.Contains(id) && !ModelConstants.HasThirtyTwoX(id)))
        .Select(id => new EmulationMachineDefinition(id, MachineConfigurationConstants.ResourcePrefix + id,
            $"{EmulationModuleConstants.AssetPathPrefix}/Machines/{id}.png"))
        .ToArray();
}
