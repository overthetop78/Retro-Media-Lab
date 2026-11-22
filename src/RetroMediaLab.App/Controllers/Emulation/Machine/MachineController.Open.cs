using System.IO;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.Emulation;
using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    internal static async Task<(MachineController Controller, EmulationMachineRuntime Runtime)> OpenAsync(
        EmulationConfigurationListItem selected, AppSettings settings, Func<EmulationMachinePresentation> createPresentation,
        Func<bool> isActive, Action<Exception> showError,
        Action<IEmulationConfiguration, IReadOnlyList<string>> showSettings)
    {
        var module = selected.Module;
        var configuration = selected.Configuration;
        var presentation = EmulationVideoPresentationProfiles.Store.Get(module.Id, configuration.Id);
        var runtime = await EmulationMachineRuntimeService.CreateAsync(settings.EmulationStorageFolder, module, configuration);
        var shortcuts = EmulationShortcutMap.GlobalShortcuts(settings.EmulationShortcuts);
        string statePath = Path.Combine(settings.EmulationStateFolder, $"{module.Id}-{configuration.Id:N}.gwas");
        string title = LocExtension.GetForModule(runtime.Configuration.ModuleId, runtime.DisplayResourceKey);
        var controller = await CreateAsync(new MachineControllerOptions(
            module.Id, configuration.MachineId, configuration.Id, runtime.CreateMachine(runtime.MountedMedia),
            runtime.CreateMachine,
            (updated, cancellationToken) => EmulationMachineRuntimeService.CreateAsync(settings.EmulationStorageFolder, module, updated, cancellationToken),
            runtime.MediaDevices, runtime.MountedMedia, presentation.Renderer, presentation.Processing!,
            shortcuts, statePath, settings.EmulationCaptureFolder, title, showError, runtime.SupportsPointerCapture,
            device => EmulationMediaFolderFunctions.InitialMediaDirectory(settings, selected, device),
            (device, directory) => EmulationMediaFolderFunctions.RememberMediaDirectory(settings, selected, device, directory),
            isActive, createPresentation, runtime.PrepareMediaAsync,
            PersistRuntimeOptions: values => EmulationMachineRuntimeService.PersistRuntimeOptionsAsync(selected, values),
            PersistMountedMedia: media => EmulationMachineRuntimeService.PersistMountedMediaAsync(selected, media),
            OpenSettings: OpenSettingsAsync,
            ShortcutSettings: settings.EmulationShortcuts, Module: module, Configuration: runtime.Configuration,
            ReadShortcutSettings: () => settings.EmulationShortcuts));
        return (controller, runtime);

        async Task OpenSettingsAsync()
        {
            var configurations = await module.LoadConfigurationsAsync();
            var latest = configurations.FirstOrDefault(item => item.Id == configuration.Id) ?? configuration;
            showSettings(latest, EmulationShortcutMap.GlobalBindingValues(settings.EmulationShortcuts));
        }
    }
}
