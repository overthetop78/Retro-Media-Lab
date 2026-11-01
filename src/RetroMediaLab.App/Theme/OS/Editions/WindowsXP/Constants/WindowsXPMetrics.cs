using System.Windows;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPMetrics
{
    public const double FontSize = 11;
    public const double CaptionButtonWidth = 21;
    public const double CaptionButtonHeight = 21;
    public const double MinimumWidth = 240;
    public const double MinimumHeight = 120;
    public const double IconSize = 16;
    public static readonly Thickness ResizeBorder = new(4);
    public static readonly CornerRadius CaptionCorners = new(8, 8, 0, 0);
    public static readonly GridLength SideColumnWidth = new(5);
    public static readonly GridLength BottomRowHeight = new(5);
    public static readonly Thickness CommandMargin = new(0, 5, 5, 0);
    public static readonly Thickness CommandSpacing = new(0, 0, 2, 0);
    public static readonly Thickness CaptionTextMargin = new(7, 0, 76, 0);
    public static readonly Thickness DialogCaptionTextMargin = new(7, 0, 30, 0);
    public static readonly Thickness IconMargin = new(0, 0, 4, 0);
    public static readonly double CaptionShadowDepth = Math.Sqrt(2);
    internal const string DocumentPart = "PART_Document";
    internal const string ChromeUri = "/retromedialab.app;component/Theme/OS/Editions/WindowsXP/Styles/WindowsXPChrome.xaml";
}
