using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Contracts.Theming;

namespace RetroMediaLab.App.Theme.Common;

/// <summary>A new presentation owns this host, but only borrows its functional content.</summary>
internal sealed class ThemeElementHost : ContentControl
{
    private Action<DependencyProperty, object?>? setPresentationValue;

    internal ThemeElementSlot Slot { get; }

    internal ThemeElementHost(ThemeElementSlot slot, FrameworkElement originalParent,
        Action<DependencyProperty, object?> setPresentationValue)
    {
        Slot = slot;
        this.setPresentationValue = setPresentationValue;
        SetBinding(DataContextProperty, new Binding(nameof(DataContext)) { Source = originalParent });
        SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = originalParent });
        if (slot.VisibilitySource is { } source)
            SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = source });
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        Content = slot.Element;
    }

    internal void SetPresentationValue(DependencyProperty property, object? value)
    {
        ObjectDisposedException.ThrowIf(setPresentationValue is null, this);
        setPresentationValue(property, value);
    }

    internal void Detach()
    {
        try { Content = null; }
        finally
        {
            BindingOperations.ClearAllBindings(this);
            setPresentationValue = null;
        }
    }
}
