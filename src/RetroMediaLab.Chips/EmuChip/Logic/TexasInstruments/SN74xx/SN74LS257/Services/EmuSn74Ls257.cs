using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.EmuChip.Logic.Common.Services;
using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.EmuChip.Logic.Common.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Models;

namespace RetroMediaLab.Chips.EmuChip.Logic.TexasInstruments.SN74xx.SN74LS257.Services;

/// <summary>TI LS257B: four non-inverting 2:1 multiplexers with shared select and output disable.</summary>
public sealed class EmuSn74Ls257 : ISn74Ls257, IEmuCombinationalRangeModel
{
    private const string Part = "SN74LS257B";
    private readonly EmuCombinationalOutputs _pins = new(4);
    private readonly long[] _timing;
    private ushort _levels, _known;
    public EmuSn74Ls257(Sn74Ls257Timing timing)
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

    public void SetInputs(byte aLevels, byte aKnown, byte bLevels, byte bKnown, bool? selectB, bool? disableHigh)
    {
        if (((aLevels | aKnown | bLevels | bKnown) & ~15) != 0) throw new ArgumentOutOfRangeException(nameof(aLevels));
        SetInputs((ushort)((aLevels & aKnown) | ((bLevels & bKnown) << 4)
            | (selectB == true ? 256 : 0) | (disableHigh == true ? 512 : 0)),
            (ushort)(aKnown | (bKnown << 4) | (selectB.HasValue ? 256 : 0) | (disableHigh.HasValue ? 512 : 0)));
    }

    public void SetInputs(ushort levels, ushort known)
    {
        if (((levels | known) & ~1023) != 0) throw new ArgumentOutOfRangeException(nameof(levels));
        Span<long> delays = stackalloc long[4];
        if (!PrepareInputs(levels, known, delays, out ushort targets)) return;
        _pins.Validate(targets, delays);
        _levels = (ushort)(levels & known); _known = known;
        _pins.SetTargets(targets, delays);
    }

    public LogicOutputRange ExecuteInputRange(in LogicInputRange range) => _pins.ExecuteInputRange(in range, 1023, this);
    public EmuLogicResponse ExecuteProgram(in EmuSignalRange range) => _pins.ExecuteProgram(in range, 1023, this);
    public void ReceiveWiredInput(long time, ulong word)
    {
        if (((word | word >> 32) & 0xffffffffUL & ~1023UL) != 0)
            throw new ArgumentOutOfRangeException(nameof(word));
        ushort levels = (ushort)(word & word >> 32), known = (ushort)(word >> 32);
        Span<long> delays = stackalloc long[4];
        if (!PrepareInputs(levels, known, delays, out ushort targets)) { _pins.SetWiredTime(time); return; }
        // The owner prepares its own truth/delays directly. The common output
        // store receives prepared targets, without interface callbacks into us.
        _pins.SetWiredTargetsAt(time, targets, delays);
        _levels = levels; _known = known;
    }
    public ulong ResolveWiredOutput() => _pins.ResolveWiredOutput();


    bool IEmuCombinationalRangeModel.PrepareRangeInput(ulong input, Span<long> delays, out ushort targets)
        => PrepareInputs((ushort)(input & 0xffffffffUL), (ushort)(input >> 32), delays, out targets);

    void IEmuCombinationalRangeModel.CommitRangeInput(ulong input)
    {
        _levels = (ushort)((input & 0xffffffffUL) & (input >> 32)); _known = (ushort)(input >> 32);
    }

    private bool PrepareInputs(ushort levels, ushort known, Span<long> delays, out ushort targets)
    {
        levels &= known;
        targets = default;
        if (_levels == levels && _known == known) return false;
        targets = Resolve(levels, known);
        int changed = (_levels ^ levels) | (_known ^ known);
        for (int output = 0; output < 4; output++)
        {
            var target = (LogicPinState)((targets >> (output * 2)) & 3);
            long delay = 0;
            if ((changed & 512) != 0)
            {
                if (target == LogicPinState.HighImpedance)
                {
                    var old = _pins.Get(output);
                    delay = old == LogicPinState.High ? _timing[6] : old == LogicPinState.Low ? _timing[7] : Math.Max(_timing[6], _timing[7]);
                }
                else delay = Pair(4, target);
            }
            if (target != LogicPinState.HighImpedance)
            {
                if ((changed & 256) != 0) delay = Math.Max(delay, Pair(2, target));
                if ((changed & ((1 << output) | (16 << output))) != 0) delay = Math.Max(delay, Pair(0, target));
            }
            delays[output] = delay;
        }
        return true;
    }
    private long Pair(int offset, LogicPinState target) => target == LogicPinState.High ? _timing[offset]
        : target == LogicPinState.Low ? _timing[offset + 1] : Math.Max(_timing[offset], _timing[offset + 1]);

    private static ushort Resolve(ulong levels, ulong known)
    {
        ushort result = 0;
        for (int output = 0; output < 4; output++)
        {
            LogicPinState value = LogicPinState.Unknown;
            if ((known & 512) != 0)
            {
                if ((levels & 512) != 0) value = LogicPinState.HighImpedance;
                else
                {
                    int select = (levels & 256) != 0 ? output + 4 : output;
                    if ((known & 256) != 0)
                    {
                        if ((known & (1UL << select)) != 0)
                            value = (levels & (1UL << select)) != 0 ? LogicPinState.High : LogicPinState.Low;
                    }
                    else
                    {
                        ulong mask = 17UL << output;
                        if ((known & mask) == mask && ((levels & mask) == mask || (levels & mask) == 0))
                            value = (levels & mask) != 0 ? LogicPinState.High : LogicPinState.Low;
                    }
                }
            }
            result |= (ushort)((int)value << (output * 2));
        }
        return result;
    }
    public CombinationalState CaptureState() => _pins.Capture(Part, _levels, _known, _timing);
    public void RestoreState(CombinationalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _pins.ValidateRestore(state, Part, 1023, Resolve(state.InputLevels, state.InputKnown), _timing);
        _levels = (ushort)state.InputLevels; _known = (ushort)state.InputKnown;
        _pins.Restore(state);
    }
}
