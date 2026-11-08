using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using SerialPhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271SerialPhase;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private long _inputClockCount;
    private long? _publishedClockEvent;

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
            if (_serialPhase != SerialPhase.Inactive) return checked(_inputClockCount + 1);
            if (_phase == Phase.ExternalSeek && _delay <= 0) return null;
            return checked(_inputClockCount + Math.Max(1, _delay));
        }
    }

    /// <summary>Avance jusqu'au temps fourni par le câblage. Aucun cycle série actif n'est supprimé.</summary>
    public void AdvanceTo(long inputClockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(inputClockCount, _inputClockCount);
        if (inputClockCount == _inputClockCount) return;
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
            ++_inputClockCount;
            TickCore();
        }
        PublishClockEvent();
    }

    private void PublishClockEvent()
    {
        var next = NextClockEvent;
        if (_publishedClockEvent == next) return;
        _publishedClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }
}
