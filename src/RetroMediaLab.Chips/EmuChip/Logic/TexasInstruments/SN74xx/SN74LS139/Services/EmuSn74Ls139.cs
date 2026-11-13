using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.EmuChip.Logic.Common.Services;
using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.EmuChip.Logic.Common.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;
using System.Numerics;

namespace RetroMediaLab.Chips.EmuChip.Logic.TexasInstruments.SN74xx.SN74LS139.Services;

/// <summary>TI LS139A: two independent active-low one-of-four decoders.</summary>
public sealed class EmuSn74Ls139 : ISn74Ls139, IEmuCombinationalRangeModel
{
    private const string Part = "SN74LS139A";
    private readonly EmuCombinationalOutputs _pins = new(8);
    private readonly long[] _timing;
    private byte _levels, _known;
    // Each independent decoder has only three ternary inputs. The complete
    // 64-entry levels/known table includes noncanonical words as well.
    private static readonly byte[] DecoderTable = BuildDecoderTable();
    public EmuSn74Ls139(Sn74Ls139Timing timing)
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
        Span<long> delays = stackalloc long[8];
        if (!PrepareInputs(levels, known, delays, out ushort targets)) return;
        _pins.Validate(targets, delays);
        _levels = (byte)(levels & known); _known = known;
        _pins.SetTargets(targets, delays);
    }

    public LogicOutputRange ExecuteInputRange(in LogicInputRange range) => _pins.ExecuteInputRange(in range, 63, this);
    public EmuLogicResponse ExecuteProgram(in EmuSignalRange range) => _pins.ExecuteProgram(in range, 63, this);
    public void ReceiveWiredInput(long time, ulong word)
    {
        if (((word | word >> 32) & 0xffffffffUL & ~63UL) != 0)
            throw new ArgumentOutOfRangeException(nameof(word));
        byte levels = (byte)(word & word >> 32), known = (byte)(word >> 32);
        Span<long> delays = stackalloc long[8];
        if (!PrepareInputs(levels, known, delays, out ushort targets)) { _pins.SetWiredTime(time); return; }
        // The owner prepares its own truth/delays directly. The common output
        // store receives prepared targets, without interface callbacks into us.
        _pins.SetWiredTargetsAt(time, targets, delays);
        _levels = levels; _known = known;
    }
    public ulong ResolveWiredOutput() => _pins.ResolveWiredOutput();


    bool IEmuCombinationalRangeModel.PrepareRangeInput(ulong input, Span<long> delays, out ushort targets)
        => PrepareInputs((byte)(input & 0xffffffffUL), (byte)(input >> 32), delays, out targets);

    void IEmuCombinationalRangeModel.CommitRangeInput(ulong input)
    {
        _levels = (byte)((input & 0xffffffffUL) & (input >> 32)); _known = (byte)(input >> 32);
    }

    private bool PrepareInputs(byte levels, byte known, Span<long> delays, out ushort targets)
    {
        levels &= known;
        targets = default;
        if (_levels == levels && _known == known) return false;
        targets = Resolve(levels, known);
        byte changed = (byte)((_levels ^ levels) | (_known ^ known));
        uint changedTargets = (uint)(targets ^ _pins.Targets);
        while (changedTargets != 0)
        {
            int output = BitOperations.TrailingZeroCount(changedTargets) >> 1;
            changedTargets &= ~(3u << (output * 2));
            int inputs = (changed >> ((output / 4) * 3)) & 7;
            var target = (LogicPinState)((targets >> (output * 2)) & 3);
            long delay = 0;
            if ((inputs & 1) != 0) delay = Delay((output & 1) == 0 ? 0 : 2, target);
            if ((inputs & 2) != 0) delay = Math.Max(delay, Delay((output & 2) == 0 ? 0 : 2, target));
            if ((inputs & 4) != 0) delay = Math.Max(delay, Delay(4, target));
            delays[output] = delay;
        }
        return true;
    }
    private long Delay(int path, LogicPinState target) => target == LogicPinState.High ? _timing[path]
        : target == LogicPinState.Low ? _timing[path + 1] : Math.Max(_timing[path], _timing[path + 1]);

    private static ushort Resolve(ulong levels, ulong known)
        => (ushort)(DecoderTable[(int)((levels & 7) | ((known & 7) << 3))]
            | (DecoderTable[(int)(((levels >> 3) & 7) | (known & 56))] << 8));

    private static byte[] BuildDecoderTable()
    {
        var table = new byte[64];
        for (int index = 0; index < table.Length; index++)
        {
            int k = index >> 3, l = (index & 7) & k;
            for(int output=0;output<4;output++)
            {
                bool excluded = (l & 4) != 0 || ((l ^ output) & k & 3) != 0;
                var value = excluded ? LogicPinState.High : k == 7 ? LogicPinState.Low : LogicPinState.Unknown;
                table[index] |= (byte)((int)value << (output * 2));
            }
        }
        return table;
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
