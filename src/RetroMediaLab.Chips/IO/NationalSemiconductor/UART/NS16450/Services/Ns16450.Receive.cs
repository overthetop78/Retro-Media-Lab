using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool _rxd = true;
    private int _receiveStage = C.IdleReceive;
    private int _receiveTicks;
    private int _receiveValue;
    private byte _receiveControl;
    private bool _receiveParityError;
    private bool _receiveBreak;
    private int _lowTicks;
    private int _lowFrameTicks;
    private int _markTicks;
    private int _receiveInterruptTicks;
    private byte _receiveInterruptStatus;
    private byte _pendingReceiveInterruptStatus;

    private void ClearReceiveInterrupt(byte mask)
    {
        _receiveInterruptStatus &= unchecked((byte)~mask);
        _pendingReceiveInterruptStatus &= unchecked((byte)~mask);
    }

    private void ScheduleReceiveInterrupt(byte status)
    {
        _pendingReceiveInterruptStatus |= status;
        _receiveInterruptTicks = C.ReceiveInterruptDelay;
    }

    private void ReceiveClock()
    {
        if (_receiveInterruptTicks != 0 && --_receiveInterruptTicks == 0)
        {
            _receiveInterruptStatus |= _pendingReceiveInterruptStatus;
            _pendingReceiveInterruptStatus = 0;
            UpdateInterrupt();
            if (_reset) return;
        }
        var high = ReceiveInput;
        if (high)
        {
            _lowTicks = 0;
            if (_receiveBreak)
            {
                if (++_markTicks >= C.HalfBit) _receiveBreak = false;
                return;
            }
        }
        else
        {
            _markTicks = 0;
            if (_lowTicks == 0)
                _lowFrameTicks = C.Oversampling * (1 + DataBits(_lineControl)
                    + ((_lineControl & C.ParityEnable) != 0 ? 1 : 0)) + StopTicks(_lineControl);
            if (_lowTicks <= _lowFrameTicks) _lowTicks++;
            if (!_receiveBreak && _lowTicks > _lowFrameTicks)
            {
                _receiveBreak = true;
                _receiveStage = C.IdleReceive;
                _receiveTicks = 0;
                _lineStatus |= C.BreakInterrupt;
                ScheduleReceiveInterrupt(C.BreakInterrupt);
            }
            if (_receiveBreak) return;
        }
        if (_receiveStage == C.ResynchronizingReceive)
        {
            if (high)
            {
                _receiveStage = C.IdleReceive;
                _receiveTicks = 0;
                return;
            }
            if (++_receiveTicks < C.ResynchronizationSamples) return;
            _receiveControl = _lineControl;
            _receiveStage = 0;
            _receiveValue = 0;
            _receiveParityError = false;
            return;
        }
        if (_receiveStage == C.IdleReceive)
        {
            if (high) { _receiveTicks = 0; return; }
            if (++_receiveTicks < C.HalfBit) return;
            _receiveControl = _lineControl;
            _receiveStage = 0;
            _receiveTicks = _receiveValue = 0;
            _receiveParityError = false;
            return;
        }
        if (++_receiveTicks < C.Oversampling) return;
        _receiveTicks = 0;
        var bits = DataBits(_receiveControl);
        if (_receiveStage < bits)
        {
            if (high) _receiveValue |= 1 << _receiveStage;
            _receiveStage++;
            return;
        }
        if ((_receiveControl & C.ParityEnable) != 0 && _receiveStage == bits)
        {
            _receiveParityError = high != ParityBit(_receiveValue, _receiveControl);
            _receiveStage++;
            return;
        }
        var errors = (_receiveParityError ? C.ParityError : 0) | (!high ? C.FramingError : 0)
            | ((_lineStatus & C.DataReady) != 0 ? C.OverrunError : 0);
        _received = (byte)_receiveValue;
        _lineStatus |= (byte)(C.DataReady | errors);
        _receiveStage = high ? C.IdleReceive : C.ResynchronizingReceive;
        ScheduleReceiveInterrupt((byte)(C.DataReady | errors));
    }
}
