using RetroMediaLab.Emulation.Sharp.Emulators.Px68k.Dictionaries;
using CommonSettings = RetroMediaLab.Emulation.Sharp.Common.Machines.Common.Functions.SettingsDescriptionFunctions;
using Options = RetroMediaLab.Emulation.Sharp.Emulators.Px68k.Dictionaries.OptionCatalog;

namespace RetroMediaLab.Emulation.Sharp.Emulators.Px68k.Functions;

internal static class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<CoreOption> GetOptions(MachineConfiguration? configuration) =>
        CommonSettings.NativeOptions(configuration, Options.Options, RuntimeOptionFunctions.Defaults);

    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration) =>
        CommonSettings.NativeBlocks(configuration, GetOptions(configuration));
}
