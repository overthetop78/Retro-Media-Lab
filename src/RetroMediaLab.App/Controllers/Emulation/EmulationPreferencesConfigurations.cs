using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Presenters.Emulation.Configurations;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Controllers.Emulation;

internal sealed class EmulationPreferencesConfigurations(IReadOnlyList<IEmulationModule> modules)
{
    private bool _loading;
    private Task? _initialLoad;
    internal bool Loaded { get; private set; }
    internal IReadOnlyList<EmulationConfigurationTableRow> Rows { get; private set; } = [];
    internal IReadOnlyList<IEmulationModule> ConfiguredModules { get; private set; } = [];
    internal IReadOnlyList<EmulationMachineDefinition> VisibleMachines { get; private set; } = [];
    internal IReadOnlyList<EmulationConfigurationTableRow> VisibleRows { get; private set; } = [];
    internal IEmulationModule? SelectedModule { get; private set; }
    internal string? SelectedMachineId { get; private set; }
    internal event Action? Changed;

    internal Task EnsureLoadedAsync()
    {
        if (Loaded) return Task.CompletedTask;
        if (_loading) return _initialLoad ?? Task.CompletedTask;
        return _initialLoad = LoadInitialAsync();
    }

    internal Task WaitForInitialLoadAsync() => _initialLoad ?? Task.CompletedTask;

    private async Task LoadInitialAsync()
    {
        _loading = true;
        try
        {
            await ReloadAsync();
            Loaded = true;
        }
        finally { _loading = false; }
    }

    internal async Task ReloadAsync()
    {
        var configurations = await EmulationConfigurationPersistenceFunctions.LoadAllAsync(modules);
        Rows = EmulationConfigurationTablePresenter.CreateRows(configurations);
        Refresh();
    }

    internal async Task DeleteAsync(EmulationConfigurationTableRow row)
    {
        await EmulationConfigurationPersistenceFunctions.DeleteAsync(row.Module, row.Configuration.Id);
        await ReloadAsync();
        await EmulationConfigurationNotifications.NotifyDeletedAsync();
    }

    internal void RefreshLocalizedRows()
    {
        Rows = EmulationConfigurationTablePresenter.CreateRows(Rows.Select(row => (row.Module, row.Configuration)));
        Refresh();
    }

    internal void SelectModule(IEmulationModule module)
    {
        SelectedModule = module;
        SelectedMachineId = null;
        Refresh();
    }

    internal void SelectMachine(string? machineId)
    {
        SelectedMachineId = machineId;
        Refresh();
    }

    private void Refresh()
    {
        ConfiguredModules = modules.Where(module => Rows.Any(row => row.Module.Id == module.Id)).ToArray();
        SelectedModule = ConfiguredModules.FirstOrDefault(module => module.Id == SelectedModule?.Id)
            ?? ConfiguredModules.FirstOrDefault();
        var moduleRows = Rows.Where(row => row.Module.Id == SelectedModule?.Id).ToArray();
        var machineIds = moduleRows.Select(row => row.Configuration.MachineId).ToHashSet(StringComparer.Ordinal);
        if (SelectedMachineId is { } machineId && !machineIds.Contains(machineId)) SelectedMachineId = null;
        VisibleMachines = SelectedModule?.Machines.Where(machine => machineIds.Contains(machine.Id)).ToArray() ?? [];
        VisibleRows = SelectedMachineId is null ? moduleRows
            : moduleRows.Where(row => row.Configuration.MachineId == SelectedMachineId).ToArray();
        Changed?.Invoke();
    }
}
