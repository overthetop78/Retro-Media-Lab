using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.ViewModels.Main;
using GWGUI.App.Views.Controls.Read;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31AcquisitionViewTests(StaExecutionScenarios sta)
{
    private const string CapturePath = @"virtual\capture.scp";

    [Fact]
    public Task NewFormKeepsOriginalElementsInTheirParentsAndUsesExistingActions() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ReadTabSection { DataContext = model };
        var status = new ApplicationStatusBar { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var originalFolderParent = source.FolderBlock.Input.Parent;
        var originalExecuteParent = source.ExecuteActionButton.Parent;
        var advanced = 0;
        var executions = 0;
        source.ExecuteRequested += (_, _) => executions++;
        using (var view = new AcquisitionView(source, status, () => advanced++, () => { }))
        {
            var input = Assert.IsType<TextBox>(view.FindName("Folder"));
            Assert.NotSame(source.FolderBlock.Input, input);
            input.Text = @"virtual\archive";
            Assert.Equal(@"virtual\archive", model.Read.Folder);
            var filename = Assert.IsType<TextBox>(view.FindName("FileName"));
            filename.Text = "capture";
            Assert.Equal("capture", model.Read.FileName);
            var execute = Assert.IsType<Button>(view.FindName("Execute"));
            Assert.NotSame(source.ExecuteActionButton, execute);
            execute.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, executions);
            Assert.IsType<MenuItem>(view.FindName("AdvancedCommand")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, advanced);
            Assert.Same(originalFolderParent, source.FolderBlock.Input.Parent);
            Assert.Same(originalExecuteParent, source.ExecuteActionButton.Parent);
        }
        Assert.Equal(@"virtual\archive", model.Read.Folder);
        Assert.Same(originalFolderParent, source.FolderBlock.Input.Parent);
    });

    [Fact]
    public Task TabsFollowTheExistingModeAndRawRevolutionsRemainInTheRawPage() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ReadTabSection { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var closes = 0;
        using var view = new AcquisitionView(source, new ApplicationStatusBar(), () => { }, () => closes++);
        var raw = Assert.IsType<TabItem>(view.FindName("Raw"));
        var known = Assert.IsType<TabItem>(view.FindName("Known"));
        Assert.True(raw.IsSelected);
        known.IsSelected = true;
        Assert.True(source.ImageBlock.KnownFormatRadio.IsChecked);
        Assert.False(source.ImageBlock.RawScpRadio.IsChecked);
        var revolutions = Assert.IsType<TextBox>(view.FindName("Revolutions"));
        Assert.Same(raw.Content, revolutions.Parent);
        Assert.NotSame(known.Content, revolutions.Parent);
        revolutions.Text = "5";
        Assert.Equal("5", model.Read.Revs.Value);
        Assert.IsType<CheckBox>(view.FindName("RevolutionsEnabled")).IsChecked = true;
        Assert.True(model.Read.Revs.Enabled);
        source.ImageBlock.RawScpRadio.IsChecked = true;
        Assert.True(raw.IsSelected);
        Assert.False(known.IsSelected);
        source.ImageBlock.KnownFormatRadio.IsEnabled = false;
        Assert.False(known.IsEnabled);
        Assert.IsType<MenuItem>(view.FindName("CloseCommand")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, closes);
    });

    [Fact]
    public Task HistoryUsesCaptureDataAndSharedConsultationCommands() => sta.Run(() =>
    {
        var source = new ReadTabSection();
        var status = new ApplicationStatusBar();
        string? requested = null;
        source.CompletionBlock.ExploreRequested += path => requested = path;
        using var view = new AcquisitionView(source, status, () => { }, () => { });
        var history = Assert.IsType<ListBox>(view.FindName("History"));
        var exploreMenu = Assert.IsType<MenuItem>(view.FindName("ExploreCommand"));
        Assert.False(exploreMenu.IsEnabled);
        source.CompletionBlock.BeginCapture(CapturePath);
        var capture = Assert.Single(source.CompletionBlock.Captures);
        Assert.Same(capture, Assert.Single(history.Items.Cast<object>()));
        source.CompletionBlock.CompleteCapture(CapturePath, "complete");
        history.SelectedItem = capture;
        Assert.IsType<Button>(view.FindName("Explore")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(CapturePath, requested);
        Assert.True(exploreMenu.IsEnabled);
        requested = null;
        exploreMenu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(CapturePath, requested);
        source.CompletionBlock.DiscardCapture(CapturePath);
        Assert.Empty(history.Items);
    });

    [Fact]
    public Task EverySelectedHistoryColumnUsesTheSelectionTextColor() => sta.Run(() =>
    {
        var source = new ReadTabSection();
        source.CompletionBlock.BeginCapture(CapturePath);
        source.CompletionBlock.CompleteCapture(CapturePath, "complete");
        using var view = new AcquisitionView(source, new ApplicationStatusBar(), () => { }, () => { });
        using var frame = new Windows31Frame(new Windows31WindowPlacement(new Rect(0, 0, 850, 560),
            new Rect(0, 0, 850, 560), new Size(320, 240)), view);
        var history = Assert.IsType<ListBox>(view.FindName("History"));
        history.SelectedIndex = 0;
        frame.Measure(new Size(850, 560));
        frame.Arrange(new Rect(0, 0, 850, 560));
        frame.UpdateLayout();
        var row = Assert.IsType<ListBoxItem>(history.ItemContainerGenerator.ContainerFromIndex(0));
        var expected = Assert.IsType<SolidColorBrush>(Windows31Metrics.ActiveCaptionTextBrush).Color;
        var labels = TextChildren(row).ToArray();
        Assert.Equal(3, labels.Length);
        Assert.All(labels, text => Assert.Equal(expected, Assert.IsType<SolidColorBrush>(text.Foreground).Color));
    });

    private static IEnumerable<TextBlock> TextChildren(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is TextBlock text) yield return text;
            foreach (var nested in TextChildren(child)) yield return nested;
        }
    }

    [Fact]
    public Task ClosingTheViewDetachesInputsAndLeavesBaseFunctional() => sta.Run(() =>
    {
        var source = new ReadTabSection();
        var status = new ApplicationStatusBar();
        var view = new AcquisitionView(source, status, () => { }, () => { });
        TextBox? input = null;
        Button? execute = null;
        try
        {
            input = Assert.IsType<TextBox>(view.FindName("Folder"));
            execute = Assert.IsType<Button>(view.FindName("Execute"));
            source.FolderBlock.Text = "before";
            Assert.Equal("before", input.Text);
        }
        finally { view.Dispose(); }
        var calls = 0;
        source.ExecuteRequested += (_, _) => calls++;
        source.FolderBlock.Text = "after";
        Assert.Equal("before", input!.Text);
        execute!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(0, calls);
        source.ExecuteActionButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(1, calls);
        Assert.Null(view.Content);
    });
}
