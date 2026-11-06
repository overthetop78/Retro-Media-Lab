using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Enums;
using C = RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Constants.Acia6850Constants;

namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

public sealed partial class Acia6850
{
    private Acia6850TransmitState _transmitState;
    private byte _transmitData;
    private byte _transmitShift;
    private bool _transmitPending;
    private bool _transmitLine = true;
    private bool _transmitParity;
    private int _transmitStep;
    private int _transmitStopCount;
    private int _transmitClockCounter;

    /// <summary>Un front descendant TxC. CTS bloque le prochain caractere, pas celui deja en cours.</summary>
    public void ClockTransmit()
    {
        if (_masterReset) return;
        if (_transmitClockCounter > 0 && --_transmitClockCounter != 0) return;
        _transmitClockCounter = _divider;

        if (_transmitState == Acia6850TransmitState.Data)
        {
            bool nextLine;
            if (_transmitStep < _dataBits)
            {
                nextLine = (_transmitShift & (C.LeastSignificantBit << _transmitStep)) != 0;
                _transmitParity ^= nextLine;
                ++_transmitStep;
            }
            else if (_hasParity && _transmitStep == _dataBits)
            {
                nextLine = _transmitParity != _oddParity;
                ++_transmitStep;
            }
            else
            {
                nextLine = true;
                _transmitStopCount = C.OneStopBit;
                _transmitState = Acia6850TransmitState.Stop;
            }
            SetTransmitLine(nextLine);
            return;
        }
        if (_transmitState == Acia6850TransmitState.Stop && _transmitStopCount < _stopBits)
        {
            ++_transmitStopCount;
            return;
        }

        _transmitState = Acia6850TransmitState.Idle;
        if (!_transmitPending || _ctsBlocked || _break)
        {
            SetTransmitLine(true);
            return;
        }
        _transmitShift = _transmitData;
        _transmitPending = false;
        _transmitStep = 0;
        _transmitParity = false;
        _transmitState = Acia6850TransmitState.Data;
        var lineChanged = _transmitLine;
        _transmitLine = false;
        _status |= C.TransmitDataEmpty;
        UpdateIrq();
        if (lineChanged) TransmitPinsChanged?.Invoke(false, RtsHigh);
    }

    /// <summary>Adapte une periode de l'horloge de carte a TxC et a l'echantillonnage DCD.
    /// RxC peut etre fourni independamment par ClockReceive.</summary>
    public void Tick()
    {
        _clockPhase += _inputClockRate;
        if (_clockPhase < _ticksPerSecond) return;
        do
        {
            _clockPhase -= _ticksPerSecond;
            SampleDcd();
            ClockTransmit();
        }
        while (_clockPhase >= _ticksPerSecond);
    }
}
