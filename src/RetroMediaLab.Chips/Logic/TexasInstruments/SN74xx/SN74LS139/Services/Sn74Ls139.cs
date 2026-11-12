using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.Common.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Services;

/// <summary>TI LS139A: two independent active-low one-of-four decoders.</summary>
public sealed class Sn74Ls139 : ISn74Ls139
{
    private const string Part = "SN74LS139A";
    private readonly CombinationalOutputs _pins = new(8);
    private readonly long[] _timing;
    private byte _levels, _known;
    public Sn74Ls139(Sn74Ls139Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        _timing = (long[])timing.Values.Clone();
    }
    public long TimeNanoseconds => _pins.Time;
    public long? NextEventNanoseconds => _pins.Next;
    public Action<long?>? NextEventNanosecondsChanged { get => _pins.DeadlineChanged; set => _pins.DeadlineChanged = value; }
    public Action<int, LogicPinState>? OutputChanged { get => _pins.Changed; set => _pins.Changed = value; }
    public LogicPinState GetOutput(int output) => _pins.Get(output);
    public void AdvanceTo(long nanoseconds) => _pins.AdvanceTo(nanoseconds);
    public void SetInputs(int decoder, bool? a, bool? b, bool? enableHigh)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)decoder, 2U);
        int shift = decoder * 3;
        byte levels = (byte)((_levels & ~(7 << shift))
            | (((a == true ? 1 : 0) | (b == true ? 2 : 0) | (enableHigh == true ? 4 : 0)) << shift));
        byte known = (byte)((_known & ~(7 << shift))
            | (((a.HasValue ? 1 : 0) | (b.HasValue ? 2 : 0) | (enableHigh.HasValue ? 4 : 0)) << shift));
        SetInputs(levels, known);
    }

    public void SetInputs(byte levels, byte known)
    {
        if (((levels | known) & ~63) != 0) throw new ArgumentOutOfRangeException(nameof(levels));
        levels &= known;
        if (_levels == levels && _known == known) return;
        ushort targets = Resolve(levels, known);
        byte changed = (byte)((_levels ^ levels) | (_known ^ known));
        Span<long> delays = stackalloc long[8];
        for (int output = 0; output < 8; output++)
        {
            int inputs = (changed >> ((output / 4) * 3)) & 7;
            var target = (LogicPinState)((targets >> (output * 2)) & 3);
            long delay = 0;
            if ((inputs & 1) != 0) delay = Delay((output & 1) == 0 ? 0 : 2, target);
            if ((inputs & 2) != 0) delay = Math.Max(delay, Delay((output & 2) == 0 ? 0 : 2, target));
            if ((inputs & 4) != 0) delay = Math.Max(delay, Delay(4, target));
            delays[output] = delay;
        }
        _pins.Validate(targets, delays);
        _levels = levels; _known = known;
        _pins.SetTargets(targets, delays);
    }
    private long Delay(int path, LogicPinState target) => target == LogicPinState.High ? _timing[path]
        : target == LogicPinState.Low ? _timing[path + 1] : Math.Max(_timing[path], _timing[path + 1]);

    private static ushort Resolve(ulong levels, ulong known)
    {
        ushort result = 0;
        for (int output = 0; output < 8; output++)
        {
            int shift = (output / 4) * 3;
            int k = (int)(known >> shift) & 7, l = (int)(levels >> shift) & k;
            int selection = output & 3;
            bool excluded = ((l & 4) != 0) || (((l ^ selection) & k & 3) != 0);
            var value = excluded ? LogicPinState.High : k == 7 ? LogicPinState.Low : LogicPinState.Unknown;
            result |= (ushort)((int)value << (output * 2));
        }
        return result;
    }
    public CombinationalState CaptureState() => _pins.Capture(Part, _levels, _known, _timing);
    public void RestoreState(CombinationalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _pins.ValidateRestore(state, Part, 63, Resolve(state.InputLevels, state.InputKnown), _timing);
        for (int output = 0; output < 8; output++)
            if (((state.Outputs >> (output * 2)) & 3) == 3) throw new ArgumentException(nameof(state));
        _levels = (byte)state.InputLevels; _known = (byte)state.InputKnown;
        _pins.Restore(state);
    }
}
