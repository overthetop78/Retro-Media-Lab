using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.ViewModels.Main;
using GWGUI.App.Views.Controls.Read;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31AcquisitionOptionsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task NewFieldsKeepModelNotificationsAndOriginalControls() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ReadAdvancedSection { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var notifications = 0;
        source.InputChanged += (_, _) => notifications++;
        var originalValue = Assert.IsType<TextBox>(source.FindName("RetriesValue"));
        var parent = originalValue.Parent;
        using var view = new AcquisitionOptionsView(source);
        var value = Assert.IsType<TextBox>(view.Field(AcquisitionOptionField.RetriesValue));
        Assert.NotSame(originalValue, value);
        value.Text = "7";
        Assert.Equal("7", originalValue.Text);
        Assert.Equal("7", model.Read.Retries.Value);
        Assert.True(notifications > 0);
        Assert.Same(parent, originalValue.Parent);
        foreach (var field in Enum.GetValues<AcquisitionOptionField>().Where(field => field is not AcquisitionOptionField.RevsEnabled and not AcquisitionOptionField.RevsValue))
            Assert.NotSame(source.FindName(field.ToString()), view.Field(field));
    });

    [Fact]
    public Task ChoiceElementsRemainIndependentAndFollowTheirTranslatedCaptions() => sta.Run(() =>
    {
        var source = new ReadAdvancedSection();
        using var view = new AcquisitionOptionsView(source);
        var original = source.SequenceKindComboBox;
        var target = Assert.IsType<ComboBox>(view.Field(AcquisitionOptionField.SequenceKind));
        var changes = 0;
        source.SequenceKindChanged += (_, _) => changes++;
        Assert.Equal(original.Items.Count, target.Items.Count);
        for (var index = 0; index < original.Items.Count; index++)
            Assert.NotSame(original.Items[index], target.Items[index]);
        var caption = Assert.IsType<ComboBoxItem>(original.Items[0]);
        var targetCaption = Assert.IsType<ComboBoxItem>(target.Items[0]);
        caption.Content = "translated";
        Assert.Equal("translated", targetCaption.Content);
        target.SelectedIndex = 1;
        Assert.Equal(1, original.SelectedIndex);
        Assert.True(changes > 0);
        original.IsEnabled = false;
        Assert.False(target.IsEnabled);
    });

    [Fact]
    public Task ExistingExclusiveOptionHandlersStillRunAndDisposalDetachesEverything() => sta.Run(() =>
    {
        var source = new ReadAdvancedSection();
        var fake = Assert.IsType<CheckBox>(source.FindName("FakeIndexEnabled"));
        var hard = Assert.IsType<CheckBox>(source.FindName("HardSectors"));
        source.FakeIndexChecked += (_, _) => hard.IsChecked = false;
        source.HardSectorsChecked += (_, _) => fake.IsChecked = false;
        var view = new AcquisitionOptionsView(source);
        CheckBox? newFake = null;
        TextBox? newValue = null;
        try
        {
            newFake = Assert.IsType<CheckBox>(view.Field(AcquisitionOptionField.FakeIndexEnabled));
            var newHard = Assert.IsType<CheckBox>(view.Field(AcquisitionOptionField.HardSectors));
            newHard.IsChecked = true;
            newFake.IsChecked = true;
            Assert.False(newHard.IsChecked);
            Assert.True(fake.IsChecked);
            newValue = Assert.IsType<TextBox>(view.Field(AcquisitionOptionField.FakeIndexValue));
            newValue.Text = "300";
        }
        finally { view.Dispose(); }
        fake.IsChecked = false;
        Assert.True(newFake!.IsChecked);
        Assert.Null(view.Content);
        var originalValue = Assert.IsType<TextBox>(source.FindName("FakeIndexValue"));
        originalValue.Text = "360";
        Assert.Equal("300", newValue!.Text);
        newValue.Text = "400";
        Assert.Equal("360", originalValue.Text);
        view.Dispose();
    });
}
