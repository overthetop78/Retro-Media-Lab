using RetroMediaLab.App.Views.Dialogs.Conversion;
using RetroMediaLab.MediaEngine.Images.Conversion;

namespace RetroMediaLab.Tests.Interface.Themes;

public sealed class Windows31ConversionConflictRowsTests
{
    [Fact]
    public void ChoiceChangesNotifyEveryPresentationAndRetainTheActualOutput()
    {
        var output = new ConversionOutput("ibm.720", ".ima", "virtual/disk.ima", false);
        var row = new ConversionConflictRow(output);
        Assert.Same(output, row.Output);
        Assert.Equal(ConversionConflictChoice.Number, row.Choice);
        var changes = new List<string?>();
        row.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        foreach (var option in row.Choices)
        {
            if (ReferenceEquals(row.SelectedChoice, option)) continue;
            changes.Clear();
            row.SelectedChoice = option;
            Assert.Equal(option.Value, row.Choice);
            Assert.Equal(new[] { nameof(row.SelectedChoice), nameof(row.Choice) }, changes);
            row.SelectedChoice = option;
            Assert.Equal(2, changes.Count);
        }
    }
}
