using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.Emulation;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Services;

namespace RetroMediaLab.App.Controllers.Emulation.Options;

internal sealed class EmulationModuleConfigurationSession(IEmulationModule module, VideoPresentationProfileStore profiles)
{
    private IReadOnlyList<IEmulationConfiguration> _saved = [];
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    internal bool IsSaved(Guid id) => _saved.Any(configuration => configuration.Id == id);

    internal bool HasSavedMachine(string machineId) =>
        _saved.Any(configuration => configuration.MachineId == machineId);

    internal async Task LoadAsync() => _saved = await module.LoadConfigurationsAsync();

    internal IEmulationConfiguration SelectForEditing(string machineId, Guid? preferredId = null) =>
        EmulationConfigurationPersistenceFunctions.SelectForEditing(module, _saved, machineId, preferredId);

    internal void RemoveDraft(string machineId) => EmulationConfigurationDraftStore.Remove(module.Id, machineId);

    internal EmulationVideoPresentationProfile GetVideoProfile(Guid id) => profiles.Get(module.Id, id);

    internal IEmulationConfiguration ApplyVideoRenderer(IEmulationConfiguration configuration) =>
        EmulationVideoConfigurationFunctions.Apply(module, configuration, GetVideoProfile(configuration.Id).Renderer);

    internal IEmulationConfiguration ChangeVideoRenderer(IEmulationConfiguration configuration, EmulationVideoRenderer renderer)
    {
        profiles.Set(module.Id, configuration.Id, GetVideoProfile(configuration.Id) with { Renderer = renderer });
        return EmulationVideoConfigurationFunctions.Apply(module, configuration, renderer);
    }

    internal void SetVideoProcessing(IEmulationConfiguration configuration, EmulationVideoProcessingConfiguration processing)
    {
        var profile = GetVideoProfile(configuration.Id);
        profiles.Set(module.Id, configuration.Id, profile with { Processing = processing });
        if (!IsSaved(configuration.Id)) EmulationConfigurationDraftStore.Set(module.Id, configuration);
    }

    internal async Task SaveBeforeSwitchingAsync(IEmulationConfiguration configuration, Action<IEmulationConfiguration> notifySaved)
    {
        if (IsSaved(configuration.Id)) await PersistAsync(configuration, notifySaved);
        else EmulationConfigurationDraftStore.Set(module.Id, configuration);
    }

    internal async Task PersistAsync(IEmulationConfiguration configuration, Action<IEmulationConfiguration> notifySaved)
    {
        var wasSaved = IsSaved(configuration.Id);
        await _saveGate.WaitAsync();
        try
        {
            if (await EmulationConfigurationPersistenceFunctions.PersistAsync(module, configuration, wasSaved, profiles: profiles))
            {
                _saved = _saved.Select(item => item.Id == configuration.Id ? configuration : item).ToArray();
                notifySaved(configuration);
            }
        }
        finally { _saveGate.Release(); }
    }

    internal Task SaveAsync(IEmulationConfiguration configuration) =>
        EmulationConfigurationPersistenceFunctions.SaveAsync(module, configuration, profiles);

    internal Task SaveVideoProfileAsync(Guid id) =>
        IsSaved(id) ? profiles.SaveAsync(module.Id, id) : Task.CompletedTask;
}
