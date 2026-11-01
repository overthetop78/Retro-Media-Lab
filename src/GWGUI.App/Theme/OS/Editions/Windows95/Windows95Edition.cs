using GWGUI.App.Contracts.Theming;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

internal static class Windows95Edition
{
    internal const string Id = nameof(Windows95);
    private const string NameResource = "Options.Themes.OS.Windows95";
    private const string DefaultStyle = "Default";
    private const string DefaultResource = "Profile.Default";
    private const string Preview = "/gwgui.app;component/Theme/OS/Editions/Windows95/Assets/Previews/Default.png";

    internal static ThemeEditionDefinition Definition { get; } = new(Id, NameResource,
        Array.AsReadOnly<ThemeStyleDefinition>([new(DefaultStyle, DefaultResource)]), DefaultStyle)
    {
        PreviewUri = _ => Preview,
        CreateWorkspace = (root, tabs) => new Windows95Workspace(root, tabs)
    };
}
