using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using SerialPhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271SerialPhase;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private long _inputClockCount;
    private long? _publishedClockEvent;

    // Reference path for differential checks; clocks and bus transactions are unchanged.
    internal bool GroupSerialReadClocks { get; set; } = true;

    public int InputClockFrequency => _clock;
    public int ControllerClockFrequency => _chipClock;
    public long InputClockCount => _inputClockCount;
    public event Action<long?>? NextClockEventChanged;

    /// <summary>Instant du prochain traitement interne, en périodes de l'entrée temporelle.
    /// Les entrées de broches peuvent provoquer une échéance plus proche.</summary>
    public long? NextClockEvent
    {
        get
        {
            if (_resetAsserted) return null;
            if (_commandPending || _parameterPending) return checked(_inputClockCount + 1);
            if (_phase == Phase.Idle) return null;
            if (_serialPhase != SerialPhase.Inactive)
            {
                if (_serialPhase == SerialPhase.Writing) return checked(_inputClockCount + 1);
                var cycles = SerialReadClocksToEvent();
                return cycles.HasValue ? checked(_inputClockCount + cycles.Value) : null;
            }
            if (_phase == Phase.ExternalSeek && _delay <= 0) return null;
            return checked(_inputClockCount + Math.Max(1, _delay));
        }
    }

    /// <summary>Avance jusqu'au temps fourni par le câblage. Aucun cycle série actif n'est supprimé.</summary>
    public void AdvanceTo(long inputClockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(inputClockCount, _inputClockCount);
        if (inputClockCount == _inputClockCount) return;
        // Une seule période ne peut amortir l'admission des préfixes de la boucle.
        // Réutiliser le même Tick, avec ses effets et sa publication d'échéance.
        if (inputClockCount - _inputClockCount == 1) { Tick(); return; }
        while (_inputClockCount < inputClockCount)
        {
            if (_resetAsserted || (!_commandPending && !_parameterPending &&
                (_phase == Phase.Idle || (_phase == Phase.ExternalSeek && _delay <= 0))))
            {
                _inputClockCount = inputClockCount;
                break;
            }
            if (!_commandPending && !_parameterPending && _serialPhase == SerialPhase.Inactive && _delay > 1)
            {
                var skipped = (int)Math.Min(inputClockCount - _inputClockCount, _delay - 1);
                _delay -= skipped;
                _inputClockCount += skipped;
                continue;
            }
            if (!_commandPending && !_parameterPending && _serialPhase != SerialPhase.Inactive)
            {
                var next = SerialReadClocksToEvent();
                var skipped = Math.Min(inputClockCount - _inputClockCount, next.HasValue ? next.Value - 1 : long.MaxValue);
                if (skipped > 0)
                {
                    AdvanceQuietSerialReadClocks(skipped);
                    _inputClockCount += skipped;
                    continue;
                }
            }
            ++_inputClockCount;
            TickCore();
        }
        PublishClockEvent();
    }

    private long? SerialReadClocksToEvent()
    {
        // Writes can publish a pulse on each controller cycle. Keep their exact
        // scalar path, as well as the reference path used by differential checks.
        if (!GroupSerialReadClocks || _serialPhase == SerialPhase.Writing) return 1;
        long? next = _awaitingData ? Math.Max(1, _serialRequestTicks) : null;
        if (_serialClockStarted)
        {
            var remaining = (long)_clock * Constants.Intel8271Constants.ChipCyclesPerBit - _serialFraction;
            var bitCycles = Math.Max(1, (remaining + _chipClock - 1) / _chipClock);
            next = next.HasValue ? Math.Min(next.Value, bitCycles) : bitCycles;
        }
        return next;
    }

    private void AdvanceQuietSerialReadClocks(long cycles)
    {
        // Only arithmetic updates before the next bit/timeout are represented.
        // Input changes are still applied after AdvanceTo by the physical wiring.
        var bitPeriod = (long)_clock * Constants.Intel8271Constants.ChipCyclesPerBit;
        if (_readPulseSeen && _readPulseGap < bitPeriod)
        {
            var toSaturation = (bitPeriod - _readPulseGap + _chipClock - 1) / _chipClock;
            _readPulseGap += Math.Min(cycles, toSaturation) * _chipClock;
        }
        if (_awaitingData) _serialRequestTicks -= (int)cycles;
        if (_serialClockStarted) _serialFraction += cycles * _chipClock;
    }

    private void PublishClockEvent()
    {
        var next = NextClockEvent;
        if (_publishedClockEvent == next) return;
        _publishedClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }
}
