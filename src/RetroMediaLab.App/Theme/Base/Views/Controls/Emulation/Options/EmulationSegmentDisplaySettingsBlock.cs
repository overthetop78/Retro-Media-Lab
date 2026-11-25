using System.Windows;
using Catalog = RetroMediaLab.VideoPresentation.Dictionaries.EmulationVideoProcessingCatalog;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection
{
    private FrameworkElement CreateSegmentDisplayPanel()
    {
        var display = _configuration.SegmentDisplay;
        var section = Section("Emulation.Video.Technology.SegmentDisplay");
        section.Children.Add(ChoiceField(ParameterKey(Catalog.SegmentDisplayLayout), Catalog.SegmentDisplayLayoutResourceKeys,
            display.Layout, value => SetSegmentDisplay(_configuration.SegmentDisplay with { Layout = value })));
        section.Children.Add(ChoiceField(ParameterKey(Catalog.SegmentDisplayTechnology), Catalog.SegmentDisplayTechnologyResourceKeys,
            display.Technology, value =>
            {
                SetSegmentDisplay(_configuration.SegmentDisplay with { Technology = value });
                RebuildContent();
            }));
        section.Children.Add(ChoiceField(ParameterKey(Catalog.SegmentDisplayColor), Catalog.SegmentColors(display.Technology),
            display.Color, value => SetSegmentDisplay(_configuration.SegmentDisplay with { Color = value })));
        return section;
    }
}
