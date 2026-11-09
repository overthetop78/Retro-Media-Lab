namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Interfaces;

public interface ISn74S163
{
    long ClockFrequency { get; }
    long InputClockCount { get; }
    bool ClockHigh { get; }
    byte Count { get; }
    bool Carry { get; }
    Action<byte, bool>? OutputsChanged { get; set; }
    void SetClock(bool high);
    void SetClear(bool high);
    void SetLoad(bool high);
    void SetEnable(bool p, bool t);
    void SetData(byte data);
    void Clock();
    void AdvanceClocks(long clocks);
}
