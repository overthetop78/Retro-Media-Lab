namespace RetroMediaLab.App.Contracts.Input;

internal sealed record GlobalShortcutBinding(string Action, KeyboardChord Chord, string? SourceBinding = null);
