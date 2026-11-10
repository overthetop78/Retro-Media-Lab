namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;

public sealed partial class Sn74Ls93
{
    private Action<long, long, long>? _qaPatternChanged;
    private Action<long?>? _outputDeadlineChanged;
    private bool _qaPatternActive;
    private long _publishedOutput;
    private bool _groupObservedIntervals = true;
    internal bool GroupObservedIntervals
    {
        get => _groupObservedIntervals;
        set { if (_groupObservedIntervals == value) return; _groupObservedIntervals = value; PublishDeadline(); }
    }

    /// <summary>QA waveform: next falling date, period, high duration, in ns.
    /// All zeros revoke the description at TimeNanoseconds, before the cause
    /// changes. The wire owner must then resume ordinary QA notifications.</summary>
    public Action<long, long, long>? QaPeriodicClockChanged
    {
        get => _qaPatternChanged;
        set { InvalidateQaPattern(); _qaPatternChanged = value; UpdateQaPattern(); }
    }

    public long? NextOutputEventNanoseconds => ConnectedResponseDeadline() is var next && next != 0 ? next : null;
    public Action<long?>? NextOutputEventNanosecondsChanged
    {
        get => _outputDeadlineChanged;
        set => _outputDeadlineChanged = value;
    }

    private void InvalidateQaPattern()
    {
        if (!_qaPatternActive) return;
        _qaPatternActive = false;
        _qaPatternChanged?.Invoke(0, 0, 0);
    }

    private void UpdateQaPattern()
    {
        if (_qaPatternActive || _qaPatternChanged is null || _resets == 3
            || _inputPeriod[0] == 0 || _inputFall[0] == 0 || (_clearing & 1) != 0) return;
        var inputPeriod = _inputPeriod[0];
        if (inputPeriod <= Math.Max(_rise[0], _fall[0]) || inputPeriod > long.MaxValue / 2) return;
        long nextFall;
        if ((_pending & 1) != 0 && (_targets & 1) == 0) nextFall = _due[0];
        else
        {
            var afterPending = (_pending & 1) == 0 ? _outputs : _targets;
            var delay = (afterPending & 1) != 0 ? _fall[0] : inputPeriod + _fall[0];
            if (_inputFall[0] > long.MaxValue - delay) return;
            nextFall = _inputFall[0] + delay;
        }
        var high = inputPeriod + _fall[0] - _rise[0];
        // An asynchronous clear can truncate the first low interval. Retain
        // explicit edges until the actual pin belongs to the described phase.
        var remaining = nextFall - _time;
        if (remaining > inputPeriod * 2 || ((_outputs & 1) != 0) != (remaining <= high)) return;
        _qaPatternActive = true;
        _qaPatternChanged(nextFall, inputPeriod * 2, high);
    }

    private long ConnectedResponseDeadline()
    {
        if (_observed == 0 && (_qaPatternChanged is null || _qaPatternActive)) return 0;
        if (_groupObservedIntervals && TryObservedSectionDeadline(out var connected)) return connected;
        // Unobserved QA cannot affect the other section without an external wire.
        // Its waveform recipient carries that wire, so only already pending
        // responses (including asynchronous reset) can require board attention.
        if ((_periodicInputs & 1) != 0 && (_observed & 1) == 0)
        {
            // A described QA edge is consumed by its waveform receiver. CKB
            // and its pending ripple remain independent observable causes.
            var response = (_pending & ~1) != 0 ? _next : 0;
            return EarlierInput(response, _inputFall[1]);
        }
        if (_periodicInputs == 1 && _resets == 3) return _next;
        return ReceivedDeadline();
    }

    private bool TryObservedSectionDeadline(out long next)
    {
        next = 0;
        var observedB = (byte)(_observed & 14);
        if ((_observed & 3) != 0 || observedB == 0 || _nextChanged is not null
            || _resets == 3 || _clearing != 0 || _inputPeriod[1] <= Timing.MaximumRipple
            || _inputFall[1] == 0 || (_qaPatternChanged is not null && !_qaPatternActive)) return false;
        var pending = (byte)(_pending & 14);
        if ((pending & (pending - 1)) != 0) return false;
        var after = _outputs;
        if (pending != 0)
        {
            var bit = System.Numerics.BitOperations.TrailingZeroCount((uint)pending);
            var due = _due[bit];
            while (true)
            {
                var mask = (byte)(1 << bit);
                if ((observedB & mask) != 0) next = EarlierInput(next, due);
                var falling = (after & mask) != 0; after ^= mask;
                if (!falling || bit == 3) break;
                ++bit;
                var delay = (after & (1 << bit)) != 0 ? _fall[bit] : _rise[bit];
                if (due > long.MaxValue - delay) return false;
                due += delay;
            }
            if (due > _inputFall[1]) return false;
        }
        var firstBit = System.Numerics.BitOperations.TrailingZeroCount((uint)observedB);
        var modulus = 1 << (firstBit - 1);
        var distance = modulus - ((after >> 1) & (modulus - 1));
        var first = _inputFall[1]; var period = _inputPeriod[1];
        if (distance != 1 && period > (long.MaxValue - first) / (distance - 1L)) return false;
        var response = first + (distance - 1L) * period;
        for (var bit = 1; bit < firstBit; bit++)
        {
            if (response > long.MaxValue - _fall[bit]) return false;
            response += _fall[bit];
        }
        var lastDelay = (after & (1 << firstBit)) != 0 ? _fall[firstBit] : _rise[firstBit];
        if (response > long.MaxValue - lastDelay) return false;
        next = EarlierInput(next, response + lastDelay);
        return true;
    }
}
