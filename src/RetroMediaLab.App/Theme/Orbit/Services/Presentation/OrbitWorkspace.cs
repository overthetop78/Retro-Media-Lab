using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.Orbit.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Shell;

namespace RetroMediaLab.App.Theme.Orbit;

internal sealed partial class OrbitWorkspace : Grid, IThemeWorkspace
{
    private sealed record ShellElement(UIElement Element, bool HitTestVisible, KeyboardNavigationMode Navigation);
    private readonly Grid root;
    private readonly TabControl sourceTabs;
    private readonly Visibility previousTabsVisibility;
    private readonly OrbitShell shell = new();
    private readonly TabControl media = new();
    private readonly List<ShellElement> previousShell = [];
    private readonly Dictionary<string, TabItem> sourcePages = [];
    private readonly Dictionary<string, TabItem> mediaPages = [];
    private readonly List<IDisposable> documents = [];
    private readonly List<Action> detach = [];
    private readonly HashSet<OrbitWindow> propertyWindows = [];
    private OrbitCommandCatalog? commandCatalog;
    private OrbitCommandPalette? commands;
    private OrbitWindowRouter? router;
    private EmulationSection? machineSource;
    private bool disposed;
    private bool selecting;
    private const int OverlayLayer = int.MaxValue;

    internal OrbitWorkspace(Grid root, TabControl tabs)
    {
        this.root = root;
        sourceTabs = tabs;
        previousTabsVisibility = tabs.Visibility;
        try
        {
            var menu = root.Children.OfType<MainMenu>().Single();
            commandCatalog = new OrbitCommandCatalog([menu.OptionsMenuItem, menu.EmulationMenuItem, menu.HelpMenuItem]);
            commands = new OrbitCommandPalette(commandCatalog);
            commands.CloseRequested += CloseCommands;
            shell.CommandContent = commands;
            shell.MediaContent = media;
            BuildApplications();
            router = new OrbitWindowRouter(this, () => null);
            Children.Add(shell);
            SetRowSpan(this, Math.Max(1, root.RowDefinitions.Count));
            SetColumnSpan(this, Math.Max(1, root.ColumnDefinitions.Count));
            SetZIndex(this, OverlayLayer);
            foreach (var element in root.Children.OfType<UIElement>())
            {
                previousShell.Add(new(element, element.IsHitTestVisible, KeyboardNavigation.GetTabNavigation(element)));
                element.SetCurrentValue(IsHitTestVisibleProperty, false);
                KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.None);
            }
            tabs.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            root.Children.Add(this);
            tabs.SelectionChanged += SourceSelected;
            media.SelectionChanged += MediaSelected;
            machineSource!.PresentationChanged += MachinesChanged;
            machineSource.MachineSelected += MachineSelected;
            RefreshMachines();
        }
        catch { Dispose(); throw; }
    }

    public bool TryShowWindow(Window window) => !disposed && router is not null && router.TryShowWindow(window);

    private T Source<T>(string key) where T : FrameworkElement
    {
        var tab = sourceTabs.Items.OfType<TabItem>().Single(item => item.Content is T);
        sourcePages.Add(key, tab);
        return (T)tab.Content;
    }

    private void SourceSelected(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || selecting || !ReferenceEquals(args.OriginalSource, sourceTabs)) return;
        var key = sourcePages.FirstOrDefault(item => ReferenceEquals(item.Value, sourceTabs.SelectedItem)).Key;
        if (key is not null) SelectPage(key);
    }

    private void MediaSelected(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || selecting || !ReferenceEquals(args.OriginalSource, media)) return;
        var key = mediaPages.FirstOrDefault(item => ReferenceEquals(item.Value, media.SelectedItem)).Key;
        if (key is not null) SelectPage(key);
    }

    private void SelectPage(string key)
    {
        if (disposed || selecting) return;
        selecting = true;
        try
        {
            if (sourcePages.TryGetValue(key, out var source)) sourceTabs.SelectedItem = source;
            if (mediaPages.TryGetValue(key, out var target))
            {
                media.SelectedItem = target;
                shell.Area = OrbitArea.Media;
                shell.ContextTitle = LocExtension.Get(key);
            }
            else if (key == EmulationId) ShowLibrary();
        }
        finally { selecting = false; }
    }

    private void CloseCommands(object? sender, EventArgs args) => shell.Area = OrbitArea.Machines;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        void Release(Action action) { try { action(); } catch (Exception error) { failure ??= error; } }
        try
        {
            sourceTabs.SelectionChanged -= SourceSelected;
            media.SelectionChanged -= MediaSelected;
            if (machineSource is not null)
            {
                machineSource.PresentationChanged -= MachinesChanged;
                machineSource.MachineSelected -= MachineSelected;
            }
            if (commands is not null) commands.CloseRequested -= CloseCommands;
            Release(() => router?.Dispose());
            foreach (var window in propertyWindows.ToArray()) Release(window.Close);
            foreach (var controller in machines.Keys.ToArray()) Release(() => ReleaseMachine(controller));
            foreach (var action in sessionDetach) Release(action);
            foreach (var action in detach) Release(action);
            Release(shell.Dispose);
            foreach (var document in documents.AsEnumerable().Reverse()) Release(document.Dispose);
            Release(() => commands?.Dispose());
            Release(() => commandCatalog?.Dispose());
        }
        finally
        {
            try
            {
                foreach (TabItem item in media.Items) item.Content = null;
                media.Items.Clear();
                sessionDetach.Clear();
                detach.Clear();
                documents.Clear();
                Children.Clear();
                root.Children.Remove(this);
                sourceTabs.SetCurrentValue(VisibilityProperty, previousTabsVisibility);
            }
            finally
            {
                foreach (var item in previousShell)
                {
                    item.Element.SetCurrentValue(IsHitTestVisibleProperty, item.HitTestVisible);
                    KeyboardNavigation.SetTabNavigation(item.Element, item.Navigation);
                }
                previousShell.Clear();
                sourcePages.Clear();
                mediaPages.Clear();
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
