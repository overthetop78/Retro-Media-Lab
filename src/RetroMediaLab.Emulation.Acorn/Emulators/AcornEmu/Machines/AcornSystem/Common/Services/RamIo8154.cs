using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>RAM et ports statiques INS8154 des cartes CPU Acorn System.</summary>
/// <remarks>Les adresses de bits portent le numéro de port et l’action set/reset ;
/// la valeur écrite n’intervient pas dans ces opérations. Les rappels représentent
/// les broches de la carte appelante, qui conserve leur propriété.</remarks>
internal sealed class RamIo8154
{
    private readonly byte[] _ram = new byte[RamIoConstants.RamBytes];
    private byte _outputA;
    private byte _outputB;
    private byte _directionA;
    private byte _directionB;

    internal Func<byte>? ReadPortA { get; set; }
    internal Func<byte>? ReadPortB { get; set; }
    internal Action<byte>? PortAPinsChanged { get; set; }
    internal Action<byte>? PortBPinsChanged { get; set; }
    internal byte Mode { get; private set; } = RamIoConstants.BasicMode;
    internal byte OutputA => _outputA;
    internal byte OutputB => _outputB;

    internal void Reset()
    {
        _outputA = _outputB = _directionA = _directionB = 0;
        Mode = RamIoConstants.BasicMode;
        PublishA();
        PublishB();
    }

    internal byte ReadRam(int address) => _ram[address & RamIoConstants.RamAddressMask];

    internal void WriteRam(int address, byte value) => _ram[address & RamIoConstants.RamAddressMask] = value;

    internal byte ReadIo(int address)
    {
        if (address == RamIoConstants.PortA) return PinsA();
        if (address == RamIoConstants.PortB) return PinsB();
        if (address < 0 || address > RamIoConstants.Mode) return byte.MaxValue;
        if (address >= RamIoConstants.FirstRegister) return 0;
        var pins = (address & RamIoConstants.PortSelection) == 0 ? PinsA() : PinsB();
        return (pins & (1 << (address & RamIoConstants.BitAddressMask))) != 0
            ? RamIoConstants.BitReadHigh : (byte)0;
    }

    internal void WriteIo(int address, byte value)
    {
        switch (address)
        {
            case RamIoConstants.PortA:
                _outputA = value;
                PublishA();
                return;
            case RamIoConstants.PortB:
                _outputB = value;
                PublishB();
                return;
            case RamIoConstants.DirectionA:
                _directionA = value;
                PublishA();
                return;
            case RamIoConstants.DirectionB:
                _directionB = value;
                PublishB();
                return;
            case RamIoConstants.Mode:
                Mode = value;
                return;
        }

        if (address < 0 || address >= RamIoConstants.FirstRegister) return;
        var bit = (byte)(1 << (address & RamIoConstants.BitAddressMask));
        var set = (address & RamIoConstants.SetBit) != 0;
        if ((address & RamIoConstants.PortSelection) == 0)
        {
            _outputA = set ? (byte)(_outputA | bit) : (byte)(_outputA & ~bit);
            PublishA();
        }
        else
        {
            _outputB = set ? (byte)(_outputB | bit) : (byte)(_outputB & ~bit);
            PublishB();
        }
    }

    private byte PinsA() => (byte)((_outputA & _directionA) | ((ReadPortA?.Invoke() ?? byte.MaxValue) & ~_directionA));
    private byte PinsB() => (byte)((_outputB & _directionB) | ((ReadPortB?.Invoke() ?? byte.MaxValue) & ~_directionB));
    private void PublishA() => PortAPinsChanged?.Invoke((byte)(_outputA | ~_directionA));
    private void PublishB() => PortBPinsChanged?.Invoke((byte)(_outputB | ~_directionB));
}
