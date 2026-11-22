using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Controllers.Emulation.Machine;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Presenters.Common;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;


namespace RetroMediaLab.App.Views.Controls.Emulation.Machine;

public sealed partial class EmulationSection
{
    private async void VideoConfigurationChanged(object? sender, EmulationConfigurationSavedEventArgs args) =>
        await ApplyToOpenMachineAsync(args.Configuration, videoOnly: true);

    private async void ConfigurationSaved(object? sender, EmulationConfigurationSavedEventArgs args)
    {
        await ReloadConfigurationsAsync();
        await ApplyToOpenMachineAsync(args.Configuration, videoOnly: false);
    }

    private Task ApplyToOpenMachineAsync(IEmulationConfiguration configuration, bool videoOnly) =>
        _openMachines.TryGetValue((configuration.ModuleId, configuration.Id), out var tab)
            && tab.Content is MachineController controller
            ? EmulationOpenMachineConfigurationFunctions.ApplyAsync(controller, _modules, configuration, videoOnly)
            : Task.CompletedTask;

    public async Task ReloadConfigurationsAsync()
    {
        await _selection.ReloadAsync();
        RefreshSelector();
    }

    private void SelectModule(IEmulationModule module)
    {
        _selection.ToggleModule(module);
        RefreshSelector();
    }

    private void SelectMachine(IEmulationModule module, EmulationMachineDefinition definition)
    {
        var selected = _selection.Find(module.Id, definition.Id);
        if (selected is null) return;

        var open = _openMachines.FirstOrDefault(item =>
        {
            if (item.Key.ModuleId != module.Id
                || item.Value.Tag is not EmulationMachineRuntime runtime)
                return false;
            return runtime.Configuration.MachineId == definition.Id;
        });
        if (open.Value is not null)
        {
            _selection.Select(module, definition.Id, open.Key.Id);
            _machines.SelectedItem = open.Value;
            RefreshSelector();
            return;
        }

        _selection.ToggleMachine(module, definition.Id, selected.Configuration.Id);
        RefreshSelector();
    }

    private void RefreshSelector()
    {
        if (_selectorRendering) return;
        _selectorRendering = true;
        try
        {
            _selectorRows.SetItems(_selection.ConfiguredModules(), _selection.SelectedModule,
                _selection.VisibleMachines(), _selection.SelectedMachineId);
            RefreshOpenButtonVisibility();
        }
        finally
        {
            _selectorRendering = false;
        }
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshOpenButtonVisibility()
    {
        var selected = _selection.SelectedConfiguration();
        var isOpen = selected is not null && _openMachines.ContainsKey((selected.Module.Id, selected.Configuration.Id));
        _open.Visibility = _selectorExpanded && selected is not null && !isOpen ? Visibility.Visible : Visibility.Collapsed;
        _open.IsEnabled = selected is not null && !isOpen;
    }

    private void ActiveMachineTabChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (_selectorRendering
            || _machines.SelectedItem is not TabItem { Tag: EmulationMachineRuntime runtime }) return;
        var module = _modules.FirstOrDefault(item => item.Id == runtime.Configuration.ModuleId);
        if (module is null) return;
        _selection.Select(module, runtime.Configuration.MachineId, runtime.Configuration.Id);
        RefreshSelector();
        NotifySelectedMachine();
    }

    private async void OpenSelectedMachine(object sender, RoutedEventArgs args)
        => await OpenSelectedConfigurationAsync();

    private async Task OpenSelectedConfigurationAsync()
    {
        var selected = _selection.SelectedConfiguration();
        if (selected is null) return;
        var key = (selected.Module.Id, selected.Configuration.Id);
        if (_openMachines.TryGetValue(key, out var existing))
        {
            _machines.SelectedItem = existing;
            NotifySelectedMachine();
            return;
        }
        try
        {
            _open.IsEnabled = false;
            await OpenMachineAsync(selected);
        }
        catch (Exception error)
        {
            ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationOpening, selected.DisplayName);
        }
        finally
        {
            RefreshSelector();
        }
    }
}
