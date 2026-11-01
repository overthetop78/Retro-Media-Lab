namespace RetroMediaLab.Emulation.Nec.Emulators.BeetlePcfx.Functions;

internal static class BeetlePcfxOptionFunctions
{
    internal static MachineConfiguration ToNative(MachineConfiguration configuration)
    => RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions.CoreSettingsFunctions.CopyConfiguredOptions(configuration);
}


