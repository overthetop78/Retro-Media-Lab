using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Interfaces;

public interface ITmm23256
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    bool IsInitialized { get; }
    bool IsTimingQualified { get; }
    Tmm23256Output Output { get; }
    Action<Tmm23256Output>? OutputChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void ReceivePins(long timeNanoseconds, Tmm23256Pins pins);
    void ReceiveParallelPins(long timeNanoseconds, ushort address, bool addressKnown, bool ceHigh, bool oeHigh);
    void AdvanceTo(long timeNanoseconds);
    Tmm23256State CaptureState();
    void RestoreState(Tmm23256State state);
}
