using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Interfaces;

public interface ITmm2009
{
    long TimeNanoseconds { get; }
    bool ElectricalTimingQualified { get; }
    Tmm2009Output Output { get; }
    Action<Tmm2009Output>? OutputChanged { get; set; }
    void ReceivePins(long timeNanoseconds, Tmm2009Pins pins);
    void AdvanceTo(long timeNanoseconds);
    Tmm2009State CaptureState();
    void RestoreState(Tmm2009State state);
}
