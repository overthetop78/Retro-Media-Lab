using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Dictionaries.Theming;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Views.Controls.Options;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class ThemeEditionSelectionTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task PaletteChoicesBelongToAnEditionAndSurviveNavigationAndSaveRetry() => sta.RunAsync(async () =>
    {
        // Exercise the generic selector with unpublished edition definitions, without registering OS.
        const string otherEdition = "TestEdition";
        var baseDefinition = ThemeCatalog.Get(AppThemeDesign.Base);
        var definition = ThemeCatalog.Get(AppThemeDesign.Studio) with
        {
            Editions = [Windows31Edition.Definition, new ThemeEditionDefinition(otherEdition, "Tab.Read",
                [new ThemeStyleDefinition(nameof(AppTheme.Light), "Options.Theme.Light")], nameof(AppTheme.Light))]
        };
        var section = new OptionsThemesSection();
        ThemeEditionSettings? saved = null;
        var successful = false;
        section.Configure(AppThemeDesign.Studio, AppTheme.Dark,
            new Dictionary<AppThemeDesign, ThemeEditionSettings>(), (_, _, edition) =>
            {
                saved = edition.Copy();
                return Task.FromResult(successful);
            }, [baseDefinition, definition]);
        Assert.False(section.SaveAction.IsEnabled);
        Assert.Equal(23, section.EditionStyleSelector.Items.Count);
        section.EditionStyleSelector.SelectedIndex = Windows31PaletteCatalog.All.ToList()
            .FindIndex(palette => palette.Id == Windows31ColorScheme.Bordeaux);
        var selected = section.EditionStyleSelector.SelectedItem;
        section.EditionSelector.SelectedIndex = 1;
        Assert.Single(section.EditionStyleSelector.Items);
        section.EditionSelector.SelectedIndex = 0;
        Assert.Equal(selected, section.EditionStyleSelector.SelectedItem);
        section.Themes.SelectedIndex = 0;
        section.Themes.SelectedIndex = 1;
        Assert.Equal(selected, section.EditionStyleSelector.SelectedItem);
        await section.SaveSelectedAsync();
        Assert.True(section.SaveAction.IsEnabled);
        Assert.Equal(Windows31Edition.Id, saved!.Edition);
        Assert.Equal(nameof(Windows31ColorScheme.Bordeaux), saved.Styles[Windows31Edition.Id]);
        Assert.Equal(nameof(AppTheme.Light), saved.Styles[otherEdition]);
        successful = true;
        await section.SaveSelectedAsync();
        Assert.False(section.SaveAction.IsEnabled);
        Assert.Equal(new[] { AppThemeDesign.Base, AppThemeDesign.Studio, AppThemeDesign.Orbit, AppThemeDesign.OS }, ThemeCatalog.All.Select(theme => theme.Id));
        Assert.Empty(ThemeCatalog.Get(AppThemeDesign.Base).Editions);
        Assert.Empty(ThemeCatalog.Get(AppThemeDesign.Studio).Editions);
        Assert.Collection(ThemeCatalog.Get(AppThemeDesign.OS).Editions,
            edition => Assert.Same(Windows31Edition.Definition, edition),
            edition => Assert.Same(RetroMediaLab.App.Theme.OS.Editions.Windows95.Windows95Edition.Definition, edition),
            edition => Assert.Same(RetroMediaLab.App.Theme.OS.Editions.WindowsXP.WindowsXPEdition.Definition, edition));
    });

    [Fact]
    public async Task EditionAndIndependentStylesPersistWithoutChangingLegacyLightDarkSettings()
    {
        const string path = "edition-settings.json";
        var settings = new AppSettings { Theme = AppTheme.Dark };
        settings.ThemeEditions[AppThemeDesign.Studio] = new ThemeEditionSettings
        {
            Edition = Windows31Edition.Id,
            Styles = { [Windows31Edition.Id] = nameof(Windows31ColorScheme.Bordeaux), ["TestEdition"] = nameof(AppTheme.Light) }
        };
        var files = new SyntheticData();
        await new JsonSettingsStore(path, files, () => DateTime.UnixEpoch).SaveAsync(settings);
        var restored = await new JsonSettingsStore(path, files, () => DateTime.UnixEpoch).LoadAsync();
        Assert.Equal(AppTheme.Dark, restored.Theme);
        Assert.True(settings.ThemeEditions[AppThemeDesign.Studio].HasSameChoices(restored.ThemeEditions[AppThemeDesign.Studio]));
        var copy = restored.ThemeEditions[AppThemeDesign.Studio].Copy();
        copy.Styles[Windows31Edition.Id] = nameof(Windows31ColorScheme.HotdogStand);
        Assert.Equal(nameof(Windows31ColorScheme.Bordeaux), restored.ThemeEditions[AppThemeDesign.Studio].Styles[Windows31Edition.Id]);
    }
}
