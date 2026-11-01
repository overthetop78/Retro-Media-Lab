using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Independent compact rows over the storage dialog's existing functional controls.</summary>
internal sealed class StorageFieldsView : StackPanel, IDisposable
{
    private const double LabelWidth = 160;
    private readonly List<SettingsFieldView> fields = [];

    internal void AddField(EmulationSettingsField definition, FrameworkElement endpoint, string label, string? displayMemberPath = null)
    {
        var field = new SettingsFieldView(definition, endpoint, () => Task.CompletedTask) { ToolTip = endpoint.ToolTip };
        fields.Add(field);
        if (displayMemberPath is not null && field.Content is ComboBox selection)
            selection.DisplayMemberPath = displayMemberPath;
        var row = new Grid { Margin = new Thickness(8, 4, 8, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = field });
        if (field.Content is CheckBox toggle)
        {
            if (endpoint is not CheckBox { Content: string }) toggle.Content = label;
            Grid.SetColumnSpan(field, 2);
        }
        else
        {
            row.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            Grid.SetColumn(field, 1);
        }
        row.Children.Add(field);
        Children.Add(row);
    }

    public void Dispose()
    {
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
