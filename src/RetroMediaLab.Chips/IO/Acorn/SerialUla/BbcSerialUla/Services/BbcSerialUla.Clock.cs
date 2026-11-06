using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

public sealed partial class BbcSerialUla
{
    private int _pendingClockTicks;
    private int _clockCountdown;

    /// <summary>Un front de l'horloge d'entrée ; les intervalles sans sortie ni échéance sont conservés localement.</summary>
    public void Tick()
    {
        if (_clockCountdown > 1)
        {
            --_clockCountdown;
            ++_pendingClockTicks;
            _transmitClockEdge = _receiveClockEdge = false;
            return;
        }
        FlushPendingClock();
        ClockInputs(_transmitInput, _requestToSendInput);
        // Une entrée mémorisée reste constante jusqu'à sa notification. Les
        // compteurs du séparateur peuvent avancer jusqu'à leur première échéance.
        _clockCountdown = _transmitDivider > 1
            && (!_rs423Selected || _receiveDivider > 1) ? NextClockDistance() : 0;
        if (_separatorActive && _clockCountdown > 1)
        {
            var separator = _edgeTicks <= _silenceTicks ? _silenceTicks - _edgeTicks + 1 : int.MaxValue;
            if (_carrierPulse > 0 && _carrierPulse < separator) separator = _carrierPulse;
            if (separator < _clockCountdown) _clockCountdown = separator;
        }
    }

    /// <summary>Chemin unitaire, avec présentation des entrées pour ce front seulement.</summary>
    public void Tick(bool transmitData, bool requestToSendHigh)
    {
        FlushPendingClock();
        _clockCountdown = 0;
        ClockInputs(transmitData, requestToSendHigh);
    }

    private int NextClockDistance()
    {
        var transmit = _transmitDivider - (_dividerCounter & (_transmitDivider - 1));
        var divider = _rs423Selected ? _receiveDivider : C.CassetteClockDivider;
        var receive = divider - (_dividerCounter & (divider - 1));
        var edges = transmit < receive ? transmit : receive;
        var clocks = (edges * _clockDenominator - _sourcePhase + C.MasterClock - 1) / C.MasterClock;
        var increment = (long)C.MasterClock * C.SynthesisSteps;
        var period = _clockDenominator * (_transmitInput ? C.HighToneDivider : C.LowToneDivider);
        var synthesis = (period - _synthesisPhase + increment - 1) / increment;
        return (int)(clocks < synthesis ? clocks : synthesis);
    }

    private void FlushPendingClock()
    {
        if (_pendingClockTicks == 0) return;
        // L'intervalle s'arrête AVANT le premier front TxC/RxC ou pas de
        // synthèse. Seuls les diviseurs libres et leurs résidus ont avancé.
        var phase = _sourcePhase + (long)_pendingClockTicks * C.MasterClock;
        _dividerCounter = (int)((_dividerCounter + phase / _clockDenominator) & C.DividerCounterMask);
        _sourcePhase = phase % _clockDenominator;
        _synthesisPhase += (long)_pendingClockTicks * C.MasterClock * C.SynthesisSteps;
        if (_separatorActive)
        {
            if (_edgeTicks <= _silenceTicks)
                _edgeTicks = Math.Min(_silenceTicks + 1, _edgeTicks + _pendingClockTicks);
            if (_carrierPulse > 0) _carrierPulse -= _pendingClockTicks;
        }
        _pendingClockTicks = 0;
    }
}
