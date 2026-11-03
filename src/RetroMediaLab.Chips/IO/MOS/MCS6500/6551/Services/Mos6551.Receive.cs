using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

public sealed partial class Mos6551
{
    private int _receiveStep = C.IdleReceive;
    private int _receiveTicks;
    private int _receiveValue;
    private bool _receiveParity;
    private bool _receiveParityError;
    private bool _receiveWaitMark;
    private uint _echoHistory;
    private bool _echoDelayedRxd = true;
    private bool _echoAwaitStart;

    private void ReceiveClock()
    {
        if (_echo)
        {
            _echoDelayedRxd = (_echoHistory & (C.LeastSignificantBit << (C.HalfBit - C.LeastSignificantBit))) != 0;
            _echoHistory = (_echoHistory << C.LeastSignificantBit) | (_rxd ? C.LeastSignificantBit : 0u);
        }
        if (_dcd) { _receiveStep = C.IdleReceive; _receiveTicks = 0; return; }
        if (_receiveStep == C.IdleReceive)
        {
            if (!_enabled) return;
            if (_receiveWaitMark)
            {
                if (_rxd) _receiveWaitMark = false;
                return;
            }
            if (_receiveTicks == 0 && _rxd) return;
            if (++_receiveTicks < C.HalfBit) return;
            _receiveTicks = 0;
            if (_rxd) return; // Le niveau à mi-start qualifie le start, pas tous les échantillons intermédiaires.
            _receiveStep = 0;
            _receiveValue = 0;
            _receiveParity = _receiveParityError = false;
            if (!_cts && (_status & C.Overrun) == 0) _echoAwaitStart = false;
            return;
        }
        var payload = _dataBits + (_parity != 0 ? C.LeastSignificantBit : 0);
        var stopSample = _stopTicks == C.OneAndHalfStopTicks
            ? C.OneAndHalfStopSample : _stopTicks + C.ReceiveStopSampleOffset;
        if (++_receiveTicks < (_receiveStep < payload ? C.Oversampling : stopSample)) return;
        _receiveTicks = 0;
        if (_receiveStep < _dataBits)
        {
            if (_rxd) _receiveValue |= C.LeastSignificantBit << _receiveStep;
            _receiveParity ^= _rxd;
            ++_receiveStep;
            return;
        }
        if (_receiveStep < payload)
        {
            _receiveParity ^= _rxd;
            _receiveParityError = _parity == C.OddParity ? !_receiveParity
                : _parity == C.EvenParity && _receiveParity;
            ++_receiveStep;
            return;
        }
        var errors = (_receiveParityError ? C.ParityError : 0) | (!_rxd ? C.FramingError : 0);
        if ((_status & C.ReceiveFull) != 0)
        {
            errors |= C.Overrun;
            _echoAwaitStart = true;
        }
        else
            _receiveData = (byte)_receiveValue;
        _status = (byte)((_status & ~(C.ParityError | C.FramingError)) | C.ReceiveFull | errors);
        if (_receiveInterrupt) RaiseInterrupt(C.ReceiveFull);
        _receiveWaitMark = !_rxd && _receiveValue == 0;
        _receiveStep = C.IdleReceive;
    }
}
