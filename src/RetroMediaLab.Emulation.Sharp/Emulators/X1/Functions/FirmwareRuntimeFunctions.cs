using RetroMediaLab.Emulation.Sharp.Emulators.Common.Interop.Functions;
using System.IO;
using RetroMediaLab.Emulation.Sharp.Emulators.X1.Constants;
using SharedFirmwareRuntimeFunctions = RetroMediaLab.Emulation.Sharp.Emulators.Common.Interop.Functions.FirmwareRuntimeFunctions;

namespace RetroMediaLab.Emulation.Sharp.Emulators.X1.Functions;

internal static class FirmwareRuntimeFunctions
{
    internal static void Prepare(MachineConfiguration configuration, string systemDirectory)
    {
        var name = configuration.Model == MachineModel.X1 ? FirmwareRuntimeConstants.IplFileName : FirmwareRuntimeConstants.TurboIplFileName;
        SharedFirmwareRuntimeFunctions.CopyVerified(configuration, FirmwareCategory.Ipl, Path.Combine(systemDirectory, FirmwareRuntimeConstants.SystemDirectory, name));
    }
}
