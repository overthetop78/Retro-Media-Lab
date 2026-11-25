using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.Emulation;
using RetroMediaLab.Infrastructure.Settings.Emulation;

namespace RetroMediaLab.App.Functions.Emulation.Shortcuts;

internal static class EmulationShortcutMap
{
    internal static IReadOnlyDictionary<EmulationKey, EmulationKey> KeyboardMap(IReadOnlyDictionary<string, EmulationKey>? mappings)
    {
        if (mappings is null || mappings.Count == 0) return new Dictionary<EmulationKey, EmulationKey>();
        var result = new Dictionary<EmulationKey, EmulationKey>();
        foreach (var mapping in mappings)
            if (Enum.TryParse<EmulationKey>(mapping.Key, true, out var emulationKey) &&
                mapping.Value != EmulationKey.Unknown)
                result[mapping.Value] = emulationKey;
        return result;
    }

    internal static IReadOnlyList<KeyboardShortcutBinding> KeyboardShortcuts(IReadOnlyDictionary<string, string>? mappings)
    {
        if (mappings is null || mappings.Count == 0) return [];
        var result = new List<KeyboardShortcutBinding>();
        foreach (var mapping in mappings)
        {
            if (Enum.TryParse<EmulationKey>(mapping.Key, true, out var emulationKey) &&
                KeyboardChordFunctions.TryParse(mapping.Value, out var chord) && !KeyboardChordFunctions.IsWindowsReserved(chord))
                result.Add(new KeyboardShortcutBinding(chord, emulationKey));
        }
        return result;
    }

    internal static IReadOnlyList<GlobalShortcutBinding> GlobalShortcuts(IReadOnlyDictionary<string, string>? mappings) =>
        ParseShortcuts(mappings?.Where(IsGlobalShortcut));

    internal static string[] GlobalBindingValues(IReadOnlyDictionary<string, string> mappings) =>
        mappings.Where(IsGlobalShortcut).Select(mapping => mapping.Value).ToArray();

    internal static IReadOnlyList<GlobalShortcutBinding> OsdShortcuts(IReadOnlyDictionary<string, string>? mappings) =>
        ParseShortcuts(mappings?.Where(mapping => EmulationShortcutDefaults.OsdValues.ContainsKey(mapping.Key)
            || mapping.Key == EmulationShortcutDefaults.ToggleMute || IsVolumeShortcut(mapping.Key)));

    internal static bool IsVolumeShortcut(string action) =>
        action is EmulationShortcutDefaults.VolumeDown or EmulationShortcutDefaults.VolumeUp;

    internal static bool IsOsdToggle(string action) =>
        OsdPrimaryAction(action) is EmulationShortcutDefaults.OsdWithPause or EmulationShortcutDefaults.OsdWithoutPause
            or EmulationShortcutDefaults.OsdStatistics;

    internal static string OsdPrimaryAction(string action) =>
        action.EndsWith(EmulationShortcutDefaults.AlternativeSuffix, StringComparison.Ordinal)
            ? action[..^EmulationShortcutDefaults.AlternativeSuffix.Length]
            : action;

    private static bool IsGlobalShortcut(KeyValuePair<string, string> mapping) =>
        !EmulationShortcutDefaults.OsdValues.ContainsKey(mapping.Key);

    private static IReadOnlyList<GlobalShortcutBinding> ParseShortcuts(IEnumerable<KeyValuePair<string, string>>? mappings) => mappings is null
        ? []
        : mappings.Select(mapping =>
        {
            var hasKeyboard = InputBindingSyntax.TryGetKeyboardChord(mapping.Value, out var chord);
            if (hasKeyboard && KeyboardChordFunctions.IsWindowsReserved(chord)) return null;
            if (InputBindingSyntax.HasNonKeyboardSource(mapping.Value))
                return EmulationSdk.InputProcessing.IsValidBinding(mapping.Value) ? new GlobalShortcutBinding(mapping.Key, chord, mapping.Value) : null;
            return hasKeyboard ? new GlobalShortcutBinding(mapping.Key, chord) : null;
        }).Where(binding => binding is not null).Cast<GlobalShortcutBinding>().ToArray();

    internal static IReadOnlyList<InputBindingDefinition> GlobalShortcutDefinitions() =>
    [
        Shortcut(EmulationShortcutDefaults.OpenSettings, RetroMediaLab.App.Constants.Localization.EmulationResourceKeys.MachineSettings),
        Shortcut(EmulationShortcutDefaults.ReleaseMouse, "Emulation.Shortcut.ReleaseMouse"),
        Shortcut(EmulationShortcutDefaults.PauseResume, "Emulation.Shortcut.PauseResume"),
        Shortcut(EmulationShortcutDefaults.ToggleFullscreen, "Emulation.Shortcut.Fullscreen"),
        Shortcut(EmulationShortcutDefaults.Power, "Emulation.Shortcut.Power"),
        Shortcut(EmulationShortcutDefaults.SoftReset, "Emulation.Shortcut.SoftReset"),
        Shortcut(EmulationShortcutDefaults.HardReset, "Emulation.Shortcut.HardReset"),
        Shortcut(EmulationShortcutDefaults.QuickSave, "Emulation.Shortcut.QuickSave"),
        Shortcut(EmulationShortcutDefaults.QuickLoad, "Emulation.Shortcut.QuickLoad"),
        Shortcut(EmulationShortcutDefaults.Screenshot, "Emulation.Shortcut.Screenshot"),
        Shortcut(EmulationShortcutDefaults.ToggleMute, "Emulation.Shortcut.Mute"),
        Shortcut(EmulationShortcutDefaults.VolumeDown, "Emulation.Shortcut.VolumeDown"),
        Shortcut(EmulationShortcutDefaults.VolumeUp, "Emulation.Shortcut.VolumeUp"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdWithPause, "OsdWithPause"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdWithoutPause, "OsdWithoutPause"),
        new(EmulationShortcutDefaults.OsdStatistics, "Emulation.Shortcut.OsdStatistics",
            EmulationShortcutDefaults.OsdValues[EmulationShortcutDefaults.OsdStatistics]),
        .. OsdShortcut(EmulationShortcutDefaults.OsdUp, "OsdUp"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdDown, "OsdDown"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdLeft, "OsdLeft"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdRight, "OsdRight"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdConfirm, "OsdConfirm"),
        .. OsdShortcut(EmulationShortcutDefaults.OsdCancel, "OsdCancel")
    ];

    private static InputBindingDefinition Shortcut(string id, string resourceKey) => new(id, resourceKey, EmulationShortcutDefaults.Values[id]);

    private static InputBindingDefinition[] OsdShortcut(string id, string resourceSuffix)
    {
        var alternative = id + EmulationShortcutDefaults.AlternativeSuffix;
        return
        [
            new(id, "Emulation.Shortcut." + resourceSuffix, EmulationShortcutDefaults.OsdValues[id]),
            new(alternative, "Emulation.Shortcut." + resourceSuffix + "Alternative", EmulationShortcutDefaults.OsdValues[alternative])
        ];
    }

    internal static string ResolveShortcutText(IReadOnlyList<GlobalShortcutBinding> configuredShortcuts, string action)
    {
        var configured = configuredShortcuts.FirstOrDefault(item => item.Action == action);
        if (configured?.SourceBinding is { } sourceBinding) return sourceBinding;
        var binding = configured?.Chord;
        if (binding is null && EmulationShortcutDefaults.Values.TryGetValue(action, out var fallback))
            KeyboardChordFunctions.TryParse(fallback, out binding);
        return binding is null ? string.Empty : KeyboardChordFunctions.Format(binding.Modifiers, binding.Keys);
    }

    internal static string? CommandShortcut(MachineCommandId id) => id switch
    {
        MachineCommandId.Power => EmulationShortcutDefaults.Power,
        MachineCommandId.PauseResume => EmulationShortcutDefaults.PauseResume,
        MachineCommandId.SoftReset => EmulationShortcutDefaults.SoftReset,
        MachineCommandId.HardReset => EmulationShortcutDefaults.HardReset,
        MachineCommandId.QuickSave => EmulationShortcutDefaults.QuickSave,
        MachineCommandId.QuickLoad => EmulationShortcutDefaults.QuickLoad,
        MachineCommandId.CaptureScreen => EmulationShortcutDefaults.Screenshot,
        MachineCommandId.Fullscreen => EmulationShortcutDefaults.ToggleFullscreen,
        MachineCommandId.Audio => EmulationShortcutDefaults.ToggleMute,
        MachineCommandId.CoreOptions => EmulationShortcutDefaults.OpenSettings,
        _ => null
    };
}
