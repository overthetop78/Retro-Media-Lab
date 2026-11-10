namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Interfaces;

/// <summary>Fourteen-stage falling-edge counter; output word uses Q1 in bit zero.</summary>
public interface ICd4020B
{
    long ClockFrequency { get; }
    long InputClockCount { get; }
    long TimeNanoseconds { get; }
    bool ClockHigh { get; }
    bool ResetHigh { get; }
    /// <summary>Diagnostic state of all physical stages, including internal Q2 and Q3.</summary>
    ushort StageLevels { get; }
    /// <summary>Physical output pins only; Q2/Q3 bits are always absent.</summary>
    ushort OutputLevels { get; }
    long? NextEventNanoseconds { get; }
    Action<ushort, ushort>? OutputsChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void SetObservedOutputs(ushort mask);
    void SetClock(bool high);
    void SetReset(bool high);
    void AdvanceTo(long nanoseconds);
    /// <summary>Complete periods with stable external inputs and exact integral-ns period.
    /// Output feedback and changes of subscription remain effective.</summary>
    void AdvanceClocks(long clocks);
}
