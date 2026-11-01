using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPSystemMetrics
{
    private const string MetricsUri = "pack://application:,,,/gwgui.app;component/Theme/OS/Editions/WindowsXP/Assets/Metrics/SystemMetrics.json";
    private const string SystemSection = "sysmetrics";
    private const string WindowSection = "window";
    private const string FaceKey = "btnface";
    private const string DesktopKey = "background";
    private const string ShadowKey = "btnshadow";
    private const string HighlightKey = "btnhighlight";
    private const string SelectionKey = "highlight";
    private const string CaptionFontKey = "captionfont";
    private const string DialogFontKey = "msgboxfont";
    private const string CaptionTextKey = "captiontext";
    private const string InactiveCaptionTextKey = "inactivecaptiontext";
    private const string CaptionShadowKey = "textshadowcolor";
    private const string BoldFlag = "bold";
    private const string StartSection = "start::button";
    private const string UserSection = "startpanel.userpane";
    private const string TaskSection = "taskband::toolbar";
    private const string FontKey = "font";
    private const string TextColorKey = "textcolor";
    private const double ReferenceDpi = 96;
    private const double PointsPerInch = 72;
    private const int RedIndex = 0;
    private const int GreenIndex = 1;
    private const int BlueIndex = 2;
    private const int FamilyIndex = 0;
    private const int SizeIndex = 1;
    private const int WeightIndex = 2;
    private static readonly Lazy<Dictionary<WindowsXPColorScheme, Dictionary<string, Dictionary<string, string>>>> Values = new(Load);

    internal static SolidColorBrush Face(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, FaceKey);
    internal static SolidColorBrush Desktop(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, DesktopKey);
    internal static SolidColorBrush ControlShadow(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, ShadowKey);
    internal static SolidColorBrush ControlHighlight(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, HighlightKey);
    internal static SolidColorBrush Selection(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, SelectionKey);
    internal static SolidColorBrush CaptionText(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, CaptionTextKey);
    internal static SolidColorBrush InactiveCaptionText(WindowsXPColorScheme scheme) => Brush(scheme, SystemSection, InactiveCaptionTextKey);
    internal static SolidColorBrush CaptionShadow(WindowsXPColorScheme scheme) => Brush(scheme, WindowSection, CaptionShadowKey);
    internal static WindowsXPFont CaptionFont(WindowsXPColorScheme scheme) => Font(scheme, CaptionFontKey);
    internal static WindowsXPFont DialogFont(WindowsXPColorScheme scheme) => Font(scheme, DialogFontKey);
    internal static WindowsXPFont StartFont(WindowsXPColorScheme scheme) => Font(scheme, StartSection, FontKey);
    internal static WindowsXPFont UserFont(WindowsXPColorScheme scheme) => Font(scheme, UserSection, FontKey);
    internal static SolidColorBrush StartText(WindowsXPColorScheme scheme) => Brush(scheme, StartSection, TextColorKey);
    internal static SolidColorBrush TaskText(WindowsXPColorScheme scheme) => Brush(scheme, TaskSection, TextColorKey);

    private static SolidColorBrush Brush(WindowsXPColorScheme scheme, string section, string key)
    {
        var parts = Values.Value[scheme][section][key].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var brush = new SolidColorBrush(Color.FromRgb(byte.Parse(parts[RedIndex], CultureInfo.InvariantCulture),
            byte.Parse(parts[GreenIndex], CultureInfo.InvariantCulture), byte.Parse(parts[BlueIndex], CultureInfo.InvariantCulture)));
        brush.Freeze();
        return brush;
    }

    private static WindowsXPFont Font(WindowsXPColorScheme scheme, string key) => Font(scheme, SystemSection, key);

    private static WindowsXPFont Font(WindowsXPColorScheme scheme, string section, string key)
    {
        var parts = Values.Value[scheme][section][key].Split(',', StringSplitOptions.TrimEntries);
        var size = Math.Round(double.Parse(parts[SizeIndex], CultureInfo.InvariantCulture) * ReferenceDpi / PointsPerInch,
            MidpointRounding.AwayFromZero);
        return new WindowsXPFont(new FontFamily(parts[FamilyIndex]), size,
            parts.Length > WeightIndex && parts[WeightIndex].Equals(BoldFlag, StringComparison.OrdinalIgnoreCase)
                ? FontWeights.Bold : FontWeights.Normal);
    }

    private static Dictionary<WindowsXPColorScheme, Dictionary<string, Dictionary<string, string>>> Load()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        using Stream stream = Application.GetResourceStream(new Uri(MetricsUri, UriKind.Absolute))?.Stream
            ?? throw new InvalidDataException(nameof(MetricsUri));
        return JsonSerializer.Deserialize<Dictionary<WindowsXPColorScheme, Dictionary<string, Dictionary<string, string>>>>(stream, options)
            ?? throw new InvalidDataException(nameof(Values));
    }
}
