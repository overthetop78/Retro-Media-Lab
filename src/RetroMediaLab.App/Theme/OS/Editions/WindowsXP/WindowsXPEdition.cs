using RetroMediaLab.App.Contracts.Theming;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPEdition
{
    internal const string Id = nameof(WindowsXP);
    private const string NameResource = "Options.Themes.OS.WindowsXP";
    private const string PaletteResource = "Options.Themes.OS.XP.";
    private const string PreviewPrefix = "/retromedialab.app;component/Theme/OS/Editions/WindowsXP/Assets/Previews/";
    private const string PreviewSuffix = ".png";

    internal static ThemeEditionDefinition Definition { get; } = new(Id, NameResource,
        Array.AsReadOnly(Enum.GetValues<WindowsXPColorScheme>().Select(scheme =>
            new ThemeStyleDefinition(scheme.ToString(), PaletteResource + scheme)).ToArray()),
        nameof(WindowsXPColorScheme.Blue))
    {
        PreviewUri = style => PreviewPrefix + style + PreviewSuffix,
        CreateWorkspace = (root, tabs) => new WindowsXPWorkspace(root, tabs),
        ApplyStyle = (target, value) => WindowsXPResources.ApplyScheme(target,
            Enum.TryParse<WindowsXPColorScheme>(value, out var scheme) && Enum.IsDefined(scheme)
                ? scheme : WindowsXPColorScheme.Blue)
    };
}
