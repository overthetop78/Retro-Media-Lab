using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ControlsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task GeneratedControlTextIgnoresApplicationTextColorAndFollowsItsOwner() => sta.Run(() =>
    {
        var resources = System.Windows.Application.Current.Resources;
        var previous = resources[typeof(TextBlock)];
        var applicationStyle = new Style(typeof(TextBlock));
        applicationStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.White));
        resources[typeof(TextBlock)] = applicationStyle;
        var panel = new StackPanel();
        var button = new Button { Content = "action" };
        var menu = new Menu();
        menu.Items.Add(new MenuItem { Header = "file" });
        var list = new ListBox { Height = 80 };
        list.Items.Add("entry");
        panel.Children.Add(button);
        panel.Children.Add(menu);
        panel.Children.Add(list);
        try
        {
            using var frame = new Windows31Frame(Placement(), panel);
            frame.Measure(new Size(900, 700));
            frame.Arrange(new Rect(0, 0, 900, 700));
            frame.UpdateLayout();
            var palette = Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault);
            foreach (var text in new[] { "action", "file", "entry" })
            {
                var rendered = Assert.Single(TextChildren(frame), block => block.Text == text);
                Assert.Equal(System.Windows.Media.Colors.Black,
                    Assert.IsType<System.Windows.Media.SolidColorBrush>(rendered.Foreground).Color);
            }
            button.IsEnabled = false;
            Assert.Same(palette[Windows31ColorRole.GrayText], Assert.Single(TextChildren(button)).Foreground);
        }
        finally
        {
            panel.Children.Clear();
            if (previous is null) resources.Remove(typeof(TextBlock));
            else resources[typeof(TextBlock)] = previous;
        }
    });

    [Fact]
    public Task IndependentTemplatesLoadForAllImplementedPrimitiveFamilies() => sta.Run(() =>
    {
        var controls = new Control[]
        {
            new Button { Content = "action" }, new CheckBox { Content = "option" }, new RadioButton { Content = "choice" },
            new TextBox { Text = "input" }, new ComboBox(), new ListBox(), new GroupBox { Header = "group" },
            new Label { Content = "label" }, new Menu(), new MenuItem { Header = "menu" }, new Separator()
        };
        var panel = new StackPanel();
        foreach (var control in controls) panel.Children.Add(control);
        using var frame = new Windows31Frame(Placement(), panel);
        var context = new ContextMenu();
        context.Resources.MergedDictionaries.Add(frame.Resources.MergedDictionaries[0]);
        panel.ContextMenu = context;
        try
        {
            context.ApplyTemplate();
            Assert.NotNull(context.Template);
            frame.Measure(new Size(900, 700));
            frame.Arrange(new Rect(0, 0, 900, 700));
            foreach (var control in controls)
            {
                control.ApplyTemplate();
                Assert.NotNull(control.Template);
                Assert.NotNull(control.Style);
            }
        }
        finally
        {
            panel.ContextMenu = null;
            context.Resources.MergedDictionaries.Clear();
            panel.Children.Clear();
        }
    });

    [Fact]
    public Task SelectionAndDisabledStatesUseTheEditionPaletteRatherThanBase() => sta.Run(() =>
    {
        var check = new CheckBox();
        var option = new RadioButton();
        var item = new ListBoxItem { Content = "capture" };
        var panel = new StackPanel();
        panel.Children.Add(check);
        panel.Children.Add(option);
        panel.Children.Add(item);
        using var frame = new Windows31Frame(Placement(), panel);
        try
        {
            frame.Measure(new Size(900, 700));
            frame.Arrange(new Rect(0, 0, 900, 700));
            check.ApplyTemplate();
            option.ApplyTemplate();
            item.ApplyTemplate();
            var mark = Assert.IsAssignableFrom<FrameworkElement>(check.Template.FindName("Mark", check));
            var dot = Assert.IsAssignableFrom<FrameworkElement>(option.Template.FindName("Dot", option));
            Assert.Equal(Visibility.Collapsed, mark.Visibility);
            Assert.Equal(Visibility.Collapsed, dot.Visibility);
            check.IsChecked = true;
            option.IsChecked = true;
            item.IsSelected = true;
            Assert.Equal(Visibility.Visible, mark.Visibility);
            Assert.Equal(Visibility.Visible, dot.Visibility);
            var palette = Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault);
            Assert.Same(palette[Windows31ColorRole.Selection], item.Background);
            Assert.Same(palette[Windows31ColorRole.SelectionText], item.Foreground);
            check.IsEnabled = false;
            Assert.Same(palette[Windows31ColorRole.GrayText], check.Foreground);
        }
        finally { panel.Children.Clear(); }
    });

    [Fact]
    public Task SourceControlsKeepTheirStylesOutsideTheEditionFrame() => sta.Run(() =>
    {
        var original = new Button();
        var previous = original.Style;
        var target = new Button();
        using (var frame = new Windows31Frame(Placement(), target))
        {
            frame.Measure(new Size(900, 700));
            frame.Arrange(new Rect(0, 0, 900, 700));
            target.ApplyTemplate();
            Assert.NotNull(target.Style);
            Assert.Same(previous, original.Style);
        }
        Assert.Null(target.Parent);
        Assert.Same(previous, original.Style);
    });

    [Fact]
    public Task ScrollingUsesBothEditionBarsAndMovesTheDocument() => sta.Run(() =>
    {
        var viewer = new ScrollViewer
        {
            Width = 180,
            Height = 120,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border { Width = 1600, Height = 1200 }
        };
        using var frame = new Windows31Frame(Placement(), viewer);
        try
        {
            Arrange(frame);
            var vertical = Assert.IsType<ScrollBar>(viewer.Template.FindName("PART_VerticalScrollBar", viewer));
            var horizontal = Assert.IsType<ScrollBar>(viewer.Template.FindName("PART_HorizontalScrollBar", viewer));
            vertical.ApplyTemplate();
            horizontal.ApplyTemplate();
            Assert.Equal(Visibility.Visible, vertical.Visibility);
            Assert.Equal(Visibility.Visible, horizontal.Visibility);
            Assert.Equal(Orientation.Vertical, ((Track)vertical.Template.FindName("PART_Track", vertical)).Orientation);
            Assert.Equal(Orientation.Horizontal, ((Track)horizontal.Template.FindName("PART_Track", horizontal)).Orientation);
            viewer.ScrollToVerticalOffset(100);
            viewer.ScrollToHorizontalOffset(200);
            viewer.UpdateLayout();
            Assert.Equal(100, viewer.VerticalOffset);
            Assert.Equal(200, viewer.HorizontalOffset);
            Assert.Equal(viewer.VerticalOffset, vertical.Value);
            Assert.Equal(viewer.HorizontalOffset, horizontal.Value);
        }
        finally { viewer.Content = null; }
    });

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public Task SlidersAndScrollbarsPreserveRangeAndPageCommands(Orientation orientation) => sta.Run(() =>
    {
        var slider = new Slider { Orientation = orientation, Minimum = 0, Maximum = 100, Value = 50, LargeChange = 10, Width = 180, Height = 180 };
        var bar = new ScrollBar { Orientation = orientation, Minimum = 0, Maximum = 100, Value = 50, LargeChange = 10, Width = 180, Height = 180 };
        var panel = new StackPanel();
        panel.Children.Add(slider);
        panel.Children.Add(bar);
        using var frame = new Windows31Frame(Placement(), panel);
        try
        {
            Arrange(frame);
            slider.ApplyTemplate();
            bar.ApplyTemplate();
            var sliderTrack = Assert.IsType<Track>(slider.Template.FindName("PART_Track", slider));
            var scrollTrack = Assert.IsType<Track>(bar.Template.FindName("PART_Track", bar));
            Assert.Equal(orientation, sliderTrack.Orientation);
            Assert.Equal(orientation, scrollTrack.Orientation);
            var increase = Assert.IsType<RoutedCommand>(sliderTrack.IncreaseRepeatButton.Command);
            increase.Execute(null, sliderTrack.IncreaseRepeatButton);
            Assert.Equal(60, slider.Value);
            slider.Value = 500;
            Assert.Equal(100, slider.Value);
            var page = Assert.IsType<RoutedCommand>(scrollTrack.DecreaseRepeatButton.Command);
            page.Execute(null, scrollTrack.DecreaseRepeatButton);
            Assert.Equal(40, bar.Value);
        }
        finally { panel.Children.Clear(); }
    });

    [Fact]
    public Task NavigationPreservesTabContentAndExpandableHierarchy() => sta.Run(() =>
    {
        var first = new TextBox { Text = "first" };
        var second = new TextBox { Text = "second" };
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "first", Content = first });
        tabs.Items.Add(new TabItem { Header = "second", Content = second });
        var tree = new TreeView();
        var folder = new TreeViewItem { Header = "folder" };
        var child = new TreeViewItem { Header = "child" };
        folder.Items.Add(child);
        tree.Items.Add(folder);
        var panel = new StackPanel();
        panel.Children.Add(tabs);
        panel.Children.Add(tree);
        using var frame = new Windows31Frame(Placement(), panel);
        try
        {
            Arrange(frame);
            tabs.SelectedIndex = 1;
            frame.UpdateLayout();
            var content = Assert.IsAssignableFrom<ContentPresenter>(tabs.Template.FindName("PART_SelectedContentHost", tabs));
            Assert.Same(second, content.Content);
            folder.ApplyTemplate();
            var expander = Assert.IsType<ToggleButton>(folder.Template.FindName("Expander", folder));
            expander.IsChecked = true;
            frame.UpdateLayout();
            Assert.True(folder.IsExpanded);
            Assert.Equal(Visibility.Visible, ((ItemsPresenter)folder.Template.FindName("ItemsHost", folder)).Visibility);
            child.IsSelected = true;
            Assert.Same(child, tree.SelectedItem);
            expander.IsChecked = false;
            Assert.False(folder.IsExpanded);
        }
        finally
        {
            panel.Children.Clear();
            tabs.Items.Clear();
            tree.Items.Clear();
            folder.Items.Clear();
        }
    });

    [Fact]
    public Task ComboBoxUsesDisplayMemberPathForTheSelectedDataObject() => sta.Run(() =>
    {
        var selector = new ComboBox { DisplayMemberPath = nameof(KeyValuePair<int, string>.Value),
            ItemsSource = new[] { new KeyValuePair<int, string>(1, "visible caption") }, SelectedIndex = 0 };
        using var frame = new Windows31Frame(Placement(), selector);
        try
        {
            Arrange(frame);
            var content = Assert.IsAssignableFrom<ContentPresenter>(selector.Template.FindName("SelectionContent", selector));
            content.ApplyTemplate();
            frame.UpdateLayout();
            Assert.Same(selector.ItemTemplateSelector, content.ContentTemplateSelector);
            var labels = TextChildren(content).ToArray();
            Assert.Contains(labels, label => label.Text == "visible caption");
            Assert.DoesNotContain(labels, label => label.Text.Contains("Key =", StringComparison.Ordinal));
        }
        finally { selector.ItemsSource = null; }
    });

    private static IEnumerable<TextBlock> TextChildren(DependencyObject root)
    {
        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            if (child is TextBlock text) yield return text;
            foreach (var nested in TextChildren(child)) yield return nested;
        }
    }

    private static void Arrange(FrameworkElement element)
    {
        element.Measure(new Size(900, 700));
        element.Arrange(new Rect(0, 0, 900, 700));
        element.UpdateLayout();
    }

    private static Windows31WindowPlacement Placement() => new(new Rect(0, 0, 1024, 768), new Rect(0, 0, 900, 700), new Size(180, 120));
}
