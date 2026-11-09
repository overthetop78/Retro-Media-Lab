namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

/// <summary>400,019 R12/R13/C6 response, normalized to the established high voltage.</summary>
/// <remarks>The switching fraction and initial capacitor charge are explicit model
/// parameters, not measured TTL characteristics. No oscillator or reset exists.</remarks>
public sealed class Vdu80CursorCircuit
{
    private readonly double _threshold;
    private double _voltageAtChange;
    private long _changedAt;
    private bool _inputHigh;
    private long _timeNanoseconds;
    private long? _nextEvent;
    private bool _outputHigh;

    public Vdu80CursorCircuit(double seriesOhms, double shuntOhms, double capacitancePicofarads,
        double switchingFraction, double initialNormalizedVoltage)
    {
        PositiveFinite(seriesOhms);
        PositiveFinite(shuntOhms);
        PositiveFinite(capacitancePicofarads);
        if (!double.IsFinite(switchingFraction) || switchingFraction <= 0 || switchingFraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(switchingFraction));
        if (!double.IsFinite(initialNormalizedVoltage) || initialNormalizedVoltage < 0 || initialNormalizedVoltage > 1)
            throw new ArgumentOutOfRangeException(nameof(initialNormalizedVoltage));
        DcGain = shuntOhms / (seriesOhms + shuntOhms);
        TimeConstantNanoseconds = seriesOhms * DcGain * capacitancePicofarads / 1000;
        PositiveFinite(TimeConstantNanoseconds);
        _threshold = switchingFraction;
        _voltageAtChange = initialNormalizedVoltage;
        _outputHigh = initialNormalizedVoltage >= switchingFraction;
        ScheduleCrossing();
    }

    public double DcGain { get; }
    public double TimeConstantNanoseconds { get; }
    public long TimeNanoseconds => _timeNanoseconds;
    public bool InputHigh => _inputHigh;
    public bool OutputHigh => _outputHigh;
    public long? NextEventNanoseconds => _nextEvent;
    public double NormalizedVoltage => VoltageAt(_timeNanoseconds);
    public Action<bool>? OutputChanged { get; set; }
    public Action<long?>? NextEventNanosecondsChanged { get; set; }

    public void SetInput(bool high)
    {
        if (_inputHigh == high) return;
        _voltageAtChange = VoltageAt(_timeNanoseconds);
        _changedAt = _timeNanoseconds;
        _inputHigh = high;
        ScheduleCrossing();
    }

    public void AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _timeNanoseconds);
        while (_nextEvent is { } due && due <= nanoseconds)
        {
            _timeNanoseconds = due;
            _outputHigh = _inputHigh;
            SetNext(null);
            OutputChanged?.Invoke(_outputHigh);
        }
        _timeNanoseconds = nanoseconds;
    }

    private double VoltageAt(long time)
    {
        var target = _inputHigh ? 1.0 : 0.0;
        return target + (_voltageAtChange - target)
            * Math.Exp(-(time - _changedAt) / TimeConstantNanoseconds);
    }

    private void ScheduleCrossing()
    {
        if (_inputHigh == _outputHigh)
        {
            SetNext(null);
            return;
        }
        var target = _inputHigh ? 1.0 : 0.0;
        var delay = -TimeConstantNanoseconds * Math.Log((_threshold - target) / (_voltageAtChange - target));
        var duration = Math.Max(1, checked((long)Math.Ceiling(delay)));
        SetNext(checked(_timeNanoseconds + duration));
    }

    private void SetNext(long? next)
    {
        if (_nextEvent == next) return;
        _nextEvent = next;
        NextEventNanosecondsChanged?.Invoke(next);
    }

    private static void PositiveFinite(double value)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
