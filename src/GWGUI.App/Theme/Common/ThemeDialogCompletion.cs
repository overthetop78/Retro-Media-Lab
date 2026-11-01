using System.Windows;
using System.Windows.Controls;

namespace GWGUI.App.Theme.Common;

/// <summary>Retains the functional dialog result when its visual presentation is owned by a theme.</summary>
internal static class ThemeDialogCompletion
{
    internal sealed record DialogCommands(Button Accept, Button Cancel);
    private static readonly DependencyProperty CommandsProperty = DependencyProperty.RegisterAttached(
        "Commands", typeof(DialogCommands), typeof(ThemeDialogCompletion));
    internal static void SetCommands(Window source, Button accept, Button cancel) => source.SetValue(CommandsProperty, new DialogCommands(accept, cancel));
    internal static DialogCommands CommandButtons(Window source) => source.GetValue(CommandsProperty) as DialogCommands
        ?? throw new InvalidOperationException(nameof(source));

    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
        "Handler", typeof(Action<bool?>), typeof(ThemeDialogCompletion));
    private static readonly DependencyProperty OwnerProperty = DependencyProperty.RegisterAttached(
        "Owner", typeof(Window), typeof(ThemeDialogCompletion));
    private static readonly DependencyProperty ResultProperty = DependencyProperty.RegisterAttached(
        "Result", typeof(bool?), typeof(ThemeDialogCompletion));

    internal static Window? Owner(Window source) => (Window?)source.GetValue(OwnerProperty);

    internal static IDisposable AttachOwner(Window source, Window owner)
    {
        var previous = source.ReadLocalValue(OwnerProperty);
        source.SetValue(OwnerProperty, owner);
        return new Attachment(source, OwnerProperty, previous);
    }

    internal static bool? Result(Window source) => (bool?)source.GetValue(ResultProperty);

    internal static void Complete(Window source, bool? result)
    {
        if (source.GetValue(HandlerProperty) is Action<bool?> complete)
        {
            source.SetValue(ResultProperty, result);
            complete(result);
        }
        else source.DialogResult = result;
    }

    internal static IDisposable Attach(Window source, Action<bool?> complete)
    {
        var previous = source.ReadLocalValue(HandlerProperty);
        source.ClearValue(ResultProperty);
        source.SetValue(HandlerProperty, complete);
        return new Attachment(source, HandlerProperty, previous);
    }

    private sealed class Attachment(Window source, DependencyProperty property, object previous) : IDisposable
    {
        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (previous == DependencyProperty.UnsetValue) source.ClearValue(property);
            else source.SetValue(property, previous);
        }
    }
}
