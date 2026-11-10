using RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Constants.SystemKeyboardOutputConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Services;

/// <summary>Latched ASCII and AKD/NSTROBE of400,013 at its decoded-key input boundary.</summary>
/// <remarks>The host adapter supplies validated codes, not a fictitious matrix or ROM.
/// A second key releases AKD for two256 kHz periods (7.8125us). Reference-clock
/// observers receive that response on the first reference edge at/after its date.
/// No queue, additional oscillator, or change to the existing repeat circuit.</remarks>
public sealed class SystemKeyboardOutput : ISystemKeyboardOutput
{
    private readonly SystemKeyboardRepeat _repeatCircuit;
    private readonly long _rolloverClocks;
    private bool _anyKeyDown, _repeat;
    private long? _restoreAt, _publishedNext;
    private int _changing;

    public SystemKeyboardOutput(int clockFrequency)
    {
        _repeatCircuit = new SystemKeyboardRepeat(clockFrequency);
        _rolloverClocks = Math.Max(1, ((long)clockFrequency * C.RolloverPeriods
            + C.EncoderFrequency - 1) / C.EncoderFrequency);
        _repeatCircuit.StrobeChanged = high => StrobeChanged?.Invoke(high);
        _repeatCircuit.NextClockEventChanged = _ => PublishDeadline();
    }

    public int ClockFrequency => _repeatCircuit.ClockFrequency;
    public long ClockCount => _repeatCircuit.ClockCount;
    public byte Data { get; private set; } = C.DataMask;
    public bool Strobe => _repeatCircuit.Strobe;
    public long? NextClockEvent => _restoreAt is not { } restore ? _repeatCircuit.NextClockEvent
        : _repeatCircuit.NextClockEvent is { } repeat ? Math.Min(restore, repeat) : restore;
    public Action<byte>? DataChanged { get; set; }
    public Action<bool>? StrobeChanged { get; set; }
    public Action<long?>? NextClockEventChanged { get; set; }

    public void SetInputs(byte? encodedData, bool anyKeyDown, bool repeat, bool newKey)
    {
        if (encodedData is > C.DataMask) throw new ArgumentOutOfRangeException(nameof(encodedData));
        if (newKey && (!anyKeyDown || !encodedData.HasValue)) throw new ArgumentException(nameof(newKey));
        var rollover = newKey && _anyKeyDown;
        var restore = rollover ? checked(ClockCount + _rolloverClocks) : anyKeyDown ? _restoreAt : null;
        _changing++;
        try
        {
            _anyKeyDown = anyKeyDown;
            _repeat = repeat;
            _restoreAt = restore;
            // Deassert the previous key before exposing the replacement code.
            // The actual reassertion is a later hardware response, not another
            // synchronous pulse which the CPU could never sample.
            if (rollover) _repeatCircuit.SetInputs(false, repeat);
            if (encodedData is { } data) SetData(data);
            _repeatCircuit.SetInputs(anyKeyDown && !_restoreAt.HasValue, repeat);
        }
        finally { _changing--; PublishDeadline(); }
    }

    public void AdvanceTo(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clocks, ClockCount);
        _changing++;
        try
        {
            while (NextClockEvent is { } due && due <= clocks)
            {
                _repeatCircuit.AdvanceTo(due);
                if (_restoreAt is { } restore && restore <= due)
                {
                    _restoreAt = null;
                    _repeatCircuit.SetInputs(_anyKeyDown, _repeat);
                }
            }
            _repeatCircuit.AdvanceTo(clocks);
        }
        finally { _changing--; PublishDeadline(); }
    }

    public void Reset()
    {
        _changing++;
        try
        {
            _restoreAt = null;
            _anyKeyDown = _repeat = false;
            _repeatCircuit.Reset();
            SetData(C.DataMask);
        }
        finally { _changing--; PublishDeadline(); }
    }

    private void SetData(byte data)
    {
        if (Data == data) return;
        Data = data;
        DataChanged?.Invoke(data);
    }

    private void PublishDeadline()
    {
        if (_changing != 0) return;
        var next = NextClockEvent;
        if (_publishedNext == next) return;
        _publishedNext = next;
        NextClockEventChanged?.Invoke(next);
    }
}
