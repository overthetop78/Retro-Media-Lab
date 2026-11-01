using System.Windows;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

internal static class Windows95Metrics
{
    public const bool CommandActiveState = true;
    public const double CaptionHeight = 18;
    public static readonly GridLength CaptionRowHeight = new(CaptionHeight);
    public const double CaptionButtonWidth = 16;
    public const double CaptionButtonHeight = 14;
    public static readonly Thickness ResizeBorder = new(4);
    public const double IconSize = 16;
    public const double FontSize = 11;
    public const double MinimumWidth = 240;
    public const double MinimumHeight = 120;
    internal const string DocumentPart = "PART_Document";
    internal const string ChromeUri = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Chrome.xaml";
}
