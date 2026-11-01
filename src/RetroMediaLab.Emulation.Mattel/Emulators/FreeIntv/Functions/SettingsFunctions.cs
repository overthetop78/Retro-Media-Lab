using CommonSettings = RetroMediaLab.Emulation.Mattel.Common.Machines.Common.Functions.SettingsDescriptionFunctions;
using RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Dictionaries;
using RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Constants;

namespace RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Functions;

internal static class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<CoreOption> GetOptions() => FreeIntvOptionCatalog.All;

    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration) =>
        [new(SettingsConstants.ResourceNativeControls, EmulationMachineTab.Controllers, SettingsConstants.ResourceNativeControls,
            GetOptions().Where(option => option.IsVisible).Select(option => CommonSettings.Native(configuration, option, EmulationMachineTab.Controllers, SettingsConstants.ResourceNativeControls)
                with { RequiresRestart = option.Key == OptionConstants.PlayerOneController }).ToArray())];
}
