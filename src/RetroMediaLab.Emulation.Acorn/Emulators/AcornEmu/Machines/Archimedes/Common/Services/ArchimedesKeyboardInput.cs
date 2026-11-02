using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

internal static class ArchimedesKeyboardInput
{
    internal static void SetInput(this ArchimedesKeyboard keyboard, EmulationInputSnapshot snapshot)
    {
        var keys = snapshot.Keys.Where(ArchimedesKeyboardMap.Keys.ContainsKey).Select(key => (int)ArchimedesKeyboardMap.Keys[key]).ToArray();
        keyboard.SetInput(keys, snapshot.Keys.Contains(EmulationKey.Break), snapshot.Pointer.Left,
            snapshot.Pointer.Middle, snapshot.Pointer.Right, snapshot.Pointer.DeltaX, snapshot.Pointer.DeltaY);
    }
}
