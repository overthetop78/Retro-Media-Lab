using RetroMediaLab.App.Contracts.Emulation.Configurations;

namespace RetroMediaLab.App.Services.Emulation;

internal static class EmulationConfigurationNotifications
{
    internal static event EventHandler<EmulationConfigurationSavedEventArgs>? ConfigurationSaved;
    internal static event EventHandler<EmulationConfigurationSavedEventArgs>? VideoConfigurationChanged;
    internal static event Func<Task>? ConfigurationDeleted;

    internal static void NotifySaved(object? sender, EmulationConfigurationSavedEventArgs args) =>
        ConfigurationSaved?.Invoke(sender, args);

    internal static void NotifyVideoChanged(object? sender, EmulationConfigurationSavedEventArgs args) =>
        VideoConfigurationChanged?.Invoke(sender, args);

    internal static async Task NotifyDeletedAsync()
    {
        foreach (Func<Task> refresh in ConfigurationDeleted?.GetInvocationList() ?? [])
            await refresh();
    }
}
