using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;
using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Enums;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool _transmitLine = true;
    private bool _transmitActive;
    private bool _transmitIdle = true;
    private uint _transmitFrame;
    private int _transmitRemaining;
    private int _transmitTicks;
    private int _transmitStopTicks;
    private int _transmitStartTicks;
    private int _transmitEmptyTicks;
    private int _transmitInterruptTicks;

    private void WriteHolding(byte value)
    {
        if (!_transmitActive && !_holdingFull) _transmitStartTicks = C.TransmitStartDelay;
        _holding = value;
        _transmitIdle = false;
        _holdingFull = true;
        _transmitEmptyTicks = _transmitInterruptTicks = 0;
        _lineStatus &= unchecked((byte)~(C.HoldingEmpty | C.TransmitterEmpty));
        _transmitInterrupt = false;
    }

    private void TransmitterEmptyClock()
    {
        if (_transmitEmptyTicks != 0 && --_transmitEmptyTicks == 0
            && !_transmitActive && !_holdingFull)
        {
            _lineStatus |= C.TransmitterEmpty;
            _transmitIdle = _transmitInterruptTicks == 0;
        }
    }

    private void TransmitClock()
    {
        if (!_externalReceiveClock) TransmitterEmptyClock();
        if (_transmitInterruptTicks != 0 && --_transmitInterruptTicks == 0)
        {
            _transmitInterrupt = true;
            UpdateInterrupt();
            if (_reset) return;
        }
        var queued = false;
        if (_transmitActive)
        {
            if (--_transmitTicks > 0) return;
            if (_transmitRemaining > 0)
            {
                _transmitLine = (_transmitFrame & 1) != 0;
                _transmitFrame >>= 1;
                _transmitRemaining--;
                _transmitTicks = _transmitRemaining == 0 ? _transmitStopTicks : C.Oversampling;
                RefreshOutputs();
                return;
            }
            _transmitActive = false;
            queued = _holdingFull;
            if (!queued)
            {
                _transmitEmptyTicks = C.TransmitEmptyDelay;
                _transmitIdle = _externalReceiveClock && _transmitInterruptTicks == 0;
            }
        }
        if (!_holdingFull)
        {
            _transmitLine = true;
            RefreshOutputs();
            return;
        }
        if (_busWriteActive && _busWriteRegister == (int)Ns16450Register.DataDivisorLow && !DivisorAccess)
            return;
        if (_transmitStartTicks != 0 && --_transmitStartTicks != 0) return;
        var bits = DataBits(_lineControl);
        var data = _holding & ((1 << bits) - 1);
        _transmitFrame = (uint)data;
        if ((_lineControl & C.ParityEnable) != 0)
        {
            if (ParityBit(data, _lineControl)) _transmitFrame |= 1u << bits;
            bits++;
        }
        _transmitFrame |= 1u << bits;
        _transmitRemaining = bits + 1;
        _transmitStopTicks = StopTicks(_lineControl);
        _transmitTicks = C.Oversampling;
        _transmitActive = true;
        _transmitLine = false;
        _holdingFull = false;
        _lineStatus |= C.HoldingEmpty;
        _lineStatus &= unchecked((byte)~C.TransmitterEmpty);
        if (queued) _transmitInterruptTicks = C.QueuedTransmitInterruptDelay;
        else _transmitInterrupt = true;
        RefreshOutputs();
        UpdateInterrupt();
    }

}
