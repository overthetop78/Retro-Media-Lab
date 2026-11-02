using C = RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Constants.Pcf8583Constants;
using State = RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Enums.Pcf8583BusState;

namespace RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Services;

/// <summary>PCF8583 : mémoire CMOS et calendrier sur les deux lignes I²C du IOC.
/// La carte fournit le temps écoulé ; aucune minuterie, ressource système ou fichier possédé.</summary>
public sealed partial class Pcf8583
{
    private readonly byte[] _memory = new byte[C.MemorySize];
    private readonly byte[] _capture = new byte[C.ClockCounterCount];
    private readonly int _ticksPerSecond;
    private readonly int _slaveAddress;
    private long _fraction;
    private State _state;
    private State _afterAcknowledge;
    private bool _scl = true;
    private bool _hostSda = true;
    private bool _pullSdaLow;
    private bool _acknowledged;
    private bool _ackClocked;
    private int _bits;
    private int _shift;
    private int _pointer;

    public Pcf8583(int ticksPerSecond, DateTime? initialTime = null, bool addressPin = false,
        ReadOnlySpan<byte> initialRam = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerSecond, C.HundredthsPerSecond);
        _ticksPerSecond = ticksPerSecond;
        _slaveAddress = C.SlaveAddress | (addressPin ? C.AddressPinBit : 0);
        if (!initialRam.IsEmpty)
        {
            if (initialRam.Length != C.RamSize) throw new ArgumentOutOfRangeException(nameof(initialRam));
            initialRam.CopyTo(_memory.AsSpan(C.RamStart));
        }
        SetTime(initialTime ?? DateTime.Now);
    }

    public bool Sda => _hostSda && !_pullSdaLow;

    /// <summary>Une remise à zéro de l'ordinateur ne coupe pas la pile du CMOS.</summary>
    public void ResetBus()
    {
        _state = State.Idle;
        _scl = _hostSda = true;
        _pullSdaLow = _acknowledged = _ackClocked = false;
        _bits = _shift = 0;
    }

    public byte[] ReadNonvolatileMemory() => (byte[])_memory.Clone();

    public void WriteNonvolatileMemory(ReadOnlySpan<byte> data)
    {
        if (data.Length != C.MemorySize) throw new ArgumentOutOfRangeException(nameof(data));
        data.CopyTo(_memory);
        _memory.AsSpan(C.Hundredths, C.ClockCounterCount).CopyTo(_capture);
        _fraction = 0;
        ResetBus();
    }

    public void WriteLines(bool scl, bool sda)
    {
        var start = _scl && scl && _hostSda && !sda;
        var stop = _scl && scl && !_hostSda && sda;
        var rising = !_scl && scl;
        var falling = _scl && !scl;
        _hostSda = sda;
        _scl = scl;
        if (start)
        {
            _state = State.Address;
            _bits = _shift = 0;
            _pullSdaLow = false;
            return;
        }
        if (stop) { _state = State.Idle; _pullSdaLow = false; return; }
        if (rising) RisingEdge();
        if (falling) FallingEdge();
    }

    private void RisingEdge()
    {
        switch (_state)
        {
            case State.Address:
            case State.Pointer:
            case State.Write:
                _shift = (_shift << 1) | (_hostSda ? 1 : 0);
                if (++_bits == C.BitsPerByte) ReceiveByte((byte)_shift);
                break;
            case State.SlaveAcknowledge:
                _ackClocked = true;
                break;
            case State.Read:
                _bits++;
                break;
            case State.MasterAcknowledge:
                _acknowledged = !_hostSda;
                _ackClocked = true;
                break;
        }
    }

    private void FallingEdge()
    {
        switch (_state)
        {
            case State.SlaveAcknowledge:
                if (!_ackClocked) { _pullSdaLow = _acknowledged; break; }
                _pullSdaLow = false;
                _state = _afterAcknowledge;
                _bits = _shift = 0;
                if (_state == State.Read) BeginRead();
                break;
            case State.Read:
                if (_bits == C.BitsPerByte)
                {
                    _pointer = (_pointer + 1) & C.AddressMask;
                    _state = State.MasterAcknowledge;
                    _ackClocked = false;
                    _pullSdaLow = false;
                }
                else _pullSdaLow = (_shift & (C.HighBit >> _bits)) == 0;
                break;
            case State.MasterAcknowledge:
                if (!_ackClocked) break;
                _state = _acknowledged ? State.Read : State.Idle;
                if (_acknowledged) BeginRead();
                break;
        }
    }

    private void ReceiveByte(byte value)
    {
        _acknowledged = true;
        switch (_state)
        {
            case State.Address:
                _acknowledged = (value & C.SlaveAddressMask) == _slaveAddress;
                _afterAcknowledge = !_acknowledged ? State.Idle : (value & C.ReadBit) != 0 ? State.Read : State.Pointer;
                if (_afterAcknowledge == State.Read && (_memory[C.Control] & C.Hold) == 0)
                    _memory.AsSpan(C.Hundredths, C.ClockCounterCount).CopyTo(_capture);
                break;
            case State.Pointer:
                _pointer = value;
                _afterAcknowledge = State.Write;
                break;
            case State.Write:
                WriteRegister(_pointer, value);
                _pointer = (_pointer + 1) & C.AddressMask;
                _afterAcknowledge = State.Write;
                break;
        }
        _state = State.SlaveAcknowledge;
        _ackClocked = false;
    }

    private void BeginRead()
    {
        _bits = 0;
        _shift = ReadRegister(_pointer);
        _pullSdaLow = (_shift & C.HighBit) == 0;
    }

    private byte ReadRegister(int address)
    {
        var value = address is >= C.Hundredths and <= C.Timer
            ? _capture[address - C.Hundredths] : _memory[address];
        if ((_memory[C.Control] & C.MaskDate) != 0)
        {
            if (address == C.Date) value &= C.DateMask;
            if (address == C.Month) value &= C.MonthMask;
        }
        return value;
    }

    private void WriteRegister(int address, byte value)
    {
        if (address == C.Control)
        {
            if ((value & C.Hold) != 0 && (_memory[C.Control] & C.Hold) == 0)
                _memory.AsSpan(C.Hundredths, C.ClockCounterCount).CopyTo(_capture);
            if ((value & C.Stop) != 0) _fraction = 0;
        }
        _memory[address] = value;
    }
}
