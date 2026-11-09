using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants.SystemClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

/// <summary>24 MHz oscillator and three synchronous counters wired as Acorn 400,005 figure 9.</summary>
/// <remarks>Counter state belongs to each SN74S163. All three capture the shared edge
/// before upstream outputs settle. No CPU reset is wired to this free-running network.</remarks>
public sealed class SystemClockGenerator : ISystemClockGenerator
{
    private readonly Sn74S163 _ic2;
    private readonly Sn74S163 _ic3;
    private readonly Sn74S163 _ic4;
    private byte _levels;
    private byte _publishedLevels;
    private long _inputClockCount;
    private bool _oscillatorHigh;
    private byte _observedClocks;
    private Action<bool>? _clock12Changed;
    private Action<bool>? _clock8Changed;
    private Action<bool>? _clock6Changed;
    private Action<bool>? _clock4Changed;
    private Action<bool>? _clock3Changed;
    private Action<bool>? _clock2Changed;
    private Action<bool>? _clock1Changed;

    public SystemClockGenerator() : this(C.DivideByThreeSeed, 0, 0) { }

    // A deterministic power-on phase, not a TI-guaranteed initial state.
    internal SystemClockGenerator(byte ic2, byte ic3, byte ic4)
    {
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
    public Action<bool>? Clock12MHzChanged { get => _clock12Changed; set => SetReceiver(ref _clock12Changed, value, C.Clock12); }
    public Action<bool>? Clock8MHzChanged { get => _clock8Changed; set => SetReceiver(ref _clock8Changed, value, C.Clock8); }
    public Action<bool>? Clock6MHzChanged { get => _clock6Changed; set => SetReceiver(ref _clock6Changed, value, C.Clock6); }
    public Action<bool>? Clock4MHzChanged { get => _clock4Changed; set => SetReceiver(ref _clock4Changed, value, C.Clock4); }
    public Action<bool>? Clock3MHzChanged { get => _clock3Changed; set => SetReceiver(ref _clock3Changed, value, C.Clock3); }
    public Action<bool>? Clock2MHzChanged { get => _clock2Changed; set => SetReceiver(ref _clock2Changed, value, C.Clock2); }
    public Action<bool>? Clock1MHzChanged { get => _clock1Changed; set => SetReceiver(ref _clock1Changed, value, C.Clock1); }
    internal Sn74S163 Ic2 => _ic2;
    internal Sn74S163 Ic3 => _ic3;
    internal Sn74S163 Ic4 => _ic4;

    public void SetOscillator(bool high)
    {
        if (_oscillatorHigh == high) return;
        _oscillatorHigh = high;
        if (high) _inputClockCount = checked(_inputClockCount + 1);
        _ic2.CaptureClock(high);
        _ic3.CaptureClock(high);
        _ic4.CaptureClock(high);
        // The falling edge still reaches each CLK pin, but this positive-edge
        // network has no pending capture or output mutation to resolve on it.
        if (!high) return;
        _ic2.ResolveClock();
        _ic3.ResolveClock();
        _ic4.ResolveClock();
        PublishOutputs();
    }

    /// <summary>One complete source period, starting and ending low.</summary>
    public void Clock()
    {
        if (_oscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
        SetOscillator(true);
        SetOscillator(false);
    }

    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_oscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
        for (long clock = 0; clock < clocks; clock++) Clock();
    }

    private void Ic2Outputs(byte count, bool carry)
    {
        _levels = (byte)((_levels & ~C.Clock8) | ((count & 1) != 0 ? C.Clock8 : 0));
        _ic2.SetClear((count & 2) == 0);
        var enable = (count & 1) != 0;
        _ic3.SetEnable(enable, enable);
    }

    private void Ic3Outputs(byte count, bool carry)
    {
        _levels = (byte)((_levels & ~(C.Clock4 | C.Clock2 | C.Clock1))
            | ((count & 1) != 0 ? C.Clock4 : 0)
            | ((count & 2) != 0 ? C.Clock2 : 0)
            | ((count & 4) != 0 ? C.Clock1 : 0));
        _ic4.SetClear(!carry);
    }

    private void Ic4Outputs(byte count, bool carry)
    {
        _levels = (byte)((_levels & ~(C.Clock12 | C.Clock6 | C.Clock3))
            | ((count & 1) != 0 ? C.Clock12 : 0)
            | ((count & 2) != 0 ? C.Clock6 : 0)
            | ((count & 4) != 0 ? C.Clock3 : 0));
    }

    private void PublishOutputs()
    {
        var changed = _levels ^ _publishedLevels;
        _publishedLevels = _levels;
        if ((changed & _observedClocks) == 0) return;
        // Keep the full changed mask: an earlier receiver can connect another
        // receiver for a later output of this same resolved edge.
        if ((changed & C.Clock12) != 0) _clock12Changed?.Invoke(Clock12MHz);
        if ((changed & C.Clock8) != 0) _clock8Changed?.Invoke(Clock8MHz);
        if ((changed & C.Clock6) != 0) _clock6Changed?.Invoke(Clock6MHz);
        if ((changed & C.Clock4) != 0) _clock4Changed?.Invoke(Clock4MHz);
        if ((changed & C.Clock3) != 0) _clock3Changed?.Invoke(Clock3MHz);
        if ((changed & C.Clock2) != 0) _clock2Changed?.Invoke(Clock2MHz);
        if ((changed & C.Clock1) != 0) _clock1Changed?.Invoke(Clock1MHz);
    }

    private void SetReceiver(ref Action<bool>? target, Action<bool>? receiver, byte output)
    {
        target = receiver;
        if (receiver is null) _observedClocks &= (byte)~output;
        else _observedClocks |= output;
    }
}
