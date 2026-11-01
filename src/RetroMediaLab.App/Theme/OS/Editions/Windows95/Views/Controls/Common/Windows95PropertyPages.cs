using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal static class Windows95PropertyPages
{
    private const double LabelValueWidth = 150;
    private const double PageMargin = 8;
    private const string ControlsUri = "/retromedialab.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml";
    private const string DesktopUri = "/retromedialab.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Desktop.xaml";
    internal const string GeneralResource = "Options.General";
    internal const string NumberingResource = "Options.Themes.OS.Numbering";
    internal const string ExpertResource = "Common.ExpertArguments";

    internal static TabControl Initialize(UserControl owner, double minimumWidth, double maximumHeight)
    {
        owner.FontFamily = new FontFamily(Windows95DesktopMetrics.FontFamily);
        owner.FontSize = Windows95Metrics.FontSize;
        owner.Background = Brushes.Silver;
        owner.Foreground = Brushes.Black;
        owner.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(DesktopUri, UriKind.Relative) });
        owner.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) });
        var tabs = new TabControl { Margin = new Thickness(PageMargin), MinWidth = minimumWidth, MaxHeight = maximumHeight };
        owner.Content = tabs;
        return tabs;
    }

    internal static Grid Page(TabControl tabs, string titleResource)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelValueWidth) });
        var page = new TabItem
        {
            Content = new ScrollViewer
            {
                Content = grid,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };
        page.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(titleResource));
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
        Grid.SetColumn(value, 1);
        page.Children.Add(value);
    }

    internal static Label Label(string titleResource)
    {
        var label = new Label();
        label.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(titleResource));
        return label;
    }
}
