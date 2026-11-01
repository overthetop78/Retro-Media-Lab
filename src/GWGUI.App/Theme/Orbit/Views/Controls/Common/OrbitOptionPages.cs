using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.Orbit.Views;

/// <summary>Continuous, collapsible property sections using independent Orbit controls.</summary>
internal static class OrbitOptionPages
{
    private const double ValueWidth = 160;
    private static readonly Thickness SectionGap = new(0, 0, 0, 18);
    private static readonly Thickness RowGap = new(0, 4, 0, 4);
    internal const string GeneralResource = "Options.General";
    internal const string NumberingResource = "Options.Themes.OS.Numbering";
    internal const string ExpertResource = "Common.ExpertArguments";

    internal static StackPanel Initialize(UserControl owner, double minimumWidth, double maximumHeight)
    {
        owner.FontFamily = new FontFamily(OrbitMetrics.FontFamily);
        owner.FontSize = OrbitMetrics.FontSize;
        owner.SetResourceReference(Control.BackgroundProperty, OrbitMetrics.WindowBrush);
        owner.SetResourceReference(Control.ForegroundProperty, OrbitMetrics.TextBrush);
        var sections = new StackPanel { Margin = OrbitMetrics.ContentPadding };
        owner.Content = new ScrollViewer
        {
            Content = sections,
            MaxHeight = maximumHeight,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        return sections;
    }

    internal static Grid Page(StackPanel sections, string resource)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueWidth) });
        var section = new Expander { Content = grid, IsExpanded = sections.Children.Count == 0, Margin = SectionGap };
        section.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(resource));
        sections.Children.Add(section);
        return grid;
    }

    internal static void Row(Grid page, FrameworkElement caption, FrameworkElement? value = null)
    {
        var index = page.RowDefinitions.Count;
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(caption, index);
        caption.Margin = RowGap;
        page.Children.Add(caption);
        if (value is null) Grid.SetColumnSpan(caption, page.ColumnDefinitions.Count);
        else
        {
            Grid.SetRow(value, index);
            Grid.SetColumn(value, page.ColumnDefinitions.Count - 1);
            value.Margin = RowGap;
            page.Children.Add(value);
        }
    }

    internal static Label Label(string resource)
    {
        var label = new Label();
        label.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(resource));
        return label;
    }
}
