using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

public sealed partial class Vdu80CharacterGenerator
{
    private long _timeNanoseconds;
    private long _fraction;
    private long? _romDeadline;
    private long? _loadDeadline;
    private long? _displayDeadline;
    private long? _cursorDeadline;
    private long _nextDeadline = long.MaxValue;
    private bool _dotClockHigh;
    private bool _characterClockHigh;

    public void SetCharacterClock(bool high)
    {
        if (_characterClockHigh == high) return;
        _characterClockHigh = high;
        if (high)
        {
            // NL sees the preceding ROM word before the new latch address propagates.
            _serializer.SetShiftLoad(false);
            _loadDeadline = checked(_timeNanoseconds + C.LoadPulseNanoseconds);
            UpdateDeadline();
            _latch.SetData((byte)(_characterData | (_delayedDisplay ? 0x80 : 0)));
        }
        _latch.SetClock(high);
    }

    /// <summary>Receive a real 12 MHz edge; on rising edges return the preceding dot interval.</summary>
    public bool? SetDotClock(bool high)
    {
        if (_dotClockHigh == high) return _videoWhite;
        _dotClockHigh = high;
        // Exact rational half-period accumulation; no per-edge floating point or division.
        _fraction += C.NanosecondsPerSecond % C.HalfDotFrequency;
        var elapsed = C.NanosecondsPerSecond / C.HalfDotFrequency;
        if (_fraction >= C.HalfDotFrequency)
        {
            _fraction -= C.HalfDotFrequency;
            elapsed++;
        }
        AdvanceLocalTime(checked(_timeNanoseconds + elapsed));
        var sample = _videoWhite;
        _serializer.SetClock(high);
        return sample;
    }

    private void AdvanceLocalTime(long target)
    {
        while (_nextDeadline <= target)
        {
            var next = _nextDeadline;
            _timeNanoseconds = next;
            if (_displayDeadline == next)
            {
                _displayDeadline = null;
                _delayedDisplay = _displayInput;
            }
            if (_romDeadline == next) _rom.AdvanceTo(next);
            if (_cursorDeadline == next) _cursorCircuit.AdvanceTo(next);
            if (_loadDeadline == next)
            {
                _loadDeadline = null;
                _serializer.SetShiftLoad(true);
            }
            UpdateDeadline();
        }
        _timeNanoseconds = target;
    }

    private void UpdateDeadline() => _nextDeadline = Math.Min(_romDeadline ?? long.MaxValue,
        Math.Min(_loadDeadline ?? long.MaxValue,
            Math.Min(_displayDeadline ?? long.MaxValue, _cursorDeadline ?? long.MaxValue)));
}
