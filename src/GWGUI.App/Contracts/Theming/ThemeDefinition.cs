using GWGUI.Infrastructure.Settings;
using GWGUI.App.Constants.Theming;
using System.Windows.Controls;

namespace GWGUI.App.Contracts.Theming;

internal sealed record ThemeDefinition(AppThemeDesign Id, string NameResourceKey, string DescriptionResourceKey,
    IReadOnlyList<AppTheme> Variants, AppTheme DefaultVariant)
{
    internal IReadOnlyList<ThemeEditionDefinition> Editions { get; init; } = [];
    internal string EditionHeadingResourceKey { get; init; } = "Options.Themes.Appearance";
    internal ThemeEditionDefinition? FindEdition(string? id) => Editions.FirstOrDefault(edition => edition.Id == id)
        ?? Editions.FirstOrDefault();
    internal Func<Grid, TabControl, IThemeWorkspace>? CreateWorkspace { get; init; }
    internal string? WindowStyleResource { get; init; }
    internal string? ControlsUri { get; init; }
    internal AppTheme Normalize(AppTheme variant) => Variants.Contains(variant) ? variant : DefaultVariant;
    internal string PaletteUri(bool dark) => ThemeConstants.PackPrefix + Id + (dark ? ThemeConstants.DarkPaletteSuffix : ThemeConstants.LightPaletteSuffix);
    internal string PreviewUri(AppTheme variant) => ThemeConstants.PackPrefix + Id + (variant == AppTheme.Dark ? ThemeConstants.DarkPreviewSuffix : ThemeConstants.LightPreviewSuffix);
}
