using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private bool _timer1Armed;
    private bool _timer1Reload;
    private int _timer1LoadDelay;
    private int _timer2LoadDelay;
    private bool _timer2Reload;
    private bool _timerPb7 = true;
    private bool _timer1JustFired;
    private bool _timer2JustFired;

    private byte ApplyTimerPb7(byte pins) => (_registers[C.Auxiliary] & C.Timer1Pb7) == 0
        ? pins : _timerPb7 ? (byte)(pins | C.Pb7) : (byte)(pins & ~C.Pb7);

    private void TickTimers()
    {
        _timer1JustFired = _timer2JustFired = false;
        if (_timer1Running)
        {
            if (_timer1LoadDelay > 0) _timer1LoadDelay--;
            else if (_timer1Reload)
            {
                _timer1 = _latch1;
                _timer1Reload = false;
            }
            else if (--_timer1 < 0)
            {
                _timer1 = C.CounterMask;
                if (_timer1Armed)
                {
                    _flags |= C.Timer1Interrupt;
                    _timer1JustFired = true;
                    _timerPb7 = !_timerPb7;
                    PortBPinsChanged?.Invoke(PortBPins);
                }
                var continuous = (_registers[C.Auxiliary] & C.Timer1Continuous) != 0;
                _timer1Armed &= continuous;
                _timer1Reload = continuous;
            }
        }
        if (!_timer2Running) return;
        if ((_registers[C.Auxiliary] & C.Timer2PulseCount) != 0)
        {
            var level = Pb6Level;
            if (_lastPb6 && !level) CountTimer2(true);
            _lastPb6 = level;
            return;
        }
        if (_timer2LoadDelay > 0) { _timer2LoadDelay--; return; }
        if (!ShiftUsesTimer) { CountTimer2(false); return; }
        if (_timer2Reload)
        {
            _timer2 = (_timer2 & C.HighByteMask) | _registers[C.Timer2Low];
            _timer2Reload = false;
        }
        else if ((_timer2 & byte.MaxValue) == 0)
        {
            var high = ((_timer2 >> C.HighByteShift) - 1) & byte.MaxValue;
            _timer2 = (high << C.HighByteShift) | byte.MaxValue;
            _timer2Reload = true;
            if (high == byte.MaxValue) FireTimer2();
            ToggleShiftClock();
        }
        else _timer2--;
    }

    private void FireTimer2()
    {
        if (!_timer2Armed) return;
        _flags |= C.Timer2Interrupt;
        _timer2JustFired = true;
        _timer2Armed = false;
    }

    private void LoadTimer1()
    {
        _timer1 = _latch1;
        _timer1Running = _timer1Armed = true;
        _timer1Reload = false;
        _timer1LoadDelay = C.TimerLoadCycles;
        _timerPb7 = false;
        PortBPinsChanged?.Invoke(PortBPins);
    }

    private void ResetTimers()
    {
        _timer1Armed = _timer1Reload = _timer2Reload = false;
        _timer1LoadDelay = _timer2LoadDelay = 0;
        _timerPb7 = true;
        _timer1JustFired = _timer2JustFired = false;
    }
}
