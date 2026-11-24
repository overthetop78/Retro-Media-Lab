using System.Windows;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateDotMatrixPanel()
    {
        var matrix = _configuration.DotMatrix;
        var section = Section("Emulation.Video.Technology.DotMatrix");
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.DotMatrixPalette], Catalog.DotMatrixPaletteResourceKeys,
            matrix.Palette, value => SetDotMatrix(_configuration.DotMatrix with { Palette = value })));
        section.Children.Add(ChoiceField(Catalog.LcdDefinition, Catalog.LedMatrixDefinitionResourceKeys,
            matrix.Definition, value => SetDotMatrix(_configuration.DotMatrix with { Definition = value })));
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.DotMatrixShape], Catalog.DotMatrixShapeResourceKeys,
            matrix.Shape, value => SetDotMatrix(_configuration.DotMatrix with { Shape = value })));
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.DotMatrixTechnology], Catalog.DotMatrixTechnologyResourceKeys,
            matrix.Technology, value => SetDotMatrix(_configuration.DotMatrix with { Technology = value })));
        return section;
    }
}
