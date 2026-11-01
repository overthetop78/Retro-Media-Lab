using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Tools;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>A distinct desktop over the retained functional sources; no Base page is displayed.</summary>
internal sealed class Windows31Workspace : Grid, IThemeWorkspace
{
    private sealed record ShellElement(UIElement Element, bool HitTestVisible, KeyboardNavigationMode TabNavigation);
    private readonly Grid root;
    private readonly TabControl tabs;
    private readonly Visibility originalTabsVisibility;
    private readonly List<ShellElement> shell = [];
    private readonly Dictionary<Windows31ApplicationId, TabItem> pages = [];
    private Windows31Desktop? desktop;
    private EventHandler? documentationHandler;
    private EventHandler? aboutHandler;
    private EventHandler? preferencesHandler;
    private EventHandler? emulationPreferencesHandler;
    private bool selecting;
    private bool disposed;
    private const int OverlayLayer = int.MaxValue;
    private const string ConsoleTitle = "Common.Console";
    private const string DocumentationTitle = "Menu.Documentation";
    private const string AboutTitle = "Menu.About";
    private static readonly Size ConsoleSize = new(760, 380);

    internal Windows31Desktop Desktop => desktop ?? throw new ObjectDisposedException(nameof(Windows31Workspace));

    internal Windows31Workspace(Grid root, TabControl tabs)
    {
        this.root = root;
        this.tabs = tabs;
        originalTabsVisibility = tabs.Visibility;
        try
        {
            var items = tabs.Items.OfType<TabItem>().ToArray();
            var read = Source<ReadTabSection>(items, Windows31ApplicationId.Read);
            var write = Source<WriteTabSection>(items, Windows31ApplicationId.Write);
            var conversion = Source<ConversionTabSection>(items, Windows31ApplicationId.Conversion);
            var visualization = Source<VisualizerTabSection>(items, Windows31ApplicationId.Visualization);
            var explorer = Source<ExplorerSection>(items, Windows31ApplicationId.Explorer);
            var tools = Source<ToolsTabSection>(items, Windows31ApplicationId.Tools);
            var emulation = Source<EmulationSection>(items, Windows31ApplicationId.Emulation);
            var status = root.Children.OfType<ApplicationStatusBar>().Single();
            var terminal = root.Children.OfType<TerminalSection>().Single();
            var menu = root.Children.OfType<MainMenu>().Single();
            desktop = new Windows31Desktop(current =>
            {
                var definitions = Windows31MediaApplications.Create(current, read, write, conversion, visualization,
                    status, explorer, tools, emulation).ToList();
                definitions.Add(new(Windows31ApplicationId.Terminal, Windows31ProgramGroup.System, ConsoleTitle,
                    () =>
                    {
                        var view = new TerminalView(terminal, () => current.CloseApplication(Windows31ApplicationId.Terminal));
                        return new Windows31Document(view, view);
                    }, ConsoleSize));
                return definitions;
            });
            desktop.ApplicationActivated += ApplicationActivated;
            preferencesHandler = (_, _) =>
            {
                var caption = LocExtension.Get("Menu.Preferences");
                var command = menu.OptionsMenuItem.Items.OfType<MenuItem>().Single(item => Equals(item.Header, caption));
                if (command.IsEnabled) command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, command));
            };
            desktop.PreferencesRequested += preferencesHandler;
            emulationPreferencesHandler = (_, _) =>
            {
                var caption = LocExtension.Get("Menu.EmulationPreferences");
                var command = menu.EmulationMenuItem.Items.OfType<MenuItem>().Single(item => Equals(item.Header, caption));
                if (command.IsEnabled) command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, command));
            };
            desktop.EmulationPreferencesRequested += emulationPreferencesHandler;
            desktop.EnablePreferences();
            desktop.ConnectMenus(menu);
            documentationHandler = (_, _) => InvokeHelp(menu, DocumentationTitle);
            aboutHandler = (_, _) => InvokeHelp(menu, AboutTitle);
            desktop.DocumentationRequested += documentationHandler;
            desktop.AboutRequested += aboutHandler;
            desktop.CloseRequested += DesktopCloseRequested;
            Children.Add(desktop);
            SetRowSpan(this, Math.Max(1, root.RowDefinitions.Count));
            SetColumnSpan(this, Math.Max(1, root.ColumnDefinitions.Count));
            Panel.SetZIndex(this, OverlayLayer);
            foreach (var element in root.Children.OfType<UIElement>())
            {
                shell.Add(new(element, element.IsHitTestVisible, KeyboardNavigation.GetTabNavigation(element)));
                element.SetCurrentValue(IsHitTestVisibleProperty, false);
                KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.None);
            }
            tabs.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            root.Children.Add(this);
            tabs.SelectionChanged += SourceTabChanged;

        }
        catch { Dispose(); throw; }
    }

    public bool TryShowWindow(Window window)
    {
        return !disposed && Desktop.TryShowWindow(window);
    }

    private T Source<T>(IReadOnlyList<TabItem> items, Windows31ApplicationId id) where T : FrameworkElement
    {
        var tab = items.Single(item => item.Content is T);
        pages.Add(id, tab);
        return (T)tab.Content;
    }

    private static void InvokeHelp(MainMenu menu, string resource)
    {
        var caption = LocExtension.Get(resource);
        var command = menu.HelpMenuItem.Items.OfType<MenuItem>().Single(item => Equals(item.Header, caption));
        if (command.IsEnabled) command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, command));
    }

    private void SourceTabChanged(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || selecting || !ReferenceEquals(args.OriginalSource, tabs)) return;
        var id = pages.FirstOrDefault(item => ReferenceEquals(item.Value, tabs.SelectedItem));
        if (id.Value is not null) Desktop.Open(id.Key);
    }

    private void ApplicationActivated(Windows31ApplicationId id)
    {
        if (disposed || selecting || !pages.TryGetValue(id, out var tab)) return;
        selecting = true;
        try { tabs.SelectedItem = tab; }
        finally { selecting = false; }
    }

    private void DesktopCloseRequested(object? sender, EventArgs args) => Window.GetWindow(this)?.Close();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            tabs.SelectionChanged -= SourceTabChanged;
            if (desktop is not null)
            {
                desktop.ApplicationActivated -= ApplicationActivated;
                desktop.PreferencesRequested -= preferencesHandler;
                desktop.EmulationPreferencesRequested -= emulationPreferencesHandler;
                desktop.CloseRequested -= DesktopCloseRequested;
                desktop.DocumentationRequested -= documentationHandler;
                desktop.AboutRequested -= aboutHandler;
                desktop.Dispose();
            }
        }
        catch (Exception error) { failure = error; }
        finally
        {
            desktop = null;
            Children.Clear();
            root.Children.Remove(this);
            tabs.SetCurrentValue(VisibilityProperty, originalTabsVisibility);
            foreach (var item in shell)
            {
                item.Element.SetCurrentValue(IsHitTestVisibleProperty, item.HitTestVisible);
                KeyboardNavigation.SetTabNavigation(item.Element, item.TabNavigation);
            }
            shell.Clear();
            pages.Clear();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
