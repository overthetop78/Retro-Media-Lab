using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronMachine : IElectronRamPins
{
    private byte _ramRow;
    private byte _ramInput;
    private bool _ramRasHigh = true;
    private bool _ramCasHigh = true;

    byte IElectronRamPins.Data => _ramInput;

    void IElectronRamPins.Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
    {
        if (_ramRasHigh && !rowStrobeHigh) _ramRow = address;
        if (_ramCasHigh && !columnStrobeHigh && !rowStrobeHigh)
        {
            var logical = ElectronRamAddress.Logical(_ramRow, address);
            var odd = (address & C.RamOddColumnMask) != 0;
            var value = _ram[logical];
            if (!writeEnableHigh)
            {
                value = ElectronRamDataPins.Merge(value, data, odd);
                _ram[logical] = value;
            }
            _ramInput = ElectronRamDataPins.Select(value, odd);
        }
        _ramRasHigh = rowStrobeHigh;
        _ramCasHigh = columnStrobeHigh;
    }
}
