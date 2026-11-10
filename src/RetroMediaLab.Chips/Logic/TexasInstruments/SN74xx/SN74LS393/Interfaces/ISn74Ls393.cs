namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Interfaces;

/// <summary>Two negative-edge ripple counters, with local propagation deadlines.</summary>
public interface ISn74Ls393
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    long? NextOutputEventNanoseconds { get; }
    Action<long?>? NextOutputEventNanosecondsChanged { get; set; }
    long GetQaRisingClockCount(int channel);
    void RequestQaClock(int channel, long? risingClockCount);
    Action<long>? FirstQaClockReached { get; set; }
    Action<long>? SecondQaClockReached { get; set; }
    /// <summary>Resolved output levels and changed observed bits, QA in bit zero.</summary>
    Action<byte, byte>? FirstOutputsChanged { get; set; }
    Action<byte, byte>? SecondOutputsChanged { get; set; }
    long GetClockFrequency(int channel);
    long GetInputClockCount(int channel);
    bool GetClock(int channel);
    bool GetClear(int channel);
    byte GetOutputs(int channel);
    void SetObservedOutputs(int channel, byte mask);
    void SetClock(int channel, bool high);
    void ConnectPeriodicClock(int channel, long nextFallingNanoseconds, long periodNanoseconds, long highNanoseconds);
    void DisconnectPeriodicClock(int channel);
    void SetClear(int channel, bool high);
    void AdvanceTo(long nanoseconds);
    /// <summary>Stable external inputs, integral-nanosecond periods only.
    /// Feedback from output notifications is retained. Does not clock the other channel.</summary>
    void AdvanceClocks(int channel, long clocks);
}
