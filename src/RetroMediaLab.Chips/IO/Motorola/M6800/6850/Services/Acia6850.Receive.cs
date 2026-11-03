using C = RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Constants.Acia6850Constants;

namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

public sealed partial class Acia6850
{
    private int _receiveStep = C.IdleReceiveStep;
    private int _receiveValue;
    private int _receiveClockCounter;
    private bool _receiveParity;
    private bool _receiveFraming;
    private bool _receiveDiscard;
    private bool _receiveWaitMark;

    public bool Receiving => _receiveStep != C.IdleReceiveStep;

    /// <summary>Le proprietaire a deja reconnu le start ; les prochains appels donnent les centres des bits utiles.</summary>
    public void BeginReceive()
    {
        SampleDcd();
        if (_masterReset || _dcdObserved || Receiving) return;
        _receiveStep = 0;
        _receiveValue = 0;
        _receiveParity = false;
        _receiveFraming = false;
        _receiveDiscard = false;
        _receiveClockCounter = 0;
        _receiveWaitMark = false;
    }

    public void CancelReceive()
    {
        _receiveStep = C.IdleReceiveStep;
        _receiveClockCounter = 0;
    }

    /// <summary>Un front montant RxC, avec RXD stable : qualification start /16 ou /64, puis centres de bits.</summary>
    public void ClockReceive(bool value)
    {
        SampleDcd();
        if (_masterReset || _dcdObserved) return;
        if (!Receiving)
        {
            if (_receiveWaitMark)
            {
                if (value) _receiveWaitMark = false;
                _receiveClockCounter = 0;
            }
            else if (value)
                _receiveClockCounter = 0;
            else if (++_receiveClockCounter >= Math.Max(C.DirectClockDivider, _divider / C.HalfBitDivisor))
                BeginReceive();
            return;
        }
        if (++_receiveClockCounter < _divider) return;
        _receiveClockCounter = 0;
        ReceiveBit(value);
    }

    /// <summary>Un centre de bit fourni par un demodulateur : donnees LSB, parite puis arrets.</summary>
    public void ReceiveBit(bool value)
    {
        SampleDcd();
        if (_masterReset || _dcdObserved || !Receiving) return;
        var payloadBits = _dataBits + (_hasParity ? C.LeastSignificantBit : 0);
        if (_receiveStep < _dataBits)
        {
            if (value) _receiveValue |= C.LeastSignificantBit << _receiveStep;
            _receiveParity ^= value;
        }
        else if (_hasParity && _receiveStep == _dataBits)
            _receiveParity ^= value;
        else if (_receiveStep == payloadBits)
            _receiveFraming = !value; // Only the first stop bit determines FE.

        // The loss starts at the midpoint of the final payload bit, before the stop interval.
        if (_receiveStep == payloadBits - C.LeastSignificantBit && (_status & C.ReceiveDataFull) != 0)
        {
            _receiveDiscard = true;
            _overrunPending = true;
            UpdateIrq();
        }
        ++_receiveStep;
        if (_receiveStep < payloadBits + _stopBits && !_receiveFraming) return;
        _receiveStep = C.IdleReceiveStep;
        _receiveWaitMark = _receiveFraming;
        if (!_receiveDiscard && (_status & C.ReceiveDataFull) == 0)
        {
            _data = (byte)(_receiveValue & ((C.LeastSignificantBit << _dataBits) - C.LeastSignificantBit));
            _status &= unchecked((byte)~C.ReceiveErrors);
            _status |= C.ReceiveDataFull;
            if (_receiveFraming) _status |= C.FramingError;
            if (_hasParity && _receiveParity != _oddParity) _status |= C.ParityError;
        }
        else if (!_receiveDiscard)
            _overrunPending = true;
        UpdateIrq();
    }

    private void SampleDcd()
    {
        if (_dcdObserved == _dcdInput) return;
        _dcdObserved = _dcdInput;
        if (_dcdObserved)
        {
            CancelReceive();
            _status &= unchecked((byte)~C.ReceiveDataFull);
            if (!_masterReset)
            {
                _dcdPending = true;
                _dcdStatusRead = false;
            }
        }
        UpdateIrq();
    }
}
