using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace GWGUI.App.Theme.Common;

/// <summary>Connects independent controls to the existing functional endpoints, without moving their visuals.</summary>
internal sealed class ThemeBindingScope : IDisposable
{
    private readonly List<Action> detach = [];
    private int sourceTransferDepth;
    private bool disposed;

    internal void OneWay(DependencyObject source, DependencyProperty sourceProperty,
        DependencyObject target, DependencyProperty targetProperty) => Bind(source, sourceProperty, target, targetProperty, false);

    internal void TwoWay(DependencyObject source, DependencyProperty sourceProperty,
        DependencyObject target, DependencyProperty targetProperty) => Bind(source, sourceProperty, target, targetProperty, true);

    private void Bind(DependencyObject source, DependencyProperty sourceProperty,
        DependencyObject target, DependencyProperty targetProperty, bool twoWay)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var sourceDescriptor = DependencyPropertyDescriptor.FromProperty(sourceProperty, source.GetType())
            ?? throw new ArgumentException(nameof(sourceProperty));
        var targetDescriptor = twoWay ? DependencyPropertyDescriptor.FromProperty(targetProperty, target.GetType())
            ?? throw new ArgumentException(nameof(targetProperty)) : null;
        EventHandler updateTarget = (_, _) => CopyToTarget();
        EventHandler updateSource = (_, _) =>
        {
            if (disposed || sourceTransferDepth != 0) return;
            var value = target.GetValue(targetProperty);
            if (Equals(value, source.GetValue(sourceProperty))) return;
            try
            {
                sourceTransferDepth++;
                source.SetCurrentValue(sourceProperty, value);
                if (BindingOperations.GetBindingExpression(source, sourceProperty) is { } binding) binding.UpdateSource();
                else if (BindingOperations.GetMultiBindingExpression(source, sourceProperty) is { } multiBinding) multiBinding.UpdateSource();
            }
            finally
            {
                sourceTransferDepth--;
                if (!disposed) CopyToTarget();
            }
        };

        try
        {
            sourceDescriptor.AddValueChanged(source, updateTarget);
            targetDescriptor?.AddValueChanged(target, updateSource);
            detach.Add(() =>
            {
                try { sourceDescriptor.RemoveValueChanged(source, updateTarget); }
                finally { targetDescriptor?.RemoveValueChanged(target, updateSource); }
            });
            CopyToTarget();
        }
        catch
        {
            try { sourceDescriptor.RemoveValueChanged(source, updateTarget); }
            finally { targetDescriptor?.RemoveValueChanged(target, updateSource); }
            throw;
        }

        void CopyToTarget()
        {
            if (disposed) return;
            try
            {
                sourceTransferDepth++;
                var value = source.GetValue(sourceProperty);
                if ((targetProperty == ContentControl.ContentProperty || targetProperty == HeaderedItemsControl.HeaderProperty)
                    && value is DependencyObject)
                    throw new ArgumentException(nameof(source));
                target.SetCurrentValue(targetProperty, value);
            }
            finally { sourceTransferDepth--; }
        }
    }

    internal void Action(ButtonBase source, ButtonBase target, bool followCaption = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (followCaption && source.Content is DependencyObject) throw new ArgumentException(nameof(followCaption));
        OneWay(source, UIElement.IsEnabledProperty, target, UIElement.IsEnabledProperty);
        OneWay(source, UIElement.VisibilityProperty, target, UIElement.VisibilityProperty);
        if (followCaption)
        {
            OneWay(source, ContentControl.ContentProperty, target, ContentControl.ContentProperty);
        }
        RoutedEventHandler invoke = (_, _) =>
        {
            Invoke(source);
        };
        target.Click += invoke;
        detach.Add(() => target.Click -= invoke);
    }

    internal void Action(ButtonBase source, MenuItem target, bool followCaption = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (followCaption && source.Content is DependencyObject) throw new ArgumentException(nameof(followCaption));
        OneWay(source, UIElement.IsEnabledProperty, target, UIElement.IsEnabledProperty);
        OneWay(source, UIElement.VisibilityProperty, target, UIElement.VisibilityProperty);
        if (followCaption) OneWay(source, ContentControl.ContentProperty, target, HeaderedItemsControl.HeaderProperty);
        RoutedEventHandler invoke = (_, args) => { Invoke(source); args.Handled = true; };
        target.Click += invoke;
        detach.Add(() => target.Click -= invoke);
    }

    private void Invoke(ButtonBase source)
    {
        if (disposed || !source.IsEnabled) return;
        source.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, source));
        if (source.Command is RoutedCommand routed)
        {
            var commandTarget = source.CommandTarget ?? source;
            if (routed.CanExecute(source.CommandParameter, commandTarget)) routed.Execute(source.CommandParameter, commandTarget);
        }
        else if (source.Command is { } command && command.CanExecute(source.CommandParameter)) command.Execute(source.CommandParameter);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            for (var index = detach.Count - 1; index >= 0; index--)
                try { detach[index](); }
                catch (Exception error) { failure ??= error; }
        }
        finally { detach.Clear(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
