using RetroMediaLab.App.Services.Settings;
using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using RetroMediaLab.App.Contracts.Emulation.Machine;
using RetroMediaLab.App.Functions.Localization;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.Infrastructure.Settings;
using System.ComponentModel;
using System.Windows;
using RetroMediaLab.App.Views.Windows.EmulationModuleOptions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Windows.EmulationPreferences;

public partial class EmulationPreferencesWindow : Window
{
    internal EmulationPreferencesSection PresentationSection => EmulationSection;
    internal void RequestPresentationClose()
    {
        if (!_closingAfterSave) BeginClose();
    }

    private readonly AppSettings _settings;
    private readonly SettingsSaveQueue _settingsSaves;
    private readonly Action<Exception> _reportSaveError;
    private bool _initializing = true;
    private bool _closingAfterSave;
    private bool _closeInProgress;
    public EmulationPreferencesWindow(AppSettings settings, ISettingsStore? settingsStore = null, string? dataDirectory = null, Action<Exception>? reportSaveError = null)
    {
        InitializeComponent();
        _settings = settings;
        _settingsSaves = new SettingsSaveQueue(settingsStore ?? StoragePaths.OpenSettingsStore(dataDirectory ?? StoragePaths.DataDirectory));
        _reportSaveError = reportSaveError ?? (error => ShowLoggedError(error, "Saving Emulation options"));
        EmulationSection.EditConfigurationRequested += async (module, configuration) =>
        {
            var window = new EmulationModuleOptionsWindow(module, configuration,
                reservedBindings: EmulationShortcutMap.GlobalBindingValues(_settings.EmulationShortcuts));
            RetroMediaLab.App.Services.Windows.WpfWindowNavigationService.ShowOwnedDialog(this, window);
            await EmulationSection.ReloadConfigurationsForWindowAsync();
        };
        EmulationSection.Configure(settings, SaveFromEditorAsync);
        _initializing = false;
        UpdateTitle();
    }

    internal void RefreshLocalizedContent()
    {
        EmulationSection.RefreshLocalizedContent();
        UpdateTitle();
    }

    internal async Task PersistSettingsAsync()
    {
        if (_initializing) return;
        await _settingsSaves.SaveAsync(_settings).ConfigureAwait(false);
    }

    internal async Task SaveFromEditorAsync()
    {
        try { await PersistSettingsAsync(); }
        catch (Exception exception) { _reportSaveError(exception); }
    }

    private void UpdateTitle()
    {
        Title = LocExtension.Get("Options.EmulationTitle");
    }

    private void ShowLoggedError(Exception exception, string context)
    {
        ErrorLog.Write(exception, context);
        var detail = ExceptionDescriptionFunctions.Describe(exception);
        WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Error.Unexpected", detail), LocExtension.Get("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => BeginClose();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_initializing || _closingAfterSave) return;
        e.Cancel = true;
        BeginClose();
    }

    private void BeginClose()
    {
        if (_closeInProgress) return;
        _closeInProgress = true;
        _ = SaveAndCloseAsync();
    }

    private async Task SaveAndCloseAsync()
    {
        try { await EmulationSection.WaitForConfigurationsAsync().ConfigureAwait(false); }
        catch (Exception error) { ErrorLog.Write(error, "Finishing configuration loading while closing Emulation options"); }
        try { await PersistSettingsAsync().ConfigureAwait(false); }
        catch (Exception exception)
        {
            ErrorLog.Write(exception, "Saving Emulation options while closing");
            var detail = ExceptionDescriptionFunctions.Describe(exception);
            try
            {
                if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (IsLoaded)
                            WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Error.SaveFailed", detail), LocExtension.Get("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }).Task.ConfigureAwait(false);
            }
            catch (Exception dialogException)
            {
                ErrorLog.Write(dialogException, "Displaying Emulation options save error");
            }
        }

        try
        {
            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                await Dispatcher.InvokeAsync(() =>
                {
                    if (_closingAfterSave) return;
                    _closingAfterSave = true;
                    _closeInProgress = false;
                    Close();
                }).Task.ConfigureAwait(false);
            else
            {
                _closingAfterSave = true;
                _closeInProgress = false;
            }
        }
        catch (Exception closeException)
        {
            _closingAfterSave = true;
            _closeInProgress = false;
            ErrorLog.Write(closeException, "Closing Emulation options after save");
        }
    }
}

