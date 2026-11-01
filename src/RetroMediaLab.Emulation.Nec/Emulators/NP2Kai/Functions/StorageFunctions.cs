using StorageConstants = RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Functions;

internal static class StorageFunctions
{
    internal static IReadOnlyList<EmulationMediaDevice> Devices(Model model) =>
        RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions.CoreStorageSettingsFunctions.ComputerDevices(model,
            StorageConstants.FirstDriveIndex, StorageConstants.DriveCount, StorageConstants.FloppyExtensions,
            StorageConstants.HardDiskExtensions, StorageConstants.FloppyLabelPrefix, StorageConstants.HardDiskLabelPrefix);
}
