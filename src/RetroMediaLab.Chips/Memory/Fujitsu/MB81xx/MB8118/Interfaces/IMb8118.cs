using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Interfaces;

public interface IMb8118
{
    long TimeNanoseconds { get; }
    long? NextEventNanoseconds { get; }
    bool IsTimingQualified { get; }
    LogicPinState Output { get; }
    Action<LogicPinState>? OutputChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void ReceivePins(long timeNanoseconds, Mb8118Pins pins);
    void ReceiveMultiplexedPins(long timeNanoseconds, byte address, bool addressKnown,
        bool rasHigh, bool casHigh, bool writeHigh, bool? data);
    void AdvanceTo(long timeNanoseconds);
    Mb8118State CaptureState();
    void RestoreState(Mb8118State state);
}
