using System.Windows;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateOledControls()
    {
        var oled = _configuration.Oled;
        var section = Section(Catalog.OledBlock);
        void Set(Func<EmulationOledVideoConfiguration, EmulationOledVideoConfiguration> change) =>
            Update(configuration => configuration with { Oled = change(configuration.Oled) });
        void Choice<T>(string label, IReadOnlyDictionary<T, string> options, T selected,
            Func<EmulationOledVideoConfiguration, T, EmulationOledVideoConfiguration> change, bool rebuild = false)
            where T : struct, Enum =>
            section.Children.Add(ChoiceField(label, options, selected, value =>
            {
                Set(config => change(config, value));
                if (rebuild) RebuildContent();
            }));

        section.Children.Add(CreatePanelSignalRow(oled, Set));
        Choice(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys, oled.Definition,
            (config, value) => config with { Definition = value });
        Choice(Catalog.LcdPanelType, Catalog.OledPanelTypeResourceKeys, oled.PanelType,
            (config, value) => config with { PanelType = value });
        Choice(Catalog.LcdPixelStructure, Catalog.OledPixelStructureResourceKeys, oled.PixelStructure,
            (config, value) => config with { PixelStructure = value });
        Choice(ParameterKey(Catalog.CrtConnection), Catalog.OledConnectionResourceKeys, oled.Connection,
            (config, value) => config with { Connection = value }, rebuild: true);
        if (oled.CanDegradeConnection())
            section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
                Catalog.CrtConnectionDegradation, oled.ConnectionDegradation, 0,
                EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum,
                value => Set(config => config with { ConnectionDegradation = value }),
                1, 38, new Thickness(0, 2, 0, 8)));
        if (oled.Connection == EmulationCrtConnection.LegacyMonitor)
            Choice(ParameterKey(Catalog.CrtMonitorPalette), Catalog.CrtMonitorPaletteResourceKeys, oled.MonitorPalette,
                (config, value) => config with { MonitorPalette = value });
        return section;
    }
}
