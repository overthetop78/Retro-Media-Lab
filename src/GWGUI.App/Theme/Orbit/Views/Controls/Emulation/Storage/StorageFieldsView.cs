using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.Orbit.Views;

/// <summary>Vertical labelled inputs over the original storage endpoints.</summary>
internal sealed class StorageFieldsView : StackPanel, IDisposable
{
    private readonly List<SettingsFieldView> fields = [];
    private static readonly Thickness FieldMargin = new(0, 0, 0, 16);
    private static readonly Thickness CaptionMargin = new(0, 0, 0, 6);
    private bool disposed;

    internal void AddField(EmulationSettingsField definition, FrameworkElement endpoint, string label, string? displayMemberPath = null)
    {
        var field = new SettingsFieldView(definition, endpoint, () => Task.CompletedTask) { ToolTip = endpoint.ToolTip };
        fields.Add(field);
        if (displayMemberPath is not null && field.Content is ComboBox selection) selection.DisplayMemberPath = displayMemberPath;
        var row = new StackPanel { Margin = FieldMargin };
        row.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = field });
        if (field.Content is CheckBox toggle)
        {
            if (endpoint is not CheckBox { Content: string }) toggle.Content = label;
        }
        else
        {
            var caption = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Margin = CaptionMargin };
            caption.SetResourceReference(TextBlock.ForegroundProperty, OrbitMetrics.MutedBrush);
            row.Children.Add(caption);
        }
        row.Children.Add(field);
        Children.Add(row);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            foreach (var field in fields)
                try { field.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { fields.Clear(); Children.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
