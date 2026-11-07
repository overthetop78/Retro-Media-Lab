using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

internal static class TapeTransportPresentationScenarios
{
    internal static void CommandsReflectCapabilitiesAndTransportState()
    {
        Assert.Equal(6, Enum.GetValues<EmulationTapeCommand>().Length);

        Assert.False(Enabled(powered: true, supported: false, EmulationTapeState.Stopped, null, EmulationTapeCommand.Play));
        Assert.False(Enabled(powered: true, supported: true, EmulationTapeState.Empty, null, EmulationTapeCommand.Play));
        Assert.False(Enabled(powered: false, supported: true, EmulationTapeState.Stopped, null, EmulationTapeCommand.Play));
        Assert.True(Enabled(powered: true, supported: true, EmulationTapeState.Stopped, null, EmulationTapeCommand.Play));

        Assert.True(TapeTransportPresentationFunctions.IsActive(EmulationTapeState.Playing, EmulationTapeCommand.Play, EmulationTapeCommand.Play));
        Assert.False(Enabled(powered: true, supported: true, EmulationTapeState.Playing, EmulationTapeCommand.Play, EmulationTapeCommand.Play));

        Assert.True(TapeTransportPresentationFunctions.IsActive(EmulationTapeState.Recording, EmulationTapeCommand.Record, EmulationTapeCommand.Record));
        Assert.False(Enabled(powered: true, supported: true, EmulationTapeState.Recording, EmulationTapeCommand.Record, EmulationTapeCommand.Record));

        Assert.True(TapeTransportPresentationFunctions.IsBlinking(EmulationTapeState.Paused, EmulationTapeCommand.Play, EmulationTapeCommand.Play));
        Assert.True(TapeTransportPresentationFunctions.IsBlinking(EmulationTapeState.Paused, EmulationTapeCommand.Record, EmulationTapeCommand.Record));
        Assert.False(TapeTransportPresentationFunctions.IsBlinking(EmulationTapeState.Paused, EmulationTapeCommand.Play, EmulationTapeCommand.Record));
    }

    internal static void PanelContainsASeparateRowWithEveryCommand()
    {
        Assert.Equal(
        [
            EmulationTapeCommand.Record,
            EmulationTapeCommand.Play,
            EmulationTapeCommand.Rewind,
            EmulationTapeCommand.FastForward,
            EmulationTapeCommand.Stop,
            EmulationTapeCommand.Pause
        ], MachineController.TapeCommandOrder);

        var commands = MachineController.TapeCommandOrder
            .Select(command => new MachineViewDeviceCommand(command, command.ToString(), command.ToString(),
                command is EmulationTapeCommand.Play or EmulationTapeCommand.Record,
                command is EmulationTapeCommand.Play or EmulationTapeCommand.Record, false, false,
                () => Task.CompletedTask))
            .ToArray();
        var view = new MachineView();
        view.SetDevices(
            [new MachineViewDevice("Tape0", "TAPE0:", "T", true, true, () => Task.CompletedTask, () => Task.CompletedTask, "Stopped", commands)],
            _ => { }, () => { });

        var deviceBorder = Assert.IsType<Border>(Assert.Single(view.DeviceStrip.Children));
        var deviceRows = Assert.IsType<StackPanel>(deviceBorder.Child);
        Assert.Equal(Orientation.Vertical, deviceRows.Orientation);
        Assert.Equal(2, deviceRows.Children.Count);
        var commandGroup = Assert.IsType<Border>(deviceRows.Children[1]);
        Assert.Equal(new CornerRadius(6), commandGroup.CornerRadius);
        var commandRow = Assert.IsType<StackPanel>(commandGroup.Child);
        Assert.Equal(Orientation.Horizontal, commandRow.Orientation);
        var buttons = commandRow.Children.Cast<Button>().ToArray();
        Assert.Equal(6, buttons.Length);
        Assert.Equal(MachineController.TapeCommandOrder, buttons.Select(button => Enum.Parse<EmulationTapeCommand>((string)button.ToolTip)).ToArray());
        Assert.True(buttons.Single(button => Equals(button.ToolTip, EmulationTapeCommand.Play.ToString())).IsEnabled);
        Assert.True(buttons.Single(button => Equals(button.ToolTip, EmulationTapeCommand.Record.ToString())).IsEnabled);
        Assert.All(buttons.Where(button => !Equals(button.ToolTip, EmulationTapeCommand.Play.ToString()) && !Equals(button.ToolTip, EmulationTapeCommand.Record.ToString())), button => Assert.False(button.IsEnabled));
        var play = buttons.Single(button => Equals(button.ToolTip, EmulationTapeCommand.Play.ToString()));
        var record = buttons.Single(button => Equals(button.ToolTip, EmulationTapeCommand.Record.ToString()));
        Assert.Equal(Color.FromRgb(22, 130, 59), ((SolidColorBrush)play.Foreground).Color);
        Assert.Equal(Color.FromRgb(180, 35, 24), ((SolidColorBrush)record.Foreground).Color);
        Assert.Equal(play.Foreground, Assert.IsType<TextBlock>(Assert.IsType<Border>(play.Content).Child).Foreground);
        Assert.Equal(record.Foreground, Assert.IsType<TextBlock>(Assert.IsType<Border>(record.Content).Child).Foreground);
        Assert.Equal(Brushes.Gray, buttons.Single(button => Equals(button.ToolTip, EmulationTapeCommand.Stop.ToString())).Foreground);

        view.SetTapeTransport("Tape0", EmulationTapeState.Playing, EmulationTapeCommand.Play, new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Play }, powered: true);
        Assert.Equal(Color.FromRgb(0, 200, 83), ((SolidColorBrush)play.Foreground).Color);
        Assert.False(play.IsEnabled);
        Assert.Equal(new Thickness(2), Assert.IsType<Border>(play.Content).BorderThickness);
    }

    internal static void DeviceHeaderKeepsEjectSpaceAndCentersLedWhenEmpty()
    {
        var view = new MachineView();
        view.SetDevices(
            [new MachineViewDevice("Cartridge0", "CART0:", "C", true, false, () => Task.CompletedTask, null)], _ => { }, () => { });

        var deviceBorder = Assert.IsType<Border>(Assert.Single(view.DeviceStrip.Children));
        var rows = Assert.IsType<StackPanel>(deviceBorder.Child);
        var header = Assert.IsType<StackPanel>(Assert.Single(rows.Children));
        var ledHost = Assert.IsType<Grid>(header.Children[0]);
        var led = Assert.IsType<System.Windows.Shapes.Ellipse>(Assert.Single(ledHost.Children));
        Assert.Equal(HorizontalAlignment.Center, led.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, led.VerticalAlignment);
        Assert.Equal(18, ledHost.Width);

        var eject = Assert.IsType<Button>(header.Children[header.Children.Count - 1]);
        Assert.Equal(22, eject.Width);
        Assert.Equal(Visibility.Hidden, eject.Visibility);
        Assert.False(eject.IsEnabled);
    }

    private static bool Enabled(bool powered, bool supported, EmulationTapeState state, EmulationTapeCommand? activeOperation, EmulationTapeCommand command) => TapeTransportPresentationFunctions.IsEnabled(powered, supported, state, activeOperation, command);
}
