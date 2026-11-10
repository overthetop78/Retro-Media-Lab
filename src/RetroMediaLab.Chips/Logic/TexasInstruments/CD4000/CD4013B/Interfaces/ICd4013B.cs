namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Interfaces;

/// <summary>Two positive-edge D flip-flops; SET and RESET are active high.</summary>
public interface ICd4013B
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<bool?, bool?>? FirstOutputsChanged { get; set; }
    Action<bool?, bool?>? SecondOutputsChanged { get; set; }
    long GetClockFrequency(int channel);
    long GetInputClockCount(int channel);
    bool GetClock(int channel);
    bool GetSet(int channel);
    bool GetReset(int channel);
    bool? GetQ(int channel);
    bool? GetNotQ(int channel);
    void SetData(int channel, bool? high);
    void SetClock(int channel, bool high);
    void SetClocks(bool firstHigh, bool secondHigh);
    void SetSet(int channel, bool high);
    void SetReset(int channel, bool high);
    void SetAsynchronousInputs(int channel, bool setHigh, bool resetHigh);
    void AdvanceTo(long nanoseconds);
    void AdvanceClocks(int channel, long clocks);
}
