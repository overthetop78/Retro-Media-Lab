using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.ViewModels.Explorer;
using GWGUI.App.ViewModels.Main;
using GWGUI.App.Views.Controls.Conversion;
using GWGUI.App.Views.Controls.Explorer;
using GWGUI.App.Views.Controls.Read;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.App.Views.Controls.Visualization;
using GWGUI.App.Views.Controls.Write;
using GWGUI.Tests.Application.TestInfrastructure;
using GWGUI.Tests.Interface.ExplorerViews;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ExplorerViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task FolderTreeUsesHistoricalBranchGlyphsWithoutSeparateExpanderButtons() => sta.Run(() =>
    {
        var source = new ExplorerSection();
        source.Display(ExplorerDocumentScenarios.Document("VOLUME"));
        using var view = new ExplorerView(source, () => { }, () => { }, () => { }, () => { });
        Arrange(view);
        var folders = Find<ListBox>(view, "Folders");
        var root = folders.Items.Cast<ExplorerFolderItem>().First();
        var branches = Descendants(folders).OfType<Windows31FolderBranch>().ToArray();
        Assert.NotEmpty(branches);
        Assert.Empty(Descendants(folders).OfType<Button>());
        Assert.Same(root, branches.First().Folder);
        Assert.Equal(root.Depth * Windows31FolderBranch.Indent + Windows31FolderBranch.GlyphSize, branches.First().ActualWidth);
        Assert.NotSame(Windows31FileImages.FolderBranch(true, false), Windows31FileImages.FolderBranch(false, false));
        Assert.NotSame(Windows31FileImages.FolderBranch(true, true), Windows31FileImages.FolderBranch(false, true));
        var expanded = root.IsExpanded;
        source.RequestToggleFolder(root);
        Arrange(view);
        Assert.Equal(!expanded, root.IsExpanded);
        Assert.Same(source.FolderList.ItemsSource, folders.ItemsSource);
    });

    [Fact]
    public Task NavigationPropertiesAndProgressShareExistingModelsWithoutReparentingBaseControls() => sta.Run(() =>
    {
        var source = new ExplorerSection();
        source.Clear();
        source.Display(ExplorerDocumentScenarios.Document("VOLUME"));
        var originalParent = source.FolderList.Parent;
        using var view = new ExplorerView(source, () => { }, () => { }, () => { }, () => { });
        using var details = new ExplorerDetailsView(source.DetailsPanel);
        var files = Find<ListBox>(view, "Files");
        Assert.Same(source.ContentsList.ItemsSource, files.ItemsSource);
        files.SelectedIndex = 0;
        files.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = Control.MouseDoubleClickEvent });
        Assert.Equal("FILE.BIN", Assert.IsType<ExplorerContentItem>(Assert.Single(files.Items.Cast<object>())).Name);
        files.SelectedIndex = 0;
        Assert.Equal("FILE.BIN", source.DetailsPanel.Presentation.Title);
        Arrange(details);
        Assert.Contains(Descendants(details).OfType<TextBlock>(), text => text.Text == "FILE.BIN");
        Assert.True(Find<MenuItem>(view, "InformationCommand").IsEnabled);
        source.SetLoading(true);
        source.SetLoadingProgress("loading-stage", "loading-detail", 47);
        Assert.Equal("loading-stage", Find<TextBlock>(view, "RecognitionStage").Text);
        Assert.Equal(47, Find<ProgressBar>(view, "RecognitionProgress").Value);
        Assert.Equal(Visibility.Visible, Find<StackPanel>(view, "Recognition").Visibility);
        source.SetLoading(false);
        source.Clear();
        Assert.Empty(files.Items);
        Assert.False(Find<MenuItem>(view, "InformationCommand").IsEnabled);
        Assert.Same(originalParent, source.FolderList.Parent);
        view.Dispose();
        var previous = Find<TextBlock>(view, "RecognitionStage").Text;
        source.SetLoadingProgress("after-disposal", string.Empty, 0);
        Assert.Equal(previous, Find<TextBlock>(view, "RecognitionStage").Text);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task MenuCommandsUseSourceActionsAndInformationStaysInsideTheDesktop() => sta.Run(() =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var read = new ReadTabSection { DataContext = model };
        var write = new WriteTabSection { DataContext = model };
        var conversion = new ConversionTabSection { DataContext = model };
        var visualization = new VisualizerTabSection();
        var status = new ApplicationStatusBar { DataContext = model };
        var source = new ExplorerSection();
        source.Display(ExplorerDocumentScenarios.Document("VOLUME"));
        var opened = 0;
        var readRequests = 0;
        RoutedEventHandler open = (_, _) => opened++;
        RoutedEventHandler request = (_, _) => readRequests++;
        source.OpenRequested += open;
        source.ReadDiskRequested += request;
        var desktop = new Windows31Desktop(owner => Windows31MediaApplications.Create(owner, read,
            write, conversion, visualization, status, source));
        try
        {
            desktop.Open(Windows31ApplicationId.Explorer);
            Arrange(desktop);
            var view = Assert.Single(Descendants(desktop).OfType<ExplorerView>());
            Find<MenuItem>(view, "OpenCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Find<MenuItem>(view, "ReadCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, opened);
            Assert.Equal(1, readRequests);
            Find<MenuItem>(view, "InformationCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Arrange(desktop);
            var details = Assert.Single(Descendants(desktop).OfType<ExplorerDetailsView>());
            Assert.Contains(Descendants(details).OfType<TextBlock>(), text => text.Text == "VOLUME");
            source.FolderList.SelectedIndex = 1;
            source.ContentsList.SelectedIndex = 0;
            Assert.Contains(Descendants(details).OfType<TextBlock>(), text => text.Text == "FILE.BIN");
            desktop.CloseApplication(Windows31ApplicationId.Explorer);
            Assert.Empty(Descendants(desktop).OfType<ExplorerDetailsView>());
            Assert.Null(view.Content);
        }
        finally
        {
            try { desktop.Dispose(); }
            finally { source.OpenRequested -= open; source.ReadDiskRequested -= request; }
        }
    });

    private static T Find<T>(FrameworkElement root, string name) => Assert.IsType<T>(root.FindName(name));
    private static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(1280, 800));
        view.Arrange(new Rect(0, 0, 1280, 800));
        view.UpdateLayout();
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
