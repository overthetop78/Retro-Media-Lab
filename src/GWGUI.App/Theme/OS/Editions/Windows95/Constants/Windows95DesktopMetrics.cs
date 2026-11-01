using System.Windows;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

internal static class Windows95DesktopMetrics
{
    internal const string FontFamily = "Microsoft Sans Serif";
    internal const string DesktopBackgroundResource = "Windows95DesktopBackground";
    internal const string FaceResource = "Windows95Face";
    public const double TaskbarHeight = 28;
    public const double LauncherWidth = 84;
    public const double LauncherHeight = 76;
    public const double LauncherIconSize = 32;
    public const double TaskWidth = 156;
    public const double TaskHeight = 22;
    public static readonly Thickness ItemMargin = new(2);
    public static readonly Thickness ButtonPadding = new(6, 2, 6, 2);
    public static readonly Thickness DesktopPadding = new(8);
    internal const string StartResource = "Options.Themes.OS.Start";
    internal const string ProgramsResource = "Options.Themes.OS.Programs";
    internal const string MediaResource = "Explorer.Category.Media";
    internal const string EmulationResource = "Tab.Emulation";
    internal const string SystemResource = "Options.Themes.OS.SystemGroup";
    internal const string StylesUri = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Desktop.xaml";
    internal const string DesktopButtonStyle = "Windows95DesktopButton";
    internal const string StartMenuStyle = "Windows95StartMenu";
    internal const string TaskButtonStyle = "Windows95TaskButton";
}
