using System.Windows;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPDesktopMetrics
{
    internal const string StylesUri = "/retromedialab.app;component/Theme/OS/Editions/WindowsXP/Styles/WindowsXPDesktop.xaml";
    internal const string StartResource = "Options.Themes.OS.Start";
    internal const string ThemesResource = "Options.Themes.Title";
    internal const string DesktopButtonStyle = "WindowsXP.DesktopButton";
    internal const string StartButtonStyle = "WindowsXP.StartButton";
    internal const string TaskButtonStyle = "WindowsXP.TaskButton";
    internal const string ProgramButtonStyle = "WindowsXP.ProgramButton";
    internal const double MenuWidth = 380;
    internal const double MenuHeight = 440;
    internal const double MenuHeaderHeight = 64;
    internal const double MenuFooterHeight = 40;
    internal const double TaskbarHeight = 30;
    internal const double StartWidth = 99;
    internal const double StartLogoWidth = 25;
    internal const double StartLogoHeight = 20;
    internal const double TaskWidth = 160;
    internal const double MinimumTaskWidth = 80;
    internal const double TaskPagingButtonWidth = 18;
    internal const double TaskArrowSize = 7;
    internal const string PreviousTaskArrow = "M 5,0 L 0,3 L 5,6";
    internal const string NextTaskArrow = "M 0,0 L 5,3 L 0,6";
    internal const double TaskHeight = 28;
    internal const double LauncherWidth = 80;
    internal const double LauncherHeight = 78;
    internal const double LauncherIconSize = 32;
    internal const double SmallIconSize = 16;
    internal const double UserTitleSize = 18;
    internal static readonly Thickness DesktopPadding = new(8);
    internal static readonly Thickness IconGap = new(0, 0, 5, 0);
    internal static readonly Thickness StartLogoGap = new(0, 0, 3, 0);
    internal static readonly Thickness ProgramPadding = new(6);
    internal static readonly Thickness UserTitlePadding = new(12);
}
