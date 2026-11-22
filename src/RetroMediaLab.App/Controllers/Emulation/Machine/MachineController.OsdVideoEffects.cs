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
            VideoGroup(EmulationResourceKeys.VideoTemporalSettings,
                EffectIntensity(Catalog.GeneralPersistence, settings => settings.Temporal.GeneralPersistence,
                    (settings, v) => settings with { Temporal = settings.Temporal with { GeneralPersistence = v } }),
                EffectIntensity(Catalog.MotionBlur, settings => settings.Temporal.MotionBlur,
                    (settings, v) => settings with { Temporal = settings.Temporal with { MotionBlur = v } }),
                EffectIntensity(Catalog.Flicker, settings => settings.Temporal.Flicker,
                    (settings, v) => settings with { Temporal = settings.Temporal with { Flicker = v } }),
                VideoChoice(Catalog.ParameterResourceKeys[Catalog.Interlacing], VideoToggleResourceKeys,
                    settings => settings.Temporal.Interlacing > 0,
                    (settings, v) => settings with { Temporal = settings.Temporal with { Interlacing = v ? 100 : 0 } }),
                EffectIntensity(Catalog.InterlacingVisibility, settings => settings.Temporal.InterlacingVisibility,
                    (settings, v) => settings with { Temporal = settings.Temporal with { InterlacingVisibility = v } })
                    with { IsVisible = () => _video.VideoProcessing.Temporal.Interlacing > 0 },
                VideoChoice(Catalog.ParameterResourceKeys[Catalog.BlackFrameInsertion], VideoToggleResourceKeys,
                    settings => settings.Temporal.BlackFrameInsertion,
                    (settings, v) => settings with { Temporal = settings.Temporal with { BlackFrameInsertion = v } })),
            VideoGroup(EmulationResourceKeys.VideoSignalSimulationSettings,
                VideoChoice(Catalog.ParameterResourceKeys[Catalog.SignalConnection], Catalog.SignalConnectionResourceKeys,
                    settings => settings.SignalSimulation.Connection,
                    (settings, v) => settings with { SignalSimulation = settings.SignalSimulation with { Connection = v } }),
                EffectIntensity(Catalog.SignalConnectionIntensity, settings => settings.SignalSimulation.ConnectionIntensity,
                    (settings, v) => settings with { SignalSimulation = settings.SignalSimulation with { ConnectionIntensity = v } })
                    with { IsVisible = OsdSignalConnected },
                VideoChoice(Catalog.ParameterResourceKeys[Catalog.SignalStandard], Catalog.SignalStandardResourceKeys,
                    settings => settings.SignalSimulation.Standard,
                    (settings, v) => settings with { SignalSimulation = settings.SignalSimulation with { Standard = v } })
                    with { IsVisible = OsdSignalConnected },
                EffectIntensity(Catalog.SignalStandardIntensity, settings => settings.SignalSimulation.StandardIntensity,
                    (settings, v) => settings with { SignalSimulation = settings.SignalSimulation with { StandardIntensity = v } })
                    with { IsVisible = OsdSignalConnected }),
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

    private bool OsdSignalConnected() =>
        _video.VideoProcessing.SignalSimulation.Connection != EmulationSignalConnection.None;

    private Entry EffectIntensity(string parameter, Func<EmulationVideoProcessingConfiguration, int> read,
        Func<EmulationVideoProcessingConfiguration, int, EmulationVideoProcessingConfiguration> update) =>
        VideoNumber(Catalog.ParameterResourceKeys[parameter], read, update,
            EmulationVideoProcessingLimits.IntensityMinimum, EmulationVideoProcessingLimits.IntensityMaximum);
}
