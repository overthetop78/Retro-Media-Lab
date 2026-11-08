using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

/// <summary>Contacts Atom issus des entrées hôtes ; niveaux conservés jusqu'au prochain changement.</summary>
internal sealed class AtomKeyboard
{
    private readonly byte[] _pins = new byte[C.KeyboardColumnMask + 1];
    private int _column;

    internal AtomKeyboard() => Reset();
    internal bool Break { get; private set; }
    internal bool Repeat { get; private set; }
    internal void Select(byte pins) => _column = pins & C.KeyboardColumnMask;
    internal void SetInput(EmulationInputSnapshot input)
    {
        var contacts = new HashSet<EmulationKey>();
        bool? characterShift = null;
        foreach (var key in input.Keys)
        {
            var hasCharacter = input.HostCharacters?.TryGetValue(key, out _) == true;
            var character = hasCharacter ? input.HostCharacters![key] : AtomKeyboardMap.KeypadCharacter(key);
            // Comme sur le BBC, les lettres suivent leur identité hôte ; LOCK reste traité par la ROM.
            if (hasCharacter && key is >= EmulationKey.A and <= EmulationKey.Z
                && char.ToUpperInvariant(character) == key.ToString()[0])
            {
                contacts.Add(key);
                continue;
            }
            if (hasCharacter || character != '\0')
            {
                if (AtomKeyboardMap.Characters.TryGetValue(character, out var contact))
                {
                    if (character == ' ') { contacts.Add(contact.Key); continue; }
                    if (characterShift.HasValue && characterShift != contact.Shift) continue;
                    characterShift = contact.Shift;
                    contacts.Add(contact.Key);
                }
                continue;
            }
            contacts.Add(key == EmulationKey.NumpadEnter ? EmulationKey.Return
                : key == EmulationKey.Backspace ? EmulationKey.Delete : key);
        }
        var modifiers = byte.MaxValue;
        if (contacts.Contains(EmulationKey.LeftControl)) modifiers &= unchecked((byte)~C.KeyboardControl);
        var shift = characterShift ?? (contacts.Contains(EmulationKey.LeftShift) || contacts.Contains(EmulationKey.RightShift));
        if (shift || contacts.Contains(EmulationKey.Down) || contacts.Contains(EmulationKey.Left))
            modifiers &= unchecked((byte)~C.KeyboardShift);
        Array.Fill(_pins, modifiers);
        for (var column = 0; column < AtomKeyboardMap.Columns.Count; column++)
        {
            var keys = AtomKeyboardMap.Columns[column];
            for (var row = 0; row < keys.Length; row++)
                if (keys[row] != EmulationKey.Unknown && (contacts.Contains(keys[row])
                    || keys[row] == EmulationKey.Up && contacts.Contains(EmulationKey.Down)
                    || keys[row] == EmulationKey.Right && contacts.Contains(EmulationKey.Left)))
                    _pins[column] &= (byte)~(1 << row);
        }
        Break = input.Keys.Contains(EmulationKey.Break);
        Repeat = input.Keys.Contains(EmulationKey.RightControl);
    }

    internal byte Read() => _pins[_column];

    internal void Reset()
    {
        Array.Fill(_pins, byte.MaxValue);
        Break = Repeat = false;
        _column = 0;
    }
}
