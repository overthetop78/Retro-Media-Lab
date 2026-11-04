namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

/// <summary>Borrowed four-bit DRAM bus. RAS, CAS and WE are electrical levels, active low.</summary>
/// <remarks>
/// The memory owner latches row/column addresses on falling RAS/CAS respectively.
/// Data supplies the four input levels during a read, or the driven levels during a write.
/// Drive updates outputs in signal order; it does not advance the owner's clock.
/// </remarks>
public interface IElectronRamPins
{
    byte Data { get; }

    void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data);
}
