using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.App.Views.Controls.Emulation.Machine;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal partial class Windows31ProgramManager : UserControl, IDisposable
{
    private sealed class GroupWindow(Windows31ProgramGroup id, string title, ListBox applications, ListBoxItem icon)
    {
        internal Windows31ProgramGroup Id { get; } = id;
        internal string Title { get; } = title;
        internal ListBox Applications { get; } = applications;
        internal ListBoxItem Icon { get; } = icon;
        internal Windows31WindowPlacement? Placement { get; set; }
        internal Windows31Frame? Frame { get; set; }
        internal PropertyChangedEventHandler? StateHandler { get; set; }
        internal MouseButtonEventHandler? ActivateHandler { get; set; }
    }

    private const string IconListStyleKey = "IconList";
    private readonly Dictionary<Windows31ProgramGroup, GroupWindow> groups = [];
    private readonly Action<Windows31ApplicationId> open;
    private readonly List<(MenuItem Item, RoutedEventHandler Handler)> groupCommands = [];
    private object? selected;
    private Func<EmulationConfigurationListItem, Task>? openMachine;
    private EmulationConfigurationListItem[] machineConfigurations = [];
    private bool openingMachine;
    private GroupWindow? preferredGroup;
    private bool applicationActive = true;
    private int zIndex;
    private bool disposed;

    internal Windows31ProgramManager(IReadOnlyList<Windows31ApplicationDefinition> definitions, Action<Windows31ApplicationId> open)
    {
        this.open = open;
        try
        {
            InitializeComponent();
            Windows31PaletteResources.Initialize(this);
            PreferencesCommand.IsEnabled = definitions.Any(item => item.Id == Windows31ApplicationId.Preferences);
            var initialGroup = definitions.FirstOrDefault(item => item.Group == Windows31ProgramGroup.Media)?.Group ?? definitions.FirstOrDefault()?.Group;
            foreach (var grouping in definitions.GroupBy(item => item.Group))
            {
                var entry = CreateGroup(grouping.Key);
                foreach (var definition in grouping) entry.Applications.Items.Add(CreateIcon(definition.TitleResource, definition.Icon, definition.Id));
                if (grouping.Key != initialGroup) entry.Placement!.Iconify();
            }
            if (initialGroup is { } initial && groups.TryGetValue(initial, out var activeGroup)) ActivateGroup(activeGroup);
        }
        catch { Dispose(); throw; }
    }

    private GroupWindow CreateGroup(Windows31ProgramGroup id)
    {
        if (groups.TryGetValue(id, out var existing)) return existing;
        var title = GroupTitle(id);
        var applications = new ListBox { Style = (Style)FindResource(IconListStyleKey) };
        applications.SetResourceReference(BackgroundProperty, Windows31PaletteResources.Window);
        applications.SelectionChanged += SelectionChanged;
        applications.MouseDoubleClick += LauncherDoubleClick;
        applications.KeyDown += LauncherKeyDown;
        var entry = new GroupWindow(id, title, applications, CreateIcon(title, null, id));
        groups.Add(id, entry);
        OpenGroup(entry);
        var command = new MenuItem();
        command.SetBinding(MenuItem.HeaderProperty, LocExtension.CreateBinding(title));
        RoutedEventHandler handler = (_, _) => OpenGroup(entry);
        command.Click += handler;
        GroupsMenu.Items.Add(command);
        groupCommands.Add((command, handler));
        return entry;
    }

    internal void SetMachines(IReadOnlyList<EmulationConfigurationListItem> configurations,
        Func<EmulationConfigurationListItem, Task> open)
    {
        if (disposed) return;
        openMachine = open;
        var alreadyPresent = groups.ContainsKey(Windows31ProgramGroup.Emulation);
        var previousGroup = preferredGroup;
        var group = CreateGroup(Windows31ProgramGroup.Emulation);
        if (!alreadyPresent)
        {
            group.Placement!.Iconify();
            if (previousGroup is not null) ActivateGroup(previousGroup);
        }
        if (machineConfigurations.SequenceEqual(configurations)) return;
        var previous = (group.Applications.SelectedItem as ListBoxItem)?.Tag as EmulationConfigurationListItem;
        group.Applications.Items.Clear();
        machineConfigurations = configurations.ToArray();
        foreach (var configuration in configurations)
        {
            var machine = configuration.Module.Machines.FirstOrDefault(item => item.Id == configuration.Configuration.MachineId);
            var item = CreateIcon(configuration.DisplayName, EmulationAssetFunctions.Load(machine?.ImagePath), configuration, literalTitle: true);
            group.Applications.Items.Add(item);
            if (previous?.Module.Id == configuration.Module.Id && previous.Configuration.Id == configuration.Configuration.Id)
                group.Applications.SelectedItem = item;
        }
    }

    private readonly List<Windows31MenuBindings> sharedMenus = [];

    internal void ConnectMenus(GWGUI.App.Views.Controls.Shell.MainMenu source)
    {
        sharedMenus.Add(new Windows31MenuBindings(source.OptionsMenuItem, SharedOptions, source.EmulationMenuItem));
        sharedMenus.Add(new Windows31MenuBindings(source.HelpMenuItem, SharedHelp));
    }

    internal event EventHandler? CloseRequested;
    internal event EventHandler? DocumentationRequested;
    internal event EventHandler? AboutRequested;

    internal ListBoxItem CreateIcon(string title, ImageSource? image, object tag, bool literalTitle = false)
    {
        var item = new ListBoxItem { Style = (Style)FindResource(Windows31Metrics.LauncherStyleKey), Tag = tag };
        var layout = new StackPanel();
        var key = tag switch
        {
            Windows31ApplicationId application => application,
            EmulationConfigurationListItem => Windows31ApplicationId.Emulation,
            _ => Windows31ApplicationId.ProgramManager
        };
        var artwork = new Image { Source = image ?? (ImageSource)FindResource(key), Width = Windows31Metrics.IconImageSize, Height = Windows31Metrics.IconImageSize, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 0, Windows31Metrics.IconGap) };
        RenderOptions.SetBitmapScalingMode(artwork, BitmapScalingMode.NearestNeighbor);
        layout.Children.Add(artwork);
        var caption = new TextBlock { TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center };
        if (literalTitle) caption.Text = title;
        else caption.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(title));
        caption.SetBinding(TextBlock.BackgroundProperty, new Binding(nameof(Control.Background)) { Source = item });
        caption.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Control.Foreground)) { Source = item });
        layout.Children.Add(caption);
        item.Content = layout;
        return item;
    }

    internal static void SetLiteralIconTitle(ListBoxItem icon, string title)
    {
        if (icon.Content is StackPanel panel && panel.Children.OfType<TextBlock>().SingleOrDefault() is { } caption)
            caption.Text = title;
    }

    internal Style IconListStyle => (Style)FindResource(IconListStyleKey);

    internal void EnablePreferences()
    {
        PreferencesCommand.IsEnabled = true;
        EmulationPreferencesCommand.IsEnabled = true;
        var group = CreateGroup(Windows31ProgramGroup.System);
        foreach (var choice in new[]
        {
            (Id: Windows31ApplicationId.Preferences, Title: "Options.PreferencesTitle"),
            (Id: Windows31ApplicationId.EmulationPreferences, Title: "Options.EmulationTitle")
        })
            if (!group.Applications.Items.OfType<ListBoxItem>().Any(item => Equals(item.Tag, choice.Id)))
                group.Applications.Items.Add(CreateIcon(choice.Title, null, choice.Id));
    }

    internal void ShowGroup(Windows31ProgramGroup id)
    {
        if (!disposed && groups.TryGetValue(id, out var group)) OpenGroup(group);
    }

    internal void SetApplicationActive(bool active)
    {
        if (disposed) return;
        applicationActive = active;
        if (!active)
        {
            foreach (var group in groups.Values) group.Placement?.Deactivate();
            return;
        }
        var preferred = preferredGroup?.Placement?.IsVisible == true ? preferredGroup : groups.Values
            .Where(group => group.Placement?.IsVisible == true)
            .OrderByDescending(group => group.Frame is null ? 0 : Panel.GetZIndex(group.Frame)).FirstOrDefault();
        if (preferred is not null) ActivateGroup(preferred);
    }

    private static string GroupTitle(Windows31ProgramGroup group) => group switch
    {
        Windows31ProgramGroup.Media => Windows31Metrics.MediaGroupResource,
        Windows31ProgramGroup.Emulation => Windows31Metrics.EmulationGroupResource,
        Windows31ProgramGroup.System => Windows31Metrics.SystemGroupResource,
        _ => throw new ArgumentOutOfRangeException(nameof(group))
    };

    private Rect GroupBounds() => new(0, 0,
        GroupArea.ActualWidth > 0 ? GroupArea.ActualWidth : Windows31Metrics.ManagerInitialWidth,
        GroupArea.ActualHeight > 0 ? GroupArea.ActualHeight : Windows31Metrics.ManagerInitialHeight);

    private void OpenGroup(GroupWindow entry)
    {
        if (disposed) return;
        if (entry.Placement is null || entry.Placement.State == Windows31WindowState.Closed)
        {
            var offset = Windows31Metrics.DesktopInset + groups.Keys.ToList().IndexOf(entry.Id) * Windows31Metrics.CascadeStep;
            entry.Placement = new Windows31WindowPlacement(GroupBounds(), new Rect(offset, offset, Windows31Metrics.GroupInitialWidth, Windows31Metrics.GroupInitialHeight), new Size(Windows31Metrics.GroupMinimumWidth, Windows31Metrics.GroupMinimumHeight));
            entry.Frame = new Windows31Frame(entry.Placement, entry.Applications);
            entry.Frame.SetBinding(Windows31Frame.TitleProperty, LocExtension.CreateBinding(entry.Title));
            entry.StateHandler = (_, _) => UpdateGroup(entry);
            entry.ActivateHandler = (_, _) => ActivateGroup(entry);
            entry.Placement.PropertyChanged += entry.StateHandler;
            entry.Frame.PreviewMouseDown += entry.ActivateHandler;
            GroupArea.Children.Add(entry.Frame);
        }
        ActivateGroup(entry);
    }

    private void ActivateGroup(GroupWindow entry)
    {
        preferredGroup = entry;
        foreach (var other in groups.Values) if (!ReferenceEquals(entry, other)) other.Placement?.Deactivate();
        entry.Placement!.Activate();
        if (!applicationActive) entry.Placement.Deactivate();
        if (entry.Frame is not null) Panel.SetZIndex(entry.Frame, ++zIndex);
    }

    private void UpdateGroup(GroupWindow entry)
    {
        if (disposed || entry.Placement is null) return;
        var reduced = entry.Placement.State is Windows31WindowState.Iconified or Windows31WindowState.Closed;
        if (reduced && !GroupIcons.Items.Contains(entry.Icon)) GroupIcons.Items.Add(entry.Icon);
        if (!reduced) GroupIcons.Items.Remove(entry.Icon);
        if (entry.Placement.State == Windows31WindowState.Closed) ReleaseFrame(entry);
        ArrangeIcons();
    }

    private void ReleaseFrame(GroupWindow entry)
    {
        if (entry.Placement is not null && entry.StateHandler is not null) entry.Placement.PropertyChanged -= entry.StateHandler;
        try
        {
            if (entry.Frame is not null)
            {
                if (entry.ActivateHandler is not null) entry.Frame.PreviewMouseDown -= entry.ActivateHandler;
                entry.Frame.Dispose();
            }
        }
        finally
        {
            if (entry.Frame is not null) GroupArea.Children.Remove(entry.Frame);
            entry.Frame = null;
            entry.StateHandler = null;
            entry.ActivateHandler = null;
        }
    }

    private void ArrangeIcons()
    {
        GroupIcons.Width = GroupBounds().Width;
        Canvas.SetLeft(GroupIcons, 0);
        Canvas.SetTop(GroupIcons, Math.Max(0, GroupBounds().Height - Windows31Metrics.IconHeight));
    }

    private void GroupAreaSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (disposed || GroupArea.ActualWidth <= 0 || GroupArea.ActualHeight <= 0) return;
        foreach (var entry in groups.Values) entry.Placement?.SetDesktop(GroupBounds());
        ArrangeIcons();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        selected = (((ListBox)sender).SelectedItem as ListBoxItem)?.Tag;
        OpenCommand.IsEnabled = selected is Windows31ApplicationId or EmulationConfigurationListItem;
    }
    private async Task OpenSelectedAsync()
    {
        if (disposed || openingMachine) return;
        if (selected is Windows31ApplicationId id) open(id);
        else if (selected is EmulationConfigurationListItem configuration && openMachine is { } launch)
        {
            openingMachine = true;
            try { await launch(configuration); }
            finally { openingMachine = false; }
        }
    }
    private async void OpenSelectedClick(object sender, RoutedEventArgs args) => await OpenSelectedAsync();
    private async void LauncherDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left) return;
        args.Handled = true;
        await OpenSelectedAsync();
    }
    private async void LauncherKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;
        args.Handled = true;
        await OpenSelectedAsync();
    }
    private void RestoreSelectedGroup()
    {
        if (GroupIcons.SelectedItem is ListBoxItem { Tag: Windows31ProgramGroup id }) OpenGroup(groups[id]);
    }
    private void GroupIconDoubleClick(object sender, MouseButtonEventArgs args) { if (args.ChangedButton == MouseButton.Left) RestoreSelectedGroup(); }
    private void GroupIconKeyDown(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) { RestoreSelectedGroup(); args.Handled = true; } }
    private void CloseClick(object sender, RoutedEventArgs args) => CloseRequested?.Invoke(this, EventArgs.Empty);
    private void EmulationPreferencesClick(object sender, RoutedEventArgs args) => open(Windows31ApplicationId.EmulationPreferences);
    private void PreferencesClick(object sender, RoutedEventArgs args) => open(Windows31ApplicationId.Preferences);
    private void DocumentationClick(object sender, RoutedEventArgs args) => DocumentationRequested?.Invoke(this, EventArgs.Empty);
    private void AboutClick(object sender, RoutedEventArgs args) => AboutRequested?.Invoke(this, EventArgs.Empty);
    private void ArrangeIconsClick(object sender, RoutedEventArgs args) => ArrangeIcons();
    private void CascadeClick(object sender, RoutedEventArgs args)
    {
        var index = 0;
        foreach (var entry in groups.Values.Where(item => item.Placement?.IsVisible == true))
        {
            entry.Placement!.Restore();
            var offset = Windows31Metrics.DesktopInset + index++ * Windows31Metrics.CascadeStep;
            entry.Placement.MoveTo(new Point(offset, offset));
        }
    }
    private void TileClick(object sender, RoutedEventArgs args)
    {
        var visible = groups.Values.Where(item => item.Placement?.IsVisible == true).ToArray();
        if (visible.Length == 0) return;
        var area = GroupBounds();
        var columns = (int)Math.Ceiling(Math.Sqrt(visible.Length));
        var rows = (int)Math.Ceiling((double)visible.Length / columns);
        for (var index = 0; index < visible.Length; index++)
        {
            var placement = visible[index].Placement!;
            placement.Restore();
            placement.ResizeTo(new Size(area.Width / columns, area.Height / rows));
            placement.MoveTo(new Point(index % columns * area.Width / columns, index / columns * area.Height / rows));
        }
    }

    private void DetachMenu(MenuItem item)
    {
        foreach (var child in item.Items.OfType<MenuItem>()) DetachMenu(child);
        item.Click -= OpenSelectedClick;
        item.Click -= CloseClick;
        item.Click -= PreferencesClick;
        item.Click -= EmulationPreferencesClick;
        item.Click -= DocumentationClick;
        item.Click -= AboutClick;
        item.Click -= ArrangeIconsClick;
        item.Click -= CascadeClick;
        item.Click -= TileClick;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            foreach (var menu in sharedMenus)
            {
                try { menu.Dispose(); }
                catch (Exception error) { failure ??= error; }
            }
            sharedMenus.Clear();
            foreach (var entry in groups.Values)
            {
                try { ReleaseFrame(entry); }
                catch (Exception error) { failure ??= error; }
                finally
                {
                    entry.Applications.SelectionChanged -= SelectionChanged;
                    entry.Applications.MouseDoubleClick -= LauncherDoubleClick;
                    entry.Applications.KeyDown -= LauncherKeyDown;
                    entry.Applications.Items.Clear();
                }
            }
        }
        finally
        {
            if (MenuBar is not null) foreach (var item in MenuBar.Items.OfType<MenuItem>()) DetachMenu(item);
            foreach (var command in groupCommands) command.Item.Click -= command.Handler;
            groupCommands.Clear();
            groups.Clear();
            machineConfigurations = [];
            openMachine = null;
            selected = null;
            preferredGroup = null;
            if (GroupArea is not null) { GroupArea.SizeChanged -= GroupAreaSizeChanged; GroupArea.Children.Clear(); }
            if (GroupIcons is not null) { GroupIcons.MouseDoubleClick -= GroupIconDoubleClick; GroupIcons.KeyDown -= GroupIconKeyDown; GroupIcons.Items.Clear(); }
            Content = null;
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
