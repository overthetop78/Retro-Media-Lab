using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Enums;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Interfaces;

/// <summary>Digital 8255 bus and peripheral pins; external devices and wiring belong to the caller.</summary>
public interface IPpi8255
{
    Func<byte>? ReadPortA { get; set; }
    Func<byte>? ReadPortB { get; set; }
    Func<byte>? ReadPortC { get; set; }
    Action<byte>? PortAPinsChanged { get; set; }
    Action<byte>? PortBPinsChanged { get; set; }
    Action<byte>? PortCPinsChanged { get; set; }
    Action<byte, byte>? PortADriveChanged { get; set; }
    Action<byte, byte>? PortBDriveChanged { get; set; }
    Action<byte, byte>? PortCDriveChanged { get; set; }
    Action<bool>? InterruptAChanged { get; set; }
    Action<bool>? InterruptBChanged { get; set; }
    byte ControlWord { get; }
    PpiMode GroupAMode { get; }
    PpiMode GroupBMode { get; }
    byte PortADriveMask { get; }
    byte PortBDriveMask { get; }
    byte PortCDriveMask { get; }
    bool InterruptA { get; }
    bool InterruptB { get; }
    void Reset();
    void SetReset(bool high);
    /// <summary>Sets physical PC levels, including STB/ACK edges, independently of CPU selection.
    /// A ReadPortC callback must describe these same levels for handshake input pins.</summary>
    void SetPortCInput(byte levels);
    byte Read(int register);
    void Write(int register, byte value);
    /// <summary>Active-low CS/RD/WR; null means released data bus. Writes latch at WR end.</summary>
    byte? SetBus(bool chipSelectHigh, bool readHigh, bool writeHigh, int register, byte data);
}
