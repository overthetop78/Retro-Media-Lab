using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31SettingsFieldsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task NumberAndIpv4ReuseTheirConstraintsAndCommitWithoutChangingTheOriginalParent() => sta.Run(() =>
    {
        var field = Field(EmulationSettingsEditor.Number, "2") with { NumericRange = new(1, 254, 1) };
        var original = EmulationSettingsFieldControlFunctions.CreateNumber(field);
        var owner = new Border { Child = original };
        var commits = 0;
        using var view = new SettingsFieldView(field, original, () => { commits++; return Task.CompletedTask; });
        var number = Assert.IsType<ConstrainedTextBox>(view.Content);
        Assert.NotSame(original, number);
        number.Text = "999";
        Assert.Equal("2", number.Text);
        number.Text = "35";
        Assert.Equal("35", original.CommittedValue);
        number.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, number, owner)
            { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
        Assert.Equal(1, commits);
        Assert.Same(owner, original.Parent);
        var ip = EmulationSettingsFieldControlFunctions.CreateIpv4("127.0.0.1");
        using var ipView = new SettingsFieldView(Field(EmulationSettingsEditor.IPv4, ip.Text), ip, () => Task.CompletedTask);
        var address = Assert.IsType<ConstrainedTextBox>(ipView.Content);
        address.Text = "300.1.2.3";
        Assert.Equal("127.0.0.1", address.Text);
        address.Text = "192.168.0.5";
        Assert.Equal("192.168.0.5", ip.CommittedValue);
        view.Dispose();
        original.Text = "40";
        Assert.Equal("35", number.Text);
    });

    [Fact]
    public Task ChoiceRadioAndToggleKeepTheOriginalActionsAndEffectiveStates() => sta.Run(() =>
    {
        var choices = new[]
        {
            new RetroMediaLab.App.Contracts.Emulation.Settings.EmulationSettingsChoiceView(new("a", "", "Alpha"), "Alpha"),
            new RetroMediaLab.App.Contracts.Emulation.Settings.EmulationSettingsChoiceView(new("b", "", "Beta"), "Beta"),
            new RetroMediaLab.App.Contracts.Emulation.Settings.EmulationSettingsChoiceView(new("c", "", "Gamma"), "Gamma")
        };
        var field = Field(EmulationSettingsEditor.Selection, "a");
        var combo = Assert.IsType<ComboBox>(EmulationSettingsFieldControlFunctions.CreateSelection(field, choices));
        var changed = 0;
        combo.SelectionChanged += (_, _) => changed++;
        using var view = new SettingsFieldView(field, combo, () => Task.CompletedTask);
        var selected = Assert.IsType<ComboBox>(view.Content);
        selected.SelectedIndex = 2;
        Assert.Same(choices[2], combo.SelectedItem);
        Assert.Equal(1, changed);
        var radios = Assert.IsType<WrapPanel>(EmulationSettingsFieldControlFunctions.CreateSelection(field with { UseRadioButtons = true }, choices));
        using var radioView = new SettingsFieldView(field, radios, () => Task.CompletedTask);
        var newRadios = Assert.IsType<WrapPanel>(radioView.Content).Children.OfType<RadioButton>().ToArray();
        newRadios[1].IsChecked = true;
        Assert.Equal("b", EmulationSettingsFieldControlFunctions.Read(radios));
        Assert.NotEqual(((RadioButton)radios.Children[0]).GroupName, newRadios[0].GroupName);
        var toggleField = Field(EmulationSettingsEditor.Toggle, "off") with { EnabledValue = "on", DisabledValue = "off" };
        var toggle = EmulationSettingsFieldControlFunctions.CreateToggle(toggleField);
        using var toggleView = new SettingsFieldView(toggleField, toggle, () => Task.CompletedTask);
        ((CheckBox)toggleView.Content).IsChecked = true;
        Assert.Equal("on", EmulationSettingsFieldControlFunctions.Read(toggle));
        toggle.IsEnabled = false;
        Assert.False(toggleView.IsEnabled);
        combo.Visibility = Visibility.Collapsed;
        Assert.Equal(Visibility.Collapsed, view.Visibility);
        view.Dispose();
        combo.SelectedIndex = 0;
        Assert.Same(choices[2], selected.SelectedItem);
    });

    [Fact]
    public Task SlidersKeepUnitsAndAutomaticModeAndPersistOnlyOncePerValue() => sta.Run(() =>
    {
        var field = Field(EmulationSettingsEditor.Slider, "50") with { NumericRange = new(0, 100, 1, " %", true) };
        var original = Assert.IsType<Grid>(EmulationSettingsFieldControlFunctions.CreateSlider(field, choice => choice.Id));
        var commits = 0;
        using var view = new SettingsFieldView(field, original, () => { commits++; return Task.CompletedTask; });
        var body = Assert.IsType<StackPanel>(view.Content);
        var slider = body.Children.OfType<Slider>().Single();
        Assert.Equal(Orientation.Vertical, slider.Orientation);
        slider.Value = 75;
        Assert.Equal(75, ((Slider)original.Tag).Value);
        Assert.Contains("75", body.Children.OfType<TextBlock>().Single().Text);
        Assert.Contains("%", body.Children.OfType<TextBlock>().Single().Text);
        slider.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        slider.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.Equal(1, commits);
        var automaticField = field with { Value = "auto", AutomaticValue = "auto" };
        var automatic = Assert.IsType<StackPanel>(EmulationSettingsFieldControlFunctions.CreateSlider(automaticField, choice => choice.Id));
        using var automaticView = new SettingsFieldView(automaticField, automatic, () => Task.CompletedTask);
        var autoBody = Assert.IsType<StackPanel>(automaticView.Content);
        var originalToggle = automatic.Children.OfType<CheckBox>().Single();
        var autoSlider = ((StackPanel)autoBody.Children[1]).Children.OfType<Slider>().Single();
        Assert.False(autoSlider.IsEnabled);
        autoBody.Children.OfType<CheckBox>().Single().IsChecked = false;
        Assert.False(originalToggle.IsChecked);
        Assert.True(autoSlider.IsEnabled);
        view.Dispose();
        ((Slider)original.Tag).Value = 90;
        Assert.Equal(75, slider.Value);
    });

    [Fact]
    public Task PathsFirmwareNamesInformationAndButtonsUseIndependentControlsAndOriginalActions() => sta.Run(() =>
    {
        var path = new TextBox { Text = "virtual.rom" };
        var browse = new Button { Content = "browse" };
        var actions = 0;
        browse.Click += (_, _) => { actions++; path.Text = "selected.rom"; };
        var original = new Grid { Tag = path };
        original.Children.Add(path);
        original.Children.Add(browse);
        using var view = new SettingsFieldView(Field(EmulationSettingsEditor.Path, path.Text), original, () => Task.CompletedTask);
        var body = Assert.IsType<DockPanel>(view.Content);
        var ownPath = body.Children.OfType<TextBox>().Single();
        Assert.NotSame(path, ownPath);
        body.Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, actions);
        Assert.Equal("selected.rom", ownPath.Text);
        Assert.Same(original, path.Parent);
        var name = new TextBlock { Text = "System ROM", ToolTip = "system.rom" };
        var open = new Button { Content = "open" };
        var firmware = new Grid { Tag = (Func<string>)(() => "system.rom") };
        firmware.Children.Add(name);
        firmware.Children.Add(open);
        using var firmwareView = new SettingsFieldView(Field(EmulationSettingsEditor.Path, "system.rom"), firmware, () => Task.CompletedTask);
        var displayed = ((DockPanel)firmwareView.Content).Children.OfType<TextBlock>().Single();
        name.Text = "Updated system ROM";
        Assert.Equal(name.Text, displayed.Text);
        using var information = new SettingsFieldView(Field(EmulationSettingsEditor.Information, name.Text), name, () => Task.CompletedTask);
        Assert.NotSame(name, information.Content);
        var directory = new Button { Content = "directory" };
        directory.Click += (_, _) => actions++;
        using var directoryView = new SettingsFieldView(Field(EmulationSettingsEditor.OpenDirectory, "virtual"), directory, () => Task.CompletedTask);
        ((Button)directoryView.Content).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(2, actions);
        directoryView.Dispose();
        directory.Content = "later";
        Assert.Null(directoryView.Content);
    });

    private static EmulationSettingsField Field(EmulationSettingsEditor editor, string value)
        => new("field", EmulationMachineTab.General, "block", "Emulation.Machine.Model", editor, value);
}
