using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Enums;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Interfaces;
/// <summary>Contrat matériel du composant RamIo8154 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IRamIo8154
{
    Func<byte>? ReadPortA { get; set; }

    Func<byte>? ReadPortB { get; set; }

    /// <summary>Observes port A writes, including repeated values, and changes in its output drive.</summary>
    Action<byte>? PortAPinsChanged { get; set; }

    /// <summary>Observes port B writes, including repeated values, and changes in its output drive.</summary>
    Action<byte>? PortBPinsChanged { get; set; }

    /// <summary>Reports actual changes of port A driven levels or drive mask.</summary>
    Action<byte, byte>? PortADriveChanged { get; set; }
    /// <summary>Reports actual changes of port B driven levels or drive mask.</summary>
    Action<byte, byte>? PortBDriveChanged { get; set; }
    Action<bool>? InterruptChanged { get; set; }
    bool Irq { get; }
    bool StrobeHigh { get; }
    byte PortADriveMask { get; }
    byte PortBDriveMask { get; }
    RamIoMode OperatingMode { get; }
    byte Mode { get; }

    byte OutputA { get; }

    byte OutputB { get; }

    void Reset();
    void SetReset(bool high);
    void SetStrobe(bool high);
    byte? SetBus(bool cs0High, bool cs1High, bool memoryHigh, bool readHigh, bool writeHigh,
        int address, byte input);
    byte ReadRam(int address);
    void WriteRam(int address, byte value);
    byte ReadIo(int address);
    void WriteIo(int address, byte value);
}
