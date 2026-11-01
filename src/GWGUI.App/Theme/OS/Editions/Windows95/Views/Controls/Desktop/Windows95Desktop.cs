using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

/// <summary>Desktop presentation only. Launchers and observed windows remain owned by the caller.</summary>
internal sealed class Windows95Desktop : UserControl, IDisposable
{
    private readonly WrapPanel icons = new() { Orientation = Orientation.Vertical };
    private readonly WrapPanel tasks = new();
    private readonly ContextMenu startMenu = new();
    private readonly MenuItem programs = new();
    private readonly List<Action> detachDesktop = [];
    private readonly Dictionary<Window, TaskEntry> windows = [];
    private readonly List<Action> detachLaunchers = [];
    private readonly List<Windows95MenuMirror> menuMirrors = [];
    private bool disposed;

    internal Windows95Desktop(IEnumerable<Windows95LauncherDefinition> launchers)
    {
        try
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(Windows95DesktopMetrics.StylesUri, UriKind.Relative)
            });
            FontFamily = new FontFamily(Windows95DesktopMetrics.FontFamily);
            FontSize = Windows95Metrics.FontSize;
            SetResourceReference(BackgroundProperty, Windows95DesktopMetrics.DesktopBackgroundResource);
            var layout = new DockPanel();
            var taskbar = new DockPanel { Height = Windows95DesktopMetrics.TaskbarHeight, LastChildFill = true };
            taskbar.SetResourceReference(BackgroundProperty, Windows95DesktopMetrics.FaceResource);
            DockPanel.SetDock(taskbar, Dock.Bottom);
            var start = new ToggleButton
            {
                Margin = Windows95DesktopMetrics.ItemMargin,
                Padding = Windows95DesktopMetrics.ButtonPadding,
                FontWeight = FontWeights.Bold
            };
            start.SetResourceReference(StyleProperty, Windows95DesktopMetrics.TaskButtonStyle);
            start.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(Windows95DesktopMetrics.StartResource));
            start.ContextMenu = startMenu;
            startMenu.Resources.MergedDictionaries.Add(Resources);
            startMenu.SetResourceReference(StyleProperty, Windows95DesktopMetrics.StartMenuStyle);
            startMenu.PlacementTarget = start;
            startMenu.Placement = PlacementMode.Top;
            RoutedEventHandler open = (_, _) => startMenu.IsOpen = true;
            RoutedEventHandler close = (_, _) => start.IsChecked = false;
            start.Click += open;
            startMenu.Closed += close;
            detachDesktop.Add(() => { start.Click -= open; startMenu.Closed -= close; });
            taskbar.Children.Add(start);
            taskbar.Children.Add(new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = tasks
            });
            programs.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(Windows95DesktopMetrics.ProgramsResource));
            SetLaunchers(launchers);
            startMenu.Items.Add(programs);
            layout.Children.Add(taskbar);
            layout.Children.Add(new Border { Padding = Windows95DesktopMetrics.DesktopPadding, Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = icons
            } });
            Content = layout;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void ConnectMenus(IEnumerable<MenuItem> menus)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        foreach (var source in menus)
        {
            var target = new MenuItem();
            menuMirrors.Add(new Windows95MenuMirror(source, target));
            startMenu.Items.Add(target);
        }
    }

    internal void SetLaunchers(IEnumerable<Windows95LauncherDefinition> definitions)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        // Materialize before detaching: callers may derive entries from the current configuration list.
        var entries = definitions.ToArray();
        foreach (var detach in detachLaunchers) detach();
        detachLaunchers.Clear();
        icons.Children.Clear();
        programs.Items.Clear();
        foreach (var group in entries.GroupBy(item => item.Group))
        {
            var menu = new MenuItem();
            menu.SetBinding(HeaderedItemsControl.HeaderProperty, LocExtension.CreateBinding(GroupResource(group.Key)));
            foreach (var definition in group) AddLauncher(definition, menu);
            programs.Items.Add(menu);
        }
    }

    private void AddLauncher(Windows95LauncherDefinition definition, MenuItem group)
    {
        var stack = new StackPanel();
        if (definition.Icon is not null)
        {
            var image = new Image { Source = definition.Icon, Width = Windows95DesktopMetrics.LauncherIconSize, Height = Windows95DesktopMetrics.LauncherIconSize };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            stack.Children.Add(image);
        }
        stack.Children.Add(new TextBlock { Text = definition.Title, Foreground = Brushes.White, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        var button = new Button
        {
            Content = stack,
            Width = Windows95DesktopMetrics.LauncherWidth,
            Height = Windows95DesktopMetrics.LauncherHeight,
            Margin = Windows95DesktopMetrics.ItemMargin,
            ToolTip = definition.Title,
            Tag = definition.Id
        };
        button.SetResourceReference(StyleProperty, Windows95DesktopMetrics.DesktopButtonStyle);
        MouseButtonEventHandler doubleClick = (_, args) =>
        {
            if (disposed || args.ChangedButton != MouseButton.Left) return;
            definition.Launch();
            args.Handled = true;
        };
        KeyEventHandler enter = (_, args) =>
        {
            if (disposed || args.Key != Key.Enter) return;
            definition.Launch();
            args.Handled = true;
        };
        button.MouseDoubleClick += doubleClick;
        button.AddHandler(Keyboard.KeyDownEvent, enter, handledEventsToo: true);
        var item = new MenuItem { Header = definition.Title, Tag = definition.Id };
        if (definition.Icon is not null) item.Icon = new Image { Source = definition.Icon, Width = Windows95Metrics.IconSize, Height = Windows95Metrics.IconSize };
        RoutedEventHandler launch = (_, _) => { if (!disposed) definition.Launch(); };
        item.Click += launch;
        detachLaunchers.Add(() =>
        {
            button.MouseDoubleClick -= doubleClick;
            button.RemoveHandler(Keyboard.KeyDownEvent, enter);
            item.Click -= launch;
        });
        group.Items.Add(item);
        icons.Children.Add(button);
    }

    internal void RegisterWindow(Window window)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        if (windows.ContainsKey(window)) return;
        var entry = new TaskEntry(window, tasks, () => windows.Remove(window));
        windows.Add(window, entry);
    }

    private static string GroupResource(Windows95ProgramGroup group) => group switch
    {
        Windows95ProgramGroup.Media => Windows95DesktopMetrics.MediaResource,
        Windows95ProgramGroup.Emulation => Windows95DesktopMetrics.EmulationResource,
        Windows95ProgramGroup.System => Windows95DesktopMetrics.SystemResource,
        _ => throw new ArgumentOutOfRangeException(nameof(group))
    };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            startMenu.IsOpen = false;
            foreach (var entry in windows.Values.ToArray()) entry.Dispose();
        }
        finally
        {
            windows.Clear();
            foreach (var detach in detachLaunchers) detach();
            detachLaunchers.Clear();
            foreach (var detach in detachDesktop) detach();
            detachDesktop.Clear();
            foreach (var mirror in menuMirrors) mirror.Dispose();
            menuMirrors.Clear();
            startMenu.Items.Clear();
            startMenu.Resources.MergedDictionaries.Clear();
            icons.Children.Clear();
            tasks.Children.Clear();
            Content = null;
            Resources.MergedDictionaries.Clear();
        }
    }

    private sealed class TaskEntry : IDisposable
    {
        private readonly Window source;
        private readonly Panel parent;
        private readonly Action remove;
        private readonly ThemeBindingScope bindings = new();
        private readonly ToggleButton button;
        private bool detached;

        internal TaskEntry(Window source, Panel parent, Action remove)
        {
            this.source = source;
            this.parent = parent;
            this.remove = remove;
            button = new ToggleButton
            {
                Width = Windows95DesktopMetrics.TaskWidth,
                Height = Windows95DesktopMetrics.TaskHeight,
                Margin = Windows95DesktopMetrics.ItemMargin
            };
            try
            {
                button.SetResourceReference(StyleProperty, Windows95DesktopMetrics.TaskButtonStyle);
                var content = new DockPanel();
                var icon = new Image { Width = Windows95Metrics.IconSize, Height = Windows95Metrics.IconSize };
                var title = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
                content.Children.Add(icon);
                content.Children.Add(title);
                button.Content = content;
                bindings.OneWay(source, Window.TitleProperty, title, TextBlock.TextProperty);
                bindings.OneWay(source, Window.TitleProperty, button, ToolTipProperty);
                bindings.OneWay(source, Window.IconProperty, icon, Image.SourceProperty);
                button.Click += Restore;
                source.Activated += Update;
                source.Deactivated += Update;
                source.StateChanged += Update;
                source.Closed += Closed;
                parent.Children.Add(button);
                Update(null, EventArgs.Empty);
            }
            catch { Dispose(); throw; }
        }

        private void Update(object? sender, EventArgs args) => button.IsChecked = source.IsActive && source.WindowState != WindowState.Minimized;
        private void Restore(object sender, RoutedEventArgs args)
        {
            if (detached) return;
            if (source.WindowState == WindowState.Minimized) source.WindowState = WindowState.Normal;
            if (!source.IsVisible) source.Show();
            source.Activate();
            Update(null, EventArgs.Empty);
        }
        private void Closed(object? sender, EventArgs args)
        {
            try { Dispose(); }
            finally { remove(); }
        }
        public void Dispose()
        {
            if (detached) return;
            detached = true;
            try { bindings.Dispose(); }
            finally
            {
                button.Click -= Restore;
                source.Activated -= Update;
                source.Deactivated -= Update;
                source.StateChanged -= Update;
                source.Closed -= Closed;
                parent.Children.Remove(button);
                button.Content = null;
            }
        }
    }
}
