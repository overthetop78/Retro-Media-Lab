namespace RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Functions;

internal static class BeetlePceFastOptionFunctions
{
    internal static MachineConfiguration ToNative(MachineConfiguration configuration)
    => RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions.CoreSettingsFunctions.CopyConfiguredOptions(configuration);
}


