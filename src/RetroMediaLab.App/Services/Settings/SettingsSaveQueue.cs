using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Services.Settings;

internal sealed class SettingsSaveQueue(ISettingsStore store)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    internal async Task SaveAsync(AppSettings settings)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { await store.SaveAsync(settings).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }
}
