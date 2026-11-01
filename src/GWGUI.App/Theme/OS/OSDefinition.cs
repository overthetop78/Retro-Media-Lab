using GWGUI.App.Contracts.Theming;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Infrastructure.Settings;

namespace GWGUI.App.Theme.OS;

internal static class OSDefinition
{
    private const string NameResource = "Options.Themes.OS.Name";
    private const string DescriptionResource = "Options.Themes.OS.Description";
    private const string EditionResource = "Options.Themes.Edition";

    internal static ThemeDefinition Definition { get; } = new(AppThemeDesign.OS, NameResource, DescriptionResource,
        [AppTheme.Light], AppTheme.Light)
    {
        Editions = [Windows31Edition.Definition],
        EditionHeadingResourceKey = EditionResource
    };
}
