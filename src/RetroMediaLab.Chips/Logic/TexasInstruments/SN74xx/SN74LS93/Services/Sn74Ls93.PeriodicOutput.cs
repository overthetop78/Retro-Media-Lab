namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;

public sealed partial class Sn74Ls93
{
    private Action<long, long, long>? _qaPatternChanged;
    private Action<long?>? _outputDeadlineChanged;
    private bool _qaPatternActive;
    private long _publishedOutput;

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
}
