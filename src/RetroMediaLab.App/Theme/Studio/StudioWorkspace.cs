using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Views.Controls.Common;

namespace RetroMediaLab.App.Theme.Studio;

internal sealed class StudioWorkspace : Grid, IThemeWorkspace
{
    private readonly Grid root;
    private readonly TabControl tabs;
    private readonly Visibility originalVisibility;
    private readonly Canvas desktop = new() { ClipToBounds = true };
    private readonly List<Entry> entries = [];
    private bool changingSelection;
    private bool disposed;
    private int zIndex;
    private sealed record Entry(TabItem Tab, StudioPane Pane, Button Task) { internal bool Open { get; set; } }
    internal IReadOnlyList<StudioPane> Panes => entries.Select(entry => entry.Pane).ToArray();

    internal StudioWorkspace(Grid root, TabControl tabs)
    {
        this.root = root;
        this.tabs = tabs;
        originalVisibility = tabs.Visibility;
        try
        {
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(StudioConstants.ToolbarHeight) });
            RowDefinitions.Add(new RowDefinition());
            var tasks = new StackPanel { Orientation = Orientation.Horizontal };
            var taskScroller = new ScrollViewer { Content = tasks, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            taskScroller.SetResourceReference(BackgroundProperty, "CardBrush");
            Children.Add(taskScroller);
            desktop.SetResourceReference(BackgroundProperty, StudioConstants.WorkspaceBrush);
            SetRow(desktop, 1);
            Children.Add(desktop);
            SetRow(this, GetRow(tabs));
            SetColumn(this, GetColumn(tabs));
            SetRowSpan(this, GetRowSpan(tabs));
            SetColumnSpan(this, GetColumnSpan(tabs));
            foreach (var tab in tabs.Items.OfType<TabItem>())
            {
                if (tab.Content is not FrameworkElement view || tab.Header is not MainTabHeader header)
                    throw new InvalidOperationException(nameof(TabItem.Content));
                var index = entries.Count;
                tab.Content = null;
                StudioPane? pane = null;
                try
                {
                    pane = new StudioPane(header, view, this, index) { Visibility = Visibility.Collapsed };
                    var task = new Button { Margin = new Thickness(3, 5, 0, 5), Padding = new Thickness(12, 4, 12, 4) };
                    task.SetBinding(ContentControl.ContentProperty, new Binding(nameof(MainTabHeader.Text)) { Source = header });
                    task.Click += (_, _) => Activate(index);
                    tasks.Children.Add(task);
                    entries.Add(new Entry(tab, pane, task));
                    desktop.Children.Add(pane);
                }
                catch
                {
                    try { pane?.Release(); }
                    finally { tab.Content = view; }
                    throw;
                }
            }
            tabs.Visibility = Visibility.Collapsed;
            root.Children.Add(this);
            tabs.SelectionChanged += TabChanged;
            root.PreviewKeyDown += WorkspaceKeyDown;
            desktop.SizeChanged += DesktopSizeChanged;
            Activate(Math.Clamp(tabs.SelectedIndex, 0, entries.Count - 1));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void WorkspaceKeyDown(object sender, KeyEventArgs args)
    {
        if (disposed || args.Key != Key.Tab || (Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        var direction = (Keyboard.Modifiers & ModifierKeys.Shift) == 0 ? 1 : -1;
        Activate((tabs.SelectedIndex + direction + entries.Count) % entries.Count);
        args.Handled = true;
    }

    private void TabChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!changingSelection && ReferenceEquals(args.OriginalSource, tabs) && tabs.SelectedIndex >= 0)
            Activate(tabs.SelectedIndex);
    }

    internal void Activate(int index)
    {
        if (disposed || index < 0 || index >= entries.Count) return;
        var entry = entries[index];
        if (!entry.Open)
        {
            entry.Open = true;
            if (entry.Pane.RestoreBounds.Width <= 0)
            {
                var offset = StudioConstants.EdgeSpacing + entries.Count(item => item.Open) * StudioConstants.CascadeStep;
                entry.Pane.RestoreBounds = new Rect(offset, offset, StudioConstants.InitialWidth, StudioConstants.InitialHeight);
            }
        }
        changingSelection = true;
        try { tabs.SelectedIndex = index; }
        finally { changingSelection = false; }
        Panel.SetZIndex(entry.Pane, ++zIndex);
        foreach (var item in entries)
        {
            // Native video HWNDs must not overlap WPF document windows.
            item.Pane.Visibility = item.Open && (entry.Pane.IsExclusive ? ReferenceEquals(item, entry) : !item.Pane.IsExclusive)
                ? Visibility.Visible : Visibility.Collapsed;
            item.Pane.SetActive(ReferenceEquals(item, entry));
            item.Task.SetResourceReference(Control.BackgroundProperty, ReferenceEquals(item, entry) ? "SelectedBrush" : "CardBrush");
        }
        Place(entry.Pane);
    }

    internal void Minimize(int index)
    {
        if (disposed || index < 0 || index >= entries.Count) return;
        entries[index].Open = false;
        entries[index].Pane.Visibility = Visibility.Collapsed;
        var next = entries.Select((entry, candidate) => (entry, candidate)).Where(item => item.entry.Open)
            .OrderByDescending(item => Panel.GetZIndex(item.entry.Pane)).Select(item => item.candidate).DefaultIfEmpty(-1).First();
        if (next >= 0) Activate(next);
    }

    internal void ToggleMaximize(int index)
    {
        if (disposed || index < 0 || index >= entries.Count || entries[index].Pane.IsExclusive) return;
        Activate(index);
        var pane = entries[index].Pane;
        pane.IsMaximized = !pane.IsMaximized;
        Place(pane);
    }

    internal void Move(int index, double horizontal, double vertical)
    {
        if (disposed || index < 0 || index >= entries.Count) return;
        var pane = entries[index].Pane;
        if (pane.IsMaximized || pane.IsExclusive) return;
        Activate(index);
        var bounds = pane.RestoreBounds;
        bounds.Offset(horizontal, vertical);
        pane.RestoreBounds = bounds;
        Place(pane);
    }

    internal void Resize(int index, double horizontal, double vertical)
    {
        if (disposed || index < 0 || index >= entries.Count) return;
        var pane = entries[index].Pane;
        if (pane.IsMaximized || pane.IsExclusive) return;
        Activate(index);
        var bounds = pane.RestoreBounds;
        pane.RestoreBounds = new Rect(bounds.Location, new Size(Math.Max(StudioConstants.MinimumWidth, bounds.Width + horizontal), Math.Max(StudioConstants.MinimumHeight, bounds.Height + vertical)));
        Place(pane);
    }

    private void DesktopSizeChanged(object sender, SizeChangedEventArgs args)
    {
        foreach (var entry in entries.Where(entry => entry.Open)) Place(entry.Pane);
    }

    private void Place(StudioPane pane)
    {
        var width = desktop.ActualWidth;
        var height = desktop.ActualHeight;
        if (width <= 0 || height <= 0) return;
        Rect bounds;
        if (pane.IsExclusive || pane.IsMaximized) bounds = new Rect(0, 0, width, height);
        else
        {
            var saved = pane.RestoreBounds;
            if (!pane.HasPlacement)
            {
                saved = new Rect(saved.Location, new Size(Math.Min(saved.Width, width * StudioConstants.RestoredRatio), Math.Min(saved.Height, height * StudioConstants.RestoredRatio)));
                pane.HasPlacement = true;
            }
            var wantedWidth = Math.Clamp(saved.Width, Math.Min(StudioConstants.MinimumWidth, width), width);
            var wantedHeight = Math.Clamp(saved.Height, Math.Min(StudioConstants.MinimumHeight, height), height);
            bounds = new Rect(Math.Clamp(saved.X, 0, width - wantedWidth), Math.Clamp(saved.Y, 0, height - wantedHeight), wantedWidth, wantedHeight);
            pane.RestoreBounds = bounds;
        }
        pane.Width = bounds.Width;
        pane.Height = bounds.Height;
        Canvas.SetLeft(pane, bounds.X);
        Canvas.SetTop(pane, bounds.Y);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            tabs.SelectionChanged -= TabChanged;
            root.PreviewKeyDown -= WorkspaceKeyDown;
            desktop.SizeChanged -= DesktopSizeChanged;
            foreach (var entry in entries)
            {
                try { entry.Pane.Release(); }
                catch (Exception error) { failure ??= error; }
                finally
                {
                    try { entry.Tab.Content = entry.Pane.View; }
                    catch (Exception error) { failure ??= error; }
                }
            }
        }
        finally
        {
            desktop.Children.Clear();
            Children.Clear();
            entries.Clear();
            root.Children.Remove(this);
            tabs.Visibility = originalVisibility;
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
