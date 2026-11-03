using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Enums;
using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Interfaces;
using C = RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Constants.RamIoConstants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;

/// <summary>National INS8154 static RAM and programmable ports, including strobed modes.
/// Board address decoding, external pulls and interrupt polarity conversion belong to the caller.</summary>
public sealed partial class RamIo8154 : IRamIo8154
{
    private readonly byte[] _ram = new byte[C.RamBytes];
    private byte _outputA;
    private byte _outputB;
    private byte _directionA;
    private byte _directionB;
    private bool _resetHigh = true;
    private RamIoMode _operatingMode = RamIoMode.Basic;

    public Func<byte>? ReadPortA { get; set; }
    public Func<byte>? ReadPortB { get; set; }
    public Action<byte>? PortAPinsChanged { get; set; }
    public Action<byte>? PortBPinsChanged { get; set; }
    public Action<byte, byte>? PortADriveChanged { get; set; }
    public Action<byte, byte>? PortBDriveChanged { get; set; }
    public Action<bool>? InterruptChanged { get; set; }
    public byte Mode { get; private set; } = C.BasicMode;
    public byte OutputA => _outputA;
    public byte OutputB => _outputB;
    public RamIoMode OperatingMode => _operatingMode;

    public void Reset()
    {
        // NRST does not clear the independent static RAM or change external pin levels.
        _outputA = _outputB = _directionA = _directionB = 0;
        _inputA = 0;
        _request = false;
        _readActive = _writeActive = false;
        Mode = C.BasicMode;
        _operatingMode = RamIoMode.Basic;
        PublishOutputs();
    }

    /// <summary>Physical NRST level. Low inhibits RAM and I/O bus transfers.</summary>
    public void SetReset(bool high)
    {
        if (_resetHigh == high) return;
        _resetHigh = high;
        if (!high) Reset();
    }

    public byte ReadRam(int address) => _resetHigh ? _ram[address & C.RamAddressMask] : byte.MaxValue;

    public void WriteRam(int address, byte value)
    {
        if (_resetHigh) _ram[address & C.RamAddressMask] = value;
    }

    /// <summary>Atomic bus read; released data lines are resolved to high by this convenience API.</summary>
    public byte ReadIo(int address)
    {
        address &= C.RamAddressMask;
        if (!_resetHigh) return byte.MaxValue;
        if (address == C.PortA)
        {
            var data = ReadA();
            if (_operatingMode == RamIoMode.StrobedInput) EndReadIo(address);
            return data;
        }
        if (address == C.PortB) return ReadB();
        var value = BeginReadIo(address) ?? byte.MaxValue;
        return value;
    }

    public void WriteIo(int address, byte value)
    {
        address &= C.RamAddressMask;
        BeginWriteIo(address, value);
        if (address == C.PortA && IsStrobedOutput) EndWriteIo(address);
    }

    private byte? BeginReadIo(int address)
    {
        if (!_resetHigh) return null;
        if (address == C.PortA) return ReadA();
        if (address == C.PortB) return ReadB();
        if (address >= C.FirstRegister) return null; // ODR/MDR are write-only, not zero-valued registers.
        var pins = (address & C.PortSelection) == 0 ? ReadA() : ReadB();
        return (pins & (1 << (address & C.BitAddressMask))) != 0 ? C.BitReadHigh : (byte)0;
    }

    private void EndReadIo(int address)
    {
        if (!_resetHigh || address != C.PortA || _operatingMode != RamIoMode.StrobedInput) return;
        _outputB &= unchecked((byte)~C.BufferFlag);
        _request = false;
        PublishOutputs();
    }

    private void BeginWriteIo(int address, byte value)
    {
        if (!_resetHigh) return;
        switch (address)
        {
            case C.PortA:
                _outputA = value;
                PublishPortA(true);
                return;
            case C.PortB:
                _outputB = _operatingMode == RamIoMode.Basic ? value
                    : (byte)((_outputB & C.HandshakeBits) | (value & ~C.HandshakeBits));
                break;
            case C.DirectionA:
                _directionA = value;
                PublishPortA(true);
                return;
            case C.DirectionB: _directionB = value; break;
            case C.Mode:
                Mode = value;
                _operatingMode = (value & C.StrobedMode) == 0 ? RamIoMode.Basic
                    : (value & C.OutputMode) == 0 ? RamIoMode.StrobedInput
                    : (value & C.ThreeStateMode) != 0 ? RamIoMode.StrobedOutputThreeState : RamIoMode.StrobedOutput;
                _request = IsStrobedOutput;
                if (_operatingMode != RamIoMode.Basic)
                    _outputB = IsStrobedOutput ? (byte)(_outputB | C.BufferFlag)
                        : (byte)(_outputB & ~C.BufferFlag);
                break;
            default:
                if (address >= C.FirstRegister) return;
                var bit = (byte)(1 << (address & C.BitAddressMask));
                var set = (address & C.SetBit) != 0;
                if ((address & C.PortSelection) == 0)
                {
                    _outputA = set ? (byte)(_outputA | bit) : (byte)(_outputA & ~bit);
                    PublishPortA(true);
                    return;
                }
                _outputB = set ? (byte)(_outputB | bit) : (byte)(_outputB & ~bit);
                break;
        }
        PublishOutputs(observeB: address != C.Mode);
    }

    private void EndWriteIo(int address)
    {
        if (!_resetHigh || address != C.PortA || !IsStrobedOutput) return;
        _outputB &= unchecked((byte)~C.BufferFlag);
        _request = false;
        PublishOutputs();
    }
}
