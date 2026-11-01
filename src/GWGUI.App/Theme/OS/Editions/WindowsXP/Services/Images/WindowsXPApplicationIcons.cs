using System.Windows.Media;
using System.Windows.Media.Imaging;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPApplicationIcons
{
    private const string AssetsUri = "pack://application:,,,/gwgui.app;component/Theme/OS/Editions/WindowsXP/Assets/";
    private const string IconDirectory = "Icons/";
    private const string IconExtension = ".ico";
    private const string StartLogoPath = "Images/StartLogo.png";
    private const int IconWidth = 32;
    private static readonly Dictionary<WindowsXPIcon, ImageSource> Images = [];
    private static readonly Lazy<ImageSource> Logo = new(LoadLogo);
    private static readonly IReadOnlyDictionary<string, WindowsXPIcon> Resources = new Dictionary<string, WindowsXPIcon>
    {
        ["Tab.Read"] = WindowsXPIcon.Read,
        ["Tab.Write"] = WindowsXPIcon.Write,
        ["Tab.Convert"] = WindowsXPIcon.Convert,
        ["Tab.Visualize"] = WindowsXPIcon.Visualize,
        ["Tab.Explorer"] = WindowsXPIcon.Explorer,
        ["Tab.Tools"] = WindowsXPIcon.Tools,
        ["Tab.Emulation"] = WindowsXPIcon.Emulation,
        ["Common.Console"] = WindowsXPIcon.Console,
        ["Menu.Preferences"] = WindowsXPIcon.Preferences,
        ["Options.PreferencesTitle"] = WindowsXPIcon.Preferences,
        ["Options.Themes.Title"] = WindowsXPIcon.Visualize,
        ["Menu.EmulationPreferences"] = WindowsXPIcon.Emulation,
        ["Options.EmulationTitle"] = WindowsXPIcon.Emulation,
        ["Menu.Updates"] = WindowsXPIcon.Updates,
        ["Updates.Title"] = WindowsXPIcon.Updates,
        ["Menu.History"] = WindowsXPIcon.History,
        ["Logs.History"] = WindowsXPIcon.History,
        ["Menu.Diagnostics"] = WindowsXPIcon.Diagnostics,
        ["Menu.Hardware"] = WindowsXPIcon.Hardware,
        ["Menu.Documentation"] = WindowsXPIcon.Documentation,
        ["Menu.About"] = WindowsXPIcon.About
    };

    internal static ImageSource StartLogo => Logo.Value;

    internal static ImageSource ForResource(string resource) => ForIcon(Resources.GetValueOrDefault(resource, WindowsXPIcon.Generic));

    internal static ImageSource ForCaption(string caption)
    {
        var resource = Resources.Keys.FirstOrDefault(key => LocExtension.Get(key) == caption);
        return ForResource(resource ?? string.Empty);
    }

    internal static ImageSource ForIcon(WindowsXPIcon icon)
    {
        if (Images.TryGetValue(icon, out var image)) return image;
        var decoder = new IconBitmapDecoder(new Uri(AssetsUri + IconDirectory + icon + IconExtension, UriKind.Absolute),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.Where(item => item.PixelWidth == IconWidth)
            .OrderByDescending(item => item.Format.BitsPerPixel).FirstOrDefault() ?? decoder.Frames[0];
        frame.Freeze();
        Images.Add(icon, frame);
        return frame;
    }

    private static ImageSource LoadLogo()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(AssetsUri + StartLogoPath, UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
