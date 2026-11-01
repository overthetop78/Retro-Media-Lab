using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Theme.OS.Editions.Windows95;
using RetroMediaLab.App.Theme.OS.Editions.WindowsXP;
using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Theme.OS;

internal static class OSDefinition
{
    private const string NameResource = "Options.Themes.OS.Name";
    private const string DescriptionResource = "Options.Themes.OS.Description";
    private const string EditionResource = "Options.Themes.Edition";

    internal static ThemeDefinition Definition { get; } = new(AppThemeDesign.OS, NameResource, DescriptionResource,
        [AppTheme.Light], AppTheme.Light)
    {
        Editions = [Windows31Edition.Definition, Windows95Edition.Definition, WindowsXPEdition.Definition],
        EditionHeadingResourceKey = EditionResource
    };
}
