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
    public const string OsdWithPause = "osd-with-pause";
    public const string OsdWithoutPause = "osd-without-pause";
    public const string OsdStatistics = "osd-statistics";
    public const string OsdUp = "osd-up";
    public const string OsdDown = "osd-down";
    public const string OsdLeft = "osd-left";
    public const string OsdRight = "osd-right";
    public const string OsdConfirm = "osd-confirm";
    public const string OsdCancel = "osd-cancel";
    public const string AlternativeSuffix = "-alternative";

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

    public static IReadOnlyDictionary<string, string> OsdValues { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [OsdWithPause] = "Ctrl+Shift+Divide",
        [OsdWithPause + AlternativeSuffix] = "",
        [OsdWithoutPause] = "Ctrl+Shift+Multiply",
        [OsdWithoutPause + AlternativeSuffix] = "",
        [OsdStatistics] = "Ctrl+Shift+Subtract",
        [OsdUp] = "Up",
        [OsdUp + AlternativeSuffix] = "",
        [OsdDown] = "Down",
        [OsdDown + AlternativeSuffix] = "",
        [OsdLeft] = "Left",
        [OsdLeft + AlternativeSuffix] = "",
        [OsdRight] = "Right",
        [OsdRight + AlternativeSuffix] = "",
        [OsdConfirm] = "Enter",
        [OsdConfirm + AlternativeSuffix] = "",
        [OsdCancel] = "Escape",
        [OsdCancel + AlternativeSuffix] = ""
    };
}
