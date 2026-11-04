using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronRamConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Ferranti sheet 8: RA0–RA7 for the selected CPU or video address.</summary>
/// <remarks>A15 is decoded separately by write control. Column parity selects the data group.</remarks>
public static class ElectronRamAddress
{
    public static byte Row(ushort address) => (byte)(
        ((address >> C.RowLowShift) & C.RowLowMask)
        | ((address & C.RowBitFiveMask) << C.RowBitFiveShift)
        | ((address & C.RowBitSixMask) << C.RowBitSixShift)
        | ((address & C.RowBitSevenMask) >> C.RowBitSevenShift));

    public static byte Column(ushort address, bool odd) => (byte)(
        ((address >> C.ColumnLowShift) & C.ColumnLowMask)
        | ((address & C.ColumnBitSixMask) << C.ColumnBitSixShift)
        | (odd ? C.ColumnParityMask : 0));

    /// <summary>Logical byte selected by latched physical row and column pins; parity is separate.</summary>
    public static ushort Logical(byte row, byte column) => (ushort)(
        ((row & C.RowLowMask) << C.RowLowShift)
        | ((row >> C.RowBitFiveShift) & C.RowBitFiveMask)
        | ((row >> C.RowBitSixShift) & C.RowBitSixMask)
        | ((row << C.RowBitSevenShift) & C.RowBitSevenMask)
        | ((column & C.ColumnLowMask) << C.ColumnLowShift)
        | ((column >> C.ColumnBitSixShift) & C.ColumnBitSixMask));
}
