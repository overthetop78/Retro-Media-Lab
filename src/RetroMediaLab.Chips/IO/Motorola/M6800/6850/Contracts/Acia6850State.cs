using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Enums;

namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Contracts;

/// <summary>Registres et séquenceurs du MC6850, sans câblage ni modulation externe.</summary>
public sealed record Acia6850State
{
    public required int FormatVersion { get; init; }
    public required int TicksPerSecond { get; init; }
    public required int SerialClock { get; init; }
    public required bool RtsHigh { get; init; }
    public required Acia6850WordFormat WordFormat { get; init; }
    public required Acia6850TransmitControl TransmitControl { get; init; }
    public required byte Control { get; init; }
    public required byte Data { get; init; }
    public required byte Status { get; init; }
    public required bool MasterReset { get; init; }
    public required bool FirstMasterReset { get; init; }
    public required bool DcdInput { get; init; }
    public required bool DcdObserved { get; init; }
    public required bool DcdPending { get; init; }
    public required bool DcdStatusRead { get; init; }
    public required bool CtsBlocked { get; init; }
    public required bool OverrunPending { get; init; }
    public required bool ReceiveIrqEnabled { get; init; }
    public required bool TransmitIrqEnabled { get; init; }
    public required bool Break { get; init; }
    public required bool Irq { get; init; }
    public required int Divider { get; init; }
    public required int DataBits { get; init; }
    public required bool HasParity { get; init; }
    public required bool OddParity { get; init; }
    public required int StopBits { get; init; }
    public required int BaudRateOverride { get; init; }
    public required int BaudRate { get; init; }
    public required long InputClockRate { get; init; }
    public required long ClockPhase { get; init; }
    public required int ReceiveStep { get; init; }
    public required int ReceiveValue { get; init; }
    public required int ReceiveClockCounter { get; init; }
    public required bool ReceiveParity { get; init; }
    public required bool ReceiveFraming { get; init; }
    public required bool ReceiveDiscard { get; init; }
    public required bool ReceiveWaitMark { get; init; }
    public required Acia6850TransmitState TransmitState { get; init; }
    public required byte TransmitData { get; init; }
    public required byte TransmitShift { get; init; }
    public required bool TransmitPending { get; init; }
    public required bool TransmitLine { get; init; }
    public required bool TransmitParity { get; init; }
    public required int TransmitStep { get; init; }
    public required int TransmitStopCount { get; init; }
    public required int TransmitClockCounter { get; init; }
}
