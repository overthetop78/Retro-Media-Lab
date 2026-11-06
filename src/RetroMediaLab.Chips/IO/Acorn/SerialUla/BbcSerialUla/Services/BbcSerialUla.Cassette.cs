using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

public sealed partial class BbcSerialUla
{
    private bool? _cassetteInput;
    private bool _separatorActive;
    private bool _previousLevel;
    private bool _hasLevel;
    private bool _cassetteData = true;
    private int _edgeTicks;
    private int _carrierEdges;
    private int _carrierPulse;
    private long _synthesisPhase;
    private int _synthesisStep;

    public void SetCassetteInput(bool? level)
    {
        if (_cassetteInput == level) return;
        FlushPendingClock();
        _clockCountdown = 0;
        _cassetteInput = level;
        _separatorActive = true;
    }

    private void ResetSeparator()
    {
        _cassetteInput = null;
        _separatorActive = true;
        _hasLevel = false;
        _cassetteData = true;
        _edgeTicks = _carrierEdges = _carrierPulse = 0;
    }

    private void TickSeparator()
    {
        var pinsChanged = false;
        var silenceExpired = false;
        if (_carrierPulse > 0 && --_carrierPulse == 0) pinsChanged = true;
        if (_edgeTicks <= _silenceTicks)
        {
            ++_edgeTicks;
            silenceExpired = _edgeTicks > _silenceTicks;
        }
        if (_cassetteInput is { } level)
        {
            if (_hasLevel && level != _previousLevel)
            {
                var duration = _edgeTicks;
                _edgeTicks = 0;
                if (duration >= _minimumHalfWaveTicks && duration <= _maximumHalfWaveTicks)
                {
                    var data = duration < _halfWaveThresholdTicks;
                    if (_cassetteData != data)
                    {
                        _cassetteData = data;
                        pinsChanged = true;
                    }
                    if (!_cassetteData) _carrierEdges = 0;
                    else
                    {
                        if (_carrierEdges < C.CarrierHalfWaves) ++_carrierEdges;
                        if (_carrierEdges == C.CarrierHalfWaves)
                        {
                            if (_carrierPulse == 0) pinsChanged = true;
                            _carrierPulse = _carrierPulseTicks;
                        }
                    }
                }
                else _carrierEdges = 0;
            }
            if (!_hasLevel) _edgeTicks = 0;
            _hasLevel = true;
            _previousLevel = level;
        }
        else _hasLevel = false;
        if (silenceExpired && _edgeTicks > _silenceTicks)
        {
            pinsChanged |= !_cassetteData || _carrierPulse != 0;
            _cassetteData = true;
            _carrierEdges = _carrierPulse = 0;
        }
        if (pinsChanged && !_rs423Selected) PublishSerialPins();
        // Une fois le silence établi, les niveaux et le compteur saturé ne
        // peuvent plus évoluer sans changement de l'entrée cassette.
        if (_edgeTicks > _silenceTicks && _carrierPulse == 0) _separatorActive = false;
    }

    private void TickSynthesis(bool transmitData, bool disabled)
    {
        var period = _clockDenominator * (transmitData ? C.HighToneDivider : C.LowToneDivider);
        _synthesisPhase += (long)C.MasterClock * C.SynthesisSteps;
        while (_synthesisPhase >= period)
        {
            _synthesisPhase -= period;
            _synthesisStep = (_synthesisStep + 1) % C.SynthesisSteps;
        }
        _cassetteOutput = disabled ? (short)0 : C.Sine[_synthesisStep];
    }
}
