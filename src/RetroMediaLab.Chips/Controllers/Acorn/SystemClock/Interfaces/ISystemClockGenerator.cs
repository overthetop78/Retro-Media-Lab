namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;

/// <summary>Clock network of Acorn CPU board 400,005, before CPU WAIT and READY gates.</summary>
public interface ISystemClockGenerator
{
    long OscillatorFrequency { get; }
    long InputClockCount { get; }
    bool OscillatorHigh { get; }
    bool Clock12MHz { get; }
    bool Clock8MHz { get; }
    bool Clock6MHz { get; }
    bool Clock4MHz { get; }
    bool Clock3MHz { get; }
    bool Clock2MHz { get; }
    bool Clock1MHz { get; }
    Action<bool>? Clock12MHzChanged { get; set; }
    Action<bool>? Clock8MHzChanged { get; set; }
    Action<bool>? Clock6MHzChanged { get; set; }
    Action<bool>? Clock4MHzChanged { get; set; }
    Action<bool>? Clock3MHzChanged { get; set; }
    Action<bool>? Clock2MHzChanged { get; set; }
    Action<bool>? Clock1MHzChanged { get; set; }
    IStableClockPeriodReceiver? Clock12MHzReceiver { get; set; }
    bool Clock12MHzCanGroup { get; set; }
    void SetOscillator(bool high);
    void Clock();
    void AdvanceClocks(long clocks);
    void AdvanceRepresentedNetworkClocks(long clocks);
    bool CanRepresentReferencePeriods { get; }
}
