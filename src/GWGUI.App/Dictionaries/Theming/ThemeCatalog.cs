using GWGUI.App.Constants.Theming;
using GWGUI.App.Contracts.Theming;
using GWGUI.Infrastructure.Settings;

namespace GWGUI.App.Dictionaries.Theming;

internal static class ThemeCatalog
{
    internal static IReadOnlyList<ThemeDefinition> All { get; } = Array.AsReadOnly<ThemeDefinition>(
    [
        Profile(AppThemeDesign.Base, [AppTheme.System, AppTheme.Light, AppTheme.Dark], AppTheme.System),
        Profile(AppThemeDesign.Studio, [AppTheme.Light, AppTheme.Dark], AppTheme.Dark) with
        {
            WindowStyleResource = GWGUI.App.Theme.Studio.StudioConstants.WindowStyle,
            ControlsUri = ThemeConstants.PackPrefix + "Studio/Controls.xaml",
            CreateWorkspace = (root, tabs) => new GWGUI.App.Theme.Studio.StudioWorkspace(root, tabs)
        }
    ]);

    internal static ThemeDefinition Get(AppThemeDesign id) => All.FirstOrDefault(theme => theme.Id == id) ?? All[ThemeConstants.DefaultIndex];

    private static ThemeDefinition Profile(AppThemeDesign id, IReadOnlyList<AppTheme> variants, AppTheme defaultVariant) => new(id,
        ThemeConstants.ResourcePrefix + id + ThemeConstants.NameSuffix,
        ThemeConstants.ResourcePrefix + id + ThemeConstants.DescriptionSuffix, Array.AsReadOnly(variants.ToArray()), defaultVariant);
}
