using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.App.Presenters.Input;
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
    internal async Task RefreshLiveStateAsync()
    {
        var device = _selectedDevice;
        if (device is null)
        {
            ClearControls();
            Visualizer.State = null;
            return;
        }
        if (_refreshingState) return;
        _refreshingState = true;
        try
        {
            var state = await Task.Run(() => _source.ReadState(device.Id));
            if (_selectedDevice?.Id != device.Id) return;
            ApplyLiveState(state);
        }
        catch (Exception exception)
        {
            LogOnce(exception, "Reading GameInput controller state");
            DetectionStatus.Text = LocExtension.Get("Controllers.ReadFailed");
        }
        finally { _refreshingState = false; }
    }

    private void RefreshLiveState()
    {
        var device = _selectedDevice;
        if (device is null)
        {
            ClearControls();
            Visualizer.State = null;
            return;
        }
        if (_refreshingState) return;
        _refreshingState = true;
        try
        {
            var state = _source.ReadState(device.Id);
            if (_selectedDevice?.Id != device.Id) return;
            ApplyLiveState(state);
        }
        catch (Exception exception)
        {
            LogOnce(exception, "Reading GameInput controller state");
            DetectionStatus.Text = LocExtension.Get("Controllers.ReadFailed");
        }
        finally { _refreshingState = false; }
    }

    private void ApplyLiveState(GameInputLiveState state)
    {
        _lastState = state;
        Visualizer.State = state;
        _controlRows.Update(state);
        AnalogValuesList.ItemsSource = GameInputDescriptorPresenter.Analog(state);
        if (DetectionStatus.Text == LocExtension.Get("Controllers.ReadFailed")) UpdateDetectionStatus();
        _lastLoggedFailure = null;
    }

    private void RebuildControlLabels(GameInputLiveState state) => _controlRows.RefreshLabels(state);

    private void ClearControls()
    {
        _controlRows.Clear();
        AnalogValuesList.ItemsSource = Array.Empty<ControllerAnalogRow>();
    }

}
