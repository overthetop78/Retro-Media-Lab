using System.Windows.Input;

namespace RetroMediaLab.App.Contracts.Input;

public sealed record KeyboardChord(ModifierKeys Modifiers, IReadOnlyList<Key> Keys);
