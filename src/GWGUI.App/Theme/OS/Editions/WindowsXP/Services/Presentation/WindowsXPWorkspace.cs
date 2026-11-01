using System.Collections.Specialized;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using GWGUI.App.Contracts.Theming;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.OS.Editions.WindowsXP.Views;
using GWGUI.App.Views.Controls.Emulation.Machine;
using GWGUI.App.Views.Controls.Shell;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP;

internal sealed partial class WindowsXPWorkspace : Grid, IThemeWorkspace
{
    private sealed record ShellElement(UIElement Element, bool HitTestVisible, KeyboardNavigationMode TabNavigation);
    private sealed record ApplicationEntry(WindowsXPWindow Window, FrameworkElement View, EventHandler Activated, EventHandler Closed);
    private readonly Grid root;
    private readonly TabControl tabs;
    private readonly Visibility originalTabsVisibility;
    private readonly List<ShellElement> shell = [];
    private readonly Dictionary<string, TabItem> pages = [];
    private readonly Dictionary<string, ApplicationEntry> applications = [];
    private readonly List<WindowsXPLauncherDefinition> launchers = [];
    private readonly List<Action> detachMenus = [];
    private readonly Dictionary<FrameworkElement, IDisposable> propertyDocuments = [];
    private WindowsXPColorScheme Scheme => WindowsXPResources.CurrentScheme(root);
    private WindowsXPDesktop? desktop;
    private WindowsXPWindowRouter? router;
    private EmulationSection? machineSource;
    private MainMenu? menu;
    private bool selecting;
    private bool disposed;
    private const int OverlayLayer = int.MaxValue;

    internal WindowsXPWorkspace(Grid root, TabControl tabs)
    {
        this.root = root;
        this.tabs = tabs;
        originalTabsVisibility = tabs.Visibility;
        try
        {
            menu = root.Children.OfType<MainMenu>().Single();
            BuildApplications();
            desktop = new WindowsXPDesktop(Scheme, LocExtension.Get("App.Title"), launchers);
            desktop.ThemesRequested += ThemesRequested;
            desktop.SetBinding(WindowsXPDesktop.SchemeProperty, new Binding
            {
                Source = root,
                Path = new PropertyPath(WindowsXPResources.SchemeProperty),
                TargetNullValue = WindowsXPColorScheme.Blue,
                FallbackValue = WindowsXPColorScheme.Blue
            });
            desktop.ConnectMenus([menu.OptionsMenuItem, menu.EmulationMenuItem, menu.HelpMenuItem]);
            router = new WindowsXPWindowRouter(this, RegisterWindow, () => Scheme);
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
        WindowsXPWindow? window = null;
        FrameworkElement? view = null;
        ApplicationEntry? entry = null;
        var attached = false;
        try
        {
            view = create(() => window?.Close());
            window = new WindowsXPWindow { Width = size.Width, Height = size.Height, Document = view,
                Owner = Window.GetWindow(this), Icon = WindowsXPApplicationIcons.ForResource(title), ShowActivated = IsVisible, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            window.SetBinding(Window.TitleProperty, LocExtension.CreateBinding(title));
            EventHandler activated = (_, _) => SelectPage(id);
            EventHandler closed = (_, _) => ReleaseApplication(id);
            entry = new(window, view, activated, closed);
            applications.Add(id, entry);
            window.Activated += activated;
            window.Closed += closed;
            RegisterWindow(window);
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
                finally { if (entry is null) ReleaseView(view); }
            }
        }
    }

    private void RegisterWindow(Window window)
    {
        if (window is WindowsXPWindow xp)
            xp.SetBinding(WindowsXPWindow.SchemeProperty, new Binding
            {
                Source = root,
                Path = new PropertyPath(WindowsXPResources.SchemeProperty),
                TargetNullValue = WindowsXPColorScheme.Blue,
                FallbackValue = WindowsXPColorScheme.Blue
            });
        desktop!.RegisterWindow(window);
    }

    private void ReleaseView(FrameworkElement? view)
    {
        try { (view as IDisposable)?.Dispose(); }
        finally
        {
            if (view is not null && propertyDocuments.Remove(view, out var document)) document.Dispose();
        }
    }

    private void ReleaseApplication(string id)
    {
        if (!applications.Remove(id, out var entry)) return;
        entry.Window.Activated -= entry.Activated;
        entry.Window.Closed -= entry.Closed;
        try { entry.Window.Close(); }
        finally { ReleaseView(entry.View); }
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
