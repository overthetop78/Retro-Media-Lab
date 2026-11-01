using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Input;

internal sealed record KeyboardShortcutBinding(KeyboardChord Chord, EmulationKey EmulationKey);
