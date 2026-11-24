using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.Emulation;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private static readonly IReadOnlyDictionary<bool, string> VideoToggleResourceKeys = new Dictionary<bool, string>
    {
        [false] = "Emulation.Value.Disabled",
        [true] = "Emulation.Value.Enabled"
    };

    private Entry OsdCrtSettings() =>
        VideoGroup(EmulationResourceKeys.VideoTechnologyCrt,
            CrtChoice(Catalog.CrtColorStandards, Catalog.CrtColorStandardsResourceKeys,
                c => c.ColorStandards, (c, v) => c with { ColorStandards = v }),
            CrtChoice(Catalog.CrtFieldRate, Catalog.CrtFieldRateResourceKeys,
                c => c.FieldRate, (c, v) => c with { FieldRate = v }),
            CrtChoice(Catalog.CrtMask, Catalog.CrtMaskResourceKeys, c => c.Mask, (c, v) => c with { Mask = v }),
            CrtChoice(Catalog.CrtPhosphorColor, Catalog.CrtPhosphorColorResourceKeys,
                c => c.PhosphorColor, (c, v) => c with { PhosphorColor = v })
                with { IsVisible = () => _video.VideoProcessing.Crt.Mask == EmulationCrtMask.Monochrome },
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.CrtBeamWidth], settings => settings.Crt.BeamWidth,
                (settings, value) => settings with { Crt = settings.Crt with { BeamWidth = value } },
                EmulationVideoProcessingLimits.AdjustmentMinimum, EmulationVideoProcessingLimits.AdjustmentMaximum, signed: true),
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.CrtCurvature], settings => settings.Crt.Curvature,
                (settings, value) => settings with { Crt = settings.Crt with { Curvature = value } },
                EmulationVideoProcessingLimits.CrtCurvatureMinimum, EmulationVideoProcessingLimits.CrtCurvatureMaximum),
            CrtChoice(Catalog.CrtConnection, Catalog.CrtConnectionResourceKeys,
                c => c.Connection, (c, v) => c with { Connection = v }),
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.CrtConnectionDegradation],
                settings => settings.Crt.ConnectionDegradation,
                (settings, value) => settings with { Crt = settings.Crt with { ConnectionDegradation = value } },
                0, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum)
                with { IsVisible = () => _video.VideoProcessing.Crt.CanDegradeConnection() },
            CrtChoice(Catalog.CrtMonitorPalette, Catalog.CrtMonitorPaletteResourceKeys,
                c => c.MonitorPalette, (c, v) => c with { MonitorPalette = v })
                with { IsVisible = () => _video.VideoProcessing.Crt.Connection == EmulationCrtConnection.LegacyMonitor })
            with { IsVisible = () => _video.VideoProcessing.DisplayTechnology == EmulationVideoDisplayTechnology.Crt };

    private Entry CrtChoice<T>(string parameter, IReadOnlyDictionary<T, string> keys, Func<EmulationCrtVideoConfiguration, T> read,
        Func<EmulationCrtVideoConfiguration, T, EmulationCrtVideoConfiguration> update) where T : notnull =>
        VideoChoice(Catalog.ParameterResourceKeys[parameter], keys, settings => read(settings.Crt),
            (settings, value) => settings with { Crt = update(settings.Crt, value) });
}
