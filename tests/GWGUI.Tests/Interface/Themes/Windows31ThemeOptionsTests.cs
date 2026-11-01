using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Dictionaries.Theming;
using GWGUI.App.Theme.OS;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Options;
using GWGUI.Infrastructure.Settings;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ThemeOptionsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task IndependentDisplayPanelKeepsThemeVariantAndPaletteAsSeparateSavedChoices() => sta.Run(() =>
    {
        var source = new OptionsThemesSection();
        var saves = new List<(AppThemeDesign Design, AppTheme Appearance, ThemeEditionSettings Edition)>();
        source.Configure(AppThemeDesign.Base, AppTheme.Light, new Dictionary<AppThemeDesign, ThemeEditionSettings>(),
            (design, appearance, edition) => { saves.Add((design, appearance, edition.Copy())); return Task.FromResult(true); },
            [ThemeCatalog.Get(AppThemeDesign.Base), OSDefinition.Definition]);
        var originalParent = source.Themes.Parent;
        using var view = new ThemeOptionsView(source);
        var choices = Field<ComboBox>(view, "ThemeChoices");
        Assert.Same(source.Themes.ItemsSource, choices.ItemsSource);
        var variants = Field<WrapPanel>(view, "Variants");
        var dark = source.AppearanceOptions.ToList().FindIndex(option => Equals(option.Tag, AppTheme.Dark));
        ((RadioButton)variants.Children[dark]).IsChecked = true;
        Field<Button>(view, "ChooseAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(AppTheme.Dark, saves.Last().Appearance);
        choices.SelectedIndex = 1;
        Assert.Equal(AppThemeDesign.OS, source.SelectedTheme);
        Assert.Equal(Visibility.Collapsed, variants.Visibility);
        var editions = Field<ComboBox>(view, "EditionChoices");
        var palettes = Field<ComboBox>(view, "EditionAppearance");
        Assert.Equal(3, editions.Items.Count);
        Assert.Equal(23, palettes.Items.Count);
        palettes.SelectedIndex = Windows31PaletteCatalog.All.ToList().FindIndex(palette => palette.Id == Windows31ColorScheme.Bordeaux);
        Field<Button>(view, "ChooseAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var saved = saves.Last();
        Assert.Equal(AppThemeDesign.OS, saved.Design);
        Assert.Equal(Windows31Edition.Id, saved.Edition.Edition);
        Assert.Equal(nameof(Windows31ColorScheme.Bordeaux), saved.Edition.Styles[Windows31Edition.Id]);
        Assert.Equal(AppTheme.Light, saved.Appearance);
        Assert.Same(originalParent, source.Themes.Parent);
        view.Dispose();
        choices.SelectedIndex = 0;
        Assert.Equal(AppThemeDesign.OS, source.SelectedTheme);
        Assert.Null(view.Content);
    });

    private static T Field<T>(FrameworkElement root, string name) => Assert.IsType<T>(root.FindName(name));
}
