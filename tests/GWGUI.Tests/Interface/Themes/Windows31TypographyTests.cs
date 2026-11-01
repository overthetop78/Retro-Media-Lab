using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31TypographyTests(StaExecutionScenarios sta)
{
    private const string FontRoot = "pack://application:,,,/gwgui.app;component/Theme/OS/Editions/Windows31/Assets/Fonts/";

    [Fact]
    public Task EmbeddedFacesLoadWithRasterCellWidthsAndCompositeFallback() => sta.Run(() =>
    {
        var resources = new ResourceDictionary { Source = new Uri(Windows31Metrics.ControlsResource, UriKind.Relative) };
        foreach (var sample in new[]
        {
            (File: "SansVGA.ttf", Key: Windows31Metrics.BodyFontResource, Size: Windows31Metrics.BodyFontSize, Width: 7d, Weight: FontWeights.Normal),
            (File: "SystemVGA.ttf", Key: Windows31Metrics.CaptionFontResource, Size: Windows31Metrics.CaptionFontSize, Width: 8d, Weight: FontWeights.Bold),
            (File: "FixedVGA.ttf", Key: Windows31Metrics.FixedFontResource, Size: Windows31Metrics.FixedFontSize, Width: 8d, Weight: FontWeights.Normal)
        })
        {
            var physical = new GlyphTypeface(new Uri(FontRoot + sample.File));
            var glyph = physical.CharacterToGlyphMap['A'];
            Assert.Equal(sample.Width, physical.AdvanceWidths[glyph] * sample.Size, 6);
            Assert.Contains('é', physical.CharacterToGlyphMap.Keys.Select(code => (char)code));
            Assert.DoesNotContain(0x2026, physical.CharacterToGlyphMap.Keys);
            Assert.DoesNotContain(0x2014, physical.CharacterToGlyphMap.Keys);
            var family = Assert.IsType<FontFamily>(resources[sample.Key]);
            var text = new FormattedText("A", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(family, FontStyles.Normal, sample.Weight, FontStretches.Normal), sample.Size, Brushes.Black, 1);
            Assert.Equal(sample.Width, text.WidthIncludingTrailingWhitespace, 6);
        }
    });

}
