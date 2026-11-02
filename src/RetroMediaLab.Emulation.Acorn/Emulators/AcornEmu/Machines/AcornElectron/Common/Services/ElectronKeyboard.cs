using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed class ElectronKeyboard
{
    private readonly HashSet<EmulationKey> _keys = [];
    internal bool Break => _keys.Contains(EmulationKey.Break);

    internal void SetInput(EmulationInputSnapshot snapshot)
    {
        _keys.Clear();
        _keys.UnionWith(snapshot.Keys);
        if (_keys.Contains(EmulationKey.RightControl)) _keys.Add(EmulationKey.LeftControl);
        if (_keys.Contains(EmulationKey.RightShift)) _keys.Add(EmulationKey.LeftShift);
    }

    internal void Reset() => _keys.Clear();

    internal byte Read(int address)
    {
        byte result = 0;
        for (var line = 0; line < C.KeyboardLines; line++)
        {
            if ((address & (1 << line)) != 0) continue;
            for (var bit = 0; bit < C.KeyboardBits; bit++)
            {
                var key = ElectronKeyboardMap.Lines[line][bit];
                if (key != EmulationKey.Unknown && _keys.Contains(key)) result |= (byte)(1 << bit);
            }
        }
        return result;
    }
}
