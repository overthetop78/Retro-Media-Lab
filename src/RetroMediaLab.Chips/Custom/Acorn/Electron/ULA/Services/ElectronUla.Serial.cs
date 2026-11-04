using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronUlaConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronUla
{
    private byte _received;
    private int _transmitBits;
    private bool _transmitSynchronized;
    private int _transmitFrame;
    private bool _outputLevel;
    private bool _lastInputLevel;
    private bool _haveInputEdge;
    private int _receiveStep = -1;
    private int _receivePhase;
    private int _carrierPhase;
    private int _carrierBits;
    private bool _carrierReported;
    private bool _receiveMark;

    private void ResetSerial()
    {
        _received = 0;
        _transmitBits = _transmitFrame = 0;
        _transmitSynchronized = false;
        _outputLevel = false;
        ResetReceiver();
    }

    private void ResetReceiver()
    {
        _receiveStep = -1;
        _receivePhase = _carrierPhase = _carrierBits = 0;
        _carrierReported = false;
        _receiveMark = false;
        _lastInputLevel = _haveInputEdge = false;
    }

    private void TickSerial()
    {
        if (_comms == C.TapeOutputMode)
        {
            TickTransmitter();
            return;
        }
        if (_comms == C.SoundMode)
        {
            TickTransmitCounter(_commsCounter.TickSound(), shiftData: false);
            if (_commsCounter.ReceiveClockEdge) TickReceiveCounter(_receiveMark);
        }
        _cassette.SetOutputLevel(false);
        // Selecting another source does not assert the receiver reset pins.
        // In unused mode there are no counter clocks: preserve the entire receive phase.
        if (_comms != C.TapeInputMode) return;
        // Media presence belongs to the transport, not the receiver reset circuit.
        // An absent signal presents a stable low comparator level.
        var quietPhase = _commsCounter.TickInput();
        var level = _cassette.InputPresent && _cassette.InputLevel;
        if (level != _lastInputLevel)
        {
            var period = _commsCounter.TakeInputPeriod();
            if (_haveInputEdge) ReceivePeriod(period);
            _haveInputEdge = true;
        }
        else if (quietPhase) TickReceiveCounter(_receiveMark);
        _lastInputLevel = level;
    }

    private void TickTransmitter()
    {
        if (_transmitBits == 0) _status |= C.TransmitEmptyInterrupt;

        TickTransmitCounter(_commsCounter.TickOutput(), shiftData: true);
        if (_commsCounter.ReceiveClockEdge) TickReceiveCounter(_receiveMark);
        var mark = _transmitBits == 0 || !_transmitSynchronized || (_transmitFrame & 1) != 0;
        _outputLevel = mark ? _commsCounter.MarkLevel : _commsCounter.SpaceLevel;
        _cassette.SetOutputLevel(_outputLevel);
    }

    private void TickTransmitCounter(bool frameClock, bool shiftData)
    {
        if (!frameClock || _transmitBits == 0) return;
        if (!_transmitSynchronized) _transmitSynchronized = true;
        else
        {
            // The start cell is separate from DREG. Data shifts only in output mode.
            if (_transmitBits == C.TapeFrameBits || shiftData) _transmitFrame >>= 1;
            _transmitBits--;
            if (_transmitBits == C.TapeTransmitReadyBits) _status |= C.TransmitEmptyInterrupt;
        }
    }

    private void LoadTransmitter(byte value)
    {
        _transmitFrame = (value << C.TapeFirstDataPosition) | (1 << C.TapeStopBitPosition);
        _transmitBits = C.TapeFrameBits;
        _transmitSynchronized = false;
        _status &= unchecked((byte)~C.TransmitEmptyInterrupt);
    }

    private void ReceivePeriod(int period)
    {
        var mark = period < C.TapePeriodThreshold;
        _receiveMark = mark;
        var clocks = mark ? C.CommsCounterStep : C.TapeReceiveMidpoint;
        if (_receiveStep < 0)
        {
            if (mark)
            {
                if (++_carrierPhase < C.TapeMarkPeriodsPerBit) return;
                _carrierPhase = 0;
                if (!_carrierReported && ++_carrierBits >= C.TapeCarrierBits)
                {
                    _carrierReported = true;
                    _status |= C.HighToneInterrupt;
                    _status &= unchecked((byte)~C.ReceiveFullInterrupt);
                }
                return;
            }
            else
            {
                _receiveStep = 0;
                _receivePhase = _carrierPhase = _carrierBits = 0;
                _carrierReported = false;
            }
        }

        for (var clock = 0; clock < clocks; clock++) TickReceiveCounter(mark);
    }

    private void TickReceiveCounter(bool mark)
    {
        if (_receiveStep < 0)
        {
            if (mark) return;
            _receiveStep = 0;
            _receivePhase = _carrierPhase = _carrierBits = 0;
            _carrierReported = false;
        }
        _receivePhase = (_receivePhase + C.CommsCounterStep) & C.TapeReceivePhaseMask;
        if (_receivePhase != C.TapeReceiveMidpoint) return;
        if (_receiveStep == C.TapeReceiveStartAndDataBits)
        {
            if (mark)
            {
                // Stop and carrier leave CD intact and hold the phase counter rearmed.
                _receiveStep = -1;
                _receivePhase = 0;
                return;
            }
            // A low stop is also the next start; it must pass through CD.
            _receiveStep = 0;
        }
        if (_receiveStep == 0) _status &= unchecked((byte)~C.ReceiveFullInterrupt);
        _received = (byte)((_received >> C.TapeReceiveShift) | (mark ? C.TapeReceiveHighBit : 0));
        if (++_receiveStep == C.TapeReceiveStartAndDataBits) _status |= C.ReceiveFullInterrupt;
    }
}
