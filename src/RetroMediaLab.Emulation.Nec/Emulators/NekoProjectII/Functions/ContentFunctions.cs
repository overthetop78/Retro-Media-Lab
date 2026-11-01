using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Nec.Emulators.NekoProjectII.Constants;
using FirmwareConstants = RetroMediaLab.Emulation.Nec.Emulators.NekoProjectII.Constants.FirmwareConstants;

namespace RetroMediaLab.Emulation.Nec.Emulators.NekoProjectII.Functions;

internal static class ContentFunctions
{
    internal static string Prepare(IReadOnlyList<MediaConfiguration> media, string contentDirectory, string systemDirectory) =>
        Pc98ContentFunctions.Prepare(media, contentDirectory, systemDirectory,
            FirmwareConstants.SystemSubdirectory, ContentConstants.Profiles, ContentConstants.HardDiskKeys,
            ContentConstants.ExecutableName, ContentConstants.CommandBufferLimit,
            StorageConstants.FirstDriveIndex, StorageConstants.DriveCount);
}
