using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

internal static class Windows95ApplicationIcons
{
    private const string IconUri = "pack://application:,,,/gwgui.app;component/Theme/OS/Editions/Windows95/Assets/Icons/shell-{0}.ico";
    private const int GenericIcon = 20;
    private const int IconWidth = 32;
    private static readonly IReadOnlyDictionary<string, int> Resources = new Dictionary<string, int>
    {
        ["Tab.Read"] = 7,
        ["Tab.Write"] = 2,
        ["Tab.Convert"] = 25,
        ["Tab.Visualize"] = 23,
        ["Tab.Explorer"] = 4,
        ["Tab.Tools"] = 22,
        ["Tab.Emulation"] = 16,
        ["Common.Console"] = 3,
        ["Menu.Preferences"] = 35,
        ["Options.PreferencesTitle"] = 35,
        ["Menu.EmulationPreferences"] = 16,
        ["Options.EmulationTitle"] = 16,
        ["Menu.Updates"] = 14,
        ["Updates.Title"] = 14,
        ["Menu.History"] = 133,
        ["Logs.History"] = 133,
        ["Menu.Diagnostics"] = 13,
        ["Menu.Hardware"] = 27,
        ["Menu.Documentation"] = 24,
        ["Menu.About"] = 40
    };
    private static readonly Dictionary<int, ImageSource> Images = [];

    internal static ImageSource ForResource(string resource) => Load(Resources.GetValueOrDefault(resource, GenericIcon));

    internal static ImageSource ForCaption(string caption)
    {
        var resource = Resources.Keys.FirstOrDefault(key => LocExtension.Get(key) == caption);
        return ForResource(resource ?? string.Empty);
    }

    private static ImageSource Load(int id)
    {
        if (Images.TryGetValue(id, out var image)) return image;
        var uri = new Uri(string.Format(CultureInfo.InvariantCulture, IconUri, id), UriKind.Absolute);
        var decoder = new IconBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.Where(item => item.PixelWidth == IconWidth).OrderByDescending(item => item.Format.BitsPerPixel).FirstOrDefault()
            ?? decoder.Frames[0];
        frame.Freeze();
        Images.Add(id, frame);
        return frame;
    }
}
