using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GWGUI.App.Theme.Common;

/// <summary>Constructs only new scalar controls; their existing endpoints retain their original parents.</summary>
internal sealed class ThemeBoundFields<TField>(FrameworkElement source, double margin) : IDisposable where TField : struct, Enum
{
    private readonly ThemeBindingScope bindings = new();
    private readonly Dictionary<TField, FrameworkElement> fields = [];
    private bool disposed;

    internal FrameworkElement Field(TField field) => fields[field];

    private T Original<T>(TField field) where T : FrameworkElement =>
        source.FindName(field.ToString()) as T ?? throw new InvalidOperationException(field.ToString());

    internal CheckBox Toggle(TField field)
    {
        var original = Original<CheckBox>(field);
        var target = new CheckBox { Margin = new Thickness(margin), VerticalAlignment = VerticalAlignment.Center };
        bindings.OneWay(original, ContentControl.ContentProperty, target, ContentControl.ContentProperty);
        bindings.TwoWay(original, ToggleButton.IsCheckedProperty, target, ToggleButton.IsCheckedProperty);
        Availability(original, target);
        fields.Add(field, target);
        return target;
    }

    internal FrameworkElement Value(TField field)
    {
        if (source.FindName(field.ToString()) is ComboBox) return Choice(field);
        var original = Original<TextBox>(field);
        var target = new TextBox { Margin = new Thickness(margin), VerticalContentAlignment = VerticalAlignment.Center };
        bindings.TwoWay(original, TextBox.TextProperty, target, TextBox.TextProperty);
        bindings.OneWay(original, TextBoxBase.IsReadOnlyProperty, target, TextBoxBase.IsReadOnlyProperty);
        Availability(original, target);
        fields.Add(field, target);
        return target;
    }

    internal ComboBox Choice(TField field)
    {
        var original = Original<ComboBox>(field);
        var target = new ComboBox { Margin = new Thickness(margin) };
        if (original.ItemsSource is not null)
        {
            bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
            bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        }
        else
            foreach (ComboBoxItem item in original.Items)
            {
                var copy = new ComboBoxItem();
                bindings.OneWay(item, ContentControl.ContentProperty, copy, ContentControl.ContentProperty);
                Availability(item, copy);
                target.Items.Add(copy);
            }
        bindings.TwoWay(original, Selector.SelectedIndexProperty, target, Selector.SelectedIndexProperty);
        Availability(original, target);
        fields.Add(field, target);
        return target;
    }

    internal Button Action(TField field)
    {
        var original = Original<Button>(field);
        var target = new Button { Margin = new Thickness(margin) };
        bindings.Action(original, target, followCaption: true);
        fields.Add(field, target);
        return target;
    }

    internal TextBlock Information(TField field)
    {
        var original = Original<TextBlock>(field);
        var target = new TextBlock { Margin = new Thickness(margin), TextWrapping = TextWrapping.Wrap };
        bindings.OneWay(original, TextBlock.TextProperty, target, TextBlock.TextProperty);
        Availability(original, target);
        fields.Add(field, target);
        return target;
    }

    private void Availability(FrameworkElement original, FrameworkElement target)
    {
        bindings.OneWay(original, UIElement.IsEnabledProperty, target, UIElement.IsEnabledProperty);
        bindings.OneWay(original, UIElement.VisibilityProperty, target, UIElement.VisibilityProperty);
        bindings.OneWay(original, FrameworkElement.ToolTipProperty, target, FrameworkElement.ToolTipProperty);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            foreach (var choice in fields.Values.OfType<ComboBox>())
                if (choice.ItemsSource is not null) choice.ItemsSource = null;
            fields.Clear();
        }
    }
}
