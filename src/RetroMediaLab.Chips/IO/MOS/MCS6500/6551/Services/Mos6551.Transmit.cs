using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Constants.Mos6551Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;

public sealed partial class Mos6551
{
    private Mos6551TransmitState _transmitState;
    private Mos6551TransmitOutput _transmitOutput;
    private byte _transmitShift;
    private int _transmitTicks;
    private int _transmitStep;
    private bool _transmitParity;
    private bool _breakPending;
    private bool _pendingTransmitInterrupt;

    private void TransmitClock()
    {
        if (_pendingTransmitInterrupt)
        {
            _pendingTransmitInterrupt = false;
            if (_transmitInterrupt) RaiseInterrupt(C.TransmitEmpty);
        }
        if (_echo)
        {
            SetTransmit(!_cts && !_echoAwaitStart && (_status & C.Overrun) == 0 ? _echoDelayedRxd : true);
            return;
        }
        // Une écriture en période MARK n'attend pas le prochain cycle périodique d'IRQ.
        if (_transmitOutput == Mos6551TransmitOutput.Mark && !_cts
            && ((!Rts && (_status & C.TransmitEmpty) == 0) || _breakPending))
            _transmitState = Mos6551TransmitState.Start;
        if (_transmitState == Mos6551TransmitState.Start)
        {
            StartTransmit();
            return;
        }
        ++_transmitTicks;
        if (_transmitState == Mos6551TransmitState.Data)
        {
            if (_transmitTicks < C.Oversampling) return;
            _transmitTicks = 0;
            if (_transmitStep < _dataBits)
            {
                var bit = (_transmitShift & (C.LeastSignificantBit << _transmitStep)) != 0;
                _transmitParity ^= bit;
                ++_transmitStep;
                DriveTransmit(bit);
            }
            else if (_parity != 0 && _transmitStep == _dataBits)
            {
                var bit = _parity switch
                {
                    C.OddParity => !_transmitParity,
                    C.EvenParity => _transmitParity,
                    C.MarkParity => true,
                    _ => false
                };
                ++_transmitStep;
                DriveTransmit(bit);
            }
            else
            {
                _transmitState = Mos6551TransmitState.Stop;
                DriveTransmit(true);
            }
            return;
        }
        if (_transmitTicks < _stopTicks) return;
        if (_transmitOutput == Mos6551TransmitOutput.Break)
        {
            if (_break) { _transmitTicks = _stopTicks; return; }
            _transmitOutput = Mos6551TransmitOutput.Data;
            _transmitTicks = 0;
            DriveTransmit(true); // Le break complet est suivi d'un stop avant le prochain start.
            return;
        }
        StartTransmit();
    }

    private void StartTransmit()
    {
        _transmitTicks = _transmitStep = 0;
        _transmitParity = false;
        _transmitShift = _transmitData;
        _transmitState = Mos6551TransmitState.Data;
        if (_cts || Rts && !_breakPending)
            _transmitOutput = Mos6551TransmitOutput.Mark;
        else if ((_status & C.TransmitEmpty) == 0)
        {
            _transmitOutput = Mos6551TransmitOutput.Data;
            _status |= C.TransmitEmpty;
        }
        else if (_break || _breakPending)
        {
            _transmitOutput = Mos6551TransmitOutput.Break;
            _breakPending = false;
        }
        else
            _transmitOutput = Mos6551TransmitOutput.Mark;
        _pendingTransmitInterrupt = _transmitInterrupt && _transmitOutput != Mos6551TransmitOutput.Break;
        DriveTransmit(false);
    }

    private void DriveTransmit(bool bit) => SetTransmit(_cts || _transmitOutput == Mos6551TransmitOutput.Mark
        || (_transmitOutput != Mos6551TransmitOutput.Break && bit));
}
