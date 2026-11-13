using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.Common.Services;
using RetroMediaLab.Chips.Logic.Common.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;
using System.Numerics;

namespace RetroMediaLab.Chips.EmuChip.Logic.TexasInstruments.SN74xx.SN74LS32.Services;

/// <summary>Four independent positive OR gates, with no clock or reset.</summary>
public sealed class EmuSn74Ls32 : ISn74Ls32, ICombinationalRangeModel
{
    private const string Part = "SN74LS32";
    private readonly CombinationalOutputs _pins = new(4);
    private readonly long[] _timing;
    private byte _levels, _known;
    public EmuSn74Ls32(Sn74Ls32Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        _timing = [timing.Rise, timing.Fall];
    }
    public long TimeNanoseconds => _pins.Time;
    public long? NextEventNanoseconds => _pins.Next;
    public Action<long?>? NextEventNanosecondsChanged { get => _pins.DeadlineChanged; set => _pins.DeadlineChanged = value; }
    public Action<int, LogicPinState>? OutputChanged { get => _pins.Changed; set => _pins.Changed = value; }
    public LogicPinState GetOutput(int gate) => _pins.Get(gate);
    public void AdvanceTo(long nanoseconds) => _pins.AdvanceTo(nanoseconds);

    public void SetInputs(int gate, bool? a, bool? b)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)gate, 4U);
        int shift = gate * 2;
        byte levels = (byte)((_levels & ~(3 << shift)) | ((a == true ? 1 : 0) << shift) | ((b == true ? 2 : 0) << shift));
        byte known = (byte)((_known & ~(3 << shift)) | ((a.HasValue ? 1 : 0) << shift) | ((b.HasValue ? 2 : 0) << shift));
        SetPacked(levels, known);
    }

    public void SetInputs(byte aLevels, byte aKnown, byte bLevels, byte bKnown)
    {
        if (((aLevels | aKnown | bLevels | bKnown) & 0xf0) != 0) throw new ArgumentOutOfRangeException(nameof(aLevels));
        byte levels = 0, known = 0;
        for (int gate = 0; gate < 4; gate++)
        {
            levels |= (byte)((((aLevels & aKnown) >> gate) & 1) << (gate * 2));
            levels |= (byte)((((bLevels & bKnown) >> gate) & 1) << (gate * 2 + 1));
            known |= (byte)(((aKnown >> gate) & 1) << (gate * 2));
            known |= (byte)(((bKnown >> gate) & 1) << (gate * 2 + 1));
        }
        SetPacked(levels, known);
    }

    private void SetPacked(byte levels, byte known)
    {
        Span<long> delays = stackalloc long[4];
        if (!PrepareInputs(levels, known, delays, out ushort targets)) return;
        _pins.Validate(targets, delays);
        _levels = (byte)(levels & known); _known = known;
        _pins.SetTargets(targets, delays);
    }

    public LogicOutputRange ExecuteInputRange(in LogicInputRange range) => _pins.ExecuteInputRange(in range, 255, this);

    bool ICombinationalRangeModel.PrepareRangeInput(in TimedLogicInput input, Span<long> delays, out ushort targets)
        => PrepareInputs((byte)input.Levels, (byte)input.Known, delays, out targets);

    void ICombinationalRangeModel.CommitRangeInput(in TimedLogicInput input)
    {
        _levels = (byte)(input.Levels & input.Known); _known = (byte)input.Known;
    }

    private bool PrepareInputs(byte levels, byte known, Span<long> delays, out ushort targets)
    {
        levels &= known;
        targets = default;
        if (_levels == levels && _known == known) return false;
        targets = Resolve(levels, known);
        uint changedTargets = (uint)(targets ^ _pins.Targets);
        while (changedTargets != 0)
        {
            int gate = BitOperations.TrailingZeroCount(changedTargets) >> 1;
            changedTargets &= ~(3u << (gate * 2));
            var target = (LogicPinState)((targets >> (gate * 2)) & 3);
            delays[gate] = target == LogicPinState.High ? _timing[0] : target == LogicPinState.Low ? _timing[1] : Math.Max(_timing[0], _timing[1]);
        }
        return true;
    }

    private static ushort Resolve(ulong levels, ulong known)
    {
        ulong qualified = levels & known;
        ulong high = (qualified | (qualified >> 1)) & 0x55;
        ulong low = known & (known >> 1) & ~high & 0x55;
        return (ushort)((high << 1) | low);
    }
    public CombinationalState CaptureState() => _pins.Capture(Part, _levels, _known, _timing);
    public void RestoreState(CombinationalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _pins.ValidateRestore(state, Part, 0xff, Resolve(state.InputLevels, state.InputKnown), _timing);
        // Push-pull OR outputs never enter a known high-impedance state.
        for (int gate = 0; gate < 4; gate++)
            if (((state.Outputs >> (gate * 2)) & 3) == 3) throw new ArgumentException(nameof(state));
        _levels = (byte)state.InputLevels; _known = (byte)state.InputKnown;
        _pins.Restore(state);
    }
}
