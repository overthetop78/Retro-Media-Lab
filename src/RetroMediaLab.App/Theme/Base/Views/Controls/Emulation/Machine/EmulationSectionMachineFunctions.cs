using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Services.Audio;
using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Views.Windows.EmulationModuleOptions;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;
using System.Runtime.ExceptionServices;


namespace RetroMediaLab.App.Views.Controls.Emulation.Machine;

public sealed partial class EmulationSection
{
    private async Task OpenMachineAsync(EmulationConfigurationListItem selected)
    {
        var presentation = RetroMediaLab.App.Services.Emulation.EmulationVideoPresentationProfiles.Store.Get(selected.Module.Id, selected.Configuration.Id);
        var runtime = await CreateRuntimeAsync(selected.Module, selected.Configuration);
        MachineController? view = null;
        view = await MachineController.CreateAsync(new MachineControllerOptions(
            selected.Module.Id, selected.Configuration.MachineId, selected.Configuration.Id,
            runtime.CreateMachine(runtime.MountedMedia), runtime.CreateMachine,
            (configuration, cancellationToken) => CreateRuntimeAsync(selected.Module, configuration, cancellationToken),
            runtime.MediaDevices, runtime.MountedMedia, presentation.Renderer,
            presentation.Processing!,
            EmulationShortcutMap.GlobalShortcuts(_settings.EmulationShortcuts),
            Path.Combine(_settings.EmulationStateFolder, $"{selected.Module.Id}-{selected.Configuration.Id:N}.gwas"),
            _settings.EmulationCaptureFolder, RuntimeDisplayName(runtime),
            error => ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationOpening, selected.DisplayName),
            runtime.SupportsPointerCapture,
            device => InitialMediaDirectory(selected, device),
            (device, directory) => RememberMediaDirectory(selected, device, directory),
            () => ReferenceEquals(_machines.SelectedContent, view),
            runtime.PrepareMediaAsync,
            null,
            PersistRuntimeOptionsAsync,
            PersistMountedMedia: PersistMountedMediaAsync,
            OpenSettings: OpenSettingsAsync));
        async Task OpenSettingsAsync()
        {
            var configuration = (await selected.Module.LoadConfigurationsAsync()).FirstOrDefault(item => item.Id == selected.Configuration.Id) ?? selected.Configuration;
            var dialog = new EmulationModuleOptionsWindow(selected.Module, initialConfiguration: configuration,
                reservedBindings: _settings.EmulationShortcuts.Values);
            try { WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), dialog); }
            finally { WpfWindowNavigationService.CloseOwnedDialog(dialog); }
        }

        async Task PersistMountedMediaAsync(IReadOnlyList<EmulationMedia> media)
        {
            if (selected.Module is not IEmulationStorageSettingsManager storage) return;
            var configuration = (await selected.Module.LoadConfigurationsAsync()).FirstOrDefault(item => item.Id == selected.Configuration.Id) ?? selected.Configuration;
            var settings = storage.DescribeStorageSettings(configuration);
            await selected.Module.SaveConfigurationAsync(storage.ApplyStorageSettings(configuration,
                settings with { MountedMedia = media.Where(item => item.IsInserted).ToArray() }));
        }

        async Task PersistRuntimeOptionsAsync(IReadOnlyDictionary<string, string> values)
        {
            var configuration = (await selected.Module.LoadConfigurationsAsync()).FirstOrDefault(item => item.Id == selected.Configuration.Id) ?? selected.Configuration;
            var nullableValues = values.ToDictionary(item => item.Key, item => (string?)item.Value, StringComparer.Ordinal);
            await selected.Module.SaveConfigurationAsync(selected.Module.ApplySettings(configuration, nullableValues));
        }
        await AddMachineAsync(selected, runtime, view, view.StopAsync);
    }

    private async ValueTask<EmulationMachineRuntime> CreateRuntimeAsync(IEmulationModule module, IEmulationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        configuration = EmulationVideoConfigurationFunctions.Apply(module, configuration,
            RetroMediaLab.App.Services.Emulation.EmulationVideoPresentationProfiles.Store.Get(module.Id, configuration.Id).Renderer);
        var moduleRoot = Path.Combine(_settings.EmulationStorageFolder, module.Id);
        var runtime = await module.CreateRuntimeAsync(configuration,
            new EmulationRuntimeServices(Path.Combine(moduleRoot, "Sessions"), Path.Combine(moduleRoot, "States"), Path.Combine(moduleRoot, "Converted"), Environment.ProcessPath!, (device, latency) => new WasapiAudioOutput(device, latency)), cancellationToken);
        var devices = (module as IEmulationStorageSettingsManager)?.DescribeStorageSettings(runtime.Configuration).DeviceSettings;
        return runtime with { MediaDevices = runtime.MediaDevices.Select(device => EmulationMediaIconFunctions.WithConfiguredModel(device, devices)).ToArray() };
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
