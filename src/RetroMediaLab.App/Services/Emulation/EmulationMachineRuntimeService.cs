using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Services.Audio;
using RetroMediaLab.Emulation;
using System.IO;
using System.Runtime.ExceptionServices;

namespace RetroMediaLab.App.Services.Emulation;

internal static class EmulationMachineRuntimeService
{
    internal static async Task StopAllAsync(IEnumerable<Func<Task>> stopMachines)
    {
        ExceptionDispatchInfo? failure = null;
        foreach (var stop in stopMachines)
        {
            try { await stop(); }
            catch (Exception error) { failure ??= ExceptionDispatchInfo.Capture(error); }
        }
        failure?.Throw();
    }

    internal static async ValueTask<EmulationMachineRuntime> CreateAsync(string storageFolder, IEmulationModule module, IEmulationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        configuration = EmulationVideoConfigurationFunctions.Apply(module, configuration,
            EmulationVideoPresentationProfiles.Store.Get(module.Id, configuration.Id).Renderer);
        var moduleRoot = Path.Combine(storageFolder, module.Id);
        var runtime = await module.CreateRuntimeAsync(configuration,
            new EmulationRuntimeServices(Path.Combine(moduleRoot, "Sessions"), Path.Combine(moduleRoot, "States"), Path.Combine(moduleRoot, "Converted"), Environment.ProcessPath!, (device, latency) => new WasapiAudioOutput(device, latency)), cancellationToken);
        var devices = (module as IEmulationStorageSettingsManager)?.DescribeStorageSettings(runtime.Configuration).DeviceSettings;
        return runtime with { MediaDevices = runtime.MediaDevices.Select(device => EmulationMediaIconFunctions.WithConfiguredModel(device, devices)).ToArray() };
    }

    internal static async Task PersistMountedMediaAsync(EmulationConfigurationListItem selected, IReadOnlyList<EmulationMedia> media)
    {
        if (selected.Module is not IEmulationStorageSettingsManager storage) return;
        var configuration = (await selected.Module.LoadConfigurationsAsync()).FirstOrDefault(item => item.Id == selected.Configuration.Id) ?? selected.Configuration;
        var settings = storage.DescribeStorageSettings(configuration);
        await selected.Module.SaveConfigurationAsync(storage.ApplyStorageSettings(configuration,
            settings with { MountedMedia = media.Where(item => item.IsInserted).ToArray() }));
    }

    internal static async Task PersistRuntimeOptionsAsync(EmulationConfigurationListItem selected, IReadOnlyDictionary<string, string> values)
    {
        var configuration = (await selected.Module.LoadConfigurationsAsync()).FirstOrDefault(item => item.Id == selected.Configuration.Id) ?? selected.Configuration;
        var nullableValues = values.ToDictionary(item => item.Key, item => (string?)item.Value, StringComparer.Ordinal);
        await selected.Module.SaveConfigurationAsync(selected.Module.ApplySettings(configuration, nullableValues));
    }
}
