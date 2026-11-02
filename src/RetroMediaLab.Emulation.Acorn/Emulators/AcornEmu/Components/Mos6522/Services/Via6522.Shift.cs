using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Constants.Via6522Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Services;

internal sealed partial class Via6522
{
    private bool _shiftActive;
    private bool _shiftClock = true;
    private bool _shiftOutputComplete;
    private int _shiftBits;
    private int _shiftClockDelay;
    private int _shiftIrqDelay;

    internal Action<bool>? Cb1PinsChanged { get; set; }
    internal bool Cb1 => ShiftMode is ViaShiftMode.Disabled or ViaShiftMode.InputExternalClock or ViaShiftMode.OutputExternalClock
        ? _cb1 : _shiftClock;
    private ViaShiftMode ShiftMode => (ViaShiftMode)(_registers[C.Auxiliary] >> C.ShiftModeShift & C.ShiftModeMask);
    private bool ShiftOutput => ShiftMode >= ViaShiftMode.OutputTimer2Continuous;
    private bool ShiftUsesTimer => ShiftMode is ViaShiftMode.InputTimer2 or ViaShiftMode.OutputTimer2Continuous or ViaShiftMode.OutputTimer2;

    private void StartShift()
    {
        _flags &= unchecked((byte)~C.ShiftInterrupt);
        _shiftBits = _shiftIrqDelay = 0;
        _shiftOutputComplete = false;
        _shiftActive = ShiftMode != ViaShiftMode.Disabled;
        _shiftClockDelay = C.ShiftClockDelay;
        SetShiftClock(true);
        if (!ShiftUsesTimer || _timer2Running) return;
        _timer2 = C.HighByteMask | _registers[C.Timer2Low];
        _timer2Running = true;
        _timer2LoadDelay = C.TimerLoadCycles;
    }

    private void ConfigureShift(ViaShiftMode previous)
    {
        if (previous == ShiftMode) return;
        _shiftActive = _shiftOutputComplete = false;
        _shiftBits = _shiftIrqDelay = 0;
        SetShiftClock(true);
        ConfigureControlOutputs();
    }

    private void ResetShift()
    {
        _shiftActive = _shiftOutputComplete = false;
        _shiftBits = _shiftIrqDelay = 0;
        _shiftClockDelay = C.ShiftClockDelay;
        SetShiftClock(true);
    }

    private void TickShift()
    {
        if (_shiftIrqDelay > 0 && --_shiftIrqDelay == 0) _flags |= C.ShiftInterrupt;
        if (!_shiftActive || ShiftMode is not (ViaShiftMode.InputSystemClock or ViaShiftMode.OutputSystemClock)) return;
        if (--_shiftClockDelay != 0) return;
        _shiftClockDelay = C.ShiftClockDelay;
        ToggleShiftClock();
    }

    private void SetShiftClock(bool level)
    {
        if (_shiftClock == level) return;
        _shiftClock = level;
        Cb1PinsChanged?.Invoke(level);
    }

    private void ToggleShiftClock()
    {
        if (!_shiftActive) return;
        SetShiftClock(!_shiftClock);
        ShiftEdge(_shiftClock, false);
    }

    private void ShiftEdge(bool level, bool external)
    {
        if (!_shiftActive) return;
        if (ShiftOutput)
        {
            if (!level)
            {
                var outgoing = (_registers[C.Shift] & C.Pb7) != 0;
                SetControlOutput(false, outgoing);
                _registers[C.Shift] = (byte)((_registers[C.Shift] << 1) | (outgoing ? 1 : 0));
                if (++_shiftBits != C.ShiftBitCount) return;
                _shiftBits = 0;
                if (ShiftMode == ViaShiftMode.OutputTimer2Continuous) return;
                if (external) _flags |= C.ShiftInterrupt;
                else _shiftOutputComplete = true;
            }
            else if (_shiftOutputComplete)
            {
                _flags |= C.ShiftInterrupt;
                _shiftActive = _shiftOutputComplete = false;
            }
        }
        else if (level)
        {
            _registers[C.Shift] = (byte)((_registers[C.Shift] << 1) | (_cb2 ? 1 : 0));
            if (++_shiftBits != C.ShiftBitCount) return;
            _shiftBits = 0;
            if (external) _flags |= C.ShiftInterrupt;
            else
            {
                _shiftActive = false;
                _shiftIrqDelay = C.ShiftClockDelay;
            }
        }
    }
}
