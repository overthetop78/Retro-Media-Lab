using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>Generated content text follows its owner, independently of application text styles.</summary>
internal sealed class Windows31ContentPresenter : ContentPresenter
{
    public Windows31ContentPresenter()
    {
        Resources.Add(typeof(TextBlock), OwnerTextStyle(typeof(TextBlock)));
        Resources.Add(typeof(AccessText), OwnerTextStyle(typeof(AccessText)));
    }

    private static Style OwnerTextStyle(Type textType)
    {
        var style = new Style(textType);
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new Binding(nameof(Control.Foreground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Control), 1)
        }));
        return style;
    }
}
