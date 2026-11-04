using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Four DRAM data lines: even and odd groups share the same physical pins.</summary>
public static class ElectronRamDataPins
{
    public static byte Select(byte data, bool odd)
    {
        var group = (odd ? data >> C.LaneShift : data) & C.EvenMask;
        group = (group | (group >> C.LaneShift)) & C.PairMask;
        return (byte)((group | (group >> C.GroupShift)) & C.NibbleMask);
    }

    /// <summary>A write strobe replaces one group, preserving the four unwired bits.</summary>
    public static byte Merge(byte previous, byte pins, bool odd) => odd
        ? (byte)((previous & C.EvenMask) | (Spread(pins) << C.LaneShift))
        : (byte)((previous & C.OddMask) | Spread(pins));

    internal static int Spread(byte pins)
    {
        var group = pins & C.NibbleMask;
        group = (group | (group << C.GroupShift)) & C.PairMask;
        return (group | (group << C.LaneShift)) & C.EvenMask;
    }
}
