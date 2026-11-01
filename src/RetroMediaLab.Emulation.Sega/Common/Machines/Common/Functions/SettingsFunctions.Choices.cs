using CommonCoreHostFunctions = RetroMediaLab.Emulation.Sega.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static partial class SettingsDescriptionFunctions
{
    private static EmulationSettingsChoice Invariant(string id, string text, long? numericValue = null) => CommonCoreHostFunctions.HardwareSettings.InvariantChoice(id, text, numericValue);
}
