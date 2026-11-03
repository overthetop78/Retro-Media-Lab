using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private bool _shiftActive;
    private bool _shiftArmed;
    private bool _shiftClock = true;
    private bool _shiftClockPin = true;
    private bool _shiftOutputComplete;
    private bool _shiftFinishOutputPending;
    private int _shiftBits;
    private int _shiftClockDelay;
    private bool _externalShiftPending;
    private bool _internalShiftPending;
    private bool _internalShiftValue;
    private bool _shiftInterruptPending;
    private bool _shiftLeadingPending;

    public Action<bool>? Cb1PinsChanged { get; set; }
    public bool Cb1 => _shiftInternalClock ? _shiftClockPin : _cb1;
    private ViaShiftMode _shiftMode;
    private bool _shiftOutput;
    private bool _shiftUsesTimer;
    private bool _shiftInternalClock;
    private bool _shiftSystemClock;

    private void StartShift()
    {
        var previousOutputs = ControlOutputs;
        var keepClockPhase = _shiftActive && _shiftInternalClock;
        _flags &= unchecked((byte)~C.ShiftInterrupt);
        _shiftBits = 0;
        _shiftOutputComplete = _shiftFinishOutputPending = false;
        _externalShiftPending = _internalShiftPending = _shiftInterruptPending = _shiftLeadingPending = false;
        _shiftActive = _shiftArmed = _shiftMode != ViaShiftMode.Disabled;
        if (!keepClockPhase)
        {
            _shiftClockDelay = C.ShiftClockDelay + C.ShiftStartDelay;
            SetShiftClock(true);
        }
        else UpdateShiftLeadingPending();
        EnsureShiftTimer();
        PublishControlOwnership(previousOutputs);
    }

    private void EnsureShiftTimer()
    {
        if (!_shiftUsesTimer || _timer2Running) return;
        _timer2 = C.HighByteMask | _registers[C.Timer2Low];
        _timer2Running = true;
        _timer2LoadDelay = C.TimerLoadCycles;
    }

    private void ConfigureShift(ViaShiftMode previous)
    {
        if (_shiftMode == ViaShiftMode.Disabled) _flags &= unchecked((byte)~C.ShiftInterrupt);
        if (previous == _shiftMode) return;
        _externalShiftPending = _internalShiftPending = false;
        _shiftActive = _shiftArmed && _shiftMode != ViaShiftMode.Disabled;
        if (_shiftMode == ViaShiftMode.Disabled)
        {
            _shiftInterruptPending = false;
        }
        else if (_shiftActive) EnsureShiftTimer();
        if (!_shiftOutput && _shiftMode != ViaShiftMode.Disabled)
            _shiftOutputComplete = _shiftFinishOutputPending = false;
        if (ShiftOwnsCb2) _cb2Pulse = 0;
        else RestoreCb2Control();
        UpdateShiftLeadingPending();
    }

    private void ResetShift()
    {
        _shiftInternalClock = _shiftSystemClock = false;
        _shiftActive = _shiftArmed = _shiftOutputComplete = _shiftFinishOutputPending = false;
        _externalShiftPending = _internalShiftPending = _shiftInterruptPending = _shiftLeadingPending = false;
        _shiftBits = 0;
        _shiftClockDelay = C.ShiftClockDelay;
        SetShiftClock(true);
    }

    private void TickShift()
    {
        if (_externalShiftPending || _internalShiftPending)
        {
            var external = _externalShiftPending;
            _externalShiftPending = false;
            _internalShiftPending = false;
            ShiftInput(external, external ? _cb2 : _internalShiftValue);
        }
        if (!_shiftActive || !_shiftSystemClock || _shiftFinishOutputPending) return;
        if (--_shiftClockDelay != 0) return;
        _shiftClockDelay = C.ShiftClockDelay;
        ToggleShiftClock();
    }

    private void SetShiftClock(bool level)
    {
        _shiftClock = level;
        PublishShiftClock(captureInput: false);
    }

    private void PublishShiftClock(bool captureInput = true)
    {
        if (_shiftClockPin == _shiftClock) return;
        var previous = _shiftClockPin;
        _shiftClockPin = _shiftClock;
        if (_shiftInternalClock)
        {
            CaptureCb1Edge(previous, _shiftClockPin);
            if (captureInput && _shiftClockPin && !_shiftOutput && _shiftActive)
            {
                _internalShiftValue = _cb2;
                _internalShiftPending = true;
            }
        }
        Cb1PinsChanged?.Invoke(_shiftClockPin);
    }

    private void PublishShiftLeadingEdge()
    {
        if (_shiftInternalClock) PublishShiftClock();
        if (_shiftFinishOutputPending && _shiftMode != ViaShiftMode.Disabled)
        {
            var previousOutputs = ControlOutputs;
            _shiftFinishOutputPending = false;
            _flags |= C.ShiftInterrupt;
            _shiftActive = _shiftArmed = _shiftOutputComplete = false;
            RestoreCb2Control();
            PublishControlOwnership(previousOutputs);
        }
        if (_shiftInterruptPending)
        {
            _shiftInterruptPending = false;
            _flags |= C.ShiftInterrupt;
        }
        UpdateShiftLeadingPending();
    }

    private void UpdateShiftLeadingPending() => _shiftLeadingPending =
        (_shiftInternalClock && _shiftClockPin != _shiftClock) || _shiftInterruptPending
        || (_shiftFinishOutputPending && _shiftActive);

    private void ToggleShiftClock()
    {
        if (!_shiftActive || _shiftFinishOutputPending) return;
        _shiftClock = !_shiftClock;
        _shiftLeadingPending = true;
        if (_shiftOutput) ShiftEdge(_shiftClock, false);
    }

    private void ShiftEdge(bool level, bool external)
    {
        if (!_shiftActive) return;
        if (_shiftOutput)
        {
            if (!level)
            {
                var outgoing = (_registers[C.Shift] & C.Pb7) != 0;
                SetControlOutput(false, outgoing);
                _registers[C.Shift] = (byte)((_registers[C.Shift] << 1) | (outgoing ? 1 : 0));
                if (_shiftMode == ViaShiftMode.OutputTimer2Continuous) return;
                if (++_shiftBits != C.ShiftBitCount) return;
                _shiftBits = 0;
                if (external) _flags |= C.ShiftInterrupt;
                else _shiftOutputComplete = true;
            }
            else if (_shiftOutputComplete)
            {
                _shiftFinishOutputPending = true;
            }
        }
    }

    private void ShiftInput(bool external, bool input)
    {
        if (!_shiftActive) return;
        _registers[C.Shift] = (byte)((_registers[C.Shift] << 1) | (input ? 1 : 0));
        if (++_shiftBits != C.ShiftBitCount) return;
        _shiftBits = 0;
        if (external) _flags |= C.ShiftInterrupt;
        else
        {
            _shiftActive = _shiftArmed = false;
            _shiftInterruptPending = true;
            _shiftLeadingPending = true;
        }
    }
}
