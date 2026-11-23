using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.Emulation;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdVideoEffectsSettings() =>
        VideoGroup(EmulationResourceKeys.VideoTabEffects,
        [
            VideoGroup(EmulationResourceKeys.VideoStylisticSettings,
                EffectIntensity(Catalog.Grain, settings => settings.Stylistic.Grain,
                    (settings, v) => settings with { Stylistic = settings.Stylistic with { Grain = v } }),
                EffectIntensity(Catalog.Vhs, settings => settings.Stylistic.Vhs,
                    (settings, v) => settings with { Stylistic = settings.Stylistic with { Vhs = v } }),
                EffectIntensity(Catalog.ChromaticAberration, settings => settings.Stylistic.ChromaticAberration,
                    (settings, v) => settings with { Stylistic = settings.Stylistic with { ChromaticAberration = v } }),
                EffectIntensity(Catalog.Bloom, settings => settings.Stylistic.Bloom,
                    (settings, v) => settings with { Stylistic = settings.Stylistic with { Bloom = v } }),
                VideoChoice(Catalog.ParameterResourceKeys[Catalog.Sepia], VideoToggleResourceKeys,
                    settings => settings.Stylistic.Sepia,
                    (settings, v) => settings with { Stylistic = settings.Stylistic with { Sepia = v } })),
            .. OsdModuleGroups(EmulationMachineTab.Video, EmulationVideoSettingsSection.Effects)
        ]);

    private Entry EffectIntensity(string parameter, Func<EmulationVideoProcessingConfiguration, int> read,
        Func<EmulationVideoProcessingConfiguration, int, EmulationVideoProcessingConfiguration> update) =>
        VideoNumber(Catalog.ParameterResourceKeys[parameter], read, update,
            EmulationVideoProcessingLimits.IntensityMinimum, EmulationVideoProcessingLimits.IntensityMaximum);
}
