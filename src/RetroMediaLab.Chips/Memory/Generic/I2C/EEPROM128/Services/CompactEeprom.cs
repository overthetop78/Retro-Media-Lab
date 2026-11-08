using T = RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Constants.Pcd8572TimingConstants;

namespace RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;

/// <summary>EEPROM série du Compact, pilotée par PB5 (SCL) et PB4 (SDA), sans horloge 146818.</summary>
public sealed partial class CompactEeprom
:RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Interfaces.ICompactEeprom{
    private enum Phase { Idle, Receive, Acknowledge, Transmit, MasterAcknowledge }
    private const int Size = 128;
    private const byte ClockMask = 0x20;
    private const byte DataMask = 0x10;
    private const byte DeviceMask = 0xfe;
    private const byte DeviceAddress = 0xa0;
    private const int HardwareAddressCount = 8;
    private readonly byte _deviceAddress;
    private readonly byte[] _bytes = new byte[Size];
    private Phase _phase;
    private bool _clock = true;
    private bool _data = true;
    private bool _output = true;
    private bool _device;
    private bool _pointer;
    private bool _read;
    private bool _ackSeen;
    private bool _accepted;
    private int _bits;
    private byte _shift;
    private byte _address;
    public CompactEeprom() : this(0) { }

    /// <summary>Straps A2/A1/A0 de l'adresse I²C ; la carte Compact les relie à zéro.</summary>
    public CompactEeprom(int hardwareAddress) : this(hardwareAddress, T.DefaultReferenceFrequency) { }

    /// <summary>La durée fournie caractérise le cycle RC ; elle ne cadence pas le bus I²C.</summary>
    public CompactEeprom(int hardwareAddress, long referenceFrequency,
        int writeCycleMicroseconds = T.NominalWriteMicroseconds)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)hardwareAddress, (uint)HardwareAddressCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceFrequency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(writeCycleMicroseconds);
        ReferenceFrequency = referenceFrequency;
        _writeCycleClocks = checked((long)(((UInt128)(ulong)referenceFrequency * (uint)writeCycleMicroseconds
            + T.MicrosecondsPerSecond - 1) / T.MicrosecondsPerSecond));
        _deviceAddress = (byte)(DeviceAddress | (hardwareAddress << 1));
    }
    public byte ReadPort => (byte)(0xcf | (_clock ? ClockMask : 0) | (_data && _output ? DataMask : 0));
    public void WritePort(byte value)
    {
        var clock = (value & ClockMask) != 0;
        var data = (value & DataMask) != 0;
        // SDA est à drain ouvert : une libération par le maître ne produit
        // aucun front si le composant maintient encore la ligne à zéro.
        var previousLine = _data && _output;
        var line = data && _output;
        if (clock && _clock && line != previousLine)
        {
            if (line) StartWriteCycle();
            else if (!Writing) _writeCount = 0;
            _phase = line || Writing ? Phase.Idle : Phase.Receive;
            _device = true; _bits = 0; _shift = 0; _output = true;
        }
        else if (clock != _clock)
        {
            if (clock) Rising(line); else Falling();
        }
        _clock = clock; _data = data;
    }
    private void Rising(bool data)
    {
        switch (_phase)
        {
            case Phase.Receive:
                _shift = (byte)((_shift << 1) | (data ? 1 : 0));
                if (++_bits != 8) break;
                _accepted = true;
                if (_device)
                {
                    _accepted = (_shift & DeviceMask) == _deviceAddress;
                    _read = (_shift & 1) != 0; _pointer = !_read; _device = false;
                }
                else if (_pointer) { _address = (byte)(_shift & (Size - 1)); _pointer = false; }
                else if (_writeCount < T.WriteBufferBytes)
                {
                    _writeAddresses[_writeCount] = _address;
                    _writeValues[_writeCount++] = _shift;
                    _address = (byte)((_address + 1) & (Size - 1));
                }
                else _accepted = false;
                _bits = 0; _ackSeen = false; _phase = Phase.Acknowledge;
                break;
            case Phase.Acknowledge: _ackSeen = true; break;
            case Phase.Transmit: _bits++; break;
            case Phase.MasterAcknowledge:
                _accepted = !data;
                if (_accepted) _address = (byte)((_address + 1) & (Size - 1));
                _ackSeen = true;
                break;
        }
    }
    private void Falling()
    {
        switch (_phase)
        {
            case Phase.Acknowledge:
                if (!_ackSeen) { _output = !_accepted; break; }
                _output = true;
                if (!_accepted) _phase = Phase.Idle;
                else if (_read) BeginTransmit();
                else { _phase = Phase.Receive; _shift = 0; }
                break;
            case Phase.Transmit:
                if (_bits == 8) { _output = true; _ackSeen = false; _phase = Phase.MasterAcknowledge; }
                else _output = (_shift & (0x80 >> _bits)) != 0;
                break;
            case Phase.MasterAcknowledge:
                if (_ackSeen) { if (_accepted) BeginTransmit(); else { _phase = Phase.Idle; _output = true; } }
                break;
        }
    }
    private void BeginTransmit()
    {
        _shift = _bytes[_address];
        _bits = 0; _phase = Phase.Transmit; _output = (_shift & 0x80) != 0;
    }
    public byte[] Export() => _bytes.ToArray();
    public void Import(ReadOnlySpan<byte> bytes) { if (bytes.Length != Size) throw new ArgumentException(nameof(bytes)); bytes.CopyTo(_bytes); }
}
