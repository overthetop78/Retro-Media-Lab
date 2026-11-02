using C = RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Constants.Pcf8583Constants;

namespace RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Services;

public sealed partial class Pcf8583
:RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Interfaces.IPcf8583{
    private void SetTime(DateTime time)
    {
        _memory[C.Hundredths] = Bcd(time.Millisecond / C.BcdRadix);
        _memory[C.Seconds] = Bcd(time.Second);
        _memory[C.Minutes] = Bcd(time.Minute);
        _memory[C.Hours] = Bcd(time.Hour);
        _memory[C.Date] = (byte)(((time.Year & C.YearMask) << C.YearShift) | Bcd(time.Day));
        _memory[C.Month] = (byte)(((int)time.DayOfWeek << C.WeekdayShift) | Bcd(time.Month));
    }

    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if ((_memory[C.Control] & (C.Stop | C.FunctionMask)) != 0) return;
        _fraction += (long)ticks * C.HundredthsPerSecond;
        while (_fraction >= _ticksPerSecond)
        {
            _fraction -= _ticksPerSecond;
            CountHundredth();
        }
    }

    /// <summary>Entrée OSCI pour les modes référence 50 Hz et compteur d'événements.</summary>
    public void ExternalClockPulse()
    {
        if ((_memory[C.Control] & C.Stop) != 0) return;
        switch (_memory[C.Control] & C.FunctionMask)
        {
            case C.FiftyHertzMode:
                CountHundredth();
                CountHundredth();
                break;
            case C.EventMode:
                CountEvent();
                break;
        }
    }

    private void CountHundredth()
    {
        var second = IncrementBcd(C.Hundredths, C.HundredthsPerSecond);
        var minute = second && IncrementBcd(C.Seconds, C.SecondsPerMinute);
        var hour = minute && IncrementBcd(C.Minutes, C.MinutesPerHour);
        var day = hour && IncrementHours();
        if (day) IncrementDate();
        if ((_memory[C.Control] & C.EnableAlarm) == 0)
        {
            _memory[C.Control] = (byte)((_memory[C.Control] & ~(C.AlarmFlag | C.TimerFlag))
                | (FromBcd(_memory[C.Hundredths]) >= C.HalfSecond ? C.TimerFlag : 0)
                | (FromBcd(_memory[C.Seconds]) >= C.SecondsPerMinute / 2 ? C.AlarmFlag : 0));
            if (day) IncrementBcd(C.Timer, C.HundredthsPerSecond);
            return;
        }
        var timerMode = _memory[C.AlarmControl] & C.TimerModeMask;
        var timerTick = timerMode == C.TimerHundredths || timerMode == C.TimerSeconds && second
            || timerMode == C.TimerMinutes && minute || timerMode == C.TimerHours && hour
            || timerMode == C.TimerDays && day;
        if (timerTick) CountTimer();
        CheckClockAlarm();
    }

    private void CountEvent()
    {
        var hundreds = IncrementBcd(C.Hundredths, C.HundredthsPerSecond);
        var tenThousands = hundreds && IncrementBcd(C.Seconds, C.HundredthsPerSecond);
        var millions = tenThousands && IncrementBcd(C.Minutes, C.HundredthsPerSecond);
        if ((_memory[C.Control] & C.EnableAlarm) == 0)
        {
            if (millions) IncrementBcd(C.Timer, C.HundredthsPerSecond);
            return;
        }
        var mode = _memory[C.AlarmControl] & C.TimerModeMask;
        if (mode == C.EventTimerUnits || mode == C.EventTimerHundreds && hundreds
            || mode == C.EventTimerTenThousands && tenThousands || mode == C.EventTimerMillions && millions)
            CountTimer();
        if ((_memory[C.AlarmControl] & C.ClockAlarmMask) != C.EventAlarm) return;
        for (var address = C.Hundredths; address <= C.Minutes; address++)
            if (_memory[address] != _memory[address + C.AlarmControl]) return;
        _memory[C.Control] |= C.AlarmFlag;
    }

    private void CountTimer()
    {
        if (IncrementBcd(C.Timer, C.HundredthsPerSecond)) _memory[C.Control] |= C.TimerFlag;
        if ((_memory[C.AlarmControl] & C.TimerAlarmEnable) != 0 && _memory[C.Timer] == _memory[C.AlarmTimer])
            _memory[C.Control] |= C.AlarmFlag;
    }

    private bool IncrementBcd(int address, int limit)
    {
        var next = FromBcd(_memory[address]) + 1;
        var overflow = next >= limit;
        _memory[address] = Bcd(overflow ? 0 : next);
        return overflow;
    }

    private bool IncrementHours()
    {
        var flags = _memory[C.Hours] & ~C.HourMask;
        var hour = FromBcd((byte)(_memory[C.Hours] & C.HourMask)) + 1;
        if ((flags & C.TwelveHourMode) == 0)
        {
            var overflow = hour >= C.HoursPerDay;
            _memory[C.Hours] = (byte)(flags | Bcd(overflow ? 0 : hour));
            return overflow;
        }
        var midnight = hour == C.TwelveHourCycle && (flags & C.Afternoon) != 0;
        if (hour == C.TwelveHourCycle) flags ^= C.Afternoon;
        if (hour > C.TwelveHourCycle) hour = 1;
        _memory[C.Hours] = (byte)(flags | Bcd(hour));
        return midnight;
    }

    private void IncrementDate()
    {
        var year = _memory[C.Date] >> C.YearShift;
        var month = Math.Clamp(FromBcd((byte)(_memory[C.Month] & C.MonthMask)), 1, C.MonthsPerYear);
        var date = FromBcd((byte)(_memory[C.Date] & C.DateMask)) + 1;
        var weekday = ((_memory[C.Month] >> C.WeekdayShift) + 1) % C.Weekdays;
        if (date > DateTime.DaysInMonth(C.BaseYear + year, month))
        {
            date = 1;
            if (++month > C.MonthsPerYear) { month = 1; year = (year + 1) & C.YearMask; }
        }
        _memory[C.Date] = (byte)((year << C.YearShift) | Bcd(date));
        _memory[C.Month] = (byte)((weekday << C.WeekdayShift) | Bcd(month));
    }

    private void CheckClockAlarm()
    {
        var mode = _memory[C.AlarmControl] & C.ClockAlarmMask;
        if (mode == 0) return;
        for (var address = C.Hundredths; address <= C.Hours; address++)
            if (_memory[address] != _memory[address + C.AlarmControl]) return;
        if (mode == C.WeekdayAlarm && (_memory[C.AlarmControl + C.Month] & (1 << (_memory[C.Month] >> C.WeekdayShift))) == 0) return;
        if (mode == C.DatedAlarm && ((_memory[C.Date] & C.DateMask) != (_memory[C.Date + C.AlarmControl] & C.DateMask)
            || (_memory[C.Month] & C.MonthMask) != (_memory[C.Month + C.AlarmControl] & C.MonthMask))) return;
        _memory[C.Control] |= C.AlarmFlag;
    }

    public bool InterruptLow => (_memory[C.Control] & C.EnableAlarm) == 0
        ? (_memory[C.Control] & C.FunctionMask) != C.EventMode && FromBcd(_memory[C.Hundredths]) >= C.HalfSecond
        : (_memory[C.Control] & C.AlarmFlag) != 0 && (_memory[C.AlarmControl] & C.AlarmInterruptEnable) != 0
            || (_memory[C.Control] & C.TimerFlag) != 0 && (_memory[C.AlarmControl] & C.TimerInterruptEnable) != 0;

    private static byte Bcd(int value) => (byte)((value / C.BcdRadix << C.BcdShift) | value % C.BcdRadix);
    private static int FromBcd(byte value) => (value >> C.BcdShift) * C.BcdRadix + (value & C.BcdDigitMask);
}
