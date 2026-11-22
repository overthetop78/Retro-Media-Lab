using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Controllers.Emulation;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Contracts.Emulation.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Views.Controls.Emulation.Input;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Common;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;
using RetroMediaLab.App.Constants.Controls.Visual;


namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

public sealed partial class EmulationPreferencesSection : UserControl
{
    internal event Func<IEmulationModule, IEmulationConfiguration, Task>? EditConfigurationRequested;

    private readonly EmulationPreferencesConfigurations _configurationCatalog;
    private readonly EmulationConfigurationSelector _configurationSelector = new();
    private readonly EmulationConfigurationTable _configurationTable = new();
    private readonly RetroMediaLab.App.Views.Controls.Emulation.Network.EmulationNetworkSection _network = new();
    private readonly InputBindingEditor _shortcuts = new();
    private readonly TextBox _storageFolder = new();
    private readonly TextBox _captureFolder = new();
    private readonly TextBox _stateFolder = new();
    private readonly EmulationPreferencesSettings _preferencesSettings = new();
    private readonly TabControl _tabs;
    private readonly TabItem _generalTab;
    private readonly TabItem _shortcutsTab;
    private readonly List<(TabItem Tab, string ResourceKey)> _localizedTabs = [];

    public EmulationPreferencesSection() : this(EmulationModuleRegistry.Modules) { }

    internal EmulationPreferencesSection(IReadOnlyList<IEmulationModule> modules)
    {
        _configurationCatalog = new(modules);
        _configurationCatalog.Changed += RefreshConfigurationControls;
        _shortcuts.ConfigureCaptureSources(RetroMediaLab.App.Enums.Input.InputCaptureSources.Keyboard | RetroMediaLab.App.Enums.Input.InputCaptureSources.Mouse | RetroMediaLab.App.Enums.Input.InputCaptureSources.Controller, true);
        _tabs = new TabControl
        {
            Margin = new Thickness(8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        _shortcuts.BindingsChanged += async (_, _) => await SaveShortcutsAsync();
        _configurationSelector.BrandSelected += SelectModule;
        _configurationSelector.MachineSelected += (_, machine) =>
            SelectMachine(SelectedMachineId == machine.Id ? null : machine.Id);
        _configurationTable.EditRequested += async row => await EditConfigurationAsync(row);
        _configurationTable.DeleteRequested += async row => await DeleteConfigurationAsync(row);
        _storageFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _captureFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _stateFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _generalTab = AddTab(_tabs, IconGlyphs.Settings, "Emulation.Tab.General", BuildGeneralTab());
        _shortcutsTab = AddTab(_tabs, IconGlyphs.Keyboard, "Emulation.Tab.Shortcuts", BuildShortcutsTab());
        AddTab(_tabs, IconGlyphs.Configuration, "Emulation.Configuration", BuildConfigurationsTab());
        AddTab(_tabs, IconGlyphs.Connection, RetroMediaLab.App.Constants.Emulation.EmulationNetworkLayoutConstants.TitleResource, _network);
        Content = _tabs;
        Loaded += async (_, _) => await LoadConfigurationsWhenVisibleAsync();
        IsVisibleChanged += async (_, _) => await LoadConfigurationsWhenVisibleAsync();
    }

    internal TextBox StorageFolder => _storageFolder;
    internal TextBox CaptureFolder => _captureFolder;
    internal TextBox StateFolder => _stateFolder;
    internal InputBindingEditor ShortcutEditor => _shortcuts;
    internal IReadOnlyList<EmulationConfigurationTableRow> ConfigurationRows => _configurationCatalog.Rows;
    internal IEmulationModule? SelectedModule => _configurationCatalog.SelectedModule;
    internal string? SelectedMachineId => _configurationCatalog.SelectedMachineId;
    internal event EventHandler? PresentationChanged;

    internal Task RequestSaveFoldersAsync() => SaveFoldersAsync();
    internal Task RequestBrowseFolderAsync(TextBox folder) => BrowseFolderAsync(folder);
    internal Task RequestEditConfigurationAsync(EmulationConfigurationTableRow row) => EditConfigurationAsync(row);
    internal Task RequestDeleteConfigurationAsync(EmulationConfigurationTableRow row) => DeleteConfigurationAsync(row);

    internal Task EnsureConfigurationsAsync() => _configurationCatalog.EnsureLoadedAsync();

    internal Task WaitForConfigurationsAsync() => _configurationCatalog.WaitForInitialLoadAsync();

    internal void SelectModule(IEmulationModule module) => _configurationCatalog.SelectModule(module);

    internal void SelectMachine(string? machineId) => _configurationCatalog.SelectMachine(machineId);

    public void Configure(AppSettings settings, Func<Task> persistSettings)
    {
        _storageFolder.Text = settings.EmulationStorageFolder;
        _captureFolder.Text = settings.EmulationCaptureFolder;
        _stateFolder.Text = settings.EmulationStateFolder;
        _shortcuts.SetRows(EmulationShortcutMap.GlobalShortcutDefinitions(), settings.EmulationShortcuts);
        _preferencesSettings.Configure(settings, persistSettings);
    }

    internal Task ReloadConfigurationsForWindowAsync() => ReloadConfigurationsAsync();

    private async Task LoadConfigurationsWhenVisibleAsync()
    {
        if (IsLoaded && IsVisible) await EnsureConfigurationsAsync();
    }

    internal void RefreshLocalizedContent()
    {
        var shortcutValues = _shortcuts.Rows.ToDictionary(row => row.Id, row => row.Binding, StringComparer.Ordinal);
        foreach (var (tab, resourceKey) in _localizedTabs)
        {
            var text = LocExtension.Get(resourceKey);
            if (tab.Header is MainTabHeader header) header.Text = text;
        }
        _shortcuts.SetRows(EmulationShortcutMap.GlobalShortcutDefinitions(), shortcutValues);
        _shortcuts.ConfigurePresentation(LocExtension.Get("Emulation.Input.Actions"), LocExtension.Get("Emulation.Input.Binding.Search"));
        _configurationTable.RefreshLocalizedContent();
        _network.RefreshLocalizedContent();
        if (_configurationCatalog.Loaded)
        {
            _configurationCatalog.RefreshLocalizedRows();
            _ = ReloadConfigurationsAsync();
        }
    }

}
