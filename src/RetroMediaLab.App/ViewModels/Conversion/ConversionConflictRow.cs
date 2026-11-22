using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.MediaEngine.Images.Conversion;
using System.ComponentModel;
using System.IO;

namespace RetroMediaLab.App.ViewModels.Conversion;

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
