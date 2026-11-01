using Options = RetroMediaLab.Emulation.Sinclair.Emulators.Fuse.Dictionaries.OptionCatalog;
using CommonSettings = RetroMediaLab.Emulation.Sinclair.Common.Machines.Common.Functions.SettingsDescriptionFunctions;

namespace RetroMediaLab.Emulation.Sinclair.Emulators.Fuse.Functions;

internal static class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<CoreOption> GetOptions(MachineConfiguration? configuration) =>
        CommonSettings.NativeOptions(configuration, Options.Options, RuntimeOptionFunctions.Defaults, RuntimeOptionFunctions.IsAvailable);

    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration) =>
        CommonSettings.NativeBlocks(configuration, GetOptions(configuration));
}
