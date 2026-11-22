using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Presenters.Emulation.Configurations;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed class EmulationConfigurationSelection(IReadOnlyList<IEmulationModule> modules)
{
    internal IReadOnlyList<EmulationConfigurationListItem> Items { get; private set; } = [];
    internal IEmulationModule? SelectedModule { get; private set; }
    internal string? SelectedMachineId { get; private set; }
    private Guid? _selectedConfigurationId;

    internal IEmulationModule[] ConfiguredModules() =>
        modules.Where(module => Items.Any(item => item.Module.Id == module.Id)).ToArray();

    internal EmulationMachineDefinition[] VisibleMachines()
    {
        if (SelectedModule is not { } module) return [];
        var ids = Items.Where(item => item.Module.Id == module.Id)
            .Select(item => item.Configuration.MachineId).ToHashSet(StringComparer.Ordinal);
        return module.Machines.Where(machine => ids.Contains(machine.Id)).ToArray();
    }

    internal async Task ReloadAsync()
    {
        var items = new List<EmulationConfigurationListItem>();
        foreach (var module in modules)
        {
            var configurations = await EmulationConfigurationPersistenceFunctions.LoadAndRegisterAsync(module);
            foreach (var configuration in configurations)
                items.Add(new(module, configuration, EmulationConfigurationPresenter.DisplayName(module, configuration)));
        }
        Items = items;
        if (SelectedModule is not { } selected) return;
        if (!items.Any(item => item.Module.Id == selected.Id)) Select(null);
        else if (SelectedMachineId is { } machineId)
        {
            var match = Find(selected.Id, machineId);
            Select(selected, match?.Configuration.MachineId, match?.Configuration.Id);
        }
    }

    internal void Select(IEmulationModule? module, string? machineId = null, Guid? configurationId = null)
    {
        SelectedModule = module;
        SelectedMachineId = machineId;
        _selectedConfigurationId = configurationId;
    }

    internal void ToggleModule(IEmulationModule module) => Select(ReferenceEquals(SelectedModule, module) ? null : module);

    internal void ToggleMachine(IEmulationModule module, string machineId, Guid configurationId)
    {
        if (ReferenceEquals(SelectedModule, module) && SelectedMachineId == machineId) Select(module);
        else Select(module, machineId, configurationId);
    }

    internal EmulationConfigurationListItem? Find(string moduleId, string machineId)
    {
        var matches = Items.Where(item => item.Module.Id == moduleId && item.Configuration.MachineId == machineId).ToArray();
        return matches.FirstOrDefault(item => item.Configuration.Id == _selectedConfigurationId) ?? matches.FirstOrDefault();
    }

    internal EmulationConfigurationListItem? Find(string moduleId, Guid configurationId) =>
        Items.FirstOrDefault(item => item.Module.Id == moduleId && item.Configuration.Id == configurationId);

    internal EmulationConfigurationListItem? SelectedConfiguration() =>
        SelectedModule is { } module && SelectedMachineId is { } machineId ? Find(module.Id, machineId) : null;
}
