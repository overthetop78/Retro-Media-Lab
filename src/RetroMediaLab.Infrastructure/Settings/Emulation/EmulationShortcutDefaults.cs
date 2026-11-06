namespace RetroMediaLab.Infrastructure.Settings.Emulation;

public static class EmulationShortcutDefaults
{
    public const string ReleaseMouse = "release-mouse";
    public const string PauseResume = "pause-resume";
    public const string ToggleFullscreen = "toggle-fullscreen";
    public const string Power = "power";
    public const string SoftReset = "soft-reset";
    public const string HardReset = "hard-reset";
    public const string QuickSave = "quick-save";
    public const string QuickLoad = "quick-load";
    public const string Screenshot = "screenshot";
    public const string ToggleMute = "toggle-mute";
    public const string FastForward = "fast-forward";
    public const string InsertMedia = "insert-media";
    public const string EjectMedia = "eject-media";
    public const string NextMedia = "next-media";
    public const string OpenSettings = "open-machine-settings";

    public static IReadOnlyDictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ReleaseMouse] = "Ctrl+Shift+F12",
        [PauseResume] = "Ctrl+Shift+F2",
        [ToggleFullscreen] = "Ctrl+Shift+F11",
        [Power] = "Ctrl+Shift+F1",
        [SoftReset] = "Ctrl+Shift+F3",
        [HardReset] = "Ctrl+Shift+F4",
        [QuickSave] = "Ctrl+Shift+F6",
        [QuickLoad] = "Ctrl+Shift+F7",
        [Screenshot] = "Ctrl+Shift+F9",
        [ToggleMute] = "Ctrl+Shift+M",
        [OpenSettings] = "Ctrl+Shift+F5"
    };
}
