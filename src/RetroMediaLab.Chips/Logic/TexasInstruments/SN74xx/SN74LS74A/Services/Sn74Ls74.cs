using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Interfaces;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Constants.Sn74Ls74Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Services;

/// <summary>74LS74A digital function table, including separate asynchronous preset and clear.</summary>
/// <remarks>Initial Q values are model seeds, not guaranteed power-on levels.
/// Null represents an indeterminate output; no analogue resolution is invented after
/// simultaneous release of the forbidden PR=CLR=low state.</remarks>
public sealed class Sn74Ls74 : ISn74Ls74
{
    private sealed class Channel(long frequency, bool? initialQ)
    {
        internal readonly long Frequency = frequency;
        internal long Clocks;
        internal bool ClockHigh;
        internal bool Data;
        internal bool PresetHigh = true;
        internal bool ClearHigh = true;
        // Canonical levels and their knowledge, including PR=CLR=low (both high).
        internal byte Outputs = initialQ.HasValue
            ? initialQ.Value ? C.HighOutputs : C.LowOutputs : C.UnknownOutputs;
    }

    private readonly Channel[] _channels;
    public Action<bool?, bool?>? FirstOutputsChanged { get; set; }
    public Action<bool?, bool?>? SecondOutputsChanged { get; set; }

    public Sn74Ls74(long firstClockFrequency, long secondClockFrequency, bool? firstQ = null, bool? secondQ = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstClockFrequency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(secondClockFrequency);
        _channels = [new(firstClockFrequency, firstQ), new(secondClockFrequency, secondQ)];
    }

    public long GetClockFrequency(int channel) => Get(channel).Frequency;
    public long GetInputClockCount(int channel) => Get(channel).Clocks;
    public bool GetClock(int channel) => Get(channel).ClockHigh;
    public bool? GetQ(int channel) => DecodeOutput(Get(channel).Outputs, C.QMask);
    public bool? GetNotQ(int channel) => DecodeOutput(Get(channel).Outputs, C.NotQMask);
    public void SetData(int channel, bool high) => Get(channel).Data = high;

    public void SetClock(int channel, bool high)
    {
        var state = Get(channel);
        if (CaptureClock(state, high)) Notify(channel, state);
    }

    /// <summary>Capture both independent CLK inputs before publishing a shared-edge result.</summary>
    public void SetClocks(bool firstHigh, bool secondHigh)
    {
        var first = CaptureClock(_channels[0], firstHigh);
        var second = CaptureClock(_channels[1], secondHigh);
        if (first) Notify(0, _channels[0]);
        if (second) Notify(1, _channels[1]);
    }

    private static bool CaptureClock(Channel state, bool high)
    {
        if (state.ClockHigh == high) return false;
        state.ClockHigh = high;
        if (!high) return false;
        state.Clocks = checked(state.Clocks + 1);
        if (!state.PresetHigh || !state.ClearHigh) return false;
        var outputs = state.Data ? C.HighOutputs : C.LowOutputs;
        if (state.Outputs == outputs) return false;
        state.Outputs = outputs;
        return true;
    }

    public void SetPreset(int channel, bool high)
    {
        var state = Get(channel);
        SetAsynchronousInputs(channel, state, high, state.ClearHigh);
    }

    public void SetClear(int channel, bool high)
    {
        var state = Get(channel);
        SetAsynchronousInputs(channel, state, state.PresetHigh, high);
    }

    public void SetAsynchronousInputs(int channel, bool presetHigh, bool clearHigh) =>
        SetAsynchronousInputs(channel, Get(channel), presetHigh, clearHigh);

    private void SetAsynchronousInputs(int channel, Channel state, bool presetHigh, bool clearHigh)
    {
        if (state.PresetHigh == presetHigh && state.ClearHigh == clearHigh) return;
        var forbidden = !state.PresetHigh && !state.ClearHigh;
        state.PresetHigh = presetHigh;
        state.ClearHigh = clearHigh;
        var outputs = !presetHigh || !clearHigh
            ? (byte)(C.KnownMask | (!presetHigh ? C.QMask : 0) | (!clearHigh ? C.NotQMask : 0))
            : forbidden ? C.UnknownOutputs : state.Outputs;
        if (state.Outputs == outputs) return;
        state.Outputs = outputs;
        Notify(channel, state);
    }

    public void Clock(int channel)
    {
        var state = Get(channel);
        if (state.ClockHigh) throw new InvalidOperationException(nameof(channel));
        if (CaptureClock(state, true)) Notify(channel, state);
        CaptureClock(state, false);
    }

    /// <summary>Stable inputs; output feedback is re-evaluated after each notified transition.</summary>
    public void AdvanceClocks(int channel, long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        var state = Get(channel);
        if (state.ClockHigh) throw new InvalidOperationException(nameof(channel));
        while (clocks > 0)
        {
            var outputs = state.PresetHigh && state.ClearHigh
                ? state.Data ? C.HighOutputs : C.LowOutputs
                : (byte)(C.KnownMask | (!state.PresetHigh ? C.QMask : 0)
                    | (!state.ClearHigh ? C.NotQMask : 0));
            var observed = channel == 0 ? FirstOutputsChanged : SecondOutputsChanged;
            if (observed is null || state.Outputs == outputs)
            {
                state.Clocks = checked(state.Clocks + clocks);
                if (state.Outputs != outputs)
                {
                    state.Outputs = outputs;
                    Notify(channel, state);
                }
                return;
            }
            Clock(channel);
            clocks--;
        }
    }

    private Channel Get(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)channel, (uint)C.Channels);
        return _channels[channel];
    }

    private static bool? DecodeOutput(byte outputs, byte mask) =>
        (outputs & C.KnownMask) != 0 ? (outputs & mask) != 0 : null;

    private void Notify(int channel, Channel state)
    {
        var receiver = channel == 0 ? FirstOutputsChanged : SecondOutputsChanged;
        if (receiver is null) return;
        receiver(DecodeOutput(state.Outputs, C.QMask), DecodeOutput(state.Outputs, C.NotQMask));
    }
}
