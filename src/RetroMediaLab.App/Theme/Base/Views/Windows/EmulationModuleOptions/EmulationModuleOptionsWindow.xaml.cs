using System.Windows;
using System.ComponentModel;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Windows.EmulationModuleOptions;

public partial class EmulationModuleOptionsWindow : Window
{
    private readonly IEmulationModule _module;
    private readonly EmulationModuleSettingsSection _section;
    private readonly IEmulationConfiguration? _initialConfiguration;
    private Task? _initialization;
    private Task? _cleanup;
    private bool _cleanupStarted;
    private bool _cleanupCompleted;
    internal EmulationModuleSettingsSection PresentationSection => _section;
    internal Task PresentationOperation => _cleanup ?? _initialization ?? Task.CompletedTask;
    internal void RequestPresentationClose() => Close();
    internal Task InitializePresentationAsync() => _initialization ??= LoadInitialConfigurationAsync();

    public EmulationModuleOptionsWindow(IEmulationModule module, IEmulationConfiguration? initialConfiguration = null, Action<Exception>? showError = null, RetroMediaLab.VideoPresentation.Services.VideoPresentationProfileStore? profiles = null, IEnumerable<string>? reservedBindings = null)
    {
        InitializeComponent();
        _module = module;
        _initialConfiguration = initialConfiguration;
        _section = new EmulationModuleSettingsSection(module, profiles, showError, reservedBindings);
        _section.ConfigurationSaved += ConfigurationSaved;
        _section.VideoConfigurationChanged += VideoConfigurationChanged;
        EmulationVideoShaderLoadingStatus.Changed += VideoShaderLoadingChanged;
        ModuleContent.Content = _section;
        ContentRendered += LoadInitialConfiguration;
        Closing += WindowClosing;
        RefreshLocalizedContent();
    }

    internal void RefreshLocalizedContent()
    {
        Title = LocExtension.Get("Options.EmulationModuleTitle", LocExtension.GetForModule(_module, _module.DisplayResourceKey));
        _section.RefreshLocalizedContent();
    }

    private async void LoadInitialConfiguration(object? sender, EventArgs e) => await InitializePresentationAsync();

    private async Task LoadInitialConfigurationAsync()
    {
        await _section.EnsureInitializedAsync();
        if (_cleanupStarted || _initialConfiguration is null) return;
        await _section.EditConfigurationAsync(_initialConfiguration);
    }

    private static void ConfigurationSaved(object? sender, EmulationConfigurationSavedEventArgs args) => EmulationPreferencesSection.RaiseConfigurationSaved(sender ?? typeof(EmulationModuleOptionsWindow), args);

    private static void VideoConfigurationChanged(object? sender, EmulationConfigurationSavedEventArgs args) => EmulationPreferencesSection.RaiseVideoConfigurationChanged(sender ?? typeof(EmulationModuleOptionsWindow), args);

    private void VideoShaderLoadingChanged(object? sender, EmulationVideoShaderLoadingChangedEventArgs args) => _section.SetVideoShaderLoading(args.ModuleId, args.ConfigurationId, args.IsLoading);

    private void WindowClosing(object? sender, CancelEventArgs args)
    {
        if (_cleanupCompleted) return;
        args.Cancel = true;
        if (_cleanupStarted) return;
        _cleanupStarted = true;
        _cleanup = CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        try
        {
            try { if (_initialization is not null) await _initialization; }
            finally { await _section.DisposeAsync(); }
        }
        catch (Exception error)
        {
            ErrorLog.Write(error, "Closing the emulation module options window");
        }
        finally
        {
            EmulationVideoShaderLoadingStatus.Changed -= VideoShaderLoadingChanged;
            _section.ConfigurationSaved -= ConfigurationSaved;
            _section.VideoConfigurationChanged -= VideoConfigurationChanged;
            ContentRendered -= LoadInitialConfiguration;
            ModuleContent.Content = null;
            Closing -= WindowClosing;
            _cleanupCompleted = true;
            _cleanupStarted = false;
            await Dispatcher.BeginInvoke(Close);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
