using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP;

internal static class WindowsXPImageCatalog
{
    private const string AssetsPrefix = "pack://application:,,,/gwgui.app;component/Theme/OS/Editions/WindowsXP/Assets/";
    private const string MetricsPath = AssetsPrefix + "Metrics/ImageParts.json";
    private const string ImagesPath = AssetsPrefix + "Images/";
    private const string ImageSuffix = ".png";
    private const int MarginCount = 4;
    private const int FrameStateCount = 2;
    private const int StartStateCount = 3;
    private const int TaskStateCount = 6;
    private const int CaptionStateCount = 8;
    private const int InactiveOffset = 4;
    private const int NormalIndex = 0;
    private const int HotIndex = 1;
    private const int PressedIndex = 2;
    private const int DisabledCaptionIndex = 3;
    private const int InactiveFrameIndex = 1;
    private const int CheckedTaskIndex = 3;
    private const int CheckedHotTaskIndex = 4;
    private const int DisabledTaskIndex = 5;
    private static readonly Lazy<Dictionary<WindowsXPColorScheme, Dictionary<WindowsXPImagePart, WindowsXPImageDefinition>>> Definitions = new(ReadDefinitions);
    private static readonly Dictionary<(WindowsXPColorScheme, WindowsXPImagePart), BitmapSource> Images = [];
    private static readonly Dictionary<(WindowsXPColorScheme, WindowsXPImagePart, int), BitmapSource> Frames = [];

    private static readonly Dictionary<(BitmapSource, Int32Rect), BitmapSource> Slices = [];

    internal static WindowsXPImageDefinition Definition(WindowsXPColorScheme scheme, WindowsXPImagePart part) => Definitions.Value[scheme][part];

    internal static BitmapSource Frame(WindowsXPColorScheme scheme, WindowsXPImagePart part, int state)
    {
        if (Frames.TryGetValue((scheme, part, state), out var cached)) return cached;
        var definition = Definition(scheme, part);
        ArgumentOutOfRangeException.ThrowIfNegative(state);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(state, definition.ImageCount);
        if (!Images.TryGetValue((scheme, part), out var source))
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(ImagesPath + scheme + "/" + part + ImageSuffix, UriKind.Absolute);
            image.EndInit();
            if (image.PixelWidth != definition.PixelWidth || image.PixelHeight != definition.PixelHeight)
                throw new InvalidDataException(part.ToString());
            image.Freeze();
            Images.Add((scheme, part), source = image);
        }
        var x = definition.ImageLayout == WindowsXPImageLayout.Horizontal ? state * definition.FrameWidth : 0;
        var y = definition.ImageLayout == WindowsXPImageLayout.Vertical ? state * definition.FrameHeight : 0;
        var frame = new CroppedBitmap(source, new Int32Rect(x, y, definition.FrameWidth, definition.FrameHeight));
        frame.Freeze();
        Frames.Add((scheme, part, state), frame);
        return frame;
    }

    internal static int StateIndex(WindowsXPImageDefinition definition, WindowsXPImageState state, bool active)
    {
        if (definition.ImageCount == FrameStateCount) return active ? NormalIndex : InactiveFrameIndex;
        if (definition.ImageCount == StartStateCount) return state switch
        {
            WindowsXPImageState.Hot => HotIndex,
            WindowsXPImageState.Pressed or WindowsXPImageState.Checked or WindowsXPImageState.CheckedHot => PressedIndex,
            _ => NormalIndex
        };
        if (definition.ImageCount == TaskStateCount) return state switch
        {
            WindowsXPImageState.Normal => NormalIndex,
            WindowsXPImageState.Hot => HotIndex,
            WindowsXPImageState.Pressed => PressedIndex,
            WindowsXPImageState.Checked => CheckedTaskIndex,
            WindowsXPImageState.CheckedHot => CheckedHotTaskIndex,
            WindowsXPImageState.Disabled => DisabledTaskIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
        if (definition.ImageCount == CaptionStateCount)
        {
            var index = state switch
            {
                WindowsXPImageState.Normal => NormalIndex,
                WindowsXPImageState.Hot => HotIndex,
                WindowsXPImageState.Pressed => PressedIndex,
                WindowsXPImageState.Disabled => DisabledCaptionIndex,
                _ => throw new ArgumentOutOfRangeException(nameof(state))
            };
            return index + (active ? NormalIndex : InactiveOffset);
        }
        return NormalIndex;
    }

    internal static BitmapSource Slice(BitmapSource source, Int32Rect rectangle)
    {
        if (Slices.TryGetValue((source, rectangle), out var cached)) return cached;
        var slice = new CroppedBitmap(source, rectangle);
        slice.Freeze();
        Slices.Add((source, rectangle), slice);
        return slice;
    }

    private static Dictionary<WindowsXPColorScheme, Dictionary<WindowsXPImagePart, WindowsXPImageDefinition>> ReadDefinitions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        using Stream stream = Application.GetResourceStream(new Uri(MetricsPath, UriKind.Absolute))?.Stream
            ?? throw new InvalidDataException(nameof(MetricsPath));
        var definitions = JsonSerializer.Deserialize<Dictionary<WindowsXPColorScheme, Dictionary<WindowsXPImagePart, WindowsXPImageDefinition>>>(stream, options)
            ?? throw new InvalidDataException(nameof(Definitions));
        foreach (var scheme in Enum.GetValues<WindowsXPColorScheme>())
        {
            if (!definitions.TryGetValue(scheme, out var parts)) throw new InvalidDataException(scheme.ToString());
            foreach (var part in Enum.GetValues<WindowsXPImagePart>())
            {
                if (!parts.TryGetValue(part, out var definition) || definition.ImageCount <= 0 || definition.FrameWidth <= 0 || definition.FrameHeight <= 0
                    || definition.SizingMargins.Count != MarginCount || definition.ContentMargins.Count != MarginCount
                    || definition.SizingMargins.Any(value => value < 0)) throw new InvalidDataException(part.ToString());
            }
        }
        return definitions;
    }
}
