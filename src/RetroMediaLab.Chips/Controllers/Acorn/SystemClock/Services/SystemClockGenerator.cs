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
    public long InputClockCount { get; private set; }
    public bool OscillatorHigh { get; private set; }
    public bool Clock12MHz => (_levels & C.Clock12) != 0;
    public bool Clock8MHz => (_levels & C.Clock8) != 0;
    public bool Clock6MHz => (_levels & C.Clock6) != 0;
    public bool Clock4MHz => (_levels & C.Clock4) != 0;
    public bool Clock3MHz => (_levels & C.Clock3) != 0;
    public bool Clock2MHz => (_levels & C.Clock2) != 0;
    public bool Clock1MHz => (_levels & C.Clock1) != 0;
    public Action<bool>? Clock12MHzChanged { get; set; }
    public Action<bool>? Clock8MHzChanged { get; set; }
    public Action<bool>? Clock6MHzChanged { get; set; }
    public Action<bool>? Clock4MHzChanged { get; set; }
    public Action<bool>? Clock3MHzChanged { get; set; }
    public Action<bool>? Clock2MHzChanged { get; set; }
    public Action<bool>? Clock1MHzChanged { get; set; }
    internal Sn74S163 Ic2 => _ic2;
    internal Sn74S163 Ic3 => _ic3;
    internal Sn74S163 Ic4 => _ic4;

    public void SetOscillator(bool high)
    {
        if (OscillatorHigh == high) return;
        OscillatorHigh = high;
        if (high) InputClockCount = checked(InputClockCount + 1);
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
        if (OscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
        SetOscillator(true);
        SetOscillator(false);
    }

    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (OscillatorHigh) throw new InvalidOperationException(nameof(OscillatorHigh));
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
        if ((changed & C.Clock12) != 0) Clock12MHzChanged?.Invoke(Clock12MHz);
        if ((changed & C.Clock8) != 0) Clock8MHzChanged?.Invoke(Clock8MHz);
        if ((changed & C.Clock6) != 0) Clock6MHzChanged?.Invoke(Clock6MHz);
        if ((changed & C.Clock4) != 0) Clock4MHzChanged?.Invoke(Clock4MHz);
        if ((changed & C.Clock3) != 0) Clock3MHzChanged?.Invoke(Clock3MHz);
        if ((changed & C.Clock2) != 0) Clock2MHzChanged?.Invoke(Clock2MHz);
        if ((changed & C.Clock1) != 0) Clock1MHzChanged?.Invoke(Clock1MHz);
    }
}
