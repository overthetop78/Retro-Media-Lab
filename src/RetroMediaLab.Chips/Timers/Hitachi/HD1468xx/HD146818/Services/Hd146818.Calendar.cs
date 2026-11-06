using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

public sealed partial class Hd146818
{
    private int? _initialMonthLength;
    private bool _autumnAdjustmentDone;

    private void PrepareCalendarInitialization()
    {
        _initialMonthLength = null;
        _autumnAdjustmentDone = false;
        var hour = Decode((byte)(_registers[R.Hours] & R.SevenBits));
        if ((_registers[R.StatusB] & R.TwentyFourHour) == 0)
            hour = hour % R.HoursPerHalfDay + ((_registers[R.Hours] & R.Afternoon) != 0 ? R.HoursPerHalfDay : 0);
        if (hour != R.HoursPerDay - 1 || Decode(_registers[R.Minutes]) != R.MinutesPerHour - 1) return;
        var month = Decode(_registers[R.Month]);
        var day = Decode(_registers[R.Day]);
        var seconds = Decode(_registers[R.Seconds]);
        var leap = Decode(_registers[R.Year]) % R.LeapYearPeriod == 0;
        // Hitachi table 7: carry state following initialization is not an ideal Gregorian calendar.
        if (seconds == R.SecondsPerMinute - 1)
        {
            if (day == R.EarlyRolloverDay && !(month == R.February && leap))
                _initialMonthLength = R.EarlyRolloverDay;
            else if (day == R.ThirtyDayMonthEnd && month is R.April or R.June or R.September or R.November)
                _initialMonthLength = R.ThirtyDayMonthEnd + 1;
            else if (month == R.February && day == R.CommonFebruaryLastDay && !leap)
                _initialMonthLength = R.LeapFebruaryLastDay;
        }
        else if (seconds == R.SecondsPerMinute - 2 && month == R.February && day == R.CommonFebruaryLastDay && leap)
            _initialMonthLength = R.CommonFebruaryLastDay;
    }
    private byte Encode(int value) => (_registers[R.StatusB] & R.BinaryData) != 0
        ? (byte)value : (byte)((value / 10 << 4) | value % 10);

    private int Decode(byte value) => (_registers[R.StatusB] & R.BinaryData) != 0
        ? value : (value >> 4) * 10 + (value & R.LowNibble);

    private bool IncrementField(int address, int limit, int first = 0)
    {
        var value = Decode(_registers[address]) + 1;
        var carry = value >= limit;
        _registers[address] = Encode(carry ? first : value);
        return carry;
    }

    private void IncrementCalendar()
    {
        if (!IncrementField(R.Seconds, R.SecondsPerMinute)) return;
        if (!IncrementField(R.Minutes, R.MinutesPerHour)) return;
        if (ApplyDaylightSaving()) return;
        if ((_registers[R.StatusB] & R.TwentyFourHour) != 0)
        {
            if (!IncrementField(R.Hours, R.HoursPerDay)) return;
        }
        else
        {
            var hours = Decode((byte)(_registers[R.Hours] & R.SevenBits));
            var afternoon = (_registers[R.Hours] & R.Afternoon) != 0;
            if (++hours == R.HoursPerHalfDay) afternoon = !afternoon;
            else if (hours > R.HoursPerHalfDay) hours = 1;
            _registers[R.Hours] = (byte)(Encode(hours) | (afternoon ? R.Afternoon : 0));
            if (hours != R.HoursPerHalfDay || afternoon) return;
        }
        IncrementField(R.DayOfWeek, R.DaysPerWeek + 1, 1);
        _autumnAdjustmentDone = false;
        var month = Decode(_registers[R.Month]);
        var year = Decode(_registers[R.Year]);
        // Invalid calendar bytes have no specified Gregorian interpretation. Never consult a host calendar.
        if (month < 1 || month > R.MonthsPerYear) return;
        var days = R.MonthDays[month - 1];
        if (month == R.February && year % R.LeapYearPeriod == 0) days++;
        days = _initialMonthLength ?? days;
        _initialMonthLength = null;
        if (!IncrementField(R.Day, days + 1, 1)) return;
        if (!IncrementField(R.Month, R.MonthsPerYear + 1, 1)) return;
        IncrementField(R.Year, R.CenturyYears);
    }

    private bool ApplyDaylightSaving()
    {
        // DSE is explicitly prohibited for the A and X parts; it must not acquire Motorola behavior.
        if (Variant != Hd146818Variant.Hd146818 || (_registers[R.StatusB] & R.DaylightSaving) == 0
            || Decode(_registers[R.DayOfWeek]) != R.Sunday) return false;
        var month = Decode(_registers[R.Month]);
        var day = Decode(_registers[R.Day]);
        if (month is not (R.April or R.October) || day <= R.ThirtyDayMonthEnd - R.DaysPerWeek) return false;
        var hour = Decode((byte)(_registers[R.Hours] & R.SevenBits));
        var twentyFour = (_registers[R.StatusB] & R.TwentyFourHour) != 0;
        if (hour != 1 || !twentyFour && (_registers[R.Hours] & R.Afternoon) != 0) return false;
        if (month == R.April)
        {
            _registers[R.Hours] = Encode(3);
            return true;
        }
        if (_autumnAdjustmentDone) return false;
        _autumnAdjustmentDone = true;
        _registers[R.Hours] = Encode(1);
        return true;
    }

    private bool AlarmMatches() => AlarmMatches(R.Seconds, R.SecondsAlarm)
        && AlarmMatches(R.Minutes, R.MinutesAlarm) && AlarmMatches(R.Hours, R.HoursAlarm);

    private bool AlarmMatches(int field, int alarm) => (_registers[alarm] & R.AlarmDontCare) == R.AlarmDontCare
        || _registers[alarm] == _registers[field];
}
