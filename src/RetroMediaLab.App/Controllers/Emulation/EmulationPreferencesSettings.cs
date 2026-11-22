using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Controllers.Emulation;

internal sealed class EmulationPreferencesSettings
{
    private AppSettings? _settings;
    private Func<Task>? _persist;

    internal void Configure(AppSettings settings, Func<Task> persist)
    {
        _settings = settings;
        _persist = persist;
        StoragePaths.ConfigureEmulationDirectories(settings);
    }

    internal Task SaveFoldersAsync(string storage, string captures, string states)
    {
        if (_settings is not { } settings) return Task.CompletedTask;
        settings.EmulationStorageFolder = storage;
        settings.EmulationCaptureFolder = captures;
        settings.EmulationStateFolder = states;
        StoragePaths.ConfigureEmulationDirectories(settings);
        return _persist?.Invoke() ?? Task.CompletedTask;
    }

    internal Task SaveShortcutsAsync(IReadOnlyList<InputBindingRow> rows, bool hasErrors)
    {
        if (_settings is not { } settings || hasErrors) return Task.CompletedTask;
        settings.EmulationShortcuts = rows.ToDictionary(row => row.Id, row => row.Binding, StringComparer.Ordinal);
        return _persist?.Invoke() ?? Task.CompletedTask;
    }
}
