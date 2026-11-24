using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdOledSettings() =>
        VideoGroup(Catalog.OledBlock,
            OledChoice(Catalog.ParameterResourceKeys[Catalog.CrtColorStandards], Catalog.CrtColorStandardsResourceKeys,
                c => c.ColorStandards, (c, v) => c with { ColorStandards = v }),
            OledChoice(Catalog.ParameterResourceKeys[Catalog.CrtFieldRate], Catalog.CrtFieldRateResourceKeys,
                c => c.FieldRate, (c, v) => c with { FieldRate = v }),
            OledChoice(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys,
                c => c.Definition, (c, v) => c with { Definition = v }),
            OledChoice(Catalog.LcdPanelType, Catalog.OledPanelTypeResourceKeys,
                c => c.PanelType, (c, v) => c with { PanelType = v }),
            OledChoice(Catalog.LcdPixelStructure, Catalog.OledPixelStructureResourceKeys,
                c => c.PixelStructure, (c, v) => c with { PixelStructure = v }),
            OledChoice(Catalog.ParameterResourceKeys[Catalog.CrtConnection], Catalog.OledConnectionResourceKeys,
                c => c.Connection, (c, v) => c with { Connection = v }),
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.CrtConnectionDegradation], config => config.Oled.ConnectionDegradation,
                (config, v) => config with { Oled = config.Oled with { ConnectionDegradation = v } },
                0, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum)
                with { IsVisible = () => _video.VideoProcessing.Oled.CanDegradeConnection() },
            OledChoice(Catalog.ParameterResourceKeys[Catalog.CrtMonitorPalette], Catalog.CrtMonitorPaletteResourceKeys,
                c => c.MonitorPalette, (c, v) => c with { MonitorPalette = v })
                with { IsVisible = () => _video.VideoProcessing.Oled.Connection == EmulationCrtConnection.LegacyMonitor })
        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.ScreenType(_video.VideoProcessing)
            == EmulationVideoScreenType.Oled };

    private Entry OledChoice<T>(string label, IReadOnlyDictionary<T, string> keys,
        Func<EmulationOledVideoConfiguration, T> read,
        Func<EmulationOledVideoConfiguration, T, EmulationOledVideoConfiguration> update) where T : notnull =>
        VideoChoice(label, keys, config => read(config.Oled),
            (config, value) => config with { Oled = update(config.Oled, value) });
}
