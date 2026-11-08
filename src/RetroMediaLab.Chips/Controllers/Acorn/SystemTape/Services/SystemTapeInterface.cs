using C = RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Constants.SystemTapeConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Services;

/// <summary>Digital counters and flip-flops of Acorn cassette board 200.001/100.001.
/// The owner supplies PH2 timestamps and the comparator's electrical transitions.</summary>
/// <remarks>IC1 divides PH2 by 26. Its Q3 falling edges clock IC6; IC6 Q7 is NHITONE.
/// On a comparator rising edge IC4 captures Q7 before the differentiated reset clears IC6.
/// IC2 divides IC1 by 16/32, selected by CASOUT latched on IC1's reset pulse.
/// RC pulse widths and analogue amplifier gain are outside this digital interface.</remarks>
public sealed class SystemTapeInterface
{
    private long _origin;
    private long _clock;
    private int _toneCounter;
    private int _receiveCounter;
    private bool _outputBit;
    private bool _latchedOutputBit;
    private bool _comparatorHigh;
    private bool _inputBit = true;
    private bool _publishedOutput;
    private byte _publishedPins = C.DataInputPin;

    public SystemTapeInterface(int clockFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ClockFrequency = clockFrequency;
        UpdateNextEvent();
    }

    public int ClockFrequency { get; }
    public long ClockCount => _clock;
    public long NextClockEvent { get; private set; }
    public bool CassetteInput => _inputBit;
    public bool CassetteOutput => _publishedOutput;
    /// <summary>PA7 CASIN and PA5 NHITONE; other port bits are supplied by the owner.</summary>
    public byte InputPins => _publishedPins;
    public Action<bool>? CassetteOutputChanged { get; set; }
    public Action<byte>? InputPinsChanged { get; set; }
    public Action<long>? NextClockEventChanged { get; set; }

    /// <summary>Power-up of this board; a CPU reset alone does not reset its free-running counters.</summary>
    public void Reset(long clock = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clock);
        _origin = _clock = clock;
        _toneCounter = _receiveCounter = 0;
        _outputBit = _latchedOutputBit = _comparatorHigh = false;
        _inputBit = true;
        PublishOutputs();
        UpdateNextEvent();
    }

    public void SetOutputBit(bool value, long clock)
    {
        AdvanceTo(clock);
        if (_outputBit == value) return;
        // IC5.1/C2 generates a positive IC2 reset pulse on a falling CASOUT.
        if (!value) _toneCounter = 0;
        _outputBit = value;
        PublishOutputs();
        UpdateNextEvent();
    }

    /// <summary>Receive clock at IC5.3 output, after inversion of the LM358 comparator.</summary>
    public void SetComparatorInput(bool high, long clock)
    {
        AdvanceTo(clock);
        if (_comparatorHigh == high) return;
        _comparatorHigh = high;
        if (high)
        {
            _inputBit = (_receiveCounter & C.CarrierBit) == 0;
            _receiveCounter = 0;
            PublishOutputs();
            UpdateNextEvent();
        }
    }

    /// <summary>Visits every observable output transition up to this PH2 clock.</summary>
    public void AdvanceTo(long clock)
    {
        if (clock < _clock) throw new ArgumentOutOfRangeException(nameof(clock));
        while (NextClockEvent <= clock)
        {
            AdvanceCounters(NextClockEvent);
            PublishOutputs();
            UpdateNextEvent();
        }
        AdvanceCounters(clock);
    }

    private void AdvanceCounters(long clock)
    {
        var before = _clock - _origin;
        var after = clock - _origin;
        var divisions = after / C.Divider - before / C.Divider;
        if (divisions != 0)
        {
            _latchedOutputBit = _outputBit;
            _toneCounter = (int)((_toneCounter + divisions) & C.CounterMask);
        }
        _receiveCounter = (int)((_receiveCounter + ReceiveClocks(after) - ReceiveClocks(before)) & C.CounterMask);
        _clock = clock;
    }

    private static long ReceiveClocks(long clock) => clock / C.Divider * C.Q3EdgesPerDivision
        + Math.Min(clock % C.Divider / C.Q3Period, C.Q3EdgesPerDivision);

    private void PublishOutputs()
    {
        var output = (_toneCounter & (_latchedOutputBit ? C.MarkBit : C.SpaceBit)) != 0;
        if (output != _publishedOutput)
        {
            _publishedOutput = output;
            CassetteOutputChanged?.Invoke(output);
        }
        var pins = (byte)((_inputBit ? C.DataInputPin : 0)
            | ((_receiveCounter & C.CarrierBit) != 0 ? C.CarrierInputPin : 0));
        if (pins == _publishedPins) return;
        _publishedPins = pins;
        InputPinsChanged?.Invoke(pins);
    }

    private void UpdateNextEvent()
    {
        var relative = _clock - _origin;
        var toneBit = _latchedOutputBit ? C.MarkBit : C.SpaceBit;
        var divisions = _outputBit != _latchedOutputBit ? 1 : toneBit - (_toneCounter & (toneBit - 1));
        var nextTone = _origin + (relative / C.Divider + divisions) * C.Divider;
        var nextReceiveEdge = ReceiveClocks(relative) + C.CarrierBit - (_receiveCounter & (C.CarrierBit - 1));
        var nextReceive = _origin + (nextReceiveEdge - 1) / C.Q3EdgesPerDivision * C.Divider
            + ((nextReceiveEdge - 1) % C.Q3EdgesPerDivision + 1) * C.Q3Period;
        var next = Math.Min(nextTone, nextReceive);
        if (next == NextClockEvent) return;
        NextClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }
}
