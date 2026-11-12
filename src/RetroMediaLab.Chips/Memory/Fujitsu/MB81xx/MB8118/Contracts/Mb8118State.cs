using RetroMediaLab.Chips.Logic.Common.Enums;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

/// <summary>Physical high levels of the multiplexed address and control inputs.</summary>
public readonly record struct Mb8118Pins(byte Address, bool AddressKnown, bool RasHigh,
    bool CasHigh, bool WriteHigh, bool? Data);

/// <summary>Unknown/low/high encoded cells, retention timestamps and complete bus phase.</summary>
public sealed record Mb8118State(int Version, long[] Timing, byte[] Cells, long[] Refreshed,
    long Time, Mb8118Pins Pins, int Row, int Column, bool RowKnown, bool ColumnKnown,
    bool RowActive, bool CycleWrite, bool ReadCycle, bool TimingQualified, int StartupCycles,
    long RasFall, long CasFall, long RasRise, long CasRise, long AddressChanged, long DataChanged,
    long WriteFall, long WriteAt, long WriteHoldUntil, int WriteAddress, long? OutputDue, LogicPinState OutputTarget,
    LogicPinState Output);
