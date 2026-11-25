using RetroMediaLab.App.Constants.Emulation.Shortcuts;
using RetroMediaLab.App.Contracts.Emulation.Shortcuts;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Input.Bindings;
using System.Windows.Input;

namespace RetroMediaLab.App.Functions.Emulation.Shortcuts;

internal static class EmulationShortcutFunctions
{
    internal static EmulationShortcutMatch ResolveGlobal(IReadOnlyList<GlobalShortcutBinding> bindings, ModifierKeys modifiers,
        IReadOnlySet<Key> pressedKeys, Key source, bool allowAdditionalKeys = false)
    {
        var exact = bindings.FirstOrDefault(binding => binding.SourceBinding is null && KeyboardChordFunctions.Matches(binding.Chord, modifiers, pressedKeys));
        if (exact is not null) return new(EmulationShortcutMatchCategory.Global, exact);
        var matching = bindings.Where(binding => binding.SourceBinding is null && UsesKey(binding.Chord, source)
            && (allowAdditionalKeys || EmulationShortcutMap.IsVolumeShortcut(binding.Action))
            && MatchesWithAdditionalKeys(binding.Chord, modifiers, pressedKeys))
            .MaxBy(binding => binding.Chord.Keys.Count);
        if (matching is not null) return new(EmulationShortcutMatchCategory.Global, matching);
        return bindings.Any(binding => binding.SourceBinding is null && binding.Chord.Modifiers == modifiers && KeyboardChordFunctions.Contains(binding.Chord, source)) ? new EmulationShortcutMatch(EmulationShortcutMatchCategory.ReservedForGlobal) : new EmulationShortcutMatch(EmulationShortcutMatchCategory.None);
    }

    private static bool MatchesWithAdditionalKeys(KeyboardChord chord, ModifierKeys modifiers, IReadOnlySet<Key> pressedKeys) =>
        chord.Modifiers == modifiers && chord.Keys.All(pressedKeys.Contains);

    internal static bool UsesKey(KeyboardChord chord, Key key)
    {
        var modifier = KeyboardChordFunctions.ModifierForKey(key);
        return KeyboardChordFunctions.Contains(chord, key)
            || (modifier != ModifierKeys.None && chord.Modifiers.HasFlag(modifier));
    }

    internal static void ReleaseInactive(ISet<string> activeActions, IReadOnlyList<GlobalShortcutBinding> bindings, ModifierKeys modifiers, IReadOnlySet<Key> pressedKeys, bool allowAdditionalKeys = false)
    {
        activeActions.RemoveWhere(action =>
        {
            var binding = bindings.FirstOrDefault(item => item.Action == action);
            return binding is null || binding.SourceBinding is null && !(allowAdditionalKeys || EmulationShortcutMap.IsVolumeShortcut(binding.Action)
                ? MatchesWithAdditionalKeys(binding.Chord, modifiers, pressedKeys)
                : KeyboardChordFunctions.Matches(binding.Chord, modifiers, pressedKeys));
        });
    }

    internal static bool HasConflict(IEnumerable<GlobalShortcutBinding> bindings) => bindings.GroupBy(binding => binding.SourceBinding ?? KeyboardChordFunctions.Format(binding.Chord.Modifiers, binding.Chord.Keys), StringComparer.OrdinalIgnoreCase)
        .Any(group => group.Skip(EmulationShortcutConstants.UniqueChordBindingCount).Any());

    private static void RemoveWhere(this ISet<string> values, Func<string, bool> predicate)
    {
        foreach (var value in values.Where(predicate).ToArray()) values.Remove(value);
    }
}
