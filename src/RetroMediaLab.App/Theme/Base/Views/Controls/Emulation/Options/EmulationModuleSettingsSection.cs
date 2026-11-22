using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Storage;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Contracts.Emulation.Machine;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Contracts.Views.Emulation.Settings;
using RetroMediaLab.App.Controllers.Emulation.Firmware;
using RetroMediaLab.App.Controllers.Emulation.Input;
using RetroMediaLab.App.Controllers.Emulation.Options;
using RetroMediaLab.App.Controllers.Emulation.Storage;
using RetroMediaLab.App.Functions.Views.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Services.Audio;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Services.Storage;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;
using Microsoft.Win32;
using System.IO;


namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection : UserControl, IAsyncDisposable
{
    private readonly IEmulationModule _module;
    private readonly Action<Exception> _showError;
    private readonly ListBox _machines = new()
    {
        MinWidth = 220,
        BorderThickness = new Thickness(0),
        Background = System.Windows.Media.Brushes.Transparent,
        HorizontalContentAlignment = HorizontalAlignment.Stretch
    };
    private readonly EmulationVideoProcessingSettingsSection _videoProcessing = new();
    private readonly Dictionary<string, FrameworkElement> _fieldControls = new(StringComparer.Ordinal);
    private readonly Dictionary<FrameworkElement, Func<Task>> _userChangeHandlers = [];
    private readonly EmulationEmulatorManagementController? _emulatorManagement;
    private readonly EmulationFirmwareManagementController? _firmwareManagement;
    private readonly EmulationInputSettingsController? _inputSettings;
    private readonly EmulationStorageSettingsController? _storageSettings;
    private readonly EmulationModuleConfigurationSession _session;
    private IEmulationConfiguration _configuration;
    private bool _loading;
    private bool _disposed;
    private Task? _initialLoad;
    private EmulationMachineTab _selectedTab = EmulationMachineTab.General;

    internal EmulationModuleSettingsSection(IEmulationModule module, RetroMediaLab.VideoPresentation.Services.VideoPresentationProfileStore? profiles = null, Action<Exception>? showError = null, IEnumerable<string>? reservedBindings = null)
    {
        FlowDirection = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        _module = module;
        _session = new EmulationModuleConfigurationSession(module, profiles ?? EmulationVideoPresentationProfiles.Store);
        _showError = showError ?? (error => ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationManagement, LocExtension.GetForModule(_module, _module.DisplayResourceKey)));
        _machines.ItemContainerStyle = EmulationMachineChoiceLayout.CreateListItemContainerStyle();
        _machines.ItemTemplate = EmulationMachineChoiceLayout.CreateTemplate();
        var choices = module.Machines.Select(CreateMachineChoice).ToArray();
        _machines.ItemsSource = choices;
        _machines.SelectedIndex = 0;
        _configuration = module.CreateConfiguration(choices[0].Definition.Id);
        if (module is IEmulationEmulatorManager manager)
        {
            _emulatorManagement = new EmulationEmulatorManagementController(manager, () => _configuration, SetConfiguration, () => _session.HasSavedMachine(_configuration.MachineId), module as IEmulationModuleLocalization);
            _emulatorManagement.ConfigurationChanged += EmulatorConfigurationChanged;
        }
        if (module is IEmulationFirmwareManager firmwareManager)
        {
            _firmwareManagement = new EmulationFirmwareManagementController(_module, firmwareManager, () => _configuration);
        }
        if (module is IEmulationInputSettingsManager inputManager)
        {
            _inputSettings = new EmulationInputSettingsController(inputManager, reservedBindings);
            _inputSettings.SettingsChanged += async (_, _) =>
            {
                var before = _module.Describe(_configuration.MachineId, _configuration).Visibility.Tabs;
                await ExecuteUserChangeAsync();
                var after = _module.Describe(_configuration.MachineId, _configuration).Visibility.Tabs;
                if (before.GetValueOrDefault(EmulationMachineTab.Keyboard) != after.GetValueOrDefault(EmulationMachineTab.Keyboard)
                    || before.GetValueOrDefault(EmulationMachineTab.Mouse) != after.GetValueOrDefault(EmulationMachineTab.Mouse)) RebuildEditor();
            };
        }
        if (module is IEmulationStorageSettingsManager storageManager)
        {
            _storageSettings = new EmulationStorageSettingsController(_module.Id, storageManager, DefaultFolder);
            _storageSettings.SettingsChanged += async (_, _) =>
            {
                await ExecuteUserChangeAsync();
                RebuildEditor();
            };
        }
        _machines.SelectionChanged += MachineChanged;
        _videoProcessing.ConfigurationChanged += async (_, _) =>
        {
            _session.SetVideoProcessing(_configuration, _videoProcessing.Configuration);
            VideoConfigurationChanged?.Invoke(this, new EmulationConfigurationSavedEventArgs(_configuration));
            await ExecuteAsync(SaveVideoProfileAsync);
        };
        Content = BuildEditor();
        Loaded += async (_, _) => await EnsureInitializedAsync();
    }

    internal event EventHandler<EmulationConfigurationSavedEventArgs>? ConfigurationSaved;
    internal event EventHandler<EmulationConfigurationSavedEventArgs>? VideoConfigurationChanged;
    internal event EventHandler<EmulationMachineEditingContext>? EditingContextChanged;
    internal IEmulationConfiguration CurrentConfiguration => _configuration;

    private async void EmulatorConfigurationChanged(object? sender, EventArgs args) => await ExecuteUserChangeAsync();

    internal void SetVideoShaderLoading(string moduleId, Guid configurationId, bool isLoading)
    {
        if (!string.Equals(_module.Id, moduleId, StringComparison.Ordinal)
            || _configuration.Id != configurationId) return;
        _videoProcessing.SetShaderLoading(isLoading);
    }

    internal Task ReloadWhenOpenedAsync() => ExecuteAsync(ReloadAsync);
    internal Task EnsureInitializedAsync() => _initialLoad ??= ReloadWhenOpenedAsync();

    internal async Task ReloadAfterConfigurationDeletedAsync(Guid configurationId, string machineId)
    {
        if (_configuration.Id == configurationId)
            _session.RemoveDraft(machineId);

        await ReloadAsync();
    }

    internal async Task EditConfigurationAsync(IEmulationConfiguration configuration)
    {
        await _session.LoadAsync();
        _configuration = configuration;
        SelectMachine(configuration.MachineId);
        _selectedTab = EmulationMachineTab.General;
        RebuildEditor();
        if (_emulatorManagement is not null)
            await _emulatorManagement.RefreshAsync();
        NotifyEditingContextChanged();
    }

    private EmulationMachineChoice CreateMachineChoice(EmulationMachineDefinition machine) =>
        new(machine, LocExtension.GetForModule(_module, machine.DisplayResourceKey), _session.HasSavedMachine(machine.Id), EmulationAssetFunctions.Load(machine.ImagePath));

    private async Task ReloadAsync()
    {
        await _session.LoadAsync();
        var machineId = _configuration.MachineId;
        _configuration = _session.SelectForEditing(machineId, _configuration.Id);
        var choices = _module.Machines.Select(CreateMachineChoice).ToArray();
        _machines.ItemsSource = choices;
        SelectMachine(machineId);
        RebuildEditor();
        if (_emulatorManagement is not null) await _emulatorManagement.RefreshAsync();
        NotifyEditingContextChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            try { if (_initialLoad is not null) await _initialLoad; }
            finally
            {
                if (_emulatorManagement is not null)
                {
                    _emulatorManagement.ConfigurationChanged -= EmulatorConfigurationChanged;
                    await _emulatorManagement.DisposeAsync();
                }
            }
        }
        finally
        {
            try
            {
                if (_firmwareManagement is not null) await _firmwareManagement.DisposeAsync();
            }
            finally
            {
                _machines.SelectionChanged -= MachineChanged;
                PresentationChanged = null;
                presentationTabs = null;
                _fieldControls.Clear();
                _userChangeHandlers.Clear();
                Content = null;
            }
        }
    }

}
