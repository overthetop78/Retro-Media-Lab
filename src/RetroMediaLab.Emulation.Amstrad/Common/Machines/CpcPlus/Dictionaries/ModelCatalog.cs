using CommonStorageConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.Common.Constants.StorageSettingsFunctionsConstants;
using RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcPlus.Constants;
using ModelConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcPlus.Constants.ModelConstants;
using CommonModelConstants = RetroMediaLab.Emulation.Amstrad.Common.Machines.Common.Constants.ModelConstants;

namespace RetroMediaLab.Emulation.Amstrad.Common.Machines.CpcPlus.Dictionaries;

internal static class ModelCatalog
{
    internal static IReadOnlyList<Model> All { get; } =
    [
        new(ModelConstants.Cpc464Plus, ModelConstants.DisplayCpc464Plus,
            CommonModelConstants.BackendCpcPlus, CommonModelConstants.Ram64Kib,
            true, CommonStorageConstants.NoFloppyDrives, CommonStorageConstants.MaximumFloppyDrives, true, true, true, true),
        new(ModelConstants.Cpc6128Plus, ModelConstants.DisplayCpc6128Plus,
            CommonModelConstants.BackendCpcPlus, CommonModelConstants.Ram128Kib,
            true, CommonStorageConstants.SingleFloppyDrive, CommonStorageConstants.MaximumFloppyDrives, false, true, true, true)
    ];
}
