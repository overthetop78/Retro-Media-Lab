using System.Collections.ObjectModel;
using System.Windows.Media;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

internal sealed class Windows31Palette
{
    internal Windows31ColorScheme Id { get; }
    internal string NameResourceKey { get; }
    internal IReadOnlyDictionary<Windows31ColorRole, uint> ColorRefs { get; }
    private readonly IReadOnlyDictionary<Windows31ColorRole, Brush> brushes;

    internal Windows31Palette(Windows31ColorScheme id, string nameResourceKey, IDictionary<Windows31ColorRole, uint> colors)
    {
        Id = id;
        NameResourceKey = nameResourceKey;
        ColorRefs = new ReadOnlyDictionary<Windows31ColorRole, uint>(new Dictionary<Windows31ColorRole, uint>(colors));
        brushes = new ReadOnlyDictionary<Windows31ColorRole, Brush>(colors.ToDictionary(pair => pair.Key, pair => CreateBrush(pair.Value)));
    }

    internal Brush this[Windows31ColorRole role] => brushes[role];

    private static Brush CreateBrush(uint colorRef)
    {
        // COLORREF stores red first; the original high-byte flags remain in ColorRefs.
        var brush = new SolidColorBrush(Color.FromRgb((byte)(colorRef & ComponentMask),
            (byte)((colorRef >> GreenShift) & ComponentMask), (byte)((colorRef >> BlueShift) & ComponentMask)));
        brush.Freeze();
        return brush;
    }

    private const uint ComponentMask = 0xff;
    private const int GreenShift = 8;
    private const int BlueShift = 16;
}
