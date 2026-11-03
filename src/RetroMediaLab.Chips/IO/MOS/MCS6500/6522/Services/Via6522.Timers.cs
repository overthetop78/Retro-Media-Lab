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
    private byte _timerInterruptsPending;
    private bool _timer2PulseSampled;
    private bool _timer1Continuous;
    private bool _timer1Output;
    private bool _timer2PulseMode;

    private byte ApplyTimerPb7(byte pins) => !_timer1Output
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
                    _timerInterruptsPending |= C.Timer1Interrupt;
                    _timer1JustFired = true;
                }
                _timer1Armed &= _timer1Continuous;
                _timer1Reload = true;
            }
        }
        if (!_timer2Running) return;
        if (_timer2PulseMode)
        {
            if (_timer2PulseSampled) CountTimer2();
            _timer2PulseSampled = false;
            return;
        }
        if (_timer2LoadDelay > 0) { _timer2LoadDelay--; return; }
        if (!_shiftUsesTimer) { CountTimer2(); return; }
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
        _timerInterruptsPending |= C.Timer2Interrupt;
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
        _timerInterruptsPending = 0;
        _timer2PulseSampled = false;
        _timer1Continuous = _timer1Output = _timer2PulseMode = false;
    }
}
