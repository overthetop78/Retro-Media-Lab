using System.Windows;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateVfdPanel()
    {
        var vfd = _configuration.Vfd;
        var section = Section("Emulation.Video.Technology.Vfd");
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.VfdColor], Catalog.VfdColorResourceKeys,
            vfd.Color, value => SetVfd(_configuration.Vfd with { Color = value })));
        section.Children.Add(ChoiceField(Catalog.LcdDefinition, Catalog.LedMatrixDefinitionResourceKeys,
            vfd.Definition, value => SetVfd(_configuration.Vfd with { Definition = value })));
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.VfdShape], Catalog.DotMatrixShapeResourceKeys,
            vfd.Shape, value => SetVfd(_configuration.Vfd with { Shape = value })));
        return section;
    }
}
