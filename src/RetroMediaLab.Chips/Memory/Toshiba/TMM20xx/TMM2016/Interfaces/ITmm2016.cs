using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Interfaces;

public interface ITmm2016
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    bool IsTimingQualified { get; }
    Tmm2016Output Output { get; }
    Action<Tmm2016Output>? OutputChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void ReceivePins(long timeNanoseconds, Tmm2016Pins pins);
    /// <summary>Supply/retention input for a hardware profile that documents it; otherwise rejected.</summary>
    void ReceiveSupplyMillivolts(long timeNanoseconds, int millivolts);
    void AdvanceTo(long timeNanoseconds);
    Tmm2016State CaptureState();
    void RestoreState(Tmm2016State state);
}
