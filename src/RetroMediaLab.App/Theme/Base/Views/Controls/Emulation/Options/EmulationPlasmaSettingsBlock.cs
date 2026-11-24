using System.Windows;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreatePlasmaPanel()
    {
        var plasma = _configuration.Plasma;
        var section = Section("Emulation.Video.Technology.Plasma");
        void Set(Func<EmulationPlasmaVideoConfiguration, EmulationPlasmaVideoConfiguration> change) =>
            Update(configuration => configuration with { Plasma = change(configuration.Plasma) });

        section.Children.Add(CreatePanelSignalRow(plasma, Set));
        section.Children.Add(ChoiceField(Catalog.LcdDefinition, Catalog.PlasmaDefinitionResourceKeys,
            plasma.Definition, value => Set(config => config with { Definition = value })));
        section.Children.Add(ChoiceField(ParameterKey(Catalog.PlasmaCellStructure), Catalog.PlasmaCellStructureResourceKeys,
            plasma.CellStructure, value => Set(config => config with { CellStructure = value })));
        section.Children.Add(ChoiceField(ParameterKey(Catalog.CrtConnection), Catalog.PlasmaConnectionResourceKeys,
            plasma.Connection, value =>
            {
                Set(config => config with { Connection = value });
                RebuildContent();
            }));
        if (plasma.CanDegradeConnection())
            section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
                Catalog.CrtConnectionDegradation, plasma.ConnectionDegradation, 0,
                EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum,
                value => Set(config => config with { ConnectionDegradation = value }),
                1, 38, new Thickness(0, 2, 0, 8)));
        return section;
    }
}
