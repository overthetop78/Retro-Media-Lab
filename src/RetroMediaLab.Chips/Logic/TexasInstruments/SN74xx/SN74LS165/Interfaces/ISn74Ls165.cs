namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Interfaces;

/// <summary>Historical TI LS165 with direct parallel load and two interchangeable clock inputs.</summary>
public interface ISn74Ls165
{
    long ClockFrequency { get; }
    long InputClockCount { get; }
    long ShiftClockCount { get; }
    bool ClockHigh { get; }
    bool ClockInhibitHigh { get; }
    bool ShiftLoadHigh { get; }
    bool GatedClockHigh { get; }
    byte ParallelData { get; }
    bool? SerialInput { get; }
    byte? StoredData { get; }
    byte KnownBits { get; }
    bool? SerialOutput { get; }
    bool? ComplementaryOutput { get; }
    Action<bool?, bool?>? OutputsChanged { get; set; }
    void SetParallelData(byte data);
    void SetSerialInput(bool? high);
    void SetShiftLoad(bool high);
    void SetClockInhibit(bool high);
    void SetClock(bool high);
    void CaptureClock(bool high);
    void ResolveClock();
    void Clock();
    void AdvanceClocks(long clocks);
}
