using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Owns the desktop presentation, while callers retain their launch actions and windows.</summary>
internal sealed class WindowsXPDesktop : UserControl, IDisposable
{
    private readonly WrapPanel icons = new() { Orientation = Orientation.Vertical };
    private readonly StackPanel tasks = new() { Orientation = Orientation.Horizontal };
    private readonly DockPanel taskRegion = new();
    private readonly StackPanel taskPaging = new() { Orientation = Orientation.Horizontal, Visibility = Visibility.Collapsed };
    private readonly ScrollViewer taskScroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
    private readonly RepeatButton previousTask = new();
    private readonly RepeatButton nextTask = new();
    private readonly StackPanel programs = new();
    private readonly StackPanel places = new();
    private readonly Menu commands = new() { Background = Brushes.Transparent };
    private readonly List<WindowsXPMenuMirror> menuMirrors = [];
    private readonly Popup startMenu = new() { StaysOpen = false, Placement = PlacementMode.Top };
    private readonly ToggleButton start = new();
    private readonly List<Action> detachLaunchers = [];
    private readonly Dictionary<Window, TaskEntry> windows = [];
    internal static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(nameof(Scheme),
        typeof(WindowsXPColorScheme), typeof(WindowsXPDesktop),
        new PropertyMetadata(WindowsXPColorScheme.Blue, (target, args) => ((WindowsXPDesktop)target).ApplyScheme((WindowsXPColorScheme)args.NewValue)),
        value => Enum.IsDefined((WindowsXPColorScheme)value));
    internal WindowsXPColorScheme Scheme
    {
        get => (WindowsXPColorScheme)GetValue(SchemeProperty);
        set => SetValue(SchemeProperty, value);
    }
    private WindowsXPColorScheme scheme;
    private TextBlock? startText;
    private TextBlock? userText;
    private StackPanel? startContent;
    private WindowsXPResources? resources;
    private WindowsXPResources? popupResources;
    private bool disposed;

    internal event EventHandler? ThemesRequested;

    internal WindowsXPDesktop(WindowsXPColorScheme scheme, string title, IEnumerable<WindowsXPLauncherDefinition> definitions)
    {
        this.scheme = scheme;
        try
        {
            resources = new WindowsXPResources(this, scheme);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(WindowsXPDesktopMetrics.StylesUri, UriKind.Relative) });
            Background = WindowsXPSystemMetrics.Desktop(scheme);
            var layout = new DockPanel();
            var taskbar = new Grid { Height = WindowsXPDesktopMetrics.TaskbarHeight };
            DockPanel.SetDock(taskbar, Dock.Bottom);
            taskbar.Children.Add(new WindowsXPImage { Part = WindowsXPImagePart.TaskbarBackground });
            var band = new DockPanel();
            start.MinWidth = WindowsXPDesktopMetrics.StartWidth;
            start.SetResourceReference(StyleProperty, WindowsXPDesktopMetrics.StartButtonStyle);
            var startFont = WindowsXPSystemMetrics.StartFont(scheme);
            startText = new TextBlock { FontFamily = startFont.Family, FontSize = startFont.Size,
                FontWeight = startFont.Weight, FontStyle = FontStyles.Italic, Foreground = WindowsXPSystemMetrics.StartText(scheme) };
            startText.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(WindowsXPDesktopMetrics.StartResource));
            startContent = new StackPanel { Orientation = Orientation.Horizontal,
                Margin = WindowsXPImageCatalog.Definition(scheme, WindowsXPImagePart.StartButton).ContentInsets };
            startContent.Children.Add(new Image { Source = WindowsXPApplicationIcons.StartLogo, Width = WindowsXPDesktopMetrics.StartLogoWidth,
                Height = WindowsXPDesktopMetrics.StartLogoHeight, Margin = WindowsXPDesktopMetrics.StartLogoGap });
            startContent.Children.Add(startText);
            start.Content = startContent;
            start.Click += StartClicked;
            startMenu.Closed += StartClosed;
            startMenu.PlacementTarget = start;
            band.Children.Add(start);
            previousTask.Width = nextTask.Width = WindowsXPDesktopMetrics.TaskPagingButtonWidth;
            previousTask.Content = TaskArrow(WindowsXPDesktopMetrics.PreviousTaskArrow);
            nextTask.Content = TaskArrow(WindowsXPDesktopMetrics.NextTaskArrow);
            previousTask.Click += PreviousTasks;
            nextTask.Click += NextTasks;
            taskPaging.Children.Add(previousTask);
            taskPaging.Children.Add(nextTask);
            DockPanel.SetDock(taskPaging, Dock.Right);
            taskRegion.Children.Add(taskPaging);
            taskScroll.Content = tasks;
            taskScroll.ScrollChanged += TaskScrollChanged;
            taskRegion.SizeChanged += TaskRegionChanged;
            taskRegion.Children.Add(taskScroll);
            band.Children.Add(taskRegion);
            taskbar.Children.Add(band);
            layout.Children.Add(taskbar);
            layout.Children.Add(new Border { Padding = WindowsXPDesktopMetrics.DesktopPadding,
                Child = new ScrollViewer { Content = icons, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled } });
            Content = layout;
            BuildStartMenu(title);
            SetLaunchers(definitions);
            SetCurrentValue(SchemeProperty, scheme);
        }
        catch { Dispose(); throw; }
    }

    private void BuildStartMenu(string title)
    {
        var menu = new Grid { Width = WindowsXPDesktopMetrics.MenuWidth, Height = WindowsXPDesktopMetrics.MenuHeight };
        popupResources = new WindowsXPResources(menu, scheme);
        menu.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(WindowsXPDesktopMetrics.StylesUri, UriKind.Relative) });
        menu.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WindowsXPDesktopMetrics.MenuHeaderHeight) });
        menu.RowDefinitions.Add(new RowDefinition());
        menu.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WindowsXPDesktopMetrics.MenuFooterHeight) });
        menu.ColumnDefinitions.Add(new ColumnDefinition());
        menu.ColumnDefinitions.Add(new ColumnDefinition());
        var header = new Grid();
        header.Children.Add(new WindowsXPImage { Part = WindowsXPImagePart.StartUserPanel });
        var userFont = WindowsXPSystemMetrics.UserFont(scheme);
        userText = new TextBlock { Text = title, Foreground = Brushes.White, FontFamily = userFont.Family, FontSize = userFont.Size,
            FontWeight = userFont.Weight, VerticalAlignment = VerticalAlignment.Center, Margin = WindowsXPDesktopMetrics.UserTitlePadding };
        header.Children.Add(userText);
        Grid.SetColumnSpan(header, menu.ColumnDefinitions.Count);
        menu.Children.Add(header);
        AddColumn(menu, programs, WindowsXPImagePart.StartProgramsBackground, false);
        AddColumn(menu, places, WindowsXPImagePart.StartPlacesBackground, true);
        var footer = new Grid();
        footer.Children.Add(new WindowsXPImage { Part = WindowsXPImagePart.StartFooter });
        var themes = new Button { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            Padding = WindowsXPDesktopMetrics.ProgramPadding };
        themes.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(WindowsXPDesktopMetrics.ThemesResource));
        themes.Click += ChooseTheme;
        footer.Children.Add(themes);
        Grid.SetRow(footer, menu.RowDefinitions.Count - 1);
        Grid.SetColumnSpan(footer, menu.ColumnDefinitions.Count);
        menu.Children.Add(footer);
        startMenu.Child = menu;
        themeButton = themes;
    }

    private Button? themeButton;

    private void AddColumn(Grid menu, Panel content, WindowsXPImagePart part, bool right)
    {
        var column = new Grid();
        column.Children.Add(new WindowsXPImage { Part = part });
        var body = new DockPanel();
        if (right)
        {
            commands.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            DockPanel.SetDock(commands, Dock.Top);
            body.Children.Add(commands);
        }
        body.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        column.Children.Add(body);
        Grid.SetRow(column, 1);
        if (right) Grid.SetColumn(column, 1);
        menu.Children.Add(column);
    }

    internal void ConnectMenus(IEnumerable<MenuItem> menus)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        foreach (var source in menus)
        {
            var target = new MenuItem();
            menuMirrors.Add(new WindowsXPMenuMirror(source, target));
            commands.Items.Add(target);
        }
    }

    internal void SetLaunchers(IEnumerable<WindowsXPLauncherDefinition> definitions)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var entries = definitions.ToArray();
        foreach (var detach in detachLaunchers) detach();
        detachLaunchers.Clear();
        icons.Children.Clear();
        programs.Children.Clear();
        places.Children.Clear();
        foreach (var definition in entries)
        {
            var desktop = new Button { Tag = definition.Id, Width = WindowsXPDesktopMetrics.LauncherWidth,
                Height = WindowsXPDesktopMetrics.LauncherHeight, Content = LauncherContent(definition, true), ToolTip = definition.Title };
            desktop.SetResourceReference(StyleProperty, WindowsXPDesktopMetrics.DesktopButtonStyle);
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
            desktop.MouseDoubleClick += doubleClick;
            desktop.AddHandler(Keyboard.KeyDownEvent, enter, true);
            var item = new Button { Content = LauncherContent(definition, false), Tag = definition.Id };
            item.SetResourceReference(StyleProperty, WindowsXPDesktopMetrics.ProgramButtonStyle);
            RoutedEventHandler launch = (_, _) =>
            {
                startMenu.IsOpen = false;
                if (!disposed) definition.Launch();
            };
            item.Click += launch;
            detachLaunchers.Add(() =>
            {
                desktop.MouseDoubleClick -= doubleClick;
                desktop.RemoveHandler(Keyboard.KeyDownEvent, enter);
                item.Click -= launch;
            });
            icons.Children.Add(desktop);
            (definition.Group == WindowsXPProgramGroup.Media ? programs : places).Children.Add(item);
        }
    }

    private static FrameworkElement LauncherContent(WindowsXPLauncherDefinition definition, bool desktop)
    {
        var layout = new StackPanel { Orientation = desktop ? Orientation.Vertical : Orientation.Horizontal };
        layout.Children.Add(new Image { Source = definition.Icon ?? WindowsXPApplicationIcons.ForIcon(WindowsXPIcon.Generic),
            Width = WindowsXPDesktopMetrics.LauncherIconSize, Height = WindowsXPDesktopMetrics.LauncherIconSize,
            Margin = desktop ? default : WindowsXPDesktopMetrics.IconGap });
        layout.Children.Add(new TextBlock { Text = definition.Title, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center, TextAlignment = desktop ? TextAlignment.Center : TextAlignment.Left });
        return layout;
    }

    internal void RegisterWindow(Window window)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (windows.ContainsKey(window)) return;
        windows.Add(window, new TaskEntry(window, tasks, () => { windows.Remove(window); RefreshTaskWidths(); }, scheme));
        RefreshTaskWidths();
    }

    private static FrameworkElement TaskArrow(string geometry) => new System.Windows.Shapes.Path
    {
        Data = Geometry.Parse(geometry),
        Width = WindowsXPDesktopMetrics.TaskArrowSize,
        Height = WindowsXPDesktopMetrics.TaskArrowSize,
        Stretch = Stretch.Uniform,
        Stroke = Brushes.Black
    };
    private void TaskRegionChanged(object sender, SizeChangedEventArgs args) => RefreshTaskWidths();
    private void TaskScrollChanged(object sender, ScrollChangedEventArgs args)
    {
        previousTask.IsEnabled = taskScroll.HorizontalOffset > 0;
        nextTask.IsEnabled = taskScroll.HorizontalOffset < taskScroll.ScrollableWidth;
    }
    private void PreviousTasks(object sender, RoutedEventArgs args) => taskScroll.PageLeft();
    private void NextTasks(object sender, RoutedEventArgs args) => taskScroll.PageRight();
    private void RefreshTaskWidths()
    {
        if (disposed || tasks.Children.Count == 0 || taskRegion.ActualWidth <= 0)
        {
            taskPaging.Visibility = Visibility.Collapsed;
            return;
        }
        var overflow = tasks.Children.Count * WindowsXPDesktopMetrics.MinimumTaskWidth > taskRegion.ActualWidth;
        taskPaging.Visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
        var available = taskRegion.ActualWidth - (overflow ? previousTask.Width + nextTask.Width : 0);
        var width = Math.Clamp(available / tasks.Children.Count, WindowsXPDesktopMetrics.MinimumTaskWidth, WindowsXPDesktopMetrics.TaskWidth);
        foreach (FrameworkElement button in tasks.Children) button.Width = width;
    }

    private void ApplyScheme(WindowsXPColorScheme value)
    {
        if (disposed || scheme == value) return;
        scheme = value;
        WindowsXPResources.ApplyScheme(this, value);
        if (startMenu.Child is FrameworkElement menu) WindowsXPResources.ApplyScheme(menu, value);
        Background = WindowsXPSystemMetrics.Desktop(value);
        var startFont = WindowsXPSystemMetrics.StartFont(value);
        if (startText is not null)
        {
            startText.FontFamily = startFont.Family;
            startText.FontSize = startFont.Size;
            startText.FontWeight = startFont.Weight;
            startText.Foreground = WindowsXPSystemMetrics.StartText(value);
        }
        if (startContent is not null) startContent.Margin = WindowsXPImageCatalog.Definition(value, WindowsXPImagePart.StartButton).ContentInsets;
        if (userText is not null)
        {
            var font = WindowsXPSystemMetrics.UserFont(value);
            userText.FontFamily = font.Family;
            userText.FontSize = font.Size;
            userText.FontWeight = font.Weight;
        }
        foreach (var entry in windows.Values) entry.ApplyScheme(value);
    }

    private void StartClicked(object sender, RoutedEventArgs args) => startMenu.IsOpen = start.IsChecked == true;
    private void StartClosed(object? sender, EventArgs args) => start.IsChecked = false;
    private void ChooseTheme(object sender, RoutedEventArgs args)
    {
        startMenu.IsOpen = false;
        ThemesRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { startMenu.IsOpen = false; }
        finally
        {
            try
            {
                foreach (var entry in windows.Values.ToArray()) entry.Dispose();
                windows.Clear();
                previousTask.Click -= PreviousTasks;
                nextTask.Click -= NextTasks;
                taskScroll.ScrollChanged -= TaskScrollChanged;
                taskRegion.SizeChanged -= TaskRegionChanged;
                taskScroll.Content = null;
                foreach (var mirror in menuMirrors) mirror.Dispose();
                menuMirrors.Clear();
                commands.Items.Clear();
                foreach (var detach in detachLaunchers) detach();
                detachLaunchers.Clear();
                start.Click -= StartClicked;
                startMenu.Closed -= StartClosed;
                if (themeButton is not null) themeButton.Click -= ChooseTheme;
                ThemesRequested = null;
                startMenu.PlacementTarget = null;
                startMenu.Child = null;
                icons.Children.Clear();
                programs.Children.Clear();
                places.Children.Clear();
                tasks.Children.Clear();
                Content = null;
            }
            finally
            {
                try { popupResources?.Dispose(); }
                finally { resources?.Dispose(); Resources.MergedDictionaries.Clear(); }
            }
        }
    }

    private sealed class TaskEntry : IDisposable
    {
        private readonly Window window;
        private readonly Panel parent;
        private readonly Action remove;
        private readonly ToggleButton button;
        private readonly TextBlock title = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        private readonly ThemeBindingScope bindings = new();
        private bool disposed;

        internal TaskEntry(Window window, Panel parent, Action remove, WindowsXPColorScheme scheme)
        {
            this.window = window;
            this.parent = parent;
            this.remove = remove;
            button = new ToggleButton { Width = WindowsXPDesktopMetrics.TaskWidth, Height = WindowsXPDesktopMetrics.TaskHeight };
            try
            {
                button.SetResourceReference(StyleProperty, WindowsXPDesktopMetrics.TaskButtonStyle);
                var content = new DockPanel();
                var icon = new Image { Width = WindowsXPDesktopMetrics.SmallIconSize, Height = WindowsXPDesktopMetrics.SmallIconSize,
                    Margin = WindowsXPDesktopMetrics.IconGap };
                ApplyScheme(scheme);
                content.Children.Add(icon);
                content.Children.Add(title);
                button.Content = content;
                bindings.OneWay(window, Window.TitleProperty, title, TextBlock.TextProperty);
                bindings.OneWay(window, Window.IconProperty, icon, Image.SourceProperty);
                button.Click += Restore;
                window.Activated += StateChanged;
                window.Deactivated += StateChanged;
                window.StateChanged += StateChanged;
                window.Closed += Closed;
                parent.Children.Add(button);
                StateChanged(null, EventArgs.Empty);
            }
            catch { Dispose(); throw; }
        }

        private void StateChanged(object? sender, EventArgs args) => button.IsChecked = window.IsActive && window.WindowState != WindowState.Minimized;
        internal void ApplyScheme(WindowsXPColorScheme scheme) => title.Foreground = WindowsXPSystemMetrics.TaskText(scheme);
        private void Restore(object sender, RoutedEventArgs args)
        {
            if (disposed) return;
            if (window.IsActive) window.WindowState = WindowState.Minimized;
            else
            {
                if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                if (!window.IsVisible) window.Show();
                window.Activate();
            }
        }
        private void Closed(object? sender, EventArgs args)
        {
            try { Dispose(); }
            finally { remove(); }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { bindings.Dispose(); }
            finally
            {
                button.Click -= Restore;
                window.Activated -= StateChanged;
                window.Deactivated -= StateChanged;
                window.StateChanged -= StateChanged;
                window.Closed -= Closed;
                parent.Children.Remove(button);
                button.Content = null;
            }
        }
    }
}
