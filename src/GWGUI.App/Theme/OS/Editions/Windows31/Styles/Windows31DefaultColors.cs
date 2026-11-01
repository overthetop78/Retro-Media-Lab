using System.Windows;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

/// <summary>Fallback colors for edition controls displayed outside a desktop.</summary>
internal sealed class Windows31DefaultColors : ResourceDictionary
{
    public Windows31DefaultColors()
    {
        var palette = Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault);
        foreach (var role in Enum.GetValues<Windows31ColorRole>())
            Add(Windows31PaletteResources.Key(role), palette[role]);
    }
}
