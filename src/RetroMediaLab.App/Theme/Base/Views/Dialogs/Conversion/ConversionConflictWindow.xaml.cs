using RetroMediaLab.App.ViewModels.Conversion;
using RetroMediaLab.App.Theme.Common;
using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Dialogs.Conversion;
public partial class ConversionConflictWindow : Window
{
    public ObservableCollection<ConversionConflictRow> Rows { get; }

    public ConversionConflictWindow(IEnumerable<ConversionOutput> outputs)
    {
        InitializeComponent();
        Rows = new(outputs.Select(x => new ConversionConflictRow(x)));
        DataContext = this;
    }

    internal void RequestApplyAll(ConversionConflictChoice choice)
    {
        foreach (var row in Rows) row.SelectedChoice = row.Choices.Single(option => option.Value == choice);
    }

    internal void RequestContinue() => ThemeDialogCompletion.Complete(this, true);

    private void ApplyAll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && Enum.TryParse<ConversionConflictChoice>(value, out var choice))
            RequestApplyAll(choice);
        DataContext = null;
        DataContext = this;
    }

    private void Continue_Click(object sender, RoutedEventArgs e) => ThemeDialogCompletion.Complete(this, true);
}