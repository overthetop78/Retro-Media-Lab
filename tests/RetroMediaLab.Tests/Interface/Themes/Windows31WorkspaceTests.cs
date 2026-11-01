using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.ViewModels.Main;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Tools;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31WorkspaceTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task DesktopReplacesTheWholeShellAndNavigationKeepsTheExistingFunctionalPages() => sta.Run(() =>
    {
        var sources = new Sources();
        var originalChildren = sources.Root.Children.Cast<UIElement>().ToArray();
        using var workspace = Assert.IsType<Windows31Workspace>(Windows31Edition.Definition.CreateWorkspace!(sources.Root, sources.Tabs));
        Assert.Same(workspace, sources.Root.Children.Cast<UIElement>().Last());
        Assert.Equal(sources.Root.RowDefinitions.Count, Grid.GetRowSpan(workspace));
        Assert.Equal(Visibility.Collapsed, sources.Tabs.Visibility);
        Assert.All(originalChildren, element => Assert.False(element.IsHitTestVisible));
        Assert.Empty(workspace.Desktop.OpenApplications);
        workspace.Desktop.Open(Windows31ApplicationId.Write);
        Assert.IsType<WriteTabSection>(((TabItem)sources.Tabs.SelectedItem).Content);
        var read = sources.Tabs.Items.OfType<TabItem>().Single(item => item.Content is ReadTabSection);
        sources.Tabs.SelectedItem = read;
        Assert.Equal(2, workspace.Desktop.OpenApplications.Count);
        workspace.Desktop.Open(Windows31ApplicationId.Emulation);
        Assert.Equal(2, workspace.Desktop.OpenApplications.Count);
        workspace.Desktop.Open(Windows31ApplicationId.Terminal);
        Arrange(sources.Root);
        var terminal = Descendants(workspace).OfType<TerminalView>().Single();
        Assert.NotSame(sources.Terminal, terminal);
        Assert.Same(read.Content, sources.Read);
        var desktop = workspace.Desktop;
        workspace.Dispose();
        Assert.Equal(originalChildren, sources.Root.Children.Cast<UIElement>());
        Assert.Equal(Visibility.Visible, sources.Tabs.Visibility);
        Assert.All(originalChildren, element => Assert.True(element.IsHitTestVisible));
        Assert.Empty(desktop.Children);
        Assert.Null(terminal.Content);
        sources.Tabs.SelectedIndex = 3;
        Assert.Empty(desktop.OpenApplications);
    });

    [Fact]
    public Task ConsoleRetainsSeverityAndCopiesThroughTheOriginalActionWithoutMovingTheOriginalOutput() => sta.Run(() =>
    {
        var source = new TerminalSection();
        var originalParent = source.OutputTextBox.Parent;
        source.CommandTextBox.Text = "gw read capture.scp";
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run("Ready"));
        paragraph.Inlines.Add(new Run("Failure") { Foreground = Brushes.IndianRed });
        source.OutputTextBox.Document.Blocks.Clear();
        source.OutputTextBox.Document.Blocks.Add(paragraph);
        var copies = 0;
        RoutedEventHandler copied = (_, _) => copies++;
        source.CopyButton.Click += copied;
        var closes = 0;
        using var view = new TerminalView(source, () => closes++);
        try
        {
            var command = (TextBox)view.FindName("Command");
            var output = (RichTextBox)view.FindName("Output");
            Assert.Equal(source.CommandTextBox.Text, command.Text);
            Assert.NotSame(source.OutputTextBox.Document, output.Document);
            var messages = ((Paragraph)output.Document.Blocks.FirstBlock).Inlines.OfType<Run>().ToArray();
            Assert.Equal("Failure", messages[1].Text);
            Assert.Same(Brushes.IndianRed, messages[1].Foreground);
            Assert.Equal(DependencyProperty.UnsetValue, messages[0].ReadLocalValue(TextElement.ForegroundProperty));
            ((MenuItem)view.FindName("CopyCommand")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, copies);
            paragraph.Inlines.Add(new Run("Complete"));
            Assert.Contains("Complete", new TextRange(output.Document.ContentStart, output.Document.ContentEnd).Text);
            source.CommandTextBox.Text = "gw write capture.scp";
            Assert.Equal(source.CommandTextBox.Text, command.Text);
            view.Dispose();
            paragraph.Inlines.Add(new Run("Detached"));
            Assert.Empty(output.Document.Blocks);
            Assert.Same(originalParent, source.OutputTextBox.Parent);
        }
        finally { source.CopyButton.Click -= copied; }
    });

    [Fact]
    public Task MissingShellSourceFailsWithoutChangingTheOriginalRoot() => sta.Run(() =>
    {
        var sources = new Sources();
        sources.Root.Children.Remove(sources.Terminal);
        var original = sources.Root.Children.Cast<UIElement>().ToArray();
        Assert.Throws<InvalidOperationException>(() => new Windows31Workspace(sources.Root, sources.Tabs));
        Assert.Equal(original, sources.Root.Children.Cast<UIElement>());
        Assert.Equal(Visibility.Visible, sources.Tabs.Visibility);
        Assert.All(original, element => Assert.True(element.IsHitTestVisible));
    });

    internal sealed class Sources
    {
        internal Grid Root { get; } = new();
        internal TabControl Tabs { get; } = new();
        internal TerminalSection Terminal { get; } = new();
        internal ReadTabSection Read { get; }
        internal Sources()
        {
            var model = new MainWindowViewModel("hardware", "ready");
            Read = new() { DataContext = model };
            FrameworkElement[] pages = [Read, new WriteTabSection { DataContext = model }, new ConversionTabSection { DataContext = model },
                new VisualizerTabSection(), new ExplorerSection(), new ToolsTabSection(), new EmulationSection(Array.Empty<IEmulationModule>())];
            foreach (var page in pages) Tabs.Items.Add(new TabItem { Content = page });
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Root.RowDefinitions.Add(new RowDefinition());
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180) });
            Root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Root.Children.Add(new MainMenu());
            Grid.SetRow(Tabs, 1);
            Root.Children.Add(Tabs);
            Grid.SetRow(Terminal, 2);
            Root.Children.Add(Terminal);
            var status = new ApplicationStatusBar { DataContext = model };
            Grid.SetRow(status, 3);
            Root.Children.Add(status);
        }
    }
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
