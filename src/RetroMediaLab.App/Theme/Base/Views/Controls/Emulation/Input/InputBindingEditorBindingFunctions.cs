using RetroMediaLab.App.ViewModels.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace RetroMediaLab.App.Views.Controls.Emulation.Input;

public partial class InputBindingEditor
{
    private void ClearClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InputBindingRow row }) return;
        row.Binding = string.Empty;
        ValidateBindings();
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreDefaultsClicked(object sender, RoutedEventArgs e)
    {
        _bindings.RestoreDefaults();
        ValidateBindings();
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearConflictsClicked(object sender, RoutedEventArgs e)
    {
        _bindings.ClearConflicts();
        ValidateBindings();
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SearchChanged(object sender, TextChangedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(_bindings.Rows);
        var query = SearchBox.Text.Trim();
        view.Filter = item => item is InputBindingRow row && (query.Length == 0 || row.Label.Contains(query, StringComparison.CurrentCultureIgnoreCase) || row.Binding.Contains(query, StringComparison.CurrentCultureIgnoreCase));
    }

    private void ValidateBindings()
    {
        _bindings.Validate();
        BindingsList.Items.Refresh();
        RefreshAnalogSelections();
    }
}
