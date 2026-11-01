using GWGUI.Infrastructure.Settings;
using GWGUI.App.Contracts.Emulation.Configurations;
using GWGUI.App.Contracts.Emulation.Machine;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Presenters.Emulation.Configurations;
using GWGUI.App.Services.Emulation;
using GWGUI.App.Services.Storage;
using GWGUI.App.Views.Controls.Emulation.Input;
using GWGUI.App.Views.Controls.Emulation.Machine;
using GWGUI.App.Views.Controls.Common;
using System.Windows;
using System.Windows.Controls;
using GWGUI.Emulation;
using GWGUI.App.Constants.Controls.Visual;


namespace GWGUI.App.Views.Controls.Emulation.Options;

public sealed partial class EmulationPreferencesSection : UserControl
{
    public static event EventHandler<EmulationConfigurationSavedEventArgs>? ConfigurationSaved;
    public static event EventHandler<EmulationConfigurationSavedEventArgs>? VideoConfigurationChanged;
    internal static event Func<Task>? ConfigurationDeleted;
    internal event Func<IEmulationModule, IEmulationConfiguration, Task>? EditConfigurationRequested;

    private readonly IReadOnlyList<IEmulationModule> _modules;
    private readonly EmulationConfigurationSelector _configurationSelector = new();
    private string? _selectedConfigurationMachineId;
    private IEmulationModule? _selectedConfigurationModule;
    private IReadOnlyList<EmulationConfigurationTableRow> _configurationRows = [];
    private readonly EmulationConfigurationTable _configurationTable = new();
    private readonly GWGUI.App.Views.Controls.Emulation.Network.EmulationNetworkSection _network = new();
    private readonly InputBindingEditor _shortcuts = new();
    private readonly TextBox _storageFolder = new();
    private readonly TextBox _captureFolder = new();
    private readonly TextBox _stateFolder = new();
    private AppSettings? _settings;
    private Func<Task>? _persistSettings;
    private readonly TabControl _tabs;
    private readonly TabItem _generalTab;
    private readonly TabItem _shortcutsTab;
    private readonly List<(TabItem Tab, string ResourceKey)> _localizedTabs = [];
    private bool _configurationsLoaded;
    private bool _loadingConfigurations;
    private Task? _configurationLoadTask;

    public EmulationPreferencesSection() : this(EmulationModuleRegistry.Modules) { }

    internal EmulationPreferencesSection(IReadOnlyList<IEmulationModule> modules)
    {
        _modules = modules;
        _shortcuts.ConfigureCaptureSources(GWGUI.App.Enums.Input.InputCaptureSources.Keyboard | GWGUI.App.Enums.Input.InputCaptureSources.Mouse | GWGUI.App.Enums.Input.InputCaptureSources.Controller, true);
        _tabs = new TabControl
        {
            Margin = new Thickness(8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        _shortcuts.BindingsChanged += async (_, _) => await SaveShortcutsAsync();
        _configurationSelector.BrandSelected += SelectModule;
        _configurationSelector.MachineSelected += (_, machine) =>
            SelectMachine(_selectedConfigurationMachineId == machine.Id ? null : machine.Id);
        _configurationTable.EditRequested += async row => await EditConfigurationAsync(row);
        _configurationTable.DeleteRequested += async row => await DeleteConfigurationAsync(row);
        _storageFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _captureFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _stateFolder.LostKeyboardFocus += async (_, _) => await SaveFoldersAsync();
        _generalTab = AddTab(_tabs, IconGlyphs.Settings, "Emulation.Tab.General", BuildGeneralTab());
        _shortcutsTab = AddTab(_tabs, IconGlyphs.Keyboard, "Emulation.Tab.Shortcuts", BuildShortcutsTab());
        AddTab(_tabs, IconGlyphs.Configuration, "Emulation.Configuration", BuildConfigurationsTab());
        AddTab(_tabs, IconGlyphs.Connection, GWGUI.App.Constants.Emulation.EmulationNetworkLayoutConstants.TitleResource, _network);
        Content = _tabs;
        Loaded += async (_, _) => await LoadConfigurationsWhenVisibleAsync();
        IsVisibleChanged += async (_, _) => await LoadConfigurationsWhenVisibleAsync();
    }

    internal TextBox StorageFolder => _storageFolder;
    internal TextBox CaptureFolder => _captureFolder;
    internal TextBox StateFolder => _stateFolder;
    internal InputBindingEditor ShortcutEditor => _shortcuts;
    internal IReadOnlyList<EmulationConfigurationTableRow> ConfigurationRows => _configurationRows;
    internal IEmulationModule? SelectedModule => _selectedConfigurationModule;
    internal string? SelectedMachineId => _selectedConfigurationMachineId;
    internal event EventHandler? PresentationChanged;

    internal Task RequestSaveFoldersAsync() => SaveFoldersAsync();
    internal Task RequestBrowseFolderAsync(TextBox folder) => BrowseFolderAsync(folder);
    internal Task RequestEditConfigurationAsync(EmulationConfigurationTableRow row) => EditConfigurationAsync(row);
    internal Task RequestDeleteConfigurationAsync(EmulationConfigurationTableRow row) => DeleteConfigurationAsync(row);

    internal Task EnsureConfigurationsAsync()
    {
        if (_configurationsLoaded) return Task.CompletedTask;
        if (_loadingConfigurations) return _configurationLoadTask ?? Task.CompletedTask;
        return _configurationLoadTask = LoadInitialConfigurationsAsync();
    }

    internal Task WaitForConfigurationsAsync() => _configurationLoadTask ?? Task.CompletedTask;

    private async Task LoadInitialConfigurationsAsync()
    {
        _loadingConfigurations = true;
        try
        {
            await ReloadConfigurationsAsync();
            _configurationsLoaded = true;
        }
        finally { _loadingConfigurations = false; }
    }

    internal void SelectModule(IEmulationModule module)
    {
        _selectedConfigurationModule = module;
        _selectedConfigurationMachineId = null;
        RebuildConfigurationBrands();
        FilterConfigurationTable();
    }

    internal void SelectMachine(string? machineId)
    {
        _selectedConfigurationMachineId = machineId;
        RebuildConfigurationBrands();
        FilterConfigurationTable();
    }

    public void Configure(AppSettings settings, Func<Task> persistSettings)
    {
        _settings = settings;
        _persistSettings = persistSettings;
        _storageFolder.Text = settings.EmulationStorageFolder;
        _captureFolder.Text = settings.EmulationCaptureFolder;
        _stateFolder.Text = settings.EmulationStateFolder;
        _shortcuts.SetRows(GlobalShortcutDefinitions(), settings.EmulationShortcuts);
        StoragePaths.ConfigureEmulationStorageDirectory(settings.EmulationStorageFolder);
        StoragePaths.ConfigureEmulationCaptureDirectory(settings.EmulationCaptureFolder);
        StoragePaths.ConfigureEmulationStateDirectory(settings.EmulationStateFolder);
    }

    internal Task ReloadConfigurationsForWindowAsync() => ReloadConfigurationsAsync();

    private async Task LoadConfigurationsWhenVisibleAsync()
    {
        if (IsLoaded && IsVisible) await EnsureConfigurationsAsync();
    }

    internal static void RaiseConfigurationSaved(object sender, EmulationConfigurationSavedEventArgs args) => ConfigurationSaved?.Invoke(sender, args);

    internal static void RaiseVideoConfigurationChanged(object sender, EmulationConfigurationSavedEventArgs args) => VideoConfigurationChanged?.Invoke(sender, args);

    internal void RefreshLocalizedContent()
    {
        var shortcutValues = _shortcuts.Rows.ToDictionary(row => row.Id, row => row.Binding, StringComparer.Ordinal);
        foreach (var (tab, resourceKey) in _localizedTabs)
        {
            var text = LocExtension.Get(resourceKey);
            if (tab.Header is MainTabHeader header) header.Text = text;
        }
        _shortcuts.SetRows(GlobalShortcutDefinitions(), shortcutValues);
        _shortcuts.ConfigurePresentation(LocExtension.Get("Emulation.Input.Actions"), LocExtension.Get("Emulation.Input.Binding.Search"));
        _configurationTable.RefreshLocalizedContent();
        _network.RefreshLocalizedContent();
        if (_configurationsLoaded)
        {
            _configurationRows = EmulationConfigurationTablePresenter.CreateRows(_configurationRows.Select(row => (row.Module, row.Configuration)));
            RebuildConfigurationBrands();
            FilterConfigurationTable();
            _ = ReloadConfigurationsAsync();
        }
    }

}
