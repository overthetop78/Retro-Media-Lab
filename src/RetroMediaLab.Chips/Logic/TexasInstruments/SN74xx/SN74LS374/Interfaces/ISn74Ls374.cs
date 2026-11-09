namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Interfaces;

/// <summary>Eight rising-edge D flip-flops with a common active-high output control.</summary>
public interface ISn74Ls374
{
    long ClockFrequency { get; }
    long InputClockCount { get; }
    bool ClockHigh { get; }
    byte DataInput { get; }
    byte? StoredData { get; }
    bool OutputControlHigh { get; }
    bool OutputsDriven { get; }
    byte? OutputData { get; }
    Action<byte?, bool>? OutputsChanged { get; set; }
    void SetData(byte data);
    void SetOutputControl(bool high);
    void SetClock(bool high);
    void Clock();
    void AdvanceClocks(long clocks);
}
