using System.Windows;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateProjectionPanel()
    {
        var projection = _configuration.Projection;
        var section = Section("Emulation.Video.Technology.Projection");
        void Set(Func<EmulationProjectionVideoConfiguration, EmulationProjectionVideoConfiguration> change) =>
            Update(configuration => configuration with { Projection = change(configuration.Projection) });

        section.Children.Add(CreatePanelSignalRow(projection, Set));
        section.Children.Add(ChoiceField(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys,
            projection.Definition, value => Set(config => config with { Definition = value })));
        section.Children.Add(ChoiceField(Catalog.ProjectionTechnology, Catalog.ProjectionTechnologyResourceKeys,
            projection.Technology, value => Set(config => config with { Technology = value })));
        section.Children.Add(ChoiceField(Catalog.ProjectionSurface, Catalog.ProjectionSurfaceResourceKeys,
            projection.Surface, value => Set(config => config with { Surface = value })));
        section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
            Catalog.ProjectionAmbientLight, projection.AmbientLight, 0,
            EmulationProjectionVideoConfiguration.MaximumAmbientLight,
            value => Set(config => config with { AmbientLight = value }), 1, 38, new Thickness(0, 2, 0, 8)));
        section.Children.Add(ChoiceField(ParameterKey(Catalog.CrtConnection), Catalog.ProjectionConnectionResourceKeys,
            projection.Connection, value =>
            {
                Set(config => config with { Connection = value });
                RebuildContent();
            }));
        if (projection.CanDegradeConnection())
            section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
                Catalog.CrtConnectionDegradation, projection.ConnectionDegradation, 0,
                EmulationVideoProcessingLimits.CrtConnectionDegradationMaximum,
                value => Set(config => config with { ConnectionDegradation = value }), 1, 38, new Thickness(0, 2, 0, 8)));
        return section;
    }
}
