using DictionaryModels = RetroMediaLab.Emulation.Wenquxing.Common.Machines.ElectronicDictionaries.Dictionaries.HardwareModelCatalog;

namespace RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Dictionaries;

internal static class HardwareModelCatalog
{
    private static readonly IReadOnlyDictionary<MachineModel, HardwareModel> Models = DictionaryModels.All.ToDictionary(model => model.Model);
    internal static HardwareModel Get(MachineModel model) => Models.TryGetValue(model, out var hardware) ? hardware : throw new ArgumentOutOfRangeException(nameof(model));
}