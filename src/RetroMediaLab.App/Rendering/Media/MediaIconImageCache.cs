using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroMediaLab.App.Rendering.Media;

internal static class MediaIconImageCache
{
    private static readonly Dictionary<string, ImageSource> MediaImages = new(StringComparer.OrdinalIgnoreCase);

    internal static ImageSource Load(string fileName)
    {
        if (MediaImages.TryGetValue(fileName, out var cached))
            return cached;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri($"pack://application:,,,/retromedialab.app;component/Assets/Media/{fileName}", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        MediaImages[fileName] = image;
        return image;
    }
}
