using System.Windows;
using System.Windows.Media;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal static class Windows31Metrics
{
    public const double DesktopInset = 12;
    public const double IconWidth = 96;
    public const double IconHeight = 72;
    public const double IconImageSize = 32;
    public const double IconGap = 8;
    public const double CascadeStep = 24;
    public const double GroupMinimumWidth = 200;
    public const double GroupMinimumHeight = 150;
    public const double ApplicationMinimumWidth = 320;
    public const double ApplicationMinimumHeight = 240;
    public const double ManagerInitialWidth = 900;
    public const double ManagerInitialHeight = 620;
    public const double GroupInitialWidth = 500;
    public const double GroupInitialHeight = 270;
    public const string ProgramManagerResource = "Options.Themes.OS.ProgramManager";
    public const string SystemGroupResource = "Options.Themes.OS.SystemGroup";
    public const string MediaGroupResource = "Explorer.Category.Media";
    public const string EmulationGroupResource = "Tab.Emulation";
    public const string ControlsResource = "/gwgui.app;component/Theme/OS/Editions/Windows31/Styles/Windows31Controls.xaml";
    public const int GroupIconColumns = 4;
    public const string LauncherStyleKey = "ProgramLauncher";
    public const string GroupIconStyleKey = "GroupIconCommand";
    public static Brush DesktopBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.Desktop];
    public const double BodyFontSize = 13;
    public const double CaptionFontSize = 16;
    public const double FixedFontSize = 15;
    public const string BodyFontResource = "Windows31BodyFont";
    public const string CaptionFontResource = "Windows31CaptionFont";
    public const string FixedFontResource = "Windows31FixedFont";
    public const double FrameInset = 3;
    public const double CaptionHeight = 18;
    public const double CaptionCommandWidth = 18;
    public const double CaptionTextInset = 2;
    public const string MinimizeResource = "Options.Themes.Minimize";
    public const string MaximizeResource = "Options.Themes.MaximizeRestore";
    public const string CloseResource = "Common.Close";
    public const string RestoreResource = "Options.Themes.OS.Restore";
    public const string MoveResource = "Options.Themes.OS.Move";
    public const string SizeResource = "Options.Themes.OS.Size";
    public const string EnlargeResource = "Options.Themes.OS.Maximize";
    public const double ResizeBorder = 5;
    public const double ResizeCorner = 14;
    public const double KeyboardPlacementStep = 8;
    public const double SystemMenuMinimumWidth = 150;
    public const double SystemMenuRowHeight = 22;
    public const double OutlineThickness = 2;
    public const double OutlinePatternSize = 2;
    public static GridLength CaptionRowHeight { get; } = new(CaptionHeight);
    public static Thickness FramePadding { get; } = new(FrameInset);
    public static Brush FrameBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.ButtonFace];
    public static Brush EdgeBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.WindowFrame];
    public static Brush HighlightBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.ButtonHighlight];
    public static Brush ShadowBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.ButtonShadow];
    public static Brush ActiveCaptionBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.ActiveCaption];
    public static Brush InactiveCaptionBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.InactiveCaption];
    public static Brush ActiveCaptionTextBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.CaptionText];
    public static Brush InactiveCaptionTextBrush => Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault)[Windows31ColorRole.InactiveCaptionText];

}
