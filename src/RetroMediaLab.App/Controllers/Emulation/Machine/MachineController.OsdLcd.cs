using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private Entry OsdLcdSettings(bool led) =>
        VideoGroup(led ? Catalog.LedBlock : Catalog.LcdBlock,
            LcdChoice(led, Catalog.ParameterResourceKeys[Catalog.CrtColorStandards], Catalog.CrtColorStandardsResourceKeys,
                c => c.ColorStandards, (c, v) => c with { ColorStandards = v }),
            LcdChoice(led, Catalog.ParameterResourceKeys[Catalog.CrtFieldRate], Catalog.CrtFieldRateResourceKeys,
                c => c.FieldRate, (c, v) => c with { FieldRate = v }),
            LcdChoice(led, Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys,
                c => c.Definition, (c, v) => c with { Definition = v }),
            LcdChoice(led, Catalog.LcdPixelStructure, Catalog.LcdPixelStructureResourceKeys,
                c => c.PixelStructure, (c, v) => c with { PixelStructure = v }),
            LcdChoice(led, Catalog.LcdPixelOrientation, Catalog.LcdPixelOrientationResourceKeys,
                c => c.Orientation, (c, v) => c with { Orientation = v })
                with { IsVisible = () => ReadPanel(_video.VideoProcessing, led).HasPixelOrientation() },
            LcdChoice(led, Catalog.LcdPanelType, Catalog.LcdPanelTypeResourceKeys,
                c => c.PanelType, (c, v) => c with { PanelType = v }),
            LcdChoice(led, Catalog.LedBacklightType, Catalog.LedBacklightTypeResourceKeys,
                c => c.BacklightType, (c, v) => c with { BacklightType = v })
                with { IsVisible = () => led },
            VideoNumber(Catalog.LcdBacklight, config => ReadPanel(config, led).BacklightIntensity,
                (config, v) => UpdatePanel(config, led, ReadPanel(config, led) with { BacklightIntensity = v }),
                EmulationVideoProcessingLimits.IntensityMinimum, EmulationVideoProcessingLimits.IntensityMaximum),
            LcdChoice(led, Catalog.ParameterResourceKeys[Catalog.CrtConnection], Catalog.CrtConnectionResourceKeys,
                c => c.Connection, (c, v) => c with { Connection = v }),
            VideoNumber(Catalog.ParameterResourceKeys[Catalog.CrtConnectionDegradation], config => ReadPanel(config, led).ConnectionDegradation,
                (config, v) => UpdatePanel(config, led, ReadPanel(config, led) with { ConnectionDegradation = v }),
                0, EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum)
                with { IsVisible = () => ReadPanel(_video.VideoProcessing, led).CanDegradeConnection() },
            LcdChoice(led, Catalog.ParameterResourceKeys[Catalog.CrtMonitorPalette], Catalog.CrtMonitorPaletteResourceKeys,
                c => c.MonitorPalette, (c, v) => c with { MonitorPalette = v })
                with { IsVisible = () => ReadPanel(_video.VideoProcessing, led).Connection == EmulationCrtConnection.LegacyMonitor })
        with { IsVisible = () => EmulationVideoProcessingConfigurationFunctions.ScreenType(_video.VideoProcessing)
            == (led ? EmulationVideoScreenType.LedBacklitLcd : EmulationVideoScreenType.Lcd) };

    private Entry LcdChoice<T>(bool led, string label, IReadOnlyDictionary<T, string> keys,
        Func<EmulationLcdVideoConfiguration, T> read,
        Func<EmulationLcdVideoConfiguration, T, EmulationLcdVideoConfiguration> update) where T : notnull =>
        VideoChoice(label, keys, config => read(ReadPanel(config, led)),
            (config, value) => UpdatePanel(config, led, update(ReadPanel(config, led), value)));

    private static EmulationLcdVideoConfiguration ReadPanel(EmulationVideoProcessingConfiguration config, bool led) =>
        led ? config.Led : config.Lcd;

    private static EmulationVideoProcessingConfiguration UpdatePanel(EmulationVideoProcessingConfiguration config,
        bool led, EmulationLcdVideoConfiguration panel) => led ? config with { Led = panel } : config with { Lcd = panel };
}
