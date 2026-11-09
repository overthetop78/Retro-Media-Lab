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
        internal bool? Q = initialQ;
        internal bool? NotQ = initialQ.HasValue ? !initialQ.Value : null;
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
    public bool? GetQ(int channel) => Get(channel).Q;
    public bool? GetNotQ(int channel) => Get(channel).NotQ;
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
        return state.PresetHigh && state.ClearHigh && StoreOutputs(state, state.Data, !state.Data);
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
        var changed = !presetHigh || !clearHigh
            ? StoreOutputs(state, !presetHigh, !clearHigh)
            : forbidden && StoreOutputs(state, null, null);
        if (changed) Notify(channel, state);
    }

    public void Clock(int channel)
    {
        if (Get(channel).ClockHigh) throw new InvalidOperationException(nameof(channel));
        SetClock(channel, true);
        SetClock(channel, false);
    }

    /// <summary>Stable inputs; output feedback is re-evaluated after each notified transition.</summary>
    public void AdvanceClocks(int channel, long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        var state = Get(channel);
        if (state.ClockHigh) throw new InvalidOperationException(nameof(channel));
        while (clocks > 0)
        {
            bool? q = state.PresetHigh && state.ClearHigh ? state.Data : !state.PresetHigh;
            bool? notQ = state.PresetHigh && state.ClearHigh ? !state.Data : !state.ClearHigh;
            var observed = channel == 0 ? FirstOutputsChanged : SecondOutputsChanged;
            if (observed is null || state.Q == q && state.NotQ == notQ)
            {
                state.Clocks = checked(state.Clocks + clocks);
                if (StoreOutputs(state, q, notQ)) Notify(channel, state);
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

    private static bool StoreOutputs(Channel state, bool? q, bool? notQ)
    {
        if (state.Q == q && state.NotQ == notQ) return false;
        state.Q = q; state.NotQ = notQ;
        return true;
    }

    private void Notify(int channel, Channel state)
    {
        if (channel == 0) FirstOutputsChanged?.Invoke(state.Q, state.NotQ);
        else SecondOutputsChanged?.Invoke(state.Q, state.NotQ);
    }
}
