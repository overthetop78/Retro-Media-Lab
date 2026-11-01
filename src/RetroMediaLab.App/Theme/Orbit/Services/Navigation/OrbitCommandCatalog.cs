using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RetroMediaLab.App.Theme.Orbit;

/// <summary>Observes the existing command tree without moving or changing its visual elements.</summary>
internal sealed class OrbitCommandCatalog : IDisposable
{
    private readonly MenuItem[] roots;
    private readonly List<Action> detach = [];
    private DispatcherOperation? refresh;
    private bool disposed;
    private bool rebuilding;
    internal IReadOnlyList<OrbitCommand> Commands { get; private set; } = [];
    internal event EventHandler? Changed;

    internal OrbitCommandCatalog(IEnumerable<MenuItem> roots)
    {
        this.roots = roots.ToArray();
        try { Rebuild(); }
        catch { Dispose(); throw; }
    }

    private void QueueRefresh()
    {
        if (disposed || rebuilding || refresh?.Status == DispatcherOperationStatus.Pending || roots.Length == 0) return;
        refresh = roots[0].Dispatcher.BeginInvoke(() =>
        {
            refresh = null;
            if (!disposed) Rebuild();
        }, DispatcherPriority.DataBind);
    }

    private void Rebuild()
    {
        rebuilding = true;
        try
        {
            foreach (var release in detach) release();
            detach.Clear();
            var commands = new List<OrbitCommand>();
            foreach (var root in roots) Collect(root, [], true, commands);
            Commands = commands;
        }
        finally { rebuilding = false; }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Collect(MenuItem item, IReadOnlyList<string> ancestors, bool parentVisible, List<OrbitCommand> commands)
    {
        item.VerifyAccess();
        foreach (var property in new[] { HeaderedItemsControl.HeaderProperty, UIElement.VisibilityProperty,
                     UIElement.IsEnabledProperty, MenuItem.IsCheckedProperty, MenuItem.IsCheckableProperty })
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(property, item.GetType());
            EventHandler changed = (_, _) => QueueRefresh();
            descriptor.AddValueChanged(item, changed);
            detach.Add(() => descriptor.RemoveValueChanged(item, changed));
        }
        if (item.Items is INotifyCollectionChanged collection)
        {
            NotifyCollectionChangedEventHandler changed = (_, _) => QueueRefresh();
            collection.CollectionChanged += changed;
            detach.Add(() => collection.CollectionChanged -= changed);
        }
        var title = item.Header switch
        {
            TextBlock text => text.Text,
            AccessText text => text.Text,
            _ => item.Header?.ToString() ?? string.Empty
        };
        var visible = parentVisible && item.Visibility == Visibility.Visible;
        if (item.HasItems)
        {
            var path = ancestors.Append(title).ToArray();
            foreach (var child in item.Items.OfType<MenuItem>()) Collect(child, path, visible, commands);
        }
        else if (ancestors.Count != 0 && visible && !string.IsNullOrWhiteSpace(title))
            commands.Add(new OrbitCommand(item, title, string.Join(OrbitMetrics.PathSeparator, ancestors)));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            refresh?.Abort();
            foreach (var release in detach) release();
        }
        finally
        {
            refresh = null;
            detach.Clear();
            Commands = [];
            Changed = null;
        }
    }
}
