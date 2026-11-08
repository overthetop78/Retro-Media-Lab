using RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Constants.SystemKeyboardRepeatConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Services;

/// <summary>Nominal RC repeat circuit after the ASCII encoder on Acorn 400,013.</summary>
/// <remarks>The reference timestamps do not represent a separate physical oscillator.
/// The circuit owns the nominal RC deadlines; the keyboard encoder retains its code
/// independently of AKD. NSTROBE notifications carry the electrical active-low level.</remarks>
public sealed class SystemKeyboardRepeat : ISystemKeyboardRepeat
{
    private readonly long _delay;
    private readonly long _halfPeriod;
    private bool _anyKeyDown;
    private bool _repeat;

    public SystemKeyboardRepeat(int clockFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ClockFrequency = clockFrequency;
        _delay = Math.Max(1, (long)clockFrequency * C.InitialDelayMilliseconds / C.MillisecondsPerSecond);
        _halfPeriod = Math.Max(1, (long)clockFrequency * C.HalfPeriodMilliseconds / C.MillisecondsPerSecond);
    }

    public int ClockFrequency { get; }
    public long ClockCount { get; private set; }
    public bool Strobe { get; private set; }
    public long? NextClockEvent { get; private set; }
    public Action<bool>? StrobeChanged { get; set; }
    public Action<long?>? NextClockEventChanged { get; set; }

    /// <summary>Receive resolved input levels at the current reference time.</summary>
    public void SetInputs(bool anyKeyDown, bool repeat)
    {
        if (_anyKeyDown == anyKeyDown && _repeat == repeat) return;
        var keyPressed = anyKeyDown && !_anyKeyDown;
        var repeatPressed = repeat && !_repeat;
        _anyKeyDown = anyKeyDown;
        _repeat = repeat;
        if (!anyKeyDown && !repeat)
        {
            Schedule(null);
            SetStrobe(false);
        }
        else if (repeatPressed)
        {
            // REPEAT charges C3 without the initial auto-repeat delay. Its
            // inhibition deasserts NSTROBE immediately; RC oscillation follows.
            Schedule(checked(ClockCount + _halfPeriod));
            SetStrobe(false);
        }
        else if (keyPressed && !repeat)
        {
            Schedule(checked(ClockCount + _delay));
            SetStrobe(true);
        }
    }

    public void AdvanceTo(long clockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clockCount, ClockCount);
        while (NextClockEvent is { } due && due <= clockCount)
        {
            ClockCount = due;
            Schedule(checked(due + _halfPeriod));
            SetStrobe(!Strobe);
        }
        ClockCount = clockCount;
    }

    public void Reset()
    {
        _anyKeyDown = _repeat = false;
        Schedule(null);
        SetStrobe(false);
    }

    private void Schedule(long? next)
    {
        if (NextClockEvent == next) return;
        NextClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }

    private void SetStrobe(bool active)
    {
        if (Strobe == active) return;
        Strobe = active;
        StrobeChanged?.Invoke(!active);
    }
}
