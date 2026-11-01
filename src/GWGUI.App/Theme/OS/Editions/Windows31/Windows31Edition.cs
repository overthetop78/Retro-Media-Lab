using GWGUI.App.Contracts.Theming;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal static class Windows31Edition
{
    internal const string Id = nameof(Windows31);
    private const string PreviewPrefix = "/gwgui.app;component/Theme/OS/Editions/Windows31/Assets/Previews/";
    private const string PreviewSuffix = ".png";
    private const string NameResource = "Options.Themes.OS.Windows31";

    internal static ThemeEditionDefinition Definition { get; } = new(Id, NameResource,
        Array.AsReadOnly(Windows31PaletteCatalog.All.Select(palette =>
            new ThemeStyleDefinition(palette.Id.ToString(), palette.NameResourceKey)).ToArray()),
        nameof(Windows31ColorScheme.WindowsDefault))
    {
        PreviewUri = style => PreviewPrefix + style + PreviewSuffix,
        CreateWorkspace = (root, tabs) => new Windows31Workspace(root, tabs),
        ApplyStyle = (target, value) => Windows31PaletteResources.Apply(target,
            Enum.TryParse<Windows31ColorScheme>(value, out var scheme) && Enum.IsDefined(scheme)
                ? scheme : Windows31ColorScheme.WindowsDefault)
    };
}
