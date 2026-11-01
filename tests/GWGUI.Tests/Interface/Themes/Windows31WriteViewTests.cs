using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.ViewModels.Main;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.App.Views.Controls.Write;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31WriteViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task IndependentWriteFormUsesExistingDataCommandsAndAvailability() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new WriteTabSection { DataContext = model };
        var status = new ApplicationStatusBar { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var parent = source.SourceBlock.Input.Parent;
        var executeCalls = 0;
        var browseCalls = 0;
        var modifyCalls = 0;
        var advancedCalls = 0;
        var closeCalls = 0;
        source.ExecuteRequested += (_, _) => executeCalls++;
        source.SourceBlock.BrowseButton.Click += (_, _) => browseCalls++;
        source.FormatBlock.ModifyButton.Click += (_, _) => modifyCalls++;
        using var view = new WriteView(source, status, () => advancedCalls++, () => closeCalls++);
        var path = Assert.IsType<TextBox>(view.FindName("SourcePath"));
        Assert.NotSame(source.SourceBlock.Input, path);
        path.Text = @"virtual\archive.scp";
        Assert.Equal(path.Text, model.Write.SourcePath);
        Assert.Same(parent, source.SourceBlock.Input.Parent);
        Menu(view, "OpenCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, browseCalls);
        Menu(view, "WriteCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, executeCalls);
        source.ExecuteActionButton.IsEnabled = false;
        Assert.False(Menu(view, "WriteCommand").IsEnabled);
        Assert.False(Assert.IsType<Button>(view.FindName("Execute")).IsEnabled);
        Menu(view, "WriteCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, executeCalls);
        Menu(view, "ModifyCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, modifyCalls);
        Menu(view, "AdvancedCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Menu(view, "CloseCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, advancedCalls);
        Assert.Equal(1, closeCalls);
        source.FormatBlock.DetectionText.Text = "detected";
        Assert.Equal("detected", Assert.IsType<TextBlock>(view.FindName("Detection")).Text);
        source.AdvancedBlock.NoVerifyCheckBox.IsChecked = true;
        Assert.Equal(true, Assert.IsType<TextBlock>(view.FindName("Verification")).Tag);
    });

    [Fact]
    public Task ClosingWritePresentationUnsubscribesMenusAndKeepsTheOriginalFunctional() => sta.Run(() =>
    {
        var source = new WriteTabSection();
        var status = new ApplicationStatusBar();
        var actions = 0;
        source.ExecuteRequested += (_, _) => actions++;
        var view = new WriteView(source, status, () => actions++, () => actions++);
        var write = Menu(view, "WriteCommand");
        var advanced = Menu(view, "AdvancedCommand");
        var close = Menu(view, "CloseCommand");
        var input = Assert.IsType<TextBox>(view.FindName("SourcePath"));
        try
        {
            source.SourceBlock.Text = "before";
            Assert.Equal("before", input.Text);
        }
        finally { view.Dispose(); }
        source.SourceBlock.Text = "after";
        Assert.Equal("before", input.Text);
        write.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        advanced.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        close.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(0, actions);
        source.ExecuteActionButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(1, actions);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task EveryWriteOptionUsesIndependentControlsAndExistingNotifications() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new WriteAdvancedSection { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var fake = Assert.IsType<CheckBox>(source.FindName(nameof(WriteOptionField.FakeIndexEnabled)));
        var hard = Assert.IsType<CheckBox>(source.FindName(nameof(WriteOptionField.HardSectors)));
        source.FakeIndexChecked += (_, _) => hard.IsChecked = false;
        source.HardSectorsChecked += (_, _) => fake.IsChecked = false;
        var notifications = 0;
        source.InputChanged += (_, _) => notifications++;
        var view = new WriteOptionsView(source);
        TextBox? retries = null;
        try
        {
            foreach (var field in Enum.GetValues<WriteOptionField>())
                Assert.NotSame(source.FindName(field.ToString()), view.Field(field));
            retries = Assert.IsType<TextBox>(view.Field(WriteOptionField.RetriesValue));
            retries.Text = "6";
            Assert.Equal("6", model.Write.Retries.Value);
            Assert.True(notifications > 0);
            var newHard = Assert.IsType<CheckBox>(view.Field(WriteOptionField.HardSectors));
            var newFake = Assert.IsType<CheckBox>(view.Field(WriteOptionField.FakeIndexEnabled));
            newHard.IsChecked = true;
            newFake.IsChecked = true;
            Assert.False(newHard.IsChecked);
            Assert.True(fake.IsChecked);
            var density = Assert.IsType<ComboBox>(view.Field(WriteOptionField.DenselValue));
            var originalDensity = Assert.IsType<ComboBox>(source.FindName(nameof(WriteOptionField.DenselValue)));
            Assert.NotSame(originalDensity.Items[0], density.Items[0]);
            density.SelectedIndex = 1;
            Assert.Equal(1, originalDensity.SelectedIndex);
        }
        finally { view.Dispose(); }
        var originalRetries = Assert.IsType<TextBox>(source.FindName(nameof(WriteOptionField.RetriesValue)));
        originalRetries.Text = "9";
        Assert.Equal("6", retries!.Text);
        retries.Text = "4";
        Assert.Equal("9", originalRetries.Text);
        Assert.Null(view.Content);
    });

    private static MenuItem Menu(WriteView view, string name) => Assert.IsType<MenuItem>(view.FindName(name));
}
