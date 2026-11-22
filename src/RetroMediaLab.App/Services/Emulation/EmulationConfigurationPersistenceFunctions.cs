using RetroMediaLab.Emulation;
using RetroMediaLab.VideoPresentation.Services;

namespace RetroMediaLab.App.Services.Emulation;

internal static class EmulationConfigurationPersistenceFunctions
{
    internal static void ApplyEditorValues(IEmulationModule module,
        ref IEmulationConfiguration configuration, IReadOnlyDictionary<string, string?>? values,
        Func<IEmulationConfiguration, IEmulationConfiguration>? applyInput,
        Func<IEmulationConfiguration, IEmulationConfiguration>? applyStorage)
    {
        if (values is not null) configuration = module.ApplySettings(configuration, values);
        if (applyInput is not null) configuration = applyInput(configuration);
        if (applyStorage is not null) configuration = applyStorage(configuration);
    }

    internal static IEmulationConfiguration SelectForEditing(IEmulationModule module,
        IReadOnlyList<IEmulationConfiguration> saved, string machineId, Guid? preferredId = null)
    {
        var selected = preferredId is { } id ? saved.FirstOrDefault(item => item.Id == id) : null;
        return selected ?? saved.FirstOrDefault(item => item.MachineId == machineId)
            ?? (EmulationConfigurationDraftStore.TryGet(module.Id, machineId, out var draft)
                ? draft
                : module.CreateConfiguration(machineId));
    }

    internal static async Task<IReadOnlyList<IEmulationConfiguration>> LoadAndRegisterAsync(IEmulationModule module)
    {
        var configurations = await module.LoadConfigurationsAsync();
        EmulationSdk.Networks.SetConfigurations(module, configurations);
        return configurations;
    }

    internal static async Task<IReadOnlyList<(IEmulationModule Module, IEmulationConfiguration Configuration)>> LoadAllAsync(
        IReadOnlyList<IEmulationModule> modules)
    {
        var loaded = await Task.WhenAll(modules.Select(async module =>
            (Module: module, Configurations: await Task.Run(async () => await module.LoadConfigurationsAsync()))));
        foreach (var item in loaded)
            EmulationSdk.Networks.SetConfigurations(item.Module, item.Configurations);
        return loaded.SelectMany(item =>
            item.Configurations.Select(configuration => (item.Module, Configuration: configuration))).ToArray();
    }

    internal static async Task SaveAsync(IEmulationModule module, IEmulationConfiguration configuration, VideoPresentationProfileStore profiles)
    {
        await profiles.SaveAsync(module.Id, configuration.Id);
        await module.SaveConfigurationAsync(configuration);
        EmulationConfigurationDraftStore.Remove(module.Id, configuration.MachineId);
    }

    internal static async Task DeleteAsync(IEmulationModule module, Guid configurationId)
    {
        await module.DeleteConfigurationAsync(configurationId);
        EmulationVideoPresentationProfiles.Store.Delete(module.Id, configurationId);
    }

    internal static async ValueTask<bool> PersistAsync(
        IEmulationModule module,
        IEmulationConfiguration configuration,
        bool hasSavedConfiguration,
        CancellationToken cancellationToken = default,
        VideoPresentationProfileStore? profiles = null)
    {
        if (!hasSavedConfiguration)
        {
            EmulationConfigurationDraftStore.Set(module.Id, configuration);
            return false;
        }

        (profiles ?? EmulationVideoPresentationProfiles.Store).Get(module.Id, configuration.Id);
        await module.SaveConfigurationAsync(configuration, cancellationToken);
        return true;
    }
}
