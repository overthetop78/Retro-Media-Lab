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
    Action<LogicPinState>? UnrepresentedOutputChanged { get; set; }
    Action<long?>? NextUnrepresentedEventNanosecondsChanged { get; set; }
    Action? ReadOutputProgramWithdrawalRequested { get; set; }
    long? NextUnrepresentedEventNanoseconds { get; }
    bool TryDescribeReadOutputs(out Mb8118ReadOutputProgram program);
    Mb8118ReadProgram PrepareReadProgram(in Mb8118ReadCycle cycle);
    bool ReceiveReadProgram(in Mb8118ReadProgram inputs, bool? rowData, bool? columnData,
        out Mb8118ReadOutputProgram program);
    void RepresentReadOutputs();
    void WithdrawReadOutputs(long timeNanoseconds);
    void ReceivePins(long timeNanoseconds, Mb8118Pins pins);
    void ReceiveMultiplexedPins(long timeNanoseconds, byte address, bool addressKnown,
        bool rasHigh, bool casHigh, bool writeHigh, bool? data);
    void ReceiveReadCycle(Mb8118ReadCycle cycle);
    void ReceiveReadCycle(in Mb8118ReadCycle cycle, bool? rowData, bool? columnData);
    bool ReceiveReadCycle(in Mb8118ReadCycle cycle, bool? rowData, bool? columnData,
        out Mb8118ReadOutputProgram program);
    void CancelReadCycle(long timeNanoseconds);
    void AdvanceTo(long timeNanoseconds);
    Mb8118State CaptureState();
    void RestoreState(Mb8118State state);
}
