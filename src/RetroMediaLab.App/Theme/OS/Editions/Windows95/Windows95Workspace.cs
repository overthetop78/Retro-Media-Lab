using System.Collections.Specialized;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Shell;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95;

internal sealed partial class Windows95Workspace : Grid, IThemeWorkspace
{
    private sealed record ShellElement(UIElement Element, bool HitTestVisible, KeyboardNavigationMode TabNavigation);
    private sealed record ApplicationEntry(Windows95Window Window, FrameworkElement View, EventHandler Activated, EventHandler Closed);
    private readonly Grid root;
    private readonly TabControl tabs;
    private readonly Visibility originalTabsVisibility;
    private readonly List<ShellElement> shell = [];
    private readonly Dictionary<string, TabItem> pages = [];
    private readonly Dictionary<string, ApplicationEntry> applications = [];
    private readonly List<Windows95LauncherDefinition> launchers = [];
    private readonly List<Action> detachMenus = [];
    private Windows95Desktop? desktop;
    private Windows95WindowRouter? router;
    private EmulationSection? machineSource;
    private MainMenu? menu;
    private bool selecting;
    private bool disposed;
    private const int OverlayLayer = int.MaxValue;

    internal Windows95Workspace(Grid root, TabControl tabs)
    {
        this.root = root;
        this.tabs = tabs;
        originalTabsVisibility = tabs.Visibility;
        try
        {
            menu = root.Children.OfType<MainMenu>().Single();
            BuildApplications();
            desktop = new Windows95Desktop(launchers);
            desktop.ThemesRequested += ThemesRequested;
            desktop.ConnectMenus([menu.OptionsMenuItem, menu.EmulationMenuItem, menu.HelpMenuItem]);
            router = new Windows95WindowRouter(this, desktop.RegisterWindow);
            Children.Add(desktop);
            SetRowSpan(this, Math.Max(1, root.RowDefinitions.Count));
            SetColumnSpan(this, Math.Max(1, root.ColumnDefinitions.Count));
            SetZIndex(this, OverlayLayer);
            foreach (var element in root.Children.OfType<UIElement>())
            {
                shell.Add(new(element, element.IsHitTestVisible, KeyboardNavigation.GetTabNavigation(element)));
                element.SetCurrentValue(IsHitTestVisibleProperty, false);
                KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.None);
            }
            tabs.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            root.Children.Add(this);
            tabs.SelectionChanged += SourceTabChanged;
            machineSource!.PresentationChanged += MachinesChanged;
            machineSource.MachineSelected += MachineSelected;
            RefreshMachines();
        }
        catch { Dispose(); throw; }
    }

    public bool TryShowWindow(Window window) => !disposed && router is not null && router.TryShowWindow(window);

    private void ThemesRequested(object? sender, EventArgs args) { if (!disposed) menu?.RequestThemes(); }

    private T Source<T>(string id) where T : FrameworkElement
    {
        var tab = tabs.Items.OfType<TabItem>().Single(item => item.Content is T);
        pages.Add(id, tab);
        return (T)tab.Content;
    }

    private void SourceTabChanged(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || selecting || !ReferenceEquals(args.OriginalSource, tabs)) return;
        var page = pages.FirstOrDefault(pair => ReferenceEquals(pair.Value, tabs.SelectedItem));
        if (page.Value is not null) launchers.First(item => item.Id == page.Key).Launch();
    }

    private void SelectPage(string id)
    {
        if (disposed || selecting || !pages.TryGetValue(id, out var tab)) return;
        selecting = true;
        try { tabs.SelectedItem = tab; }
        finally { selecting = false; }
    }

    private void Open(string id, string title, Size size, Func<Action, FrameworkElement> create, bool modal = false)
    {
        if (disposed) return;
        if (applications.TryGetValue(id, out var existing))
        {
            if (existing.Window.WindowState == WindowState.Minimized) existing.Window.WindowState = WindowState.Normal;
            existing.Window.Activate();
            return;
        }
        Windows95Window? window = null;
        FrameworkElement? view = null;
        ApplicationEntry? entry = null;
        var attached = false;
        try
        {
            view = create(() => window?.Close());
            window = new Windows95Window { Width = size.Width, Height = size.Height, Document = view,
                Owner = Window.GetWindow(this), Icon = Windows95ApplicationIcons.ForResource(title), ShowActivated = IsVisible, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            window.SetBinding(Window.TitleProperty, LocExtension.CreateBinding(title));
            EventHandler activated = (_, _) => SelectPage(id);
            EventHandler closed = (_, _) => ReleaseApplication(id);
            entry = new(window, view, activated, closed);
            applications.Add(id, entry);
            window.Activated += activated;
            window.Closed += closed;
            desktop!.RegisterWindow(window);
            SelectPage(id);
            attached = true;
            if (modal) window.ShowDialog();
            else window.Show();
        }
        catch
        {
            if (entry is not null) ReleaseApplication(id);
            throw;
        }
        finally
        {
            if (!attached)
            {
                try { window?.Close(); }
                finally { if (entry is null) (view as IDisposable)?.Dispose(); }
            }
        }
    }

    private void ReleaseApplication(string id)
    {
        if (!applications.Remove(id, out var entry)) return;
        entry.Window.Activated -= entry.Activated;
        entry.Window.Closed -= entry.Closed;
        try { entry.Window.Close(); }
        finally { (entry.View as IDisposable)?.Dispose(); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        void Release(Action action)
        {
            try { action(); }
            catch (Exception error) { failure ??= error; }
        }
        try
        {
            tabs.SelectionChanged -= SourceTabChanged;
            if (desktop is not null) desktop.ThemesRequested -= ThemesRequested;
            if (machineSource is not null)
            {
                machineSource.PresentationChanged -= MachinesChanged;
                machineSource.MachineSelected -= MachineSelected;
            }
            Release(() => router?.Dispose());
            foreach (var entry in machines.Values.ToArray()) Release(() => ReleaseMachine(entry.Controller));
            foreach (var id in applications.Keys.ToArray()) Release(() => ReleaseApplication(id));
            foreach (var detach in detachMenus) Release(detach);
        }
        finally
        {
            try { Release(() => desktop?.Dispose()); }
            finally
            {
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
                launchers.Clear();
                detachMenus.Clear();
                router = null;
                desktop = null;
                menu = null;
                machineSource = null;
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
