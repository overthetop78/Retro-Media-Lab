using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Constants.Sn74Ls93Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;

public sealed partial class Sn74Ls93
{
    private readonly long[] _inputPeriod = new long[2], _inputHigh = new long[2],
        _inputFall = new long[2], _lastInputFall = new long[2];
    private byte _periodicInputs;
    private bool _advancingPeriodic;

    /// <summary>Receives an external periodic waveform. Its phase belongs to this
    /// input; the source can replace it at any resolved instant. No oscillator is added.</summary>
    public void ConnectPeriodicClock(int clock, long nextFallingNanoseconds, long periodNanoseconds, long highNanoseconds)
    {
        ValidateIndex(clock);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nextFallingNanoseconds, _time);
        var maximum = clock == 0 ? C.MaximumClockA : C.MaximumClockB;
        ArgumentOutOfRangeException.ThrowIfLessThan(periodNanoseconds, (C.NanosecondsPerSecond + maximum - 1) / maximum);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(highNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(highNanoseconds, periodNanoseconds);
        var remaining = nextFallingNanoseconds - _time;
        if (remaining > periodNanoseconds || InputLevel(clock) != (remaining <= highNanoseconds))
            throw new ArgumentException("The waveform must preserve the current input level and phase.");
        if (clock == 0) InvalidateQaPattern();
        _inputPeriod[clock] = periodNanoseconds;
        _inputHigh[clock] = highNanoseconds;
        _inputFall[clock] = nextFallingNanoseconds;
        _periodicInputs |= (byte)(1 << clock);
        UpdateQaPattern();
        PublishDeadline();
    }

    public void DisconnectPeriodicClock(int clock)
    {
        ValidateIndex(clock);
        if ((_periodicInputs & (1 << clock)) == 0) return;
        if (clock == 0) InvalidateQaPattern();
        if (InputLevel(clock)) _clocks |= (byte)(1 << clock);
        else _clocks &= (byte)~(1 << clock);
        _inputPeriod[clock] = _inputFall[clock] = 0;
        _periodicInputs &= (byte)~(1 << clock);
        PublishDeadline();
    }

    private bool InputLevel(int clock)
    {
        if ((_periodicInputs & (1 << clock)) == 0) return (_clocks & (1 << clock)) != 0;
        if (_inputFall[clock] == 0)
        {
            var elapsed = _time - _lastInputFall[clock];
            var low = _inputPeriod[clock] - _inputHigh[clock];
            return _resolving ? elapsed > low : elapsed >= low;
        }
        var remaining = _inputFall[clock] - _time;
        // An already due output resolves before a new input edge at the same date.
        return _resolving ? remaining < _inputHigh[clock] : remaining <= _inputHigh[clock];
    }

    private static long EarlierInput(long first, long second)
        => first == 0 ? second : second == 0 ? first : Math.Min(first, second);

    private long ReceivedDeadline()
        => EarlierInput(_next, EarlierInput(_inputFall[0], _inputFall[1]));

    private void AdvancePeriodicInputs(long target)
    {
        _advancingPeriodic = true;
        try
        {
            while (true)
            {
                if (SkipUnobservedCka(target)) continue;
                var fall = EarlierInput(_inputFall[0], _inputFall[1]);
                if (_next != 0 && _next <= target && (fall == 0 || _next <= fall))
                {
                    // Re-evaluate the waveform after every response: feedback can
                    // disconnect or replace an input before its next falling edge.
                    AdvanceResponses(_next);
                    continue;
                }
                if (fall == 0 || fall > target) break;
                _time = fall;
                for (var clock = 0; clock < 2; clock++)
                {
                    if (_inputPeriod[clock] == 0 || _inputFall[clock] != fall) continue;
                    _lastInputFall[clock] = fall;
                    _inputFall[clock] = _inputPeriod[clock] > long.MaxValue - fall ? 0 : fall + _inputPeriod[clock];
                    var mask = (byte)(1 << clock);
                    _clocks |= mask;
                    ApplyFallingClock(clock, mask);
                }
            }
            _time = target;
        }
        finally { _advancingPeriodic = false; PublishDeadline(); }
    }

    private bool SkipUnobservedCka(long target)
    {
        // CKA drives QA only: it has no internal wire to CKB. A periodic QA
        // recipient already represents these transitions; scalar observers still
        // require the ordinary response path. Keep the unfinished last response.
        if (_periodicInputs != 1 || _pending != 0 || (_observed & 1) != 0
            || _inputFall[0] == 0 || target < Math.Max(_rise[0], _fall[0])) return false;
        var lastSettled = target - Math.Max(_rise[0], _fall[0]);
        var first = _inputFall[0];
        if (first > lastSettled) return false;
        var periods = (lastSettled - first) / _inputPeriod[0] + 1;
        var last = checked(first + (periods - 1) * _inputPeriod[0]);
        _counts[0] = checked(_counts[0] + periods);
        if (_resets != 3)
        {
            _outputs ^= (byte)(periods & 1);
            _targets = (byte)((_targets & ~1) | (_outputs & 1));
        }
        _lastInputFall[0] = last;
        _inputFall[0] = _inputPeriod[0] > long.MaxValue - last ? 0 : last + _inputPeriod[0];
        _time = last + Math.Max(_rise[0], _fall[0]);
        return true;
    }
}
