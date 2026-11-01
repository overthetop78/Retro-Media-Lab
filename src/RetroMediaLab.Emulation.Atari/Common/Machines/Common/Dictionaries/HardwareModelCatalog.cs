using Atari2600Models = RetroMediaLab.Emulation.Atari.Common.Machines.Atari2600.Dictionaries.Atari2600ModelCatalog;
using Atari5200Models = RetroMediaLab.Emulation.Atari.Common.Machines.Atari5200.Dictionaries.Atari5200ModelCatalog;
using Atari7800Models = RetroMediaLab.Emulation.Atari.Common.Machines.Atari7800.Dictionaries.Atari7800ModelCatalog;
using Atari8BitModels = RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Dictionaries.Atari8BitModelCatalog;
using AtariJaguarModels = RetroMediaLab.Emulation.Atari.Common.Machines.AtariJaguar.Dictionaries.AtariJaguarModelCatalog;
using AtariLynxModels = RetroMediaLab.Emulation.Atari.Common.Machines.AtariLynx.Dictionaries.AtariLynxModelCatalog;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.Common.Dictionaries;

public static class HardwareModelCatalog
{
    private static readonly IReadOnlyList<HardwareModelDefinition> Definitions =
    [
        ..Atari8BitModels.All,
        ..Atari2600Models.All,
        ..Atari5200Models.All,
        ..Atari7800Models.All,
        ..AtariLynxModels.All,
        ..AtariJaguarModels.All
    ];

    private static readonly IReadOnlyDictionary<MachineModel, HardwareModelDefinition> ByModel = HardwareModelFunctions.Index(Definitions);

    public static IReadOnlyList<HardwareModelDefinition> All => Definitions;

    public static HardwareModelDefinition Get(MachineModel model) => ByModel.TryGetValue(model, out var definition) ? definition : throw new ArgumentOutOfRangeException(nameof(model), model, ErrorMessages.UnknownHardwareModel);
}
