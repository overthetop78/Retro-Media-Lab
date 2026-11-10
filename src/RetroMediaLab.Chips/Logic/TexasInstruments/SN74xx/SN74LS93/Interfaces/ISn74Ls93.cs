namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Interfaces;

/// <summary>Clock index0=CKA,1=CKB; reset index0=R0(1),1=R0(2); QA..QD bits0..3.</summary>
public interface ISn74Ls93
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    Action<byte, byte>? OutputsChanged { get; set; }
    byte Outputs { get; }
    long GetClockFrequency(int clock);
    long GetInputClockCount(int clock);
    bool GetClock(int clock);
    bool GetReset(int input);
    void SetClock(int clock, bool high);
    void ConnectPeriodicClock(int clock, long nextFallingNanoseconds, long periodNanoseconds, long highNanoseconds);
    void DisconnectPeriodicClock(int clock);
    Action<long, long, long>? QaPeriodicClockChanged { get; set; }
    long? NextOutputEventNanoseconds { get; }
    Action<long?>? NextOutputEventNanosecondsChanged { get; set; }
    void SetReset(int input, bool high);
    void SetResets(bool first, bool second);
    void SetObservedOutputs(byte mask);
    void AdvanceTo(long nanoseconds);
    void AdvanceClocks(int clock, long periods);
}
