namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

/// <summary>Récepteur/transmetteur MC6850 : format série, registre de données, statut et interruptions.</summary>
/// <remarks>Le démodulateur fournit les bits reçus à leur cadence. Ce composant ne connaît ni les images cassette,
/// ni le clavier, ni le MOS. Le constructeur reçoit les horloges de la carte propriétaire.</remarks>
public sealed class Acia6850(int ticksPerSecond, int serialClock)
:RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces.IAcia6850{
    private byte _control, _data, _status = 2;
    private bool _dcd, _dcdPending, _parity, _framing;
    private int _receiveStep = -1, _receiveValue, _transmitPhase, _transmitBits;
    private byte _transmitData;
    private uint _transmitFrame;
    private bool _transmitPending;
    /// <summary>Niveau série TXD : repos haut, start bas, données LSB, parité et stop ; utilisé par le modulateur de la carte.</summary>
    public bool TransmitBit => (_control & 0x60) != 0x60 && (_transmitBits == 0 || (_transmitFrame & 1) != 0);
    /// <summary>Débit imposé par le lecteur, ou zéro pour suivre la programmation du matériel par le MOS.</summary>
    public int BaudRateOverride { get; set; }
    public bool Receiving => _receiveStep >= 0;
    public int BaudRate => BaudRateOverride > 0 ? BaudRateOverride : serialClock / ((_control & 3) switch { 1 => 16, 2 => 64, _ => 1 });
    private int Format => (_control >> 2) & 7;
    private int DataBits => Format < 4 ? 7 : 8;
    private bool HasParity => Format is not (4 or 5);
    private bool OddParity => (Format & 1) != 0;
    private int StopBits => Format is 0 or 1 or 4 ? 2 : 1;
    public bool Irq => ((_control & 128) != 0 && ((_status & 1) != 0 || _dcdPending)) || ((_control & 0x60) == 0x20 && (_status & 10) == 2);

    /// <summary>Réinitialise les registres de transfert, sans déplacer le média ou le moteur.</summary>
    public void Reset()
    {
        _control = _data = 0; _status = 2; _dcd = _dcdPending = false;
        _receiveStep = -1; _transmitPhase = _transmitBits = 0;
        _transmitPending = false; _transmitFrame = uint.MaxValue;
    }
    public byte Read(bool data)
    {
        if (!data) return (byte)((_status & ~4) | (_dcd || _dcdPending ? 4 : 0) | (Irq ? 128 : 0));
        var value = _data; _status &= 0x0e; _dcdPending = false; return value;
    }
    public void Write(bool data, byte value)
    {
        if (data) { _status &= 0xfd; _transmitData = value; _transmitPending = true; }
        else if ((value & 3) == 3)
        {
            _status &= 8; _status |= 2; _dcdPending = false; _receiveStep = -1; _transmitBits = 0; _transmitPending = false;
        }
        else _control = value;
    }
    /// <summary>Le démodulateur BBC applique une impulsion DCD après détection prolongée de la porteuse.</summary>
    public void SetDcd(bool high) { if (high && !_dcd) _dcdPending = true; _dcd = high; }
    public void SetCts(bool blocked) => _status = blocked ? (byte)(_status | 8) : (byte)(_status & ~8);
    public void BeginReceive() { if (Receiving) return; _receiveStep = 0; _receiveValue = 0; _parity = _framing = false; }
    public void CancelReceive() => _receiveStep = -1;

    /// <summary>Reçoit successivement données de poids faible, parité éventuelle, puis bits d'arrêt.</summary>
    public void ReceiveBit(bool value)
    {
        if (!Receiving) return;
        if (_receiveStep < DataBits)
        {
            if (value) _receiveValue |= 1 << _receiveStep;
            _parity ^= value;
        }
        else if (HasParity && _receiveStep == DataBits) _parity ^= value;
        else if (!value) _framing = true;
        if (++_receiveStep < DataBits + (HasParity ? 1 : 0) + StopBits) return;
        _receiveStep = -1;
        if ((_status & 1) != 0) { _status |= 0x20; return; }
        _data = (byte)_receiveValue;
        _status = (byte)(_status | 1 | (_framing ? 0x10 : 0) | (HasParity && _parity != OddParity ? 0x40 : 0));
    }

    /// <summary>Décale réellement TXD ; le registre de données se libère dès son transfert au registre série.</summary>
    public void Tick()
    {
        if (_transmitBits == 0)
        {
            if (!_transmitPending || (_status & 8) != 0) return;
            var count = 1 + DataBits;
            _transmitFrame = (uint)(_transmitData & ((1 << DataBits) - 1)) << 1;
            if (HasParity)
            {
                var odd = (System.Numerics.BitOperations.PopCount((uint)_transmitData & ((1u << DataBits) - 1)) & 1) != 0;
                if (odd != OddParity) _transmitFrame |= 1u << count;
                count++;
            }
            _transmitFrame |= ((1u << StopBits) - 1) << count;
            _transmitBits = count + StopBits; _transmitPhase = 0;
            _transmitPending = false; _status |= 2;
        }
        _transmitPhase += BaudRate;
        if (_transmitPhase < ticksPerSecond) return;
        _transmitPhase -= ticksPerSecond;
        _transmitFrame >>= 1;
        --_transmitBits;
    }
}
