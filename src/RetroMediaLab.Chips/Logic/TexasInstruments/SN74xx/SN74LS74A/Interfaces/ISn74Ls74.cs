namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Interfaces;

/// <summary>Two independent positive-edge D flip-flops; channels are numbered zero and one.</summary>
public interface ISn74Ls74
{
    long GetClockFrequency(int channel);
    long GetInputClockCount(int channel);
    bool GetClock(int channel);
    bool? GetQ(int channel);
    bool? GetNotQ(int channel);
    Action<bool?, bool?>? FirstOutputsChanged { get; set; }
    Action<bool?, bool?>? SecondOutputsChanged { get; set; }
    void SetData(int channel, bool high);
    void SetClock(int channel, bool high);
    void SetClocks(bool firstHigh, bool secondHigh);
    void SetPreset(int channel, bool high);
    void SetClear(int channel, bool high);
    void SetAsynchronousInputs(int channel, bool presetHigh, bool clearHigh);
    void Clock(int channel);
    void AdvanceClocks(int channel, long clocks);
}
