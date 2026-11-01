using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

/// <summary>Retains the existing command tree without borrowing its visual elements.</summary>
internal sealed class Windows95MenuMirror : IDisposable
{
    private readonly MenuItem source;
    private readonly MenuItem target;
    private readonly ThemeBindingScope bindings = new();
    private readonly List<Windows95MenuMirror> children = [];
    private readonly INotifyCollectionChanged? collection;
    private bool disposed;

    internal Windows95MenuMirror(MenuItem source, MenuItem target)
    {
        this.source = source;
        this.target = target;
        collection = source.Items as INotifyCollectionChanged;
        try
        {
            bindings.OneWay(source, HeaderedItemsControl.HeaderProperty, target, HeaderedItemsControl.HeaderProperty);
            bindings.OneWay(source, UIElement.IsEnabledProperty, target, UIElement.IsEnabledProperty);
            bindings.OneWay(source, UIElement.VisibilityProperty, target, UIElement.VisibilityProperty);
            bindings.OneWay(source, MenuItem.IsCheckableProperty, target, MenuItem.IsCheckableProperty);
            bindings.OneWay(source, MenuItem.IsCheckedProperty, target, MenuItem.IsCheckedProperty);
            target.Icon = new Image { Source = Windows95ApplicationIcons.ForCaption(source.Header?.ToString() ?? string.Empty),
                Width = Windows95Metrics.IconSize, Height = Windows95Metrics.IconSize };
            target.Click += Execute;
            if (collection is not null) collection.CollectionChanged += Changed;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }

    private void Execute(object sender, RoutedEventArgs args)
    {
        if (disposed || source.HasItems || !ReferenceEquals(args.OriginalSource, target)) return;
        args.Handled = true;
        if (source.IsEnabled) source.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, source));
    }
    private void Changed(object? sender, NotifyCollectionChangedEventArgs args) { if (!disposed) Rebuild(); }
    private void Rebuild()
    {
        foreach (var child in children) child.Dispose();
        children.Clear();
        target.Items.Clear();
        foreach (var item in source.Items)
        {
            if (item is MenuItem command)
            {
                var clone = new MenuItem();
                children.Add(new Windows95MenuMirror(command, clone));
                target.Items.Add(clone);
            }
            else if (item is Separator) target.Items.Add(new Separator());
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (collection is not null) collection.CollectionChanged -= Changed;
            target.Click -= Execute;
            foreach (var child in children) child.Dispose();
        }
        finally
        {
            children.Clear();
            target.Items.Clear();
            target.Icon = null;
            bindings.Dispose();
        }
    }
}
