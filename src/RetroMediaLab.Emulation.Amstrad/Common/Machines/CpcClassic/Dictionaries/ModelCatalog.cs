using CommonStorageConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.Common.Constants.StorageSettingsFunctionsConstants;
using RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcClassic.Constants;
using ModelConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcClassic.Constants.ModelConstants;
using CommonModelConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.Common.Constants.ModelConstants;

namespace RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcClassic.Dictionaries;

internal static class ModelCatalog
{
    internal static IReadOnlyList<Model> All { get; } =
    [
        new(ModelConstants.Cpc464, ModelConstants.DisplayCpc464, CommonModelConstants.BackendCpc464, CommonModelConstants.Ram64Kib, true, CommonStorageConstants.NoFloppyDrives, CommonStorageConstants.MaximumFloppyDrives, true, true, false, false),
        new(ModelConstants.Cpc664, ModelConstants.DisplayCpc664, CommonModelConstants.BackendCpc664, CommonModelConstants.Ram64Kib, true, CommonStorageConstants.SingleFloppyDrive, CommonStorageConstants.MaximumFloppyDrives, false, true, false, false),
        new(ModelConstants.Cpc6128, ModelConstants.DisplayCpc6128, CommonModelConstants.BackendCpc6128,
            CommonModelConstants.Ram128Kib, true, CommonStorageConstants.SingleFloppyDrive, CommonStorageConstants.MaximumFloppyDrives, false, true, false, false)
    ];
}
