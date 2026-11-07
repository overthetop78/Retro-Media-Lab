using RetroMediaLab.App.Constants.Controls.Visual;

namespace RetroMediaLab.App.Constants.Machine;

internal static class MachinePresentationConstants
{
    internal const string CaptureFileExtension = ".png";
    internal const string CaptureTimestampFormat = "yyyyMMdd-HHmmss";
    internal const string CaptureStem = "capture";
    internal const string FileNameSeparator = "-";
    internal const string Direct3D11Renderer = "Direct3D 11";
    internal const string WpfRenderer = "WPF";
    internal const string StatusFormat = "{0} × {1} · {2:0.0} Hz · {3:0.0} FPS";
    internal const string ExecutionRateResourceKey = "Emulation.Status.ExecutionRate";
    internal const double PercentScale = 100d;
    internal const double StatusMaximumWidth = 164d;
    internal const double StatusOverlayHeight = StatusMaximumWidth;
    internal const double StatusOverlayMargin = 8d;
    internal const double StatusOverlayPadding = 8d;
    internal const byte StatusOverlayAlpha = 72;
    internal const double StatusRowHeight = 28d;
    internal const double StatusFontSize = 11d;
    internal const double StatusIconWidth = 22d;
    internal const string SizeStatusFormat = "{0} × {1}";
    internal const string FpsStatusFormat = "{0:0.0} FPS";
    internal const string FrequencyStatusFormat = "{0:0.0} Hz";
    internal const string StatusSeparator = " · ";
    internal static readonly System.Windows.Media.Color RendererStatusColor = System.Windows.Media.Color.FromRgb(155, 211, 218);
    internal static readonly System.Windows.Media.Color SizeStatusColor = System.Windows.Media.Color.FromRgb(185, 213, 239);
    internal static readonly System.Windows.Media.Color FpsStatusColor = System.Windows.Media.Color.FromRgb(174, 218, 188);
    internal static readonly System.Windows.Media.Color FrequencyStatusColor = System.Windows.Media.Color.FromRgb(227, 202, 158);
    internal static readonly System.Windows.Media.Color ExecutionStatusColor = System.Windows.Media.Color.FromRgb(208, 189, 229);
    internal const int TooltipDelayMilliseconds = 250;
    internal const int TooltipDurationMilliseconds = 10000;
    internal static string FloppyGlyph => IconGlyphs.FloppyDisk;
    internal static string HardDiskGlyph => IconGlyphs.HardDisk;
    internal static string CompactDiscGlyph => IconGlyphs.OpticalDisc;
    internal static string CartridgeGlyph => IconGlyphs.Controller;
    internal static string TapeGlyph => IconGlyphs.Tape;
    internal static string MemoryCardGlyph => IconGlyphs.Memory;
    internal const double DefaultAspectRatio = 4d / 3d;
    internal const double WideToolbarMinimumWidth = 1450d;
    internal const double EmptyMeasurement = 0d;
    internal const int InactiveFramePending = 0;
    internal const int ActiveFramePending = 1;
    internal const int FrameRateWindowSeconds = 1;
    internal const int UiFrameNotificationMilliseconds = 100;
    internal const int FirstDuplicateSuffix = 2;
}
