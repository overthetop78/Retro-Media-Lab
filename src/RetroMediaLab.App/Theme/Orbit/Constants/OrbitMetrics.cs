using System.Windows;

namespace RetroMediaLab.App.Theme.Orbit;

internal static class OrbitMetrics
{
    internal const double FontSize = 12;
    internal const double HeadingSize = 23;
    internal const double RailWidth = 88;
    public static readonly GridLength RailColumn = new(RailWidth);
    internal const double HeaderHeight = 60;
    internal const double StatusHeight = 28;
    internal const double DrawerWidth = 300;
    internal const double JournalHeight = 170;
    internal const double MinimumWidth = 900;
    internal const double MinimumHeight = 600;
    internal const double SmallIconSize = 20;
    internal const double MachineIconSize = 48;
    internal const string FontFamily = "Segoe UI";
    internal const string ControlsUri = "/retromedialab.app;component/Theme/Orbit/Styles/OrbitControls.xaml";
    internal const string DarkUri = "/retromedialab.app;component/Theme/Orbit/Dark.xaml";
    internal const string LightUri = "/retromedialab.app;component/Theme/Orbit/Light.xaml";
    internal const string ResourcePrefix = "Orbit.";
    internal const string WindowBrush = ResourcePrefix + "Window";
    internal const string SurfaceBrush = ResourcePrefix + "Surface";
    internal const string ControlBrush = ResourcePrefix + "Control";
    internal const string TextBrush = ResourcePrefix + "Text";
    internal const string MutedBrush = ResourcePrefix + "Muted";
    internal const string AccentBrush = ResourcePrefix + "Accent";
    internal const string BorderBrush = ResourcePrefix + "Border";
    internal const string SelectionBrush = ResourcePrefix + "Selection";
    internal const string HoverBrush = ResourcePrefix + "Hover";
    internal const string PathSeparator = " › ";
    internal static readonly Thickness ContentPadding = new(20);
    internal static readonly Thickness FieldGap = new(0, 0, 0, 12);
}
