using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

public sealed partial class Hd146818
{
    private long _halfClocks;
    private long _dividerOrigin;
    private long _referenceClock;
    private long? _publishedNextClockEvent;

    public long ClockCount => _halfClocks / R.HalfClocksPerCycle;
    public long ClockHalfCount => _halfClocks;
    private uint HalfClockFrequency => (uint)ClockFrequency * R.HalfClocksPerCycle;

    public long? NextClockEvent
    {
        get
        {
            var next = NextOscillatorEvent();
            if (next == long.MaxValue) return null;
            var time = (UInt128)(ulong)next * (ulong)ReferenceFrequency;
            return checked((long)((time + HalfClockFrequency - 1) / HalfClockFrequency));
        }
    }

    /// <summary>Référence absolue monotone. La conversion entière conserve exactement toute fraction d'oscillation.</summary>
    public void AdvanceTo(long referenceClock)
    {
        if (referenceClock < _referenceClock) throw new ArgumentOutOfRangeException(nameof(referenceClock));
        var target = checked((long)((UInt128)(ulong)referenceClock * HalfClockFrequency / (ulong)ReferenceFrequency));
        if (target < _halfClocks) throw new ArgumentOutOfRangeException(nameof(referenceClock));
        AdvanceHalfClocks(target - _halfClocks);
        _referenceClock = referenceClock;
    }

    public void Clock() => AdvanceClocks(1);
    public void AdvanceClocks(long clocks)
    {
        if (clocks < 0) throw new ArgumentOutOfRangeException(nameof(clocks));
        AdvanceHalfClocks(checked(clocks * R.HalfClocksPerCycle));
    }

    /// <summary>Front externe OSC1 ; les deux phases sont distinctes, y compris sur CKOUT non divisé.</summary>
    public void SetOscillator(bool high)
    {
        if (high != OscillatorHigh) AdvanceHalfClocks(1);
    }

    public void SetClockFrequencySelect(bool high)
    {
        if (_clockFrequencySelect == high) return;
        _clockFrequencySelect = high;
        PublishClockOutputs();
        PublishNextClockEvent();
    }

    /// <summary>Chaque transition observable est traitée ; une sortie sans récepteur ne crée aucun appel périodique.</summary>
    public void AdvanceHalfClocks(long halfClocks)
    {
        if (halfClocks < 0) throw new ArgumentOutOfRangeException(nameof(halfClocks));
        var target = checked(_halfClocks + halfClocks);
        while (_halfClocks < target)
        {
            var period = PeriodicHalfPeriod;
            var periodicDue = period == 0 ? long.MaxValue : NextDividerEdge(period);
            _halfClocks = Math.Min(target, NextOscillatorEvent());
            if (periodicDue <= _halfClocks && !_reset) _registers[R.StatusC] |= R.PeriodicFlag;
            if (UpdatesEnabled)
            {
                if (_halfClocks == _updateEnd) CompleteUpdate();
                else if (_updateEnd == long.MaxValue && _halfClocks >= _nextUip)
                {
                    _updateInProgress = true;
                    _updateEnd = _nextUip + WarningHalfClocks + UpdateHalfClocks;
                }
            }
            UpdateIrq();
            PublishClockOutputs();
        }
        PublishNextClockEvent();
    }

    private long NextOscillatorEvent()
    {
        var next = UpdatesEnabled ? (_updateEnd != long.MaxValue ? _updateEnd : _nextUip) : long.MaxValue;
        var period = PeriodicHalfPeriod;
        if (period != 0 && !_reset && (_registers[R.StatusC] & R.PeriodicFlag) == 0)
            next = Math.Min(next, NextDividerEdge(period));
        if (period != 0 && _squareWaveChanged is not null && (_registers[R.StatusB] & R.SquareWaveEnable) != 0)
            next = Math.Min(next, NextDividerEdge(period / R.HalfClocksPerCycle));
        if (_clockOutChanged is not null)
        {
            var halfPeriod = _clockFrequencySelect ? 1 : R.ClockOutDivider;
            next = Math.Min(next, _halfClocks + halfPeriod - _halfClocks % halfPeriod);
        }
        return next;
    }

    private long NextDividerEdge(long period) => _halfClocks + period - (_halfClocks - _dividerOrigin) % period;

    private void PublishNextClockEvent()
    {
        var next = NextClockEvent;
        if (_publishedNextClockEvent == next) return;
        _publishedNextClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }

    private void PublishClockOutputs()
    {
        var period = PeriodicHalfPeriod;
        var squareWave = Running && (_registers[R.StatusB] & R.SquareWaveEnable) != 0 &&
            (period == 0 || ((_halfClocks - _dividerOrigin) / (period / R.HalfClocksPerCycle) & 1) != 0);
        if (squareWave != _squareWave)
        {
            _squareWave = squareWave;
            _squareWaveChanged?.Invoke(squareWave);
        }
        var clockOut = _clockFrequencySelect ? OscillatorHigh : (_halfClocks / R.ClockOutDivider & 1) != 0;
        if (clockOut == _clockOut) return;
        _clockOut = clockOut;
        _clockOutChanged?.Invoke(clockOut);
    }
}
