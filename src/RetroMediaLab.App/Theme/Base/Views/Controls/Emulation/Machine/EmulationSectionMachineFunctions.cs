using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Views.Windows.EmulationModuleOptions;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;
using System.Runtime.ExceptionServices;


namespace RetroMediaLab.App.Views.Controls.Emulation.Machine;

public sealed partial class EmulationSection
{
    private async Task OpenMachineAsync(EmulationConfigurationListItem selected)
    {
        MachineController? view = null;
        var opened = await MachineController.OpenAsync(selected, _settings, () => new MachineView(),
            () => ReferenceEquals(_machines.SelectedContent, view),
            error => ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationOpening, selected.DisplayName),
            ShowSettings);
        view = opened.Controller;
        bool added = false;
        try
        {
            await AddMachineAsync(selected, opened.Runtime, view, view.StopAsync);
            added = true;
        }
        finally { if (!added) await view.StopAsync(); }

        void ShowSettings(IEmulationConfiguration configuration, IReadOnlyList<string> reservedBindings)
        {
            var dialog = new EmulationModuleOptionsWindow(selected.Module, initialConfiguration: configuration, reservedBindings: reservedBindings);
            try { WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), dialog); }
            finally { WpfWindowNavigationService.CloseOwnedDialog(dialog); }
        }
    }

    internal Task AddMachineAsync(EmulationConfigurationListItem selected, EmulationMachineRuntime runtime, FrameworkElement view, Func<Task> stop)
    {
        var key = (selected.Module.Id, selected.Configuration.Id);
        var tab = new TabItem { Content = view, Tag = runtime, Padding = new Thickness(18, 9, 14, 9) };
        tab.SetResourceReference(StyleProperty, ControlVisualConstants.MainTabItemStyleResource);
        tab.Header = CreateMachineTabHeader(MachineTitle(selected), selected.DisplayName, () => CloseMachineAsync(key, tab, stop));
        _openMachines.Add(key, tab);
        _machines.Items.Add(tab);
        _machines.SelectedItem = tab;
        PresentationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    internal async Task CloseMachineAsync((string ModuleId, Guid Id) key, TabItem tab, Func<Task> stop)
    {
        if (!_openMachines.ContainsKey(key)) return;
        var stopped = false;
        try { await stop(); stopped = true; }
        finally
        {
            if (stopped || tab.Content is MachineController { IsStopped: true })
            {
                _openMachines.Remove(key);
                _machines.Items.Remove(tab);
                RefreshSelector();
            }
        }
    }

    public async Task StopAllAsync()
    {
        Exception? failure = null;
        var tabs = _openMachines.Values.ToArray();
        foreach (var tab in tabs)
        {
            try { if (tab.Content is MachineController machine) await machine.StopAsync(); }
            catch (Exception error) { failure ??= error; }
        }
        _openMachines.Clear();
        foreach (var tab in tabs) _machines.Items.Remove(tab);
        RefreshSelector();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
