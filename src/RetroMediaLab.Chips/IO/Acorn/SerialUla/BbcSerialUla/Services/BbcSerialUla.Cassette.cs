using C = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services;

public sealed partial class BbcSerialUla
{
    private bool? _cassetteInput;
    private bool _previousLevel;
    private bool _hasLevel;
    private bool _cassetteData = true;
    private int _edgeTicks;
    private int _carrierEdges;
    private int _carrierPulse;
    private long _synthesisPhase;
    private int _synthesisStep;

    public void SetCassetteInput(bool? level) => _cassetteInput = level;

    private void ResetSeparator()
    {
        _cassetteInput = null;
        _hasLevel = false;
        _cassetteData = true;
        _edgeTicks = _carrierEdges = _carrierPulse = 0;
    }

    private void TickSeparator()
    {
        if (_carrierPulse > 0) --_carrierPulse;
        if (_edgeTicks <= _silenceTicks) ++_edgeTicks;
        if (_cassetteInput is { } level)
        {
            if (_hasLevel && level != _previousLevel)
            {
                var duration = _edgeTicks;
                _edgeTicks = 0;
                if (duration >= _minimumHalfWaveTicks && duration <= _maximumHalfWaveTicks)
                {
                    _cassetteData = duration < _halfWaveThresholdTicks;
                    if (!_cassetteData) _carrierEdges = 0;
                    else
                    {
                        if (_carrierEdges < C.CarrierHalfWaves) ++_carrierEdges;
                        if (_carrierEdges == C.CarrierHalfWaves) _carrierPulse = _carrierPulseTicks;
                    }
                }
                else _carrierEdges = 0;
            }
            if (!_hasLevel) _edgeTicks = 0;
            _hasLevel = true;
            _previousLevel = level;
        }
        else _hasLevel = false;
        if (_edgeTicks > _silenceTicks)
        {
            _cassetteData = true;
            _carrierEdges = _carrierPulse = 0;
        }
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
        CassetteOutput = disabled ? (short)0 : C.Sine[_synthesisStep];
    }
}
