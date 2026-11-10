namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
    private Action<long, long, long>? _clock4Pattern;
    private bool _clock4PatternPublished;

    /// <summary>Receives the next falling edge, full period and high duration of
    /// IC3/QA in oscillator rising-edge counts. Published once after the physical
    /// divider network has synchronized. This supplements, never suppresses, pin
    /// observers; the owner of a wire may then detach its own scalar transport.</summary>
    public Action<long, long, long>? Clock4MHzPeriodicAvailable
    {
        get => _clock4Pattern;
        set
        {
            RequireReceiverBoundary();
            _clock4Pattern = value;
            _clock4PatternPublished = false;
            PublishClock4Pattern();
        }
    }

    private void PublishClock4Pattern()
    {
        if (!_stableDomainsAligned || _clock4PatternPublished || _clock4Pattern is null) return;
        // IC2 has already reached its /3 sequence; IC3 increments at the first
        // following enabled edge and every three oscillator periods afterwards.
        var firstToggle = _ic2Pins == 0 ? 2 : _ic2Pins == 1 ? 1 : 3;
        var falling = firstToggle + (Clock4MHz ? 0 : 3);
        var next = checked(_inputClockCount + falling);
        _clock4PatternPublished = true;
        _clock4Pattern(next, 6, 3);
    }
}
