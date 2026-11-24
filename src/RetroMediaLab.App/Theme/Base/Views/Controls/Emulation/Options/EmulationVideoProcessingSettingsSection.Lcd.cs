using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateLcdSignalControls(bool led)
    {
        var lcd = led ? _configuration.Led : _configuration.Lcd;
        var panel = Section(led ? Catalog.LedBlock : Catalog.LcdBlock);
        void Set(Func<EmulationLcdVideoConfiguration, EmulationLcdVideoConfiguration> change) =>
            Update(configuration => led ? configuration with { Led = change(configuration.Led) }
                : configuration with { Lcd = change(configuration.Lcd) });
        void AddChoice<T>(string label, IReadOnlyDictionary<T, string> choices, T selected,
            Func<EmulationLcdVideoConfiguration, T, EmulationLcdVideoConfiguration> change, bool rebuild = false)
            where T : struct, Enum =>
            panel.Children.Add(ChoiceField(label, choices, selected, value =>
            {
                Set(config => change(config, value));
                if (rebuild) RebuildContent();
            }));

        panel.Children.Add(CreatePanelSignalRow(lcd, Set));

        AddChoice(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys, lcd.Definition,
            (config, value) => config with { Definition = value });
        AddChoice(Catalog.LcdPixelStructure, Catalog.LcdPixelStructureResourceKeys, lcd.PixelStructure,
            (config, value) => config with { PixelStructure = value }, rebuild: true);
        if (lcd.HasPixelOrientation())
            AddChoice(Catalog.LcdPixelOrientation, Catalog.LcdPixelOrientationResourceKeys, lcd.Orientation,
                (config, value) => config with { Orientation = value });
        AddChoice(Catalog.LcdPanelType, Catalog.LcdPanelTypeResourceKeys, lcd.PanelType,
            (config, value) => config with { PanelType = value });
        if (led)
            AddChoice(Catalog.LedBacklightType, Catalog.LedBacklightTypeResourceKeys, lcd.BacklightType,
                (config, value) => config with { BacklightType = value });
        AddIntensity(panel, Catalog.FixedPixelBacklight, lcd.BacklightIntensity,
            value => Set(config => config with { BacklightIntensity = value }));

        AddChoice(ParameterKey(Catalog.CrtConnection), Catalog.CrtConnectionResourceKeys, lcd.Connection,
            (config, value) => config with { Connection = value }, rebuild: true);
        if (lcd.CanDegradeConnection())
            panel.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
                Catalog.CrtConnectionDegradation, lcd.ConnectionDegradation, 0,
                EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum,
                value => Set(config => config with { ConnectionDegradation = value }),
                1, 38, new Thickness(0, 2, 0, 8)));
        if (lcd.Connection == EmulationCrtConnection.LegacyMonitor)
            AddChoice(ParameterKey(Catalog.CrtMonitorPalette), Catalog.CrtMonitorPaletteResourceKeys, lcd.MonitorPalette,
                (config, value) => config with { MonitorPalette = value });
        return panel;
    }

    private Grid CreatePanelSignalRow<T>(T panel, Action<Func<T, T>> set) where T : EmulationPanelSignalConfiguration
    {
        var signal = new Grid();
        signal.ColumnDefinitions.Add(new ColumnDefinition());
        signal.ColumnDefinitions.Add(new ColumnDefinition());
        var standards = ChoiceField(ParameterKey(Catalog.CrtColorStandards), Catalog.CrtColorStandardsResourceKeys,
            panel.ColorStandards, value => set(config => config with { ColorStandards = value }));
        standards.Margin = new Thickness(0, 0, 6, 0);
        signal.Children.Add(standards);
        var frequency = ChoiceField(ParameterKey(Catalog.CrtFieldRate), Catalog.CrtFieldRateResourceKeys,
            panel.FieldRate, value => set(config => config with { FieldRate = value }));
        frequency.Margin = new Thickness(6, 0, 0, 0);
        Grid.SetColumn(frequency, 1);
        signal.Children.Add(frequency);
        return signal;
    }
}
