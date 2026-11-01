using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Theme.Common;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal sealed class Windows31MenuBindings : IDisposable
{
    private readonly MenuItem source;
    private readonly MenuItem target;
    private readonly MenuItem[] additional;
    private readonly List<Action> detach = [];
    private ThemeBindingScope bindings = new();
    private bool disposed;

    internal Windows31MenuBindings(MenuItem source, MenuItem target, params MenuItem[] additional)
    {
        this.source = source;
        this.target = target;
        this.additional = additional;
        try { Rebuild(); }
        catch { Dispose(); throw; }
    }

    private void Rebuild()
    {
        if (disposed) return;
        Release();
        bindings = new ThemeBindingScope();
        Watch(source);
        foreach (var item in source.Items) Copy(item, target);
        foreach (var item in additional) Copy(item, target);
    }

    private void Watch(MenuItem original)
    {
        var changes = (INotifyCollectionChanged)original.Items;
        NotifyCollectionChangedEventHandler changed = (_, _) => Rebuild();
        changes.CollectionChanged += changed;
        detach.Add(() => changes.CollectionChanged -= changed);
    }

    private void Copy(object item, MenuItem parent)
    {
        if (item is Separator) { parent.Items.Add(new Separator()); return; }
        if (item is not MenuItem original) return;
        var copy = new MenuItem();
        parent.Items.Add(copy);
        bindings.OneWay(original, HeaderedItemsControl.HeaderProperty, copy, HeaderedItemsControl.HeaderProperty);
        bindings.OneWay(original, UIElement.IsEnabledProperty, copy, UIElement.IsEnabledProperty);
        bindings.OneWay(original, UIElement.VisibilityProperty, copy, UIElement.VisibilityProperty);
        bindings.OneWay(original, MenuItem.InputGestureTextProperty, copy, MenuItem.InputGestureTextProperty);
        bindings.OneWay(original, MenuItem.IsCheckableProperty, copy, MenuItem.IsCheckableProperty);
        bindings.TwoWay(original, MenuItem.IsCheckedProperty, copy, MenuItem.IsCheckedProperty);
        RoutedEventHandler click = (_, args) =>
        {
            if (disposed || !original.IsEnabled || original.HasItems) return;
            args.Handled = true;
            original.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, original));
            if (original.Command is RoutedCommand routed)
            {
                var commandTarget = original.CommandTarget ?? original;
                if (routed.CanExecute(original.CommandParameter, commandTarget)) routed.Execute(original.CommandParameter, commandTarget);
            }
            else if (original.Command is { } command && command.CanExecute(original.CommandParameter)) command.Execute(original.CommandParameter);
        };
        copy.Click += click;
        detach.Add(() => copy.Click -= click);
        Watch(original);
        foreach (var child in original.Items) Copy(child, copy);
    }

    private void Release()
    {
        try { bindings.Dispose(); }
        finally
        {
            foreach (var action in detach) action();
            detach.Clear();
            target.Items.Clear();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Release();
    }
}
