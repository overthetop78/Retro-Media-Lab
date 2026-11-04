using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Ferranti sheet 11: four RAM data pins, independent LD0/LD1 latches and DSEL.</summary>
/// <remarks>The timing circuit supplies the load strobes; this data path owns no RAM.</remarks>
internal sealed class ElectronRamData
{
    private static readonly byte[] DecodedData = CreateDecodedData();
    private byte _pinGroups;

    // Keep the two physical groups latched; decode their fixed wiring only when read.
    internal byte Data => DecodedData[_pinGroups];

    internal void LoadEven(byte pins) => _pinGroups = (byte)((_pinGroups & C.OddPinGroupMask) | (pins & C.NibbleMask));

    internal void LoadOdd(byte pins) => _pinGroups = (byte)((_pinGroups & C.NibbleMask) | ((pins & C.NibbleMask) << C.PinGroupShift));

    private static byte[] CreateDecodedData()
    {
        var values = new byte[(C.NibbleMask + 1) * (C.NibbleMask + 1)];
        for (var pins = 0; pins < values.Length; pins++)
            values[pins] = (byte)(ElectronRamDataPins.Spread((byte)pins)
                | (ElectronRamDataPins.Spread((byte)(pins >> C.PinGroupShift)) << C.LaneShift));
        return values;
    }
}
