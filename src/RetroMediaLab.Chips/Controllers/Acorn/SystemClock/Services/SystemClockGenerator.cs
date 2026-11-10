using System.Numerics;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

/// <summary>24 MHz oscillator and three synchronous counters wired as Acorn 400,005 figure 9.</summary>
/// <remarks>Counter state belongs to each SN74S163. All three capture the shared edge
/// before upstream outputs settle. No CPU reset is wired to this free-running network.</remarks>
public sealed partial class SystemClockGenerator : ISystemClockGenerator
{
    private readonly Sn74S163 _ic2;
    private readonly Sn74S163 _ic3;
    private readonly Sn74S163 _ic4;
    private byte _levels;
    private byte _publishedLevels;
    private long _inputClockCount;
    private bool _oscillatorHigh;
    private byte _observedClocks;
    private readonly Action<bool>?[] _receivers = new Action<bool>?[C.OutputCount];
    private readonly byte[] _nextObserved = new byte[C.OutputCount + 1];

    public SystemClockGenerator() : this(C.DivideByThreeSeed, 0, 0) { }

    // A deterministic power-on phase, not a TI-guaranteed initial state.
    internal SystemClockGenerator(byte ic2, byte ic3, byte ic4)
    {
        Array.Fill(_nextObserved, (byte)C.OutputCount);
        _ic2 = new Sn74S163(C.OscillatorFrequency, ic2);
        _ic3 = new Sn74S163(C.OscillatorFrequency, ic3);
        _ic4 = new Sn74S163(C.OscillatorFrequency, ic4);
        _ic2.OutputsChanged = Ic2Outputs;
        _ic3.OutputsChanged = Ic3Outputs;
        _ic4.OutputsChanged = Ic4Outputs;
        _ic2.SetEnable(true, true);
        _ic4.SetEnable(true, true);
        Ic2Outputs(_ic2.Count, _ic2.Carry);
        Ic3Outputs(_ic3.Count, _ic3.Carry);
        Ic4Outputs(_ic4.Count, _ic4.Carry);
        _publishedLevels = _levels;
    }

    public long OscillatorFrequency => C.OscillatorFrequency;
    public long InputClockCount => _inputClockCount;
    public bool OscillatorHigh => _oscillatorHigh;
    public bool Clock12MHz => (_levels & C.Clock12) != 0;
    public bool Clock8MHz => (_levels & C.Clock8) != 0;
    public bool Clock6MHz => (_levels & C.Clock6) != 0;
    public bool Clock4MHz => (_levels & C.Clock4) != 0;
    public bool Clock3MHz => (_levels & C.Clock3) != 0;
    public bool Clock2MHz => (_levels & C.Clock2) != 0;
    public bool Clock1MHz => (_levels & C.Clock1) != 0;
    public Action<bool>? Clock12MHzChanged
    {
        get => _clock12Observer;
        set { RequireReceiverBoundary(); _clock12Observer = value; UpdateClock12Route(); }
    }
    public Action<bool>? Clock8MHzChanged { get => GetReceiver(C.Clock8); set => SetReceiver(value, C.Clock8); }
    public Action<bool>? Clock6MHzChanged { get => GetReceiver(C.Clock6); set => SetReceiver(value, C.Clock6); }
    public Action<bool>? Clock4MHzChanged
    {
        get => GetReceiver(C.Clock4);
        set { RequireReceiverBoundary(); _clock4CanChangeClock12Inputs = true; SetReceiver(value, C.Clock4); }
    }
    public Action<bool>? Clock3MHzChanged { get => GetReceiver(C.Clock3); set => SetReceiver(value, C.Clock3); }
    public Action<bool>? Clock2MHzChanged { get => GetReceiver(C.Clock2); set => SetReceiver(value, C.Clock2); }
    public Action<bool>? Clock1MHzChanged { get => GetReceiver(C.Clock1); set => SetReceiver(value, C.Clock1); }
    internal Sn74S163 Ic2 => _ic2;
    internal Sn74S163 Ic3 => _ic3;
    internal Sn74S163 Ic4 => _ic4;

    public void SetOscillator(bool high)
    {
        if (_oscillatorHigh == high) return;
        if (high) RisingClock();
        else FallingClock();
    }

    private void RisingClock()
    {
        var aligning = !_stableDomainsAligned && !_ic4ClearHigh;
        _oscillatorHigh = true;
        _inputClockCount = checked(_inputClockCount + 1);
        // All receivers start low: the source owns their shared CLK and every
        // preceding falling edge reached them. Capture before resolving outputs.
        _ic2.CaptureRisingClock();
        _ic3.CaptureRisingClock();
        _ic4.CaptureRisingClock();
        _ic2.ResolveClock();
        _ic3.ResolveClock();
        _ic4.ResolveClock();
        if (aligning && _ic2Pins == C.DivideByThreeSeed)
        {
            _stableDomainsAligned = true;
            _stableClockOrigin = _inputClockCount;
            if (_stableClock12Eligible) UpdateNextReceiverEdge();
        }
        PublishOutputs();
        PublishClock4Pattern();
    }

    private void FallingClock()
    {
        _oscillatorHigh = false;
        _ic2.ReceiveFallingClock();
        _ic3.ReceiveFallingClock();
        _ic4.ReceiveFallingClock();
    }

    /// <summary>One complete source period, starting and ending low.</summary>
    public void Clock()
    {
        if (_oscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
        ClockPeriod();
    }

    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_oscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
        if (_stableClock12Eligible) AdvanceWithStableReceiver(clocks);
        else for (long clock = 0; clock < clocks; clock++) ClockPeriod();
    }

    private void ClockPeriod()
    {
        RisingClock();
        FallingClock();
    }

    private void Ic2Outputs(byte count, bool carry)
    {
        _ic2Pins = count;
        _levels = (byte)((_levels & ~C.Clock8) | ((count & 1) << 1));
        _ic2.SetClear((count & 2) == 0);
        var enable = (count & 1) != 0;
        _ic3.SetEnable(enable, enable);
    }

    private void Ic3Outputs(byte count, bool carry)
    {
        _levels = (byte)((_levels & ~(C.Clock4 | C.Clock2 | C.Clock1))
            | ((count & 1) << 3) | ((count & 6) << 4));
        _ic4ClearHigh = !carry;
        _ic4.SetClear(_ic4ClearHigh);
    }

    private void Ic4Outputs(byte count, bool carry)
    {
        _levels = (byte)((_levels & ~(C.Clock12 | C.Clock6 | C.Clock3))
            | (count & 1) | ((count & 2) << 1) | ((count & 4) << 2));
    }

    private void PublishOutputs()
    {
        var remaining = (uint)(_levels ^ _publishedLevels);
        _publishedLevels = _levels;
        if (_stableClock12Eligible && (remaining & (C.Clock2 | (_observedClocks & C.Clock4))) != 0)
            UpdateNextReceiverEdge();
        if ((remaining & _observedClocks) == 0) return;
        var index = (int)_nextObserved[0];
        while (index < C.OutputCount)
        {
            var bit = 1u << index;
            // Preserve the original ascending order. A callback can attach or
            // detach a later output of this same edge, but cannot replay an earlier one.
            if ((remaining & bit) != 0) _receivers[index]?.Invoke((_levels & bit) != 0);
            index = _nextObserved[index + 1];
        }
    }

    private Action<bool>? GetReceiver(byte output) => _receivers[BitOperations.TrailingZeroCount((uint)output)];

    private void SetReceiver(Action<bool>? receiver, byte output)
    {
        RequireReceiverBoundary();
        _receivers[BitOperations.TrailingZeroCount((uint)output)] = receiver;
        var previous = _observedClocks;
        if (receiver is null) _observedClocks &= (byte)~output;
        else _observedClocks |= output;
        UpdateStableClockEligibility();
        if (previous == _observedClocks) return;
        // Subscription metadata only; no clock, signal or counter state is shared.
        var next = (byte)C.OutputCount;
        for (var index = C.OutputCount - 1; index >= 0; index--)
        {
            if ((_observedClocks & (1 << index)) != 0) next = (byte)index;
            _nextObserved[index] = next;
        }
    }
}
