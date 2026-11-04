using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Read-only borrowed memory callback for the standalone video constructor.</summary>
internal sealed class ElectronReadOnlyRamPins(Func<int, byte> read) : IElectronRamPins
{
    private byte _row;
    private bool _ras = true;
    private bool _cas = true;
    public byte Data { get; private set; }

    public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
    {
        if (_ras && !rowStrobeHigh) _row = address;
        if (_cas && !columnStrobeHigh && !rowStrobeHigh)
        {
            if (!writeEnableHigh) throw new InvalidOperationException();
            var value = read(ElectronRamAddress.Logical(_row, address));
            Data = ElectronRamDataPins.Select(value, (address & C.ColumnParityMask) != 0);
        }
        _ras = rowStrobeHigh;
        _cas = columnStrobeHigh;
    }
}
