using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal static class WindowsXPPropertyPages
{
    private const double ValueColumnWidth = 150;
    private const double PageInset = 10;
    private const double FrameInset = 12;
    private const string FontFamily = "Tahoma";
    internal const string GeneralResource = "Options.General";
    internal const string NumberingResource = "Options.Themes.OS.Numbering";
    internal const string ExpertResource = "Common.ExpertArguments";

    internal static TabControl Initialize(UserControl owner, double minimumWidth, double maximumHeight)
    {
        owner.FontFamily = new FontFamily(FontFamily);
        owner.FontSize = WindowsXPMetrics.FontSize;
        owner.SetResourceReference(Control.BackgroundProperty, WindowsXPResources.FaceResource);
        var tabs = new TabControl { Margin = new Thickness(FrameInset), MinWidth = minimumWidth, MaxHeight = maximumHeight };
        owner.Content = tabs;
        return tabs;
    }

    internal static Grid Page(TabControl tabs, string resource)
    {
        var grid = new Grid { Margin = new Thickness(PageInset) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueColumnWidth) });
        var page = new TabItem { Content = new ScrollViewer { Content = grid,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto } };
        page.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(resource));
        tabs.Items.Add(page);
        return grid;
    }

    internal static void Row(Grid page, FrameworkElement caption, FrameworkElement? value = null)
    {
        var row = page.RowDefinitions.Count;
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(caption, row);
        page.Children.Add(caption);
        if (value is null) { Grid.SetColumnSpan(caption, page.ColumnDefinitions.Count); return; }
        Grid.SetRow(value, row);
        Grid.SetColumn(value, page.ColumnDefinitions.Count - 1);
        page.Children.Add(value);
    }

    internal static Label Label(string resource)
    {
        var label = new Label();
        label.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(resource));
        return label;
    }
}
