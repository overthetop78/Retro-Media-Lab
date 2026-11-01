using RetroMediaLab.Infrastructure.Commands.Building;
using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.Infrastructure.HostTools;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.App.Contracts.Services.Navigation;
using RetroMediaLab.App.Enums.Services.Navigation;
using RetroMediaLab.App.Interfaces.Services.Navigation;
using RetroMediaLab.App.Views.Windows.About;
using RetroMediaLab.App.Views.Windows.Logs;
using RetroMediaLab.App.Views.Windows.EmulationPreferences;
using RetroMediaLab.App.Views.Windows.EmulationModuleOptions;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Views.Windows.Preferences;
using RetroMediaLab.App.Views.Windows.Updates;
using RetroMediaLab.App.Views.Windows.Tools;
using System.Windows;
using System.Windows.Threading;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Infrastructure.Hardware;
using RetroMediaLab.Infrastructure.Processes;
using RetroMediaLab.App.Services.Updates;

namespace RetroMediaLab.App.Services.Windows;

public sealed class WpfWindowNavigationService : IWindowNavigationService
{
    private readonly Window _owner;
    private readonly Func<AppSettings, PreferencesSection, Window> _preferences;
    private readonly Func<AppSettings, Window> _emulationPreferences;
    private readonly Func<AppSettings, string, Window> _emulationModuleOptions;
    private readonly Func<Window> _updates;
    private readonly Func<string, Window> _logs;
    private readonly Func<Window> _about;
    private readonly Func<GwToolWindowRequest, Window> _tool;
    private readonly Func<Window, Window, bool?> _show;

    public WpfWindowNavigationService(Window owner, IGwInstallationManager? hostTools = null, IGreaseweazleRunner? runner = null, IGwCommandBuilder? commandBuilder = null)
        : this(owner, hostTools, runner, commandBuilder, new PendingModuleInstallationStore())
    {
    }

    internal WpfWindowNavigationService(Window owner, IGwInstallationManager? hostTools, IGreaseweazleRunner? runner, IGwCommandBuilder? commandBuilder, PendingModuleInstallationStore pendingInstallations)
    {
        _owner = owner;
        var commandRunner = runner ?? new GreaseweazleRunner();
        var commands = commandBuilder ?? new GwCommandBuilder();
        _preferences = (settings, section) => new PreferencesWindow(settings, new GreaseweazleHardwareRegistry(new WindowsSerialDeviceDiscovery(), commandRunner, commands), hostTools, section);
        _emulationPreferences = settings => new EmulationPreferencesWindow(settings);
        _emulationModuleOptions = (settings, moduleId) =>
        {
            var module = EmulationModuleRegistry.Modules.FirstOrDefault(candidate => string.Equals(candidate.Id, moduleId, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"Emulation module '{moduleId}' is not loaded.");
            return new EmulationModuleOptionsWindow(module, reservedBindings: settings.EmulationShortcuts.Values);
        };
        _updates = () => new UpdatesWindow(pendingInstallations);
        _logs = directory => new LogHistoryWindow(directory);
        _about = () => new AboutWindow();
        _tool = request => new GwToolWindow(request.Executable, request.Verb, request.Device, request.Drive, commandRunner, commands, new ConsoleLogSession(request.LogsDirectory, () => request.Logging));
        _show = (window, dialogOwner) =>
        {
            window.Owner = dialogOwner;
            return window.ShowDialog();
        };
    }

    internal WpfWindowNavigationService(Window owner,
        Func<AppSettings, PreferencesSection, Window> preferences,
        Func<AppSettings, Window> emulationPreferences,
        Func<AppSettings, string, Window> emulationModuleOptions,
        Func<Window> updates, Func<string, Window> logs,
        Func<Window> about, Func<GwToolWindowRequest, Window> tool, Func<Window, Window, bool?> show)
    {
        _owner = owner;
        _preferences = preferences;
        _emulationPreferences = emulationPreferences;
        _emulationModuleOptions = emulationModuleOptions;
        _updates = updates;
        _logs = logs;
        _about = about;
        _tool = tool;
        _show = show;
    }

    private void Show(Window window)
    {
        if (TryShowThemeWindow(_owner, window)) return;
        _show(window, _owner);
    }

    internal static bool TryShowThemeWindow(Window owner, Window window)
    {
        for (Window? current = owner; current is not null; current = RetroMediaLab.App.Theme.Common.ThemeDialogCompletion.Owner(current) ?? current.Owner)
            if (current is RetroMediaLab.App.Views.Windows.Shell.MainWindow main && main.TryShowThemeWindow(window)) return true;
        return false;
    }

    internal static bool? ShowOwnedDialog(Window? owner, Window window)
    {
        owner ??= window.Owner ?? Application.Current.MainWindow;
        using var ownership = owner is null ? null : RetroMediaLab.App.Theme.Common.ThemeDialogCompletion.AttachOwner(window, owner);
        try
        {
            if (owner is not null && TryShowThemeWindow(owner, window)) return RetroMediaLab.App.Theme.Common.ThemeDialogCompletion.Result(window);
            Window? nativeOwner = owner;
            while (nativeOwner is not null && !nativeOwner.IsVisible)
                nativeOwner = RetroMediaLab.App.Theme.Common.ThemeDialogCompletion.Owner(nativeOwner) ?? nativeOwner.Owner;
            window.Owner = nativeOwner;
            return window.ShowDialog();
        }
        finally { CloseOwnedDialog(window); }
    }

    internal static void CloseOwnedDialog(Window window)
    {
        if (window is GwToolWindow closingTool) closingTool.RequestPresentationClose();
        if (window is RetroMediaLab.App.Views.Windows.Conversion.FileMigrationWindow closingMigration) closingMigration.RequestPresentationClose();
        if (window is UpdatesWindow closingUpdates) closingUpdates.RequestPresentationClose();
        var pending = window switch
        {
            MediaCreationDialog creation => creation.PresentationOperation,
            HardDiskDriveConfigurationDialog disk => disk.PresentationOperation,
            RetroMediaLab.App.Views.Dialogs.Common.FileSelectionDialog file => file.PresentationOperation,
            EmulationModuleOptionsWindow module => module.PresentationOperation,
            LogHistoryWindow logs => logs.PresentationOperation,
            GwToolWindow tool => tool.PresentationOperation,
            UpdatesWindow updates => updates.PresentationOperation,
            RetroMediaLab.App.Views.Windows.Conversion.FileMigrationWindow migration => migration.PresentationOperation,
            _ => Task.CompletedTask
        };
        try
        {
            if (!pending.IsCompleted)
            {
                var loop = new DispatcherFrame();
                var notification = pending.ContinueWith(_ => window.Dispatcher.Invoke(() => loop.Continue = false),
                    CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                Dispatcher.PushFrame(loop);
                notification.GetAwaiter().GetResult();
            }
            pending.GetAwaiter().GetResult();
        }
        finally { window.Close(); }
    }

    public bool ShowPreferences(AppSettings settings, PreferencesSection section = PreferencesSection.General)
    {
        Show(_preferences(settings, section));
        return true;
    }
    public void ShowEmulationPreferences(AppSettings settings) => Show(_emulationPreferences(settings));
    public void ShowEmulationModuleOptions(AppSettings settings, string moduleId) => Show(_emulationModuleOptions(settings, moduleId));
    public void ShowUpdates() => Show(_updates());
    public void ShowLogHistory(string logsDirectory) => Show(_logs(logsDirectory));
    public void ShowAbout() => Show(_about());
    public void ShowGwTool(GwToolWindowRequest request)
    {
        Show(_tool(request));
    }
}
