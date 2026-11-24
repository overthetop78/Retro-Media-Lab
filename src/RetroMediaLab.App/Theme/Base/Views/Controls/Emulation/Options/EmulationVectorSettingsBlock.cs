using System.Windows;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateVectorPanel()
    {
        var vector = _configuration.Vector;
        var section = Section("Emulation.Video.Screen.Vector");
        void Set(Func<EmulationVectorVideoConfiguration, EmulationVectorVideoConfiguration> change) =>
            Update(configuration => configuration with { Vector = change(configuration.Vector) });

        section.Children.Add(ChoiceField(EmulationResourceKeys.VideoTechnology, Catalog.VectorPhosphorResourceKeys,
            vector.PhosphorColor, value => Set(config => config with { PhosphorColor = value })));
        section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
            Catalog.VectorBeamWidth, vector.BeamWidth, EmulationVectorVideoConfiguration.MinimumBeamWidth,
            EmulationVectorVideoConfiguration.MaximumBeamWidth,
            value => Set(config => config with { BeamWidth = value }), 1, 38, new Thickness(0, 2, 0, 8)));
        section.Children.Add(EmulationSettingsFieldControlFunctions.CreateVideoSlider(
            Catalog.CrtCurvature, vector.Curvature, EmulationVideoProcessingLimits.CrtCurvatureMinimum,
            EmulationVideoProcessingLimits.CrtCurvatureMaximum,
            value => Set(config => config with { Curvature = value }), 1, 38, new Thickness(0, 2, 0, 8)));
        return section;
    }
}
