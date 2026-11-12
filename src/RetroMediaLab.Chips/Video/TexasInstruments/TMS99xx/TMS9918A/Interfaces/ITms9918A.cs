using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Interfaces;

/// <summary>Signals, external clocks and local state of the TMS9918A.</summary>
public interface ITms9918A
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    Tms9918CpuOutputs CpuOutputs { get; }
    bool IsCpuInterfaceQualified { get; }
    Tms9918ClockState ClockState { get; }
    Tms9918RasterState RasterState { get; }
    Tms9918VideoSignal VideoSignal { get; }
    Tms9918VramPins VramPins { get; }
    Action<Tms9918VramSignal>? VramPinsChanged { get; set; }
    Action<Tms9918ClockOutput>? CpuClockChanged { get; set; }
    Action<Tms9918ClockOutput>? GromClockChanged { get; set; }
    Action<Tms9918VideoSignal>? VideoSignalProduced { get; set; }
    Action<long>? FrameStarted { get; set; }
    Action<Tms9918CpuOutputs>? CpuOutputsChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void ReceiveCpuPins(long timeNanoseconds, Tms9918CpuPins pins);
    /// <summary>Advances with the currently received CPU levels, without replaying an input transition.</summary>
    void AdvanceCpuInterfaceTo(long timeNanoseconds);
    void SetReset(long timeNanoseconds, bool asserted);
    void ReceiveMasterClock(long timeNanoseconds, bool high);
    void ReceiveMasterClockTrain(Tms9918ClockTrain train);
    void SetVramReadData(long timeNanoseconds, byte data, bool known);
    void SetExternalVideoVoltage(long timeNanoseconds, double voltage, bool known);
    void SetResetSyncVoltage(long timeNanoseconds, double voltage, bool known);
    void AdvanceTo(long timeNanoseconds);
    Tms9918AState CaptureState();
    void RestoreState(Tms9918AState state);
}
