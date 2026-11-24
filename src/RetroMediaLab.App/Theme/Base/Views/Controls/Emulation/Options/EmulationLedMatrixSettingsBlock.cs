using System.Windows;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateLedMatrixPanel()
    {
        var matrix = _configuration.LedMatrix;
        var section = Section("Emulation.Video.Technology.LedMatrix");
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.LedMatrixColor], Catalog.LedMatrixColorResourceKeys,
            matrix.Color, value => SetLedMatrix(_configuration.LedMatrix with { Color = value })));
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.LedMatrixDefinition], Catalog.LedMatrixDefinitionResourceKeys,
            matrix.Definition, value => Update(config => config with { LedMatrix = config.LedMatrix with { Definition = value } })));
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.LedMatrixShape], Catalog.LedMatrixShapeResourceKeys,
            matrix.Shape, value => Update(config => config with { LedMatrix = config.LedMatrix with { Shape = value } })));
        return section;
    }
}
