using System.Windows;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateEPaperPanel()
    {
        var paper = _configuration.EPaper;
        var section = Section("Emulation.Video.Technology.EPaper");
        section.Children.Add(ChoiceField(Catalog.ParameterResourceKeys[Catalog.EPaperColorMode], Catalog.EPaperColorModeResourceKeys,
            paper.ColorMode, value => SetEPaper(_configuration.EPaper with { ColorMode = value })));
        section.Children.Add(ChoiceField(Catalog.LcdDefinition, Catalog.LcdDefinitionResourceKeys,
            paper.Definition, value => SetEPaper(_configuration.EPaper with { Definition = value })));
        return section;
    }
}
