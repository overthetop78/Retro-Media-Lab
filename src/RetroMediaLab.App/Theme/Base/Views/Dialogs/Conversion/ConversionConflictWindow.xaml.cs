using RetroMediaLab.App.Theme.Common;
using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.App.Localization.Extensions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.ComponentModel;

namespace RetroMediaLab.App.Views.Dialogs.Conversion;
public enum ConversionConflictChoice
{
    Overwrite,
    Skip,
    Number
}

public sealed record ConversionConflictOption(ConversionConflictChoice Value, string Label);
public sealed class ConversionConflictRow : INotifyPropertyChanged
{
    public ConversionOutput Output { get; }
    public string FileName => Path.GetFileName(Output.OutputPath);
    public IReadOnlyList<ConversionConflictOption> Choices { get; } = [
        new(ConversionConflictChoice.Overwrite, LocExtension.Get("Conflict.Overwrite")),
        new(ConversionConflictChoice.Skip, LocExtension.Get("Conflict.Skip")),
        new(ConversionConflictChoice.Number, LocExtension.Get("Conflict.Number"))
    ];
    private ConversionConflictOption selectedChoice = null!;
    public ConversionConflictOption SelectedChoice
    {
        get => selectedChoice;
        set
        {
            if (Equals(selectedChoice, value)) return;
            selectedChoice = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChoice)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Choice)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ConversionConflictChoice Choice => SelectedChoice.Value;

    public ConversionConflictRow(ConversionOutput output)
    {
        Output = output;
        SelectedChoice = Choices[2];
    }
}

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