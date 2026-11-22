using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Services.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RetroMediaLab.App.Views.Controls.Options;

public partial class OptionsControllersSection
{
    private async void RumbleMotor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string value } ||
            !Enum.TryParse<GameInputRumbleMotors>(value, out var motor)) return;
        await TestRumbleAsync(motor);
    }

    private void RumbleStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RumbleStrengthText is not null)
            RumbleStrengthText.Text = $"{Math.Round(e.NewValue):0} %";
    }

    internal async Task TestRumbleAsync(GameInputRumbleMotors motor)
    {
        var device = _selectedDevice;
        if (device is null || !GameInputRumbleTest.Supports(device, motor)) return;

        _testingRumble = true;
        UpdateRumbleControls();
        TestStatusText.Text = LocExtension.Get("Controllers.TestRunning");
        try
        {
            var completed = await GameInputRumbleTest.PulseAsync(
                _source, device.Id, motor, RumbleStrengthSlider.Value, _delay, ReportRumbleStopFailure);
            TestStatusText.Text = LocExtension.Get(completed ? "Controllers.TestCompleted" : "Controllers.TestFailed");
        }
        catch (Exception exception)
        {
            LogOnce(exception, "Testing GameInput rumble");
            TestStatusText.Text = LocExtension.Get("Controllers.TestFailed");
        }
        finally
        {
            _testingRumble = false;
            UpdateRumbleControls();
        }
    }

    private void StopRumble()
    {
        var device = _selectedDevice;
        if (device is null || device.RumbleMotors == GameInputRumbleMotors.None) return;
        GameInputRumbleTest.Stop(_source, device.Id, ReportRumbleStopFailure);
    }

    private void ReportRumbleStopFailure(Exception exception) => LogOnce(exception, "Stopping GameInput rumble");

    private void UpdateRumbleControls()
    {
        var motors = _selectedDevice?.RumbleMotors ?? GameInputRumbleMotors.None;
        RumblePanel.Visibility = motors == GameInputRumbleMotors.None ? Visibility.Collapsed : Visibility.Visible;
        var enabled = !_testingRumble;
        LowFrequencyRumbleButton.IsEnabled = enabled && motors.HasFlag(GameInputRumbleMotors.LowFrequency);
        HighFrequencyRumbleButton.IsEnabled = enabled && motors.HasFlag(GameInputRumbleMotors.HighFrequency);
        LeftTriggerRumbleButton.IsEnabled = enabled && motors.HasFlag(GameInputRumbleMotors.LeftTrigger);
        RightTriggerRumbleButton.IsEnabled = enabled && motors.HasFlag(GameInputRumbleMotors.RightTrigger);
        RumbleStrengthSlider.IsEnabled = enabled && motors != GameInputRumbleMotors.None;
    }

    private void LogOnce(Exception exception, string context)
    {
        var signature = $"{context}|{exception.GetType().FullName}|{exception.HResult:X8}";
        if (string.Equals(signature, _lastLoggedFailure, StringComparison.Ordinal)) return;
        _lastLoggedFailure = signature;
        _errorLogger(exception, context);
    }

}
