using SupervisionModels = RetroMediaLab.Emulation.Watara.Common.Machines.Supervision.Dictionaries.HardwareModelCatalog;

namespace RetroMediaLab.Emulation.Watara.Common.Machines.Common.Dictionaries;

internal static class HardwareModelCatalog
{
    private static readonly IReadOnlyDictionary<MachineModel, HardwareModel> Models = SupervisionModels.All.ToDictionary(model => model.Model);
    internal static HardwareModel Get(MachineModel model) => Models.TryGetValue(model, out var hardware) ? hardware : throw new ArgumentOutOfRangeException(nameof(model));
}