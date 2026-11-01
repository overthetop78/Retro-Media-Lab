using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.Emulation;

namespace GWGUI.App.Views.Controls.Emulation.Machine;

public sealed partial class EmulationSection
{
    internal event EventHandler? PresentationChanged;
    internal event Action<MachineController>? MachineSelected;

    internal IReadOnlyList<EmulationConfigurationListItem> Configurations => _configurations;

    internal IReadOnlyList<(EmulationConfigurationListItem Configuration, MachineController Controller)> OpenedMachines
    {
        get
        {
            var machines = new List<(EmulationConfigurationListItem, MachineController)>();
            foreach (var (key, tab) in _openMachines)
            {
                if (tab.Content is not MachineController controller) continue;
                var configuration = _configurations.FirstOrDefault(item => item.Module.Id == key.ModuleId && item.Configuration.Id == key.Id);
                if (configuration is null && tab.Tag is EmulationMachineRuntime runtime
                    && _modules.FirstOrDefault(candidate => candidate.Id == key.ModuleId) is { } module)
                    configuration = new(module, runtime.Configuration, RuntimeDisplayName(runtime));
                if (configuration is not null) machines.Add((configuration, controller));
            }
            return machines;
        }
    }

    internal void SelectOpenedMachine(MachineController controller)
    {
        var tab = _openMachines.Values.FirstOrDefault(item => ReferenceEquals(item.Content, controller));
        if (tab is not null) _machines.SelectedItem = tab;
    }

    internal Task CloseOpenedMachineAsync(MachineController controller)
    {
        var entry = _openMachines.FirstOrDefault(item => ReferenceEquals(item.Value.Content, controller));
        return entry.Value is null ? Task.CompletedTask : CloseMachineAsync(entry.Key, entry.Value, controller.StopAsync);
    }

    private void NotifySelectedMachine()
    {
        if (_machines.SelectedContent is MachineController controller) MachineSelected?.Invoke(controller);
    }

    internal Task RequestOpenAsync(EmulationConfigurationListItem configuration)
    {
        var current = _configurations.FirstOrDefault(item =>
            item.Module.Id == configuration.Module.Id && item.Configuration.Id == configuration.Configuration.Id);
        if (current is null) return Task.CompletedTask;
        _selectedModule = current.Module;
        _selectedMachineId = current.Configuration.MachineId;
        _selectedConfigurationId = current.Configuration.Id;
        return OpenSelectedConfigurationAsync();
    }
}
